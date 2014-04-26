﻿/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of Chimera.

Chimera is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Chimera is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Chimera.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Util;
using System.Windows.Forms;
using OpenMetaverse;
using Chimera.OpenSim.GUI;
using System.Threading;
using System.Drawing;
using Chimera.Config;
using log4net;
using Chimera.OpenSim.Interfaces;
using Chimera.Experimental.Plugins;
using Chimera.Experimental;
using System.IO;
using System.Globalization;
using Chimera.Experimental.GUI;
using Routrek.SSHCV2;
using Routrek.SSHC;
using System.Net;
using System.Net.Sockets;

namespace Chimera.OpenSim {
    public class RecorderPlugin : OpensimBotPlugin {
        private ILog Logger = LogManager.GetLogger("StatsRecorder");

        private RecorderControl mPanel;
        private AvatarMovementPlugin mMovementPlugin;
        private ExperimentalConfig mConfig;
        private Dictionary<string, Stats> mStats = new Dictionary<string, Stats>();
        private Stats mLastStat;
        private Action mTickListener;
        private int mExitCount = 0;
        private bool mRecording;

        public Stats LastStat {
            get { return mLastStat; }
        }

        public bool Recording {
            get { return mRecording; }
        }

        protected override IOpensimBotConfig BotConfig {
            get { return mConfig != null ? mConfig : mMovementPlugin.Config as ExperimentalConfig; }
        }

        protected override void OnLoggedIn() {
        }

        protected override void OnLoggingOut() {
            Core.Tick -= mTickListener;
        }

        protected override void OnLoggedOut() { }

        public override Control ControlPanel {
            get {
                if (mPanel == null)
                    mPanel = new RecorderControl(this);
                return mPanel;
            }
        }

        public override string Name {
            get { return "Recorder"; }
        }

        public override void Init(Core coordinator) {
            mTickListener = new Action(Core_Tick);
            base.Init(coordinator);

            if (Core.HasPlugin<AvatarMovementPlugin>()) {
                mMovementPlugin = Core.GetPlugin<AvatarMovementPlugin>();
                mConfig = mMovementPlugin.Config as ExperimentalConfig;
            } else
                mConfig = new ExperimentalConfig();

            LoggedInChanged += new Action<bool>(RecorderPlugin_LoggedInChanged);
        }

        void RecorderPlugin_LoggedInChanged(bool loggedIn) {
            Core.Tick += mTickListener;
        }

        public override void Close() {
            base.Close();
        }

        public override void SetForm(Form form) {
            base.SetForm(form);
            form.FormClosed += new FormClosedEventHandler(form_FormClosed);
        }

        void form_FormClosed(object sender, FormClosedEventArgs e) {
            LoadFPS();
            LoadPingTime();

            string fileName = mConfig.Timestamp.ToString(mConfig.TimestampFormat) + ".csv";
            string resultsFile = Path.GetFullPath(Path.Combine("Experiments", mConfig.ExperimentName, fileName));
            File.Delete(resultsFile);
            try {
                File.Create(resultsFile).Close();
            } catch (Exception ex) {
                Logger.Warn("Unable to create " + resultsFile + ".", ex);
            }

            /*
            Console.WriteLine("Writing out " + mStats.Count(p => p.Value.TimeStamp > mConfig.Timestamp) + " lines.");
            foreach (var stat in mStats.Values)
                Console.WriteLine(stat.ToString(mConfig.OutputKeys));
            */

            File.AppendAllText(resultsFile, "Timestamp," + mConfig.OutputKeys.Aggregate((a, k) => a + "," + k) + Environment.NewLine);
            File.AppendAllLines(resultsFile, mStats.
                Values.
                Where(s => s.TimeStamp > mConfig.Timestamp).
                Select(s => s.ToString(mConfig.OutputKeys)));
        }

        void Process_Exited(object sender, EventArgs e) {
            if (++mExitCount == Core.Frames.Count())
                LoadFPS();
        }

        public void LoadFPS() {
            Dictionary<string, List<float>> fpses = new Dictionary<string, List<float>>();

            string logTimestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
            string startTS = mConfig.Timestamp.ToString(logTimestampFormat);
            foreach (var file in Directory.
                    GetFiles(Path.Combine("Experiments", mConfig.ExperimentName)).
                    Where(f => Path.GetFileName(f).StartsWith(mConfig.Timestamp.ToString(mConfig.TimestampFormat)))) {

                string[] lines = null;
                int wait = 500;

                while (lines == null) {
                    try {
                        lines = File.ReadAllLines(file);
                    } catch (IOException e) {
                        if (wait > 60000)
                            return;
                        Logger.Debug("Problem loading log file. Waiting " + wait + "MS then trying again.");
                        //Logger.Debug("Problem loading log file. Waiting " + wait + "MS then trying again.", e);
                        Thread.Sleep(wait);
                        wait = (int) (wait * 1.5);
                    }
                }

                foreach (var line in lines.Where(l => l.Contains("FPS"))) {
                    string[] s = line.Split(' ');
                    DateTime ts = DateTime.ParseExact(s[0], logTimestampFormat, new DateTimeFormatInfo());
                    string time = ts.ToString(mConfig.TimestampFormat);

                    if (!fpses.ContainsKey(time))
                        fpses.Add(time, new List<float>());
                    fpses[time].Add(float.Parse(s[6]));
                }
            }

            int frames = Core.Frames.Count();
            foreach (var timestamp in mStats.Keys) {
                if (fpses.ContainsKey(timestamp)) {
                    List<float> list = fpses[timestamp];
                    float[] from = list.ToArray<float>();
                    float[] to = mStats[timestamp].CFPS;
                    Array.Copy(from, to, frames);
                }
            }
        }

        private bool mCopyDone = false;

        public void LoadPingTime() {
            if (!mCopyDone) {
                mCopyDone = false;
                ProcessController p = new ProcessController("cmd.exe", "C:\\Windows\\System32", "");
                p.Start();
                string local = Path.Combine(Environment.CurrentDirectory, "Experiments", mConfig.ExperimentName);
                string remote = "scp jm726@mimuve.cs.st-andrews.ac.uk:/home/opensim/opensim-0.7.3.1/bin/LocalUserStatistics.db .";
                string server = "mimuve.cs.st-andrews.ac.uk";
                string pass = "P3ngu1n!";
                string username = "jm726";
                p.SendString("scp " + username + "@" + server + ":" + remote + " " + local);
                p.PressKey("{ENTER}");
                p.SendString(pass);
                p.PressKey("{ENTER}");
                p.SendString(pass);
                p.PressKey("{ENTER}");
                p.Process.Close();
            }

            /*
            var param = new SSHConnectionParameter();
            param.UserName = "jm726";
            param.Password = "P3ngu1n!";
            param.Protocol = SSHProtocol.SSH2;
            param.AuthenticationType = AuthenticationType.Password;
            param.WindowSize = 0x1000;
            param.PreferableCipherAlgorithms = new CipherAlgorithm[] { 
                CipherAlgorithm.Blowfish, 
                CipherAlgorithm.TripleDES, 
                CipherAlgorithm.AES128, 
            };

            var reader = new Reader();
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(Dns.GetHostAddresses("mimuve.cs.st-andrews.ac.uk")[0], 22));

            SSHConnection _conn = SSHConnection.Connect(param, reader, sock);
            reader._conn = _conn;
            SSHChannel ch = _conn.OpenShell(reader);
            reader._pf = ch;
            SSHConnectionInfo info = _conn.ConnectionInfo;
            */



        }

        private class Reader : ISSHChannelEventReceiver, ISSHConnectionEventReceiver {
            public SSHConnection _conn;
            public SSHChannel _pf;

            public void OnChannelClosed() { }

            public void OnChannelEOF() { }

            public void OnChannelError(Exception error, string msg) { }

            public void OnChannelReady() { }

            public void OnData(byte[] data, int offset, int length) { }

            public void OnExtendedData(int type, byte[] data) { }

            public void OnMiscPacket(byte packet_type, byte[] data, int offset, int length) { }

            public PortForwardingCheckResult CheckPortForwardingRequest(string remote_host, int remote_port, string originator_ip, int originator_port) {
                throw new NotImplementedException();
            }

            public void EstablishPortforwarding(ISSHChannelEventReceiver receiver, SSHChannel channel) { }

            public void OnAuthenticationPrompt(string[] prompts) { }

            public void OnConnectionClosed() { }

            public void OnDebugMessage(bool always_display, byte[] msg) { }

            public void OnError(Exception error, string msg) { }

            public void OnIgnoreMessage(byte[] msg) { }

            public void OnUnknownMessage(byte type, byte[] data) { }
        }

        void Core_Tick() {
            mRecording = true;
            mLastStat = new Stats(Sim.Stats, Core.Frames.Count(), mConfig);
            string ts = mLastStat.ToString();
            if (mStats.ContainsKey(ts))
                mStats[ts] = mLastStat;
            else
                mStats.Add(mLastStat.ToString(), mLastStat);
        }

        public struct Stats {
            public float[] CFPS;
            public int[] PingTime;

            public readonly float Dilation;
            public readonly int SFPS;
            public readonly int Agents;
            public readonly int IncomingBPS;
            public readonly int OutgoingBPS;
            public readonly int ResentPackets;
            public readonly int ReceivedResends;
            public readonly float PhysicsFPS;
            public readonly float AgentUpdates;
            public readonly int Objects;
            public readonly int ScriptedObjects;
            public readonly float FrameTime;
            public readonly float NetTime;
            public readonly float ImageTime;
            public readonly float PhysicsTime;
            public readonly float ScriptTime;
            public readonly float OtherTime;
            public readonly int ChildAgents;
            public readonly int ActiveScripts;
            public DateTime TimeStamp;

            private ExperimentalConfig mConfig;

            public string Get(string key) {
                switch (key.ToUpper()) {
                    case "CFPS": return CFPS.Aggregate("", (s, v) => s + v + ",", f => f.TrimEnd(','));
                    case "PINGTIME": return PingTime.Aggregate("", (s, v) => s + v + ",", f => f.TrimEnd(','));
                    //case "CFPS": return ArrayStr(CFPS);
                    //case "PingTime": return ArrayStr(PingTime);
                    case "FT": return FrameTime.ToString();
                    case "SFPS": return SFPS.ToString();
                }
                return "UnknownKey(" + key + ")";
            }

            private string ArrayStr(Array ar) {
                string ret = "";
                foreach (var item in ar)
                    ret += item + ",";
                Console.WriteLine("From array: " + ar.GetValue(0));
                return ret.TrimEnd(',');
            }

            public Stats(Simulator.SimStats stats, int frames, ExperimentalConfig config) {
                mConfig = config;

                CFPS = new float[frames];
                PingTime = new int[frames];

                Dilation = stats.Dilation;
                SFPS = stats.FPS;
                Agents = stats.Agents;
                IncomingBPS = stats.IncomingBPS;
                OutgoingBPS = stats.OutgoingBPS;
                ResentPackets = stats.ResentPackets;
                ReceivedResends = stats.ReceivedResends;
                PhysicsFPS = stats.PhysicsFPS;
                AgentUpdates = stats.AgentUpdates;
                Objects = stats.Objects;
                ScriptedObjects = stats.ScriptedObjects;
                FrameTime = stats.FrameTime;
                NetTime = stats.NetTime;
                ImageTime = stats.ImageTime;
                PhysicsTime = stats.PhysicsTime;
                ScriptTime = stats.ScriptTime;
                OtherTime = stats.OtherTime;
                ChildAgents = stats.ChildAgents;
                ActiveScripts = stats.ActiveScripts;
                TimeStamp = DateTime.Now;
            }

            public override string ToString() {
                return TimeStamp.ToString(mConfig.TimestampFormat);
            }

            public string ToString(string[] keys) {
                Console.Write("SFPS: " + SFPS);
                Console.Write(" - FT: " + FrameTime);                Console.WriteLine(" - CFPS: " + CFPS.Aggregate("", (s, v) => s + v + ",", f => f.TrimEnd(',')));

                string line = TimeStamp.ToString(mConfig.TimestampFormat) + ",";

                foreach (var key in keys)
                    line += Get(key) + ",";

                return line.TrimEnd(',');
            }
        }
    }
}
