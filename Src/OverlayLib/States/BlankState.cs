﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Chimera.Interfaces.Overlay;
using Chimera.Overlay.Features;
using System.Drawing;

namespace Chimera.Overlay.States {
    public class BlankStateFactory : IStateFactory {
        public State Create(OverlayPlugin manager, XmlNode node) {
            return new BlankState(manager, node);
        }

        public State Create(OverlayPlugin manager, XmlNode node, System.Drawing.Rectangle clip) {
            return Create(manager, node);
        }

        public string Name {
            get { return "Blank"; }
        }
    }

    public class BlankState : State {
        private bool mUseDefaultBG;
        private bool mEnableCursor;
        private double mOpacity;
        private Color mDefaultBG;

        public BlankState(OverlayPlugin plugin, XmlNode node)
            : base(GetName(node, "Blank State"), plugin) {

            mEnableCursor = GetBool(node, true, "EnableCursor");
            mDefaultBG = GetColour(node, "blank state bg colour", Color.Transparent);
            if (mDefaultBG != Color.Transparent)
                mUseDefaultBG = true;
            else if (GetBool(node, false, "BlackBG")) {
                mDefaultBG = Color.Black;
                mUseDefaultBG = true;
            }
            mOpacity = GetDouble(node, 1.0, "Opacity");
        }

        public override IFrameState CreateWindowState(FrameOverlayManager manager) {
            IFrameState w = new FrameState(manager);
            if (mUseDefaultBG)
                w.AddFeature(new ColourFeature(mDefaultBG));
            return w;
        }

        protected override void TransitionToStart() { }

        protected override void TransitionToFinish() {
            Manager.ControlPointers = mEnableCursor;
            foreach (var frameManager in Manager.OverlayManagers)
                frameManager.Opacity = mOpacity;
        }

        protected override void TransitionFromStart() { }

        protected override void TransitionFromFinish() { }
    }
}
