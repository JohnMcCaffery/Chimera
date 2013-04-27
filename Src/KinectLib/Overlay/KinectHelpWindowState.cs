﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Overlay;

namespace Chimera.Kinect.Overlay {
    public class KinectHelpWindowState : WindowState {

        public KinectHelpWindowState(WindowOverlayManager manager)
            : base(manager) {
        }

        protected override void OnActivated() {
            Manager.Opacity = .3;
            Manager.ControlPointer = false;
        }
    }
}