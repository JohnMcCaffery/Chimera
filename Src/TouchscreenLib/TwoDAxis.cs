﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Plugins;
using Chimera.GUI.Controls.Plugins;
using System.Windows.Forms;
using Chimera.Interfaces;

namespace Touchscreen {
    public class TwoDAxis : ConstrainedAxis, ITickListener {
        private VerticalAxis mWrappedAxis;
        private bool mX;
        private bool mDown;
        private bool mWasDown;
        private ConstrainedAxisPanel mPanel;

        public TwoDAxis(VerticalAxis axis, bool x, bool right)
            : base((right ? "Right" : "Left") + (x ? "X" : "Y")) {

            mX = x;
            mWrappedAxis = axis;
            mWrappedAxis.Manager.OnPress += i => mDown = true;
            mWrappedAxis.Manager.OnRelease += i => {
                mDown = false;
                SetRawValue(0f);
            };
            if (!mX) {
                Deadzone.Changed += v => mWrappedAxis.Deadzone.Value = v;
                Scale.Changed += v => mWrappedAxis.Scale.Value = v;
            } else
                Deadzone.Changed += v => mWrappedAxis.Change();
        }

        public override UserControl ControlPanel {
            get { return mX ? base.ControlPanel : mWrappedAxis.ControlPanel; }
        }

        public override float Delta {
            get { return mX ? base.Delta : mWrappedAxis.Delta; }
        }

        public override IUpdater<float> Deadzone {
            get { return mX ? base.Deadzone : mWrappedAxis.Deadzone; }
            set {
                if (mX)
                    base.Deadzone = value;
                else
                    mWrappedAxis.Deadzone = value;
            }
        }
        public override IUpdater<float> Scale {
            get { return mX ? base.Scale : mWrappedAxis.Scale; }
            set {
                if (mX)
                    base.Scale = value;
                else
                    mWrappedAxis.Scale = value;
            }
        }

        #region ITickListener Members

        public void Init(Chimera.ITickSource source) {
            if (mX)
                source.Tick += new Action(source_Tick);
            else
                mWrappedAxis.Init(source);
        }

        void source_Tick() {
            if (mDown && mWrappedAxis.Bounds.Contains(mWrappedAxis.Manager.CursorPosition)) {
                mWasDown = true;
                SetRawValue(VerticalAxis.GetValue(mWrappedAxis.StartH + mWrappedAxis.PaddingH, mWrappedAxis.W, mWrappedAxis.Manager.CursorX));
            } else if (mWasDown) {
                mWasDown = false;
                SetRawValue(0f);
            }

        }

        #endregion
    }
}