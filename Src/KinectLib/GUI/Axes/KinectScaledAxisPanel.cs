﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chimera.Kinect.Axes;
using NuiLibDotNet;
using Chimera.Plugins;

namespace Chimera.Kinect.GUI.Axes {
    public partial class KinectScaledAxisPanel : UserControl {
        private KinectScaledAxis mAxis;
        private ChangeDelegate mChangeListener;

        public KinectScaledAxis Axis {
            get { return mAxis; }
            set {
                mAxis = value;

                stateLabel.Text = mAxis.Active.Value ? "Active" : "Disabled";

                constrainedAxisPanel.Axis = mAxis.Axis;
                mChangeListener = new ChangeDelegate(Active_OnChange);

                scalePanel.Scalar = new ScalarUpdater(mAxis.ScaleScale);
                deadzonePanel.Scalar = new ScalarUpdater(mAxis.DeadzoneScale);

                scalePanel.Max = mAxis.ScaleScale.Value * 6f;
                deadzonePanel.Max = mAxis.DeadzoneScale.Value * 6f;

                mAxis.Active.OnChange += mChangeListener;
                Disposed += new EventHandler(KinectAxisPanel_Disposed);
            }
        }

        public KinectScaledAxisPanel() {
            InitializeComponent();
        }

        public KinectScaledAxisPanel(KinectScaledAxis axis)
            : this() {

            Axis = axis;
        }

        void KinectAxisPanel_Disposed(object sender, EventArgs e) {
            mAxis.Active.OnChange -= mChangeListener;
        }

        void Active_OnChange() {
            if (Created)
                BeginInvoke(new Action(() => stateLabel.Text = mAxis.Active.Value ? "Active" : "Disabled"));
        }
    }
}
