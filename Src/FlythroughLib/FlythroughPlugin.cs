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
using Chimera;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using OpenMetaverse;
using Chimera.Util;
using System.Threading;
using Chimera.Flythrough.GUI;
using Chimera.Flythrough;
using System.Drawing;
using Chimera.Config;
using log4net;

namespace Chimera.Flythrough {
    public struct Camera {
        private Vector3 mPosition;
        private Rotation mOrientation;

        public Vector3 Position { get { return mPosition; } }
        public Rotation Orientation { get { return mOrientation; } }

        public Camera(Vector3 vector3, Rotation rotation) {
            mPosition = vector3;
            mOrientation = rotation;
        }
    }

    public class FlythroughPlugin : ISystemPlugin {
#if DEBUG
        private readonly TickStatistics mStats = new TickStatistics();
#endif
        private readonly ILog Logger = LogManager.GetLogger("Flythrough");
        private readonly Action mTickListener;

        private bool mSeparateThread = true;

        private EventSequence<Camera> mEvents = new EventSequence<Camera>();
        private Core mCore;
        private FlythroughPanel mPanel;
        private DateTime mLastTick = DateTime.Now;
        private Camera mPrev;
        private bool mEnabled = true;
        private bool mPlaying = false;
        private bool mLoop = false;
        private bool mAutoStep = true;
        private bool mTicking;
        private bool mSynchLengths;
        private int mDefaultLength = 7000;

        private double mSpeed = 1;

        public event Action<int> StepFinished;
        public event Action<int> StepStarted;

        /// <summary>
        /// The time, as it was last set. Used for debugging only.
        /// </summary>
        private int mTime;

        /// <summary>
        /// Selected whenever playback of the sequence is paused.
        /// </summary>
        public event Action OnPaused;
        /// <summary>
        /// Selected whenever playback of the sequence is unpaused.
        /// </summary>
        public event Action UnPaused;
        /// <summary>
        /// Selected whenever the sequence gets to the end, even if looping.
        /// </summary>
        public event EventHandler SequenceFinished;
        /// <summary>
        /// Selected whenever playback moves value forward one tick.
        /// Not triggered when value is set directly.
        /// </summary>
        public event Action<int> TimeChange;
        /// <summary>
        /// Selected every value the length of the event is changed.
        /// </summary>
        public event Action<int> LengthChange;
        /// <summary>
        /// Triggered whenever a flythrough is loaded.
        /// </summary>
        public event Action FlythroughLoaded;
        /// <summary>
        /// Triggered whenever a flythrough is loading.
        /// </summary>
        public event Action FlythroughLoading;

        /// <summary>
        /// All the events queued up to play.
        /// </summary>
        public FlythroughEvent<Camera>[] Events {
            get { return mEvents.ToArray(); }
        }

        public double Speed {
            get { return mSpeed; }
            set { mSpeed = value; }
        }

        /// <summary>
        /// The default length for new events
        /// </summary>
        public int DefaultLength {
            get { return mDefaultLength; }
            set { mDefaultLength = value; }
        }

        public int Count {
            get { return mEvents.Count; }
        }

        public bool SynchStreams {
            get { return mSynchLengths; }
            set { mSynchLengths = value; }
        }

        /// <summary>
        /// Where in the current sequence playback has reached.
        /// Between 0 and Length.
        /// </summary>
        public int Time {
            get { return mEvents.Time; }
            set {
                FlythroughEvent<Camera> oldEvent = mEvents.CurrentEvent;
                if (oldEvent != null && oldEvent.GlobalFinishTime <= value && !Paused && StepFinished != null)
                    StepFinished(mEvents.CurrentEventIndex);
                mTime = value;
                mEvents.Time = value;
                FlythroughEvent<Camera> evt = mEvents[value];
                if (oldEvent != evt && StepStarted != null)
                    StepStarted(mEvents.CurrentEventIndex);
                if (mEnabled && !mTicking) {
                    if (mEvents.Length > 0 && value == 0)
                        mCore.Update(mEvents.Start.Position, Vector3.Zero, mEvents.Start.Orientation, Rotation.Zero);
                    else if (evt != null)
                        mCore.Update(evt.Value.Position, Vector3.Zero, evt.Value.Orientation, Rotation.Zero);
                }
                if (TimeChange != null)
                    TimeChange(value);
            }
        }

        /// <summary>
        /// How long the entire sequence is.
        /// </summary>
        public int Length {
            get { return mEvents.Length; }
        }
        /// <summary>
        /// Where the camera should start at the beginning of the sequence.
        /// </summary>
        public Camera Start {
            get { return mEvents.Start; }
            set { mEvents.Start = value; }
        }

        /// <summary>
        /// Whether auto playback is enabled.
        /// If false time will be continually incremented by the tick length specified in the input.
        /// </summary>
        public bool Paused {
            get { return !mPlaying; }
            set {
                if (value == mPlaying && mCore != null) {
                    if (value) {
                        Pause();
                    } else
                        Play();
                }
            }
        }

        private void Pause() {
            mPlaying = false;
            if (!mSeparateThread)
                mCore.Tick -= mTickListener;
            if (OnPaused != null)
                OnPaused();

            lock (mFinishLock)
                if (!mFinished)
                    Monitor.Wait(mFinishLock, 2000);
        }

        /// <summary>
        /// If true then, whilst playing, whenever one event finishes in the main sequence the next will start to play.
        /// If false then playback will stop after each event and a new call to Play will be required to start it up.
        /// </summary>
        public bool AutoStep {
            get { return mAutoStep; }
            set { mAutoStep = value; }
        }
        /// <summary>
        /// If true then once the sequence reaches the end it will start again.
        /// </summary>
        public bool Loop {
            get { return mLoop; }
            set { mLoop = value; }
        }
        /// <summary>
        /// The event which is currently playing.
        /// </summary>
        public FlythroughEvent<Camera> CurrentEvent {
            get { return mEvents.Count == 0 ? null : mEvents.CurrentEvent; }
        }

#if DEBUG
        /// <summary>
        /// Statistics about how efficiently the update thread is running.
        /// </summary>
        public TickStatistics Statistics { 
            get { return mStats; }
        }
#endif

        public FlythroughPlugin() {
            Start = new Camera(new Vector3(128f, 128f, 60f), Rotation.Zero);
            mEvents.Start = Start;
            mEvents.LengthChange += new Action<EventSequence<Camera>, int>(mEvents_LengthChange);
            mTickListener = new Action(mCoordinator_Tick);

            FlythroughConfig cfg = new FlythroughConfig();
            mSynchLengths = cfg.SynchLengths;
        }

        public void Play() {
            if (mPlaying)
                return;

            if (UnPaused != null)
                UnPaused();

            if (mEnabled && mEvents.Length > 0) {
                if (mEvents.CurrentEvent == null)
                    Time = 0;
                mCurrent = mEvents.CurrentEvent.Value;
                mCore.Update(mCurrent.Position, Vector3.Zero, mCurrent.Orientation, Rotation.Zero);
                mPrev = mCurrent;
                mLastTick = DateTime.Now;
                if (mSeparateThread) {
                    Thread t = new Thread(FlythroughThread);
                    t.Name = "Flythrough thread";
                    //t.Priority = ThreadPriority.Highest;
                    t.Start();
                } else
                    mCore.Tick += mTickListener;
            }
        }

        internal void AddEvent(FlythroughEvent<Camera> evt) {
            mEvents.AddEvent(evt);
            if (mEvents.Count == 1)
                evt.StartValue = Start;
        }

        internal void RemoveEvent(ComboEvent evt) {
            mEvents.RemoveEvent(evt);
            mEvents[0].StartValue = Start;
        }

        public void MoveUp(ComboEvent evt) {
            mEvents.MoveUp(evt);
            if (evt.SequenceStartTime == 0)
                evt.StartValue = Start;
        }

        public void Step() {
            if (mEvents.CurrentEvent != null && mEvents.CurrentEvent.GlobalFinishTime + 1 < Length) {
                Time = CurrentEvent.GlobalFinishTime + 1;
                Play();
            }
        }

        /// <summary>
        /// Initialise the flythrough transition an xml file.
        /// </summary>
        /// <param name="file">The file to load as a flythrough.</param>
        public void Load(string file) {
            if (!File.Exists(file)) {
                Logger.Warn("Unable to load " + file + ". Ignoring load request.");
                return;
            }

            if (FlythroughLoading != null)
                FlythroughLoading();

            mEvents = new EventSequence<Camera>();
            mEvents.LengthChange += new Action<EventSequence<Camera>, int>(mEvents_LengthChange);

            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            int start = 0;
            XmlNode root = doc.GetElementsByTagName("Events")[0];

            XmlAttribute startPositionAttr = root.Attributes["StartPosition"];
            XmlAttribute startPitchAttr = root.Attributes["StartPitch"];
            XmlAttribute startYawAttr = root.Attributes["StartYaw"];
            Vector3 startPos = mCore.Position;
            double startPitch = mCore.Orientation.Pitch;
            double startYaw = mCore.Orientation.Yaw;
            if (startPositionAttr != null) Vector3.TryParse(startPositionAttr.Value, out startPos);
            if (startPitchAttr != null) double.TryParse(startPitchAttr.Value, out startPitch);
            if (startYawAttr != null) double.TryParse(startYawAttr.Value, out startYaw);
            Start = new Camera(startPos, new Rotation(startPitch, startYaw));

            foreach (XmlNode node in root.ChildNodes) {
                if (node is XmlElement) {
                    ComboEvent evt = new ComboEvent(this);
                    evt.Load(node);
                    mEvents.AddEvent(evt);
                    start = evt.SequenceStartTime + evt.Length;
                }
            }

            mCore.Update(Start.Position, Vector3.Zero, Start.Orientation, Rotation.Zero);

            if (FlythroughLoaded != null)
                FlythroughLoaded();
        }

        /// <summary>
        /// Save the flythrough as an XML file.
        /// </summary>
        /// <param name="file">The file to save the XML serialization to.</param>
        public void Save(string file) {
            XmlDocument doc = new XmlDocument();
            XmlNode root = doc.CreateElement("Events");

            XmlAttribute startPositionAttr = doc.CreateAttribute("StartPosition");
            XmlAttribute startPitchAttr = doc.CreateAttribute("StartPitch");
            XmlAttribute startYawAttr = doc.CreateAttribute("StartYaw");
            startPositionAttr.Value = Start.Position.ToString();
            startPitchAttr.Value = Start.Orientation.Pitch.ToString();
            startYawAttr.Value = Start.Orientation.Yaw.ToString();
            root.Attributes.Append(startPositionAttr);
            root.Attributes.Append(startPitchAttr);
            root.Attributes.Append(startYawAttr);

            foreach (var evt in mEvents) {
                root.AppendChild(evt.Save(doc));
            }

            doc.AppendChild(root);
            doc.Save(file);
        }

        private void mEvents_LengthChange(EventSequence<Camera> sequence, int length) {
            if (LengthChange != null)
                LengthChange(length);
        }

        private void FlythroughThread() {
            mFinished = false;
            mPlaying = true;
#if DEBUG
            mStats.Begin();
#endif
            while (mPlaying && mEvents.Length > 0) {
                IncrementTime();
                mPrev = mCurrent;
                mCurrent = mEvents.CurrentEvent.Value;
#if DEBUG
                mStats.End();
#endif

                double wait = (mCore.TickLength) - DateTime.Now.Subtract(mLastTick).TotalMilliseconds;
                if (wait <= 0)
                    Logger.Debug("Flythrough Tick overran by " + (wait * -1) + "ms.");
                else
                    System.Threading.Thread.Sleep((int)wait);
#if DEBUG
                mStats.Begin();
#endif
                mLastTick = DateTime.Now;
                mCore.Update(mCurrent.Position, mCurrent.Position - mPrev.Position, mCurrent.Orientation, mCurrent.Orientation - mPrev.Orientation);
            }
            lock (mFinishLock) {
                mFinished = true;
                Monitor.PulseAll(mFinishLock);
            }
        }

        private readonly object mFinishLock = new object();
        private bool mFinished;

        private void IncrementTime() {
            mTicking = true;
            double incD = (mCore.TickLength * mSpeed);
            int inc = incD > int.MaxValue ? int.MaxValue - mEvents.Time : (int) incD;
            if (inc < 1)
                inc = 1;
            int newTime = mEvents.Time +  inc;
            if (newTime < mEvents.Length) {
                if (mAutoStep || (newTime < mEvents.CurrentEvent.GlobalFinishTime))
                    Time = newTime;
                else {
                    Time = mEvents.CurrentEvent.GlobalFinishTime;
                    Pause();
                }
            } else {
                if (mLoop)
                    Time = 0;
                else {
                    Time = mEvents.Length;
                    mPlaying = false;
                    if (SequenceFinished != null)
                        SequenceFinished(this, null);
                }
            }
            mTicking = false;
        }

        private Camera mCurrent;

        void mCoordinator_Tick() {
#if DEBUG
            mStats.Begin();
#endif
            mCore.Update(mCurrent.Position, mCurrent.Position - mPrev.Position, mCurrent.Orientation, mCurrent.Orientation - mPrev.Orientation);

            IncrementTime();
            mPrev = mCurrent;
            mCurrent = mEvents.CurrentEvent.Value;
#if DEBUG
            mStats.End();
#endif
        }

        /*
        void mCoordinator_Tick() {
            if (mPlaying && mEvents.Length > 0) {
                if (mEvents.Time + mCoordinator.TickLength < mEvents.Length)
                    DoTick(mEvents.Time + mCoordinator.TickLength, mEvents.CurrentEvent.Value);
                else {
                    if (mLoop)
                        DoTick(0, mEvents.Start);
                    else {
                        DoTick(mEvents.Length, mEvents.CurrentEvent.Value);
                        mPlaying = false;
                        if (SequenceFinished != null)
                            SequenceFinished(this, null);
                    }
                }
            }
        }

        private void DoTick(int time, Camera o) {
            mEvents.Time = time;
            Camera n = mEvents.CurrentEvent.Value;
            if (mEnabled && mPlaying)
                mCoordinator.Update(n.Position, n.Position - o.Position, n.Orientation, n.Orientation - o.Orientation);
            if (TimeChange != null)
                TimeChange(time);
        }
        */

        #region ISystemPlugin Members

        public event Action<IPlugin, bool> EnabledChanged;

        public Control ControlPanel {
            get {
                if (mPanel == null) {
                    mPanel = new FlythroughPanel(this);
                }
                return mPanel;
            }
        }

        public string Name {
            get { return "Flythrough"; }
        }

        public bool Enabled {
            get { return mEnabled; }
            set {
                mEnabled = value;
                if (EnabledChanged != null)
                    EnabledChanged(this, value);
            }
        }

        public ConfigBase Config {
            get { throw new NotImplementedException(); }
        }

        public string State {
            get {
                string dump = "----Flythrough----" + Environment.NewLine;
                dump += String.Format("{1:-20} {2}{0}", Environment.NewLine, "# Steps:", mEvents.Count);
                dump += String.Format("{1:-20} {2}{0}", Environment.NewLine, "Length:", mEvents.Length);
                dump += String.Format("{1:-20} {2}{0}", Environment.NewLine, "Playing:", mPlaying);
                dump += String.Format("{1:-20} {2}{0}", Environment.NewLine, "Loop:", mLoop);
                dump += String.Format("{1:-20} {2}{0}", Environment.NewLine, "Set Time:", mTime);
                dump += String.Format("{1:-20} {2}{0}{0}", Environment.NewLine, "Event List Time:", mEvents.Time);

                if (mEvents.CurrentEvent != null) {
                    try {
                        dump += "--Current Event: " + mEvents.CurrentEvent.Name + Environment.NewLine;
                        dump += mEvents.CurrentEvent.State;
                    } catch (Exception e) {
                        dump += "Unable to record window of " + mEvents.CurrentEvent.Name + "." + Environment.NewLine;
                        dump += e.Message + Environment.NewLine;
                        dump += e.StackTrace;
                    }
                } else
                    dump += "No current step";
                return dump;
            }
        }

        public Core Core {
            get { return mCore; }
        }

        public void Init(Core coordinator) {
            mCore = coordinator;

            FlythroughConfig cfg = new FlythroughConfig();
            mLoop = cfg.Loop;

            if (cfg.StartFile != null)
                mCore.InitialisationComplete += new Action(mCore_InitialisationComplete);
        }

        void mCore_InitialisationComplete() {
            FlythroughConfig cfg = new FlythroughConfig();
            string file = Path.GetFullPath(cfg.StartFile);
            Logger.Info("Auto loading " + file + ".");
            Load(file);
            if (cfg.Autostart) {
                Logger.Info("Auto playing " + file + ".");
                Play();
            }
        }

        public void Close() {
            mPlaying = false;
        }

        public void Draw(System.Drawing.Graphics graphics, Func<Vector3, Point> to2D, Action redraw, Perspective perspective) {
            //Do nothing
        }

        public void SetForm(Form form) {
        }

        #endregion
    }
}
