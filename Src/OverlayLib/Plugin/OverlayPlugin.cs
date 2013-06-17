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
using Chimera.Interfaces.Overlay;
using System.IO;
using Chimera.Util;
using System.Xml;
using Chimera.Overlay.Features;
using System.Drawing;
using Chimera.Overlay.Triggers;
using System.Windows.Forms;
using Chimera.Overlay.GUI.Plugins;
using Chimera.Config;
using Chimera.Overlay.Transitions;
using Chimera.Interfaces;
using OpenMetaverse;

namespace Chimera.Overlay {
    public partial class OverlayPlugin : XmlLoader, ISystemPlugin {
        private OverlayConfig mConfig;
        private IMediaPlayer mPlayer;
        private Action mRedraw;

        public string Statistics {
            get {
                string table = "";
                table += "<TABLE BORDER=\"1\">" + Environment.NewLine;
                table += "    <TR>" + Environment.NewLine;
                table += "        <TD>State Name</TD>" + Environment.NewLine;
                table += "        <TD># Visits</TD>" + Environment.NewLine;
                table += "        <TD>Time</TD>" + Environment.NewLine;
                table += "        <TD>Longest Visit (m)</TD>" + Environment.NewLine;
                table += "        <TD>Shortest Visit (m)</TD>" + Environment.NewLine;
                table += "        <TD>Mean Visit Length (m)</TD>" + Environment.NewLine;
                table += "    </TR>" + Environment.NewLine;
                table += "</TABLE>";
                return table;
            }
        }

        internal void Dump(string reason) {
            ProcessWrangler.Dump(Statistics, reason + ".html");
        }

        #region ISystemInput members

        public event Action<IPlugin, bool> EnabledChanged;

        public UserControl ControlPanel {
            get {
                if (mPanel == null) {
                    if (mPlayer != null) {
                        //Hack to make sure the video control is created on the right thread.
                        Control c = mPlayer.Player;
                    }
                    mPanel = new OverlayPluginPanel(this);
                }
                return mPanel;
            }
        }

        public bool Enabled {
            get {
                return mConfig.LaunchOverlay;
            }
            set {
                if (mConfig.LaunchOverlay != value) {
                    if (mConfig.LaunchOverlay != value) {
                        mConfig.LaunchOverlay = value;
                        foreach (var manager in mFrameManagers.Values) {
                            if (value) {
                                manager.Launch();
                                if (OverlayLaunched != null)
                                    OverlayLaunched();
                            } else {
                                manager.Close();
                                if (OverlayClosed != null)
                                    OverlayClosed();
                            }
                        }
                    }
                    mConfig.LaunchOverlay = value;
                    if (EnabledChanged != null)
                        EnabledChanged(this, value);
                }
            }
        }

        public string Name {
            get { return "Overlay"; }
        }

        public string State {
            get { throw new NotImplementedException(); }
        }

        public ConfigBase Config {
            get { return mConfig; }
        }

        public void SetForm(Form form) {
            mMasterForm = form;
            foreach (var manager in mFrameManagers.Values)
                manager.SetForm(form);
        }
        
        /// <summary>
        /// CreateWindowState the manager. Linking it with a coordinator.
        /// </summary>
        /// <param name="coordinator">The coordinator which this state form manages state for.</param>
        public void Init(Core coordinator) {
            mCoordinator = coordinator;
            mTransitionComplete = new Action<StateTransition>(transition_Finished);
            mCoordinator.FrameAdded += new Action<Frame,EventArgs>(mCoordinator_FrameAdded);

            foreach (var window in mCoordinator.Frames)
                mCoordinator_FrameAdded(window, null);

            if (mConfig.OverlayFile != null)
                LoadXML(mConfig.OverlayFile);
        }

        public void Close() { }

        public void Draw(Graphics graphics, Func<Vector3, Point> to2D, Action redraw, Perspective perspective) {
            if (mCurrentState != null) {
                if (mRedraw == null)
                    mRedraw = redraw;
                mCurrentState.Draw(graphics, to2D, redraw, perspective);
            }
        }

        #endregion

        private bool mControlPointers = true;

        public bool ControlPointers {
            get { return mControlPointers; }
            set {
                mControlPointers = value;
                foreach (var frame in mFrameManagers.Values)
                    frame.ControlPointer = value;
            }
        }
    }
}