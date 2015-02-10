using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chimera.Config;
using Chimera.OpenSim.Plugins;

namespace Chimera.OpenSim.GUI.Controls.Plugins {
    public partial class KeyPresserPanel : UserControl {
        private KeyPresser mPlugin;
        private ViewerConfig mConfig;

        public KeyPresserPanel() {
            InitializeComponent();
        }

        public KeyPresserPanel(KeyPresser plugin) {
            InitializeComponent();

            mPlugin = plugin;
            mConfig = plugin.Config as ViewerConfig;

            mPlugin.Started += () => {
                if (InvokeRequired)
                    Invoke(new Action(() => startButton.Text = "Stop"));
                else
                    startButton.Text = "Stop";
            };

            mPlugin.Stopped += () => {
                if (InvokeRequired)
                    Invoke(new Action(() => startButton.Text = "Start"));
                else
                    startButton.Text = "Start";
            };

            intervalS.Value = new Decimal(mConfig.IntervalMS / 1000);
            stopM.Value = new Decimal(mConfig.StopM);
            shutdownBox.Checked = mConfig.AutoShutdown;

            key.Text = mConfig.Key;
        }

        private void startButton_Click(object sender, EventArgs e) {
            mPlugin.Running = !mPlugin.Running;
        }

        private void intervalS_ValueChanged(object sender, EventArgs e) {
            mConfig.IntervalMS = Decimal.ToDouble(intervalS.Value) * 1000.0;
        }

        private void shutdownM_ValueChanged(object sender, EventArgs e) {
            mConfig.StopM = Decimal.ToDouble(stopM.Value);
        }

        private void shutdownBox_CheckedChanged(object sender, EventArgs e) {
            mConfig.AutoShutdown = shutdownBox.Checked;
        }
    }
}
