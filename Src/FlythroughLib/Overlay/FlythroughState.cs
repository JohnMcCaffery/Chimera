﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Interfaces.Overlay;
using Chimera.Overlay;

namespace Chimera.Flythrough.Overlay {
    public class FlythroughState : State {
        private Flythrough mInput;
        private string mFlythrough;

        public FlythroughState(string name, StateManager manager, string flythrough)
            : base(name, manager) {

            mFlythrough = flythrough;
            mInput = manager.Coordinator.GetInput<Flythrough>();
        }

        public override IWindowState CreateWindowState(Window window) {
            return new FlythroughWindowState(window.OverlayManager);
        }

        protected override void OnActivated() {
            mInput.Coordinator.EnableUpdates = true;
            mInput.Load(mFlythrough);
            mInput.Loop = true;
            mInput.Play();
        }

        protected override void OnDeActivated() {
            mInput.Paused = true;
        }
    }
}
