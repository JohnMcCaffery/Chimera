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
using NuiLibDotNet;
using Chimera.Overlay;
using System.Xml;
using System.Drawing;
using Chimera.Overlay.Triggers;

namespace Chimera.Kinect.Overlay {
    public class SkeletonLostFactory : OverlayXmlLoader, ITriggerFactory {
        public SpecialTrigger Special {
            get { return SpecialTrigger.None; }
        }

        public string Mode {
            get { return OverlayPlugin.HOVER_MODE; }
        }

        public string Name {
            get { return "SkeletonLost"; }
        }

        public ITrigger Create(OverlayPlugin manager, XmlNode node) {
            double timeout = GetDouble(node, 30000, "Timeout");
            return new SkeletonLostTrigger(manager.Core, timeout);
        }

        public ITrigger Create(OverlayPlugin manager, XmlNode node, Rectangle clip) {
            return Create(manager, node);
        }
    }
    public class SkeletonLostTrigger : TriggerBase, ITrigger {
        private bool mActive;
        private DateTime mLost;
        private Action mTickListener;
        private Core mCoordinator;
        private bool mWaiting = false;
        private double mTimeout;

        #region ITrigger Members

        public override bool Active {
            get { return mActive; }
            set {
                if (mActive != value) {
                    mActive = value;
                    if (!value && mWaiting)
                        Nui_SkeletonFound();
                    else if (value && !Nui.HasSkeleton)
                        Nui_SkeletonLost();
                }
            }
        }

        #endregion

        /// <summary>
        /// How long after losing a skeleton the trigger fires.
        /// </summary>
        public double Timeout {
            get { return mTimeout; }
            set { mTimeout = value; }
        }

        public SkeletonLostTrigger(Core coordinator) {
            mCoordinator = coordinator;
            Nui.SkeletonLost += new SkeletonTrackDelegate(Nui_SkeletonLost);
            Nui.SkeletonFound += new SkeletonTrackDelegate(Nui_SkeletonFound);
            mTickListener = new Action(coordinator_Tick);
            if (!Nui.HasSkeleton)
                Nui_SkeletonLost();
        }

        public SkeletonLostTrigger(Core coordinator, double timeout)
            : this(coordinator) {
            mTimeout = timeout;
        }

        void Nui_SkeletonFound() {
            mCoordinator.Tick -= mTickListener;
            mWaiting = false;
        }

        void Nui_SkeletonLost() {
            if (mActive) {
                mLost = DateTime.Now;
                mWaiting = true;
                mCoordinator.Tick += mTickListener;
            }
        }

        void coordinator_Tick() {
            if (DateTime.Now.Subtract(mLost).TotalMilliseconds > mTimeout) {
                Trigger();
                mCoordinator.Tick -= mTickListener;
            }
        }

    }
}
