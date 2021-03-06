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
using NuiLibDotNet;
using C = NuiLibDotNet.Condition;
using System.Drawing;
using System.Windows.Forms;
using Chimera.Kinect.GUI;
using Chimera.Util;
using OpenMetaverse;
using Chimera.Overlay;
using Chimera.Config;
using log4net;

namespace Chimera.Kinect {
    public class SimpleKinectCursor : ISystemPlugin {
        private ILog Logger = LogManager.GetLogger("KinectCursor");

        private SimpleCursorPanel mPanel;
        private Vector mHandR;
        private Vector mHandL;
        private Vector mAnchor;
        private Scalar mWidth;
        private Scalar mHeight;
        private Scalar mLeftHandShift;
        private Scalar mLeftShift;
        private Scalar mUpShift;
        private Scalar mTopLeftX;
        private Scalar mTopLeftY;
        private Scalar mX;
        private Scalar mY;
        private Scalar mRawXRight;
        private Scalar mRawYRight;
        private Scalar mRawXLeft;
        private Scalar mRawYLeft;
        private Scalar mConstrainedXRight;
        private Scalar mConstrainedYRight;
        private Scalar mConstrainedXLeft;
        private Scalar mConstrainedYLeft;
        private Scalar mSmoothingFactor;
        private Condition mOnScreenConditionRight;
        private Condition mOnScreenConditionLeft;
        private PointF mLocation = new PointF(-1f, -1f);
        private RectangleF mBounds = new RectangleF(0f, 0f, 1f, 1f);
        private bool mEnabled;
        private bool mListening;
        //TODO add init of this to the config
        private string mWindow = "MainWindow";

        private FrameOverlayManager mManager;
        private OverlayPlugin mOverlayPlugin;

        public Vector Anchor { get { return mAnchor; } }
        public Vector HandR { get { return mHandR; } }
        public Vector HandL { get { return mHandL; } }
        public Scalar Width { get { return mWidth; } }
        public Scalar Height { get { return mHeight; } }
        public Scalar LeftHandShift { get { return mLeftHandShift; } }
        public Scalar LeftShift { get { return mLeftShift; } }
        public Scalar UpShift { get { return mUpShift; } }
        public Scalar TopLeftX { get { return mTopLeftX; } }
        public Scalar TopLeftY { get { return mTopLeftY; } }
        public Scalar RawXRight { get { return mRawXRight; } }
        public Scalar RawYRight { get { return mRawYRight; } }
        public Scalar RawXLeft { get { return mRawXLeft; } }
        public Scalar RawYLeft { get { return mRawYLeft; } }
        public Scalar ConstrainedXRight { get { return mConstrainedXRight; } }
        public Scalar ConstrainedYRight { get { return mConstrainedYRight; } }
        public Scalar ConstrainedXLeft { get { return mConstrainedXLeft; } }
        public Scalar ConstrainedYLeft { get { return mConstrainedYLeft; } }
        public Scalar X { get { return mX; } }
        public Scalar Y { get { return mY; } }
        public Scalar SmoothingFactor { get { return mSmoothingFactor; } }

        private static readonly int HAND_SMOOTHING_FRAMES = 5;
        private static readonly int ANCHOR_SMOOTHING_FRAMES = 15;
        private static readonly int ANCHOR = Nui.Hip_Centre;

        private ChangeDelegate mTickListener;

        public SimpleKinectCursor() {
            GlobalConditions.Init();

            mTickListener = new ChangeDelegate(Nui_Tick);
            mSmoothingFactor = Scalar.Create(HAND_SMOOTHING_FRAMES);
            /*
            mHandR = test ? Vector.Create("HandR", 0f, 0f, 0f) : Nui.smooth(Nui.joint(Nui.Hand_Right), mSmoothingFactor);
            mHandL = test ? Vector.Create("HandL", 0f, 0f, 0f) : Nui.smooth(Nui.joint(Nui.Hand_Left), HAND_SMOOTHING_FRAMES);
            mAnchor = test ? Vector.Create("Anchor", 0f, 0f, 0f) : Nui.smooth(Nui.joint(Nui.Hip_Centre), ANCHOR_SMOOTHING_FRAMES);
            */
            mHandR = Nui.smooth(Nui.joint(Nui.Hand_Right), mSmoothingFactor);
            mHandL = Nui.smooth(Nui.joint(Nui.Hand_Left), HAND_SMOOTHING_FRAMES);
            mAnchor = Nui.smooth(Nui.joint(Nui.Hip_Centre), ANCHOR_SMOOTHING_FRAMES);

            mLeftShift = Scalar.Create("Left Shift", .0f);
            mUpShift = Scalar.Create("Up Shift", .0f);

            mWidth = Scalar.Create("Width", .5f);
            mHeight = Scalar.Create("Height", .5f);
            //mWidth = headShoulderDiff * mWidthScale;
            //mHeight = headShoulderDiff * mHeightScale;

            mTopLeftX = Nui.x(mAnchor) - (mWidth * mLeftShift);
            mTopLeftY = Nui.y(mAnchor) + (mHeight * mUpShift);

            mRawXRight = Nui.x(mHandR) - mTopLeftX;
            mRawYRight = Nui.y(mHandR) - mTopLeftY;
            Condition xActiveRight = C.And(mRawXRight >= 0f, mRawXRight <= mWidth);
            Condition yActiveRight = C.And(mRawYRight >= 0f, mRawYRight <= mHeight);
            mConstrainedXRight = Nui.constrain(mRawXRight, .01f, mWidth, .10f, false);
            mConstrainedYRight = Nui.constrain(mRawYRight, .01f, mHeight, .10f, false);
            mOnScreenConditionRight = C.And(xActiveRight, yActiveRight);

            mLeftHandShift = Scalar.Create("LeftHandShift", .5f);
            mRawXLeft = (Nui.x(mHandL) + mLeftHandShift) - mTopLeftX;
            mRawYLeft = Nui.y(mHandL) - mTopLeftY;
            Condition xActiveLeft = C.And(mRawXLeft >= 0f, mRawXLeft <= mWidth);
            Condition yActiveLeft = C.And(mRawYLeft >= 0f, mRawYLeft <= mHeight);
            mConstrainedXLeft = Nui.constrain(mRawXLeft, .01f, mWidth, .10f, false);
            mConstrainedYLeft = Nui.constrain(mRawYLeft, .01f, mHeight, .10f, false);
            mOnScreenConditionLeft = C.And(xActiveLeft, yActiveLeft);
        }

        void Nui_Tick() {
            float x = mX.Value;
            float y = mY.Value;

            if (mLocation.X != y || mLocation.Y != y) {
                mLocation = new PointF(x, y);

                if (mBounds.Contains(mLocation) && !OnScreen) {
                    if (CursorEnter != null && mEnabled)
                        CursorEnter(this);
                } else if (!mBounds.Contains(mLocation) && OnScreen) {
                    if (CursorLeave != null && mEnabled)
                        CursorLeave(this);
                }

                if (mEnabled && Nui.HasSkeleton) {
                    if (CursorMove != null)
                        CursorMove(this, x, y);
                    mManager.UpdateCursor(x, y);
                }
            }
        }

        private void Init() {
            //mX = mConstrainedX * (float)mManager.Monitor.Bounds.Width;
            //mY = (float) mManager.Monitor.Bounds.Height - (mConstrainedY * (float)mManager.Monitor.Bounds.Height);
            mX = Nui.ifScalar(C.And(mOnScreenConditionLeft, !mOnScreenConditionRight), mConstrainedXLeft, mConstrainedXRight);
            mY = 1f - Nui.ifScalar(C.And(mOnScreenConditionLeft, !mOnScreenConditionRight), mConstrainedYLeft, mConstrainedYRight);
            //mY = 1f - mConstrainedYRight;

        }

        void Nui_SkeletonLost() {
            mManager.MoveCursorOffScreen();
        }

        #region IKinectCursorWindow Members

        public event Action<SimpleKinectCursor> CursorEnter;

        public event Action<SimpleKinectCursor> CursorLeave;

        public event Action<SimpleKinectCursor, float, float> CursorMove;

        public event Action<IPlugin, bool> EnabledChanged;

        public PointF Location {
            get { return mLocation; }
        }

        public Control ControlPanel {
            get {
                if (mPanel == null)
                    mPanel = new SimpleCursorPanel(this);
                return mPanel; 
            }
        }

        public Frame Frame {
            get { return mManager.Frame; }
        }

        public string State {
            get { return ""; }
        }

        public bool OnScreen {
            get { return Nui.HasSkeleton && mOnScreenConditionRight.Value; }
        }

        public bool Enabled {
            get { return mEnabled; }
            set {
                if (value != mEnabled) {
                    mEnabled = value;
                    if (mOverlayPlugin != null) {
                        if (value)
                            Nui.Tick += mTickListener;
                        else
                            Nui.Tick -= mTickListener;
                    }
                    if (EnabledChanged != null)
                        EnabledChanged(this, value);
                }
            }
        }

        #endregion

        #region ISystemPlugin Members

        private Action<Frame, EventArgs> mWindowAddedListener;

        public void Init(Core core) {
            if (!core.HasPlugin<OverlayPlugin>()) {
                //throw new ArgumentException("Unable to load kinect cursor. Overlay plugin is not loaded.");
                Logger.Warn("Unable to load kinect cursor. Overlay plugin is not loaded.");
                Init();
                return;
            }
            mOverlayPlugin = core.GetPlugin<OverlayPlugin>();
            if (core.HasFrame(mWindow)) {
                mManager = mOverlayPlugin[mWindow];
                Nui.SkeletonLost += new SkeletonTrackDelegate(Nui_SkeletonLost);
            } else {
                mWindowAddedListener = new Action<Chimera.Frame, EventArgs>(coordinator_WindowAdded);
                core.FrameAdded += mWindowAddedListener;
            }
            Init();
        }
        #endregion

        #region IPlugin Members

        public string Name {
            get { return "KinectCursor"; }
        }

        public ConfigBase Config {
            get { throw new NotImplementedException(); }
        }

        public void Close() { }

        public void Draw(Graphics graphics, Func<Vector3, Point> to2D, Action redraw, Perspective perspective) { }

        #endregion

        private void coordinator_WindowAdded(Frame frame, EventArgs args) {
            if (frame.Name == mWindow) {
                mManager = mOverlayPlugin[frame.Name];
                frame.Core.FrameAdded -= mWindowAddedListener;
            }
        }

        #region ISystemPlugin Members


        public void SetForm(Form form) {
        }

        #endregion
    }
}
