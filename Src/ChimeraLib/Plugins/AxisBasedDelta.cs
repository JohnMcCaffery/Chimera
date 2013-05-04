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
using Chimera.Interfaces;
using OpenMetaverse;
using Chimera.Util;
using System.Windows.Forms;
using Chimera.GUI.Controls.Plugins;
using System.Drawing;
using System.IO;
using Chimera.Config;

namespace Chimera.Plugins {
    public class AxisBasedDelta : DeltaBasedPlugin, ISystemPlugin {
        private readonly List<IAxis> mAxes = new List<IAxis>();
        private readonly string mName;
        private ITickSource mSource;
        private AxisConfig mConfig;

        private AxisBasedDeltaPanel mPanel;
        private float mScale = 1f;
        private float mRotXMove = 3f;

        public event Action<IAxis> AxisAdded;

        public float Scale {
            get { return mScale; }
            set {
                mScale = value;
                TriggerChange(this);
            }
        }

        public float RotXMove { 
            get { return mRotXMove; }
            set {
                mRotXMove = value;
                TriggerChange(this);
            }
        }

        public IEnumerable<IAxis> Axes {
            get { return mAxes; }
        }

        /// <summary>
        /// Input axes will automatically be assigned to camera axes if no axis is specified.
        /// The ordering is as follows:
        /// 1st axis: x
        /// 2nd axis: y
        /// 3rd axis: z
        /// 4th axis: pitch
        /// 5th axis : yaw
        /// Specify null if you do not which to assign that axis.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="axes"></param>
        public AxisBasedDelta(string name, params IAxis[] axes) {
            mName = name;
            mConfig = new AxisConfig(name);

            int i = 0;
            if (axes.Length > i && axes[i] != null && axes[i].Binding == AxisBinding.None)
                axes[i].Binding = AxisBinding.X;
            i++;
            if (axes.Length > i && axes[i] != null && axes[i].Binding == AxisBinding.None)
                axes[i].Binding = AxisBinding.Y;
            i++;
            if (axes.Length > i && axes[i] != null && axes[i].Binding == AxisBinding.None)
                axes[i].Binding = AxisBinding.Z;
            i++;
            if (axes.Length > i && axes[i] != null && axes[i].Binding == AxisBinding.None)
                axes[i].Binding = AxisBinding.Pitch;
            i++;
            if (axes.Length > i && axes[i] != null && axes[i].Binding == AxisBinding.None)
                axes[i].Binding = AxisBinding.Yaw;

            foreach (var axis in axes)
                if (axis != null)
                    AddAxis(axis);
        }

        public void AddAxis(IAxis axis) {
            mAxes.Add(axis);
            if (mSource != null && axis is ITickListener)
                (axis as ITickListener).Init(mSource);
            if (axis is ConstrainedAxis) {
                ConstrainedAxis ax = axis as ConstrainedAxis;
                ax.Deadzone.Value = AxConfig.GetDeadzone(axis.Name);
                ax.Scale.Value  = AxConfig.GetScale(axis.Name);
            }
            AxisBinding binding = AxConfig.GetBinding(axis.Name);
            if (binding != AxisBinding.None)
                axis.Binding = binding;
            if (AxisAdded != null)
                AxisAdded(axis);
        }

        void axis_Changed() {
            TriggerChange(this);
        }

        #region IDeltaInput Members

        public override Vector3 PositionDelta {
            get {
                float x = mAxes.Where(a => a.Binding == AxisBinding.X).Sum(a => a.Delta);
                float y = mAxes.Where(a => a.Binding == AxisBinding.Y).Sum(a => a.Delta);
                float z = mAxes.Where(a => a.Binding == AxisBinding.Z).Sum(a => a.Delta);
                return new Vector3(x, y, z) * mScale;
            }
        }

        public override Rotation OrientationDelta {
            get { 
                float p = mAxes.Where(a => a.Binding == AxisBinding.Pitch).Sum(a => a.Delta);
                float y = mAxes.Where(a => a.Binding == AxisBinding.Yaw).Sum(a => a.Delta);
                return new Rotation(p * mScale * mRotXMove, y * mScale * mRotXMove);
            }
        }

        public override void Init(Coordinator input) {
            base.Init(input);
            mSource = input;
            foreach (var axis in mAxes)
                if (axis is ITickListener)
                    (axis as ITickListener).Init(input);

            input.Tick += new Action(input_Tick);
        }

        void input_Tick() {
            TriggerChange(this);
        }

        #endregion

        #region IInput Members

        public override UserControl ControlPanel {
            get {
                if (mPanel == null)
                    mPanel = new AxisBasedDeltaPanel(this);
                return mPanel;
            }
        }

        public override string Name {
            get { return mName; }
        }

        public override string State {
            get { throw new NotImplementedException(); }
        }

        public override ConfigBase Config {
            get { throw new NotImplementedException(); }
        }

        public override void Close() { }

        public override void Draw(Func<Vector3, Point> to2D, Graphics graphics, Action redraw) { }

        #endregion

        protected virtual AxisConfig AxConfig {
            get { return mConfig; }
        }

        public class AxisConfig : ConfigFolderBase {
            private string mType;

            public AxisConfig(string type)
                : base("Movement", new string[0]) {
                mType = type;
            }

            public override string Group {
                get { return mType + "Movement"; }
            }

            protected override void InitConfig() {
                Get("Movement", "Deadzone|X|", .1f, "The deadzone for axis |X|.");
                Get("Movement", "Scale|X|", .1f, "The scale factor for axis |X|.");
                Get("Movement", "Binding|X|", "None", "The camera axis binding for axis |X|.");
            }

            public float GetDeadzone(string name) {
                return Get("Movement", "Deadzone" + name, .1f, "");
            }

            public float GetScale(string name) {
                return Get("Movement", "Scale" + name, 1f, "");
            }

            public AxisBinding GetBinding(string name) {
                string binding = Get("Movement", "Binding" + name, "None", "");
                switch (binding.ToUpper()) {
                    case "X": return AxisBinding.X;
                    case "Y": return AxisBinding.Y;
                    case "Z": return AxisBinding.Z;
                    case "Pitch": return AxisBinding.Pitch;
                    case "Yaw": return AxisBinding.Yaw;
                }
                return AxisBinding.None;
            }
        }
    }
}
