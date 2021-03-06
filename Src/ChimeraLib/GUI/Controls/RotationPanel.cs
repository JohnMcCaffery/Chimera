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
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenMetaverse;
using Chimera.Util;

namespace Chimera.GUI {
    public partial class RotationPanel : UserControl {
        private Rotation rotation = Rotation.Zero;
        public event EventHandler OnChange;
        private bool mExternalChange;
        private bool mGuiChange;

        public Rotation Value {
            get { return rotation; }
            set {
                if (rotation != null)
                    rotation.Changed -= RotationChanged;
                rotation = value;
                rotation.Changed += RotationChanged;
                RotationChanged(this, null);
                if (OnChange != null)
                    OnChange(this, null);
            }
        }
        private void RotationChanged(object source, EventArgs args) {
            if (!mGuiChange) {
                Invoke(() => {
                    mExternalChange = true;
                    vectorPanel.Value = rotation.LookAtVector;
                    pitchValue.Value = Math.Max(pitchValue.Minimum, Math.Min(pitchValue.Maximum, new decimal(rotation.Pitch)));
                    pitchSlider.Value = Math.Max(pitchSlider.Minimum, Math.Min(pitchSlider.Maximum, (int)rotation.Pitch));
                    yawValue.Value = Math.Max(yawValue.Minimum, Math.Min(yawValue.Maximum, new decimal(rotation.Yaw)));
                    yawSlider.Value = Math.Max(yawSlider.Minimum, Math.Min(yawSlider.Maximum, (int)rotation.Yaw));
                    mExternalChange = false;
                });
            }
        }
        public Quaternion Quaternion {
            get { return rotation.Quaternion; }
            set { rotation.Quaternion = value; }
        }
        public Vector3 LookAtVector {
            get { return rotation.LookAtVector; }
            set { rotation.LookAtVector = value; }
        }
        public double Yaw {
            get { return rotation.Yaw; }
            set { rotation.Yaw = value; }
        }
        public double Pitch {
            get { return rotation.Pitch; }
            set { rotation.Pitch = value; }
        }

        public RotationPanel() {
            InitializeComponent();
            Value = Rotation.Zero;
        }

        private void Invoke(Action a) {
            if (InvokeRequired && Created && !IsDisposed && !Disposing)
                BeginInvoke(a);
            else
                a();
        }

        public override string Text {
            get { return nameLabel.Text; }
            set {
                vectorPanel.Text = value;
                nameLabel.Text = value;
            }
        }

        private void rollSlider_Scroll(object sender, EventArgs e) {
            rollValue.Value = rollSlider.Value;
        }

        private void pitchSlider_Scroll(object sender, EventArgs e) {
            pitchValue.Value = pitchSlider.Value;
        }

        private void yawSlider_Scroll(object sender, EventArgs e) {
            yawValue.Value = yawSlider.Value;
        }

        private void rollValue_ValueChanged(object sender, EventArgs e) {
            if (!mExternalChange) {
                mGuiChange = true;
                //TODO implement Rotation.Roll
                //rotation.Roll = (float) decimal.ToDouble(yawValue.Value);
                mGuiChange = false;
            }
        }

        private void pitchValue_ValueChanged(object sender, EventArgs e) {
            if (!mExternalChange) {
                mGuiChange = true;
                rotation.Pitch = decimal.ToDouble(pitchValue.Value);
                if (OnChange != null)
                    OnChange(this, null);
                mGuiChange = false;
            }
        }

        private void yawValue_ValueChanged(object sender, EventArgs e) {
            if (!mExternalChange) {
                mGuiChange = true;
                rotation.Yaw = decimal.ToDouble(yawValue.Value);
                if (OnChange != null)
                    OnChange(this, null);
                mGuiChange = false;
            }
        }

        private void vectorPanel_OnChange(object sender, EventArgs e) {
            if (!mExternalChange) {
                mGuiChange = true;
                rotation.LookAtVector = vectorPanel.Value;
                mGuiChange = false;
            }
        }

        private void rpyButton_CheckedChanged(object sender, EventArgs e) {
            vectorPanel.Visible = !rpyButton.Checked;
        }

        private void lookAtButton_CheckedChanged(object sender, EventArgs e) {
            vectorPanel.Visible = lookAtButton.Checked;
        }
    }
}
