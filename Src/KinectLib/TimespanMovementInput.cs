﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuiLibDotNet;
using C = NuiLibDotNet.Condition;
using OpenMetaverse;
using System.Windows.Forms;
using Chimera.Util;
using Chimera.Kinect.GUI;

namespace Chimera.Kinect {
    public class TimespanMovementInput : IDeltaInput {
        private Scalar mWalkDiffR;
        private Scalar mWalkDiffL;
        private Scalar mWalkValR;
        private Scalar mWalkValL;
        private Scalar mWalkScale;
        private Scalar mWalkThreshold;

        private Vector mArmR;
        private Vector mArmL;
        private Scalar mFlyVal;
        private Scalar mFlyAngleR;
        private Scalar mFlyAngleL;
        private Scalar mConstrainedFlyAngleR;
        private Scalar mConstrainedFlyAngleL;
        private Scalar mFlyScale;
        private Scalar mFlyThreshold;
        private Scalar mFlyMax;
        private Scalar mFlyTimer;
        private Scalar mFlyMin;

        private Scalar mYawLean;
        private Scalar mYawTwist;
        private Scalar mYaw;
        private Scalar mYawScale;
        private Scalar mYawThreshold;

        private Scalar mWalkVal;
        private Vector3 mDelta;
        private double mPitchDelta, mYawDelta;

        private bool mWalkEnabled = true;
        private bool mFlyEnabled = true;
        private bool mYawEnabled = true;
        private bool mEnabled = true;

        private DateTime mFlyStart;
        private bool mFlying;

        private TimespanMovementPanel mPanel;

        public Scalar WalkVal { get { return mWalkVal; } }
        public Scalar WalkDiffR { get { return mWalkDiffR; } }
        public Scalar WalkDiffL { get { return mWalkDiffL; } }
        public Scalar WalkValR { get { return mWalkValR; } }
        public Scalar WalkValL { get { return mWalkValL; } }
        public Scalar WalkScale { get { return mWalkScale; } }
        public Scalar WalkThreshold { get { return mWalkThreshold; } }

        public Vector ArmR { get { return mArmR; } }
        public Vector ArmL { get { return mArmL; } }
        public Scalar FlyVal { get { return mFlyVal; } }
        public Scalar FlyAngleR { get { return mFlyAngleR; } }
        public Scalar FlyAngleL { get { return mFlyAngleL; } }
        public Scalar ConstrainedFlyAngleR { get { return mConstrainedFlyAngleR; } }
        public Scalar ConstrainedFlyAngleL { get { return mConstrainedFlyAngleL; } }
        public Scalar FlyScale { get { return mFlyScale; } }
        public Scalar FlyThreshold { get { return mFlyThreshold; } }
        public Scalar FlyMax { get { return mFlyMax; } }
        public Scalar FlyTimer { get { return mFlyTimer; } }
        public Scalar FlyMin { get { return mFlyMin; } }

        public Scalar YawLean { get { return mYawLean; } }
        public Scalar YawTwist { get { return mYawTwist; } }
        public Scalar Yaw { get { return mYaw; } }
        public Scalar YawScale { get { return mYawScale; } }
        public Scalar YawThreshold { get { return mYawThreshold; } }

        public event Action<IInput, bool> EnabledChanged;

        public bool WalkEnabled {
            get { return mWalkEnabled; }
            set { 
                mWalkEnabled = value;
                if (EnabledChanged != null)
                    EnabledChanged(this, mEnabled);
            }
        }
        public bool StrafeEnabled {
            get { return false; }
            set { }
        }
        public bool FlyEnabled {
            get { return mFlyEnabled; }
            set { 
                mFlyEnabled = value;
                if (EnabledChanged != null)
                    EnabledChanged(this, mEnabled);
            }
        }
        public bool PitchEnabled {
            get { return false; }
            set { }
        }
        public bool YawEnabled {
            get { return mYawEnabled; }
            set {
                mYawEnabled = value;
                if (EnabledChanged != null)
                    EnabledChanged(this, mEnabled);
            }
        }

        public TimespanMovementInput () {
            mWalkEnabled = true;
            mFlyEnabled = true;
            mYawEnabled = true;
            //(val * scale) + min = out
            //Scalar walkThreshold = Nui.tracker("WalkStart", 40, .65f / 40f, 0f, 20);
            mWalkThreshold = Scalar.Create(.325f);
            mWalkScale = Scalar.Create(2f);

            //Scalar flyThreshold = .2f - Nui.tracker("FlyStart", 40, .2f / 40f, 0f, 20);
            //Scalar flyMax = Nui.tracker("FlyMax", 40, .2f / 40f, .2f, 20);
            mFlyThreshold = Scalar.Create(.05f);
            mFlyMax = Scalar.Create(.4f);
            mFlyScale = Scalar.Create(.5f);

            //Scalar yawD = .1f - Nui.tracker("YawStart", 40, .1f / 40f, 0f, 20);
            //Scalar yawSpeed = 10f - Nui.tracker("YawSpeed", 40, 15f / 40f, 0f, 20);
            mYawThreshold = Scalar.Create(0.05f);
            mYawScale = Scalar.Create(1f);

            Init();

            Nui.Tick += new ChangeDelegate(Nui_Tick);
        }

        bool sw = false;

        void Nui_Tick() {
            if (!Nui.HasSkeleton)
                return;

            mDelta = Vector3.Zero;
            mYawDelta = mYawEnabled ? -mYaw.Value : 0f;
            mPitchDelta = 0.0;

            mDelta.X = mWalkEnabled ? mWalkVal.Value : 0f;

            //Put a timer on flying so that fly input greater than x will be ignored until the user has stayed in the position for y ms.
            if (mFlyVal.Value != 0f && !mFlying) {
                mFlying = true;
                if (!mFlyAllowed && mFlyVal.Value < mFlyMin.Value) {
                    mFlyAllowed = true;
                    mDelta.Z = mFlyEnabled ? mFlyVal.Value : 0f;
                    Console.WriteLine("Below Threshold");
                } else 
                    mFlyStart = DateTime.Now;
            } else if (mFlyAllowed || DateTime.Now.Subtract(mFlyStart).TotalMilliseconds > mFlyTimer.Value) {
                mDelta.Z = mFlyEnabled ? mFlyVal.Value : 0f;
                //Console.WriteLine("Time trigger " + (sw ? "+" : "-"));
                sw = !sw;
            }else if (mFlyVal.Value == 0f) {
                mFlying = false;
                mFlyAllowed = true;
            }

            if (mEnabled && Change != null)
                Change(this);
        }
        private bool mFlyAllowed;

        private void Init() {
            //Get the primary vectors.
            Vector shoulderC = Nui.joint(Nui.Shoulder_Centre);
            Vector shoulderR = Nui.joint(Nui.Shoulder_Right);
            Vector shoulderL = Nui.joint(Nui.Shoulder_Left);
            Vector elbowR = Nui.joint(Nui.Elbow_Right);
            Vector elbowL = Nui.joint(Nui.Elbow_Left);
            Vector wristR = Nui.joint(Nui.Wrist_Right);
            Vector wristL = Nui.joint(Nui.Wrist_Left);
            Vector handR = Nui.joint(Nui.Hand_Right);
            Vector handL = Nui.joint(Nui.Hand_Left);
            Vector hipC = Nui.joint(Nui.Hip_Centre);
            Vector hipR = Nui.joint(Nui.Hip_Right);
            Vector hipL = Nui.joint(Nui.Hip_Left);
            Vector head = Nui.joint(Nui.Head);
            //Condition guard = closeGuard();

            Condition heightThresholdR = Nui.y(handR) > Nui.y(hipC);
            Condition heightThresholdL = Nui.y(handL) > Nui.y(hipC);
            Condition heightThreshold = C.Or(heightThresholdL, heightThresholdR);

            Scalar dist = Nui.magnitude(shoulderC - hipC);
            Condition distanceThresholdR = Nui.x(handR - hipR) > dist;
            Condition distanceThresholdL = Nui.x(hipL - handL) > dist;
            //Condition distanceThresholdR = Nui.magnitude(handR - hipR) > dist;
            //Condition distanceThresholdL = Nui.magnitude(hipL - handL) > dist;
            Condition distanceThreshold = C.Or(distanceThresholdL, distanceThresholdR);

            Condition activeConditionR = C.Or(heightThresholdR, distanceThresholdR);
            Condition activeConditionL = C.Or(heightThresholdL, distanceThresholdL);
            Condition mActiveCondition = C.Or(heightThreshold, distanceThreshold);

            //----------- Walk----------- 
            //Left and right
            mWalkDiffR = Nui.z(handR - hipC) - .15f;
            mWalkDiffL = Nui.z(handL - hipC) - .15f;
            Scalar backwardThresh = mWalkThreshold / 5f;
            //Active
            Condition walkActiveR = C.Or(C.And(Nui.abs(mWalkDiffR) > mWalkThreshold,  mWalkDiffR < 0f), (mWalkDiffR > backwardThresh));
            Condition walkActiveL = C.Or(C.And(Nui.abs(mWalkDiffL) > mWalkThreshold,  mWalkDiffL < 0f), (mWalkDiffL > backwardThresh));
            walkActiveR = C.And(activeConditionR, walkActiveR);
            walkActiveL = C.And(activeConditionL, walkActiveL);
            //Value
            Scalar moveValR = Nui.ifScalar(mWalkDiffR < 0f, mWalkDiffR + mWalkThreshold, mWalkDiffR - backwardThresh);
            Scalar moveValL = Nui.ifScalar(mWalkDiffL < 0f, mWalkDiffL + mWalkThreshold, mWalkDiffL - backwardThresh);
            mWalkValR = Nui.ifScalar(walkActiveR, moveValR, 0f);
            mWalkValL = Nui.ifScalar(walkActiveL, moveValL, 0f);
            mWalkVal = (mWalkValL + mWalkValR) * -1f * mWalkScale;
            //mWalkVal = Nui.ifScalar(mActiveCondition, mWalkVal, 0f);


            //----------- Fly----------- 
            mArmR = handR - shoulderR;
            mArmL = handL - shoulderL;
            Vector up = Vector.Create(0f, 1f, 0f);
            mFlyAngleR = Nui.dot(up, mArmR);
            mFlyAngleL = Nui.dot(up, mArmL);
            mConstrainedFlyAngleR = Nui.constrain(mFlyAngleR, mFlyThreshold, mFlyMax, 0f, true);
            mConstrainedFlyAngleL = Nui.constrain(mFlyAngleL, mFlyThreshold, mFlyMax, 0f, true);
            Scalar flyVal = (mConstrainedFlyAngleR + mConstrainedFlyAngleL) * mFlyScale;

            //Condition flyActiveR = C.And(mFlyAngleR != 0f,  Nui.magnitude(armR) - Nui.magnitude(Nui.limit(armR, true, true, false)) < .1f);
            //Condition flyActiveL = C.And(mFlyAngleL != 0f,  Nui.magnitude(armL) - Nui.magnitude(Nui.limit(armL, true, true, false)) < .1f);
            Scalar magShoulder = Nui.magnitude(shoulderL - shoulderR);
            Scalar magR = Nui.magnitude(mArmR);
            Scalar magL = Nui.magnitude(mArmL);
            Condition flyActiveR = C.And(magR > magShoulder, Nui.abs(Nui.x(mArmR)) > Nui.abs(Nui.z(mArmR)));
            Condition flyActiveL = C.And(magL > magShoulder, Nui.abs(Nui.x(mArmL)) > Nui.abs(Nui.z(mArmL)));
            flyActiveR = C.And(mConstrainedFlyAngleR != 0f, flyActiveR);
            flyActiveL = C.And(mConstrainedFlyAngleL != 0f, flyActiveL);
            flyActiveR = C.And(activeConditionR, flyActiveR);
            flyActiveL = C.And(activeConditionL, flyActiveL);
            Condition flyActive = C.Or(flyActiveR, flyActiveL);
            //Condition flyActive = C.And(C.Or(flyActiveR, flyActiveL),  C.And(Nui.z(armR) < 0f,  Nui.z(armR) < 0f));

            mFlyVal = Nui.ifScalar(flyActive, flyVal, 0f);
            mFlyVal = Nui.ifScalar(mActiveCondition, mFlyVal, 0f);
            mFlyTimer = Scalar.Create("Fly Timer", 0f);
            mFlyMin = Scalar.Create("Fly Minimum", .01f);

            //----------- Yaw----------- 
            Vector yawCore = Nui.limit(head - hipC, true, true, false);
            // Yaw is how far the user is leaning horizontally. This is calculated the angle between vertical and the vector between the hip centre and the head.
            mYawLean = Nui.dot(Nui.normalize(yawCore), Vector.Create(1f, 0f, 0f));
            // Constrain the value, deadzone is provided by a slider.
            mYaw = Nui.constrain(mYawLean, mYawThreshold, .4f, .3f, true) * mYawScale;

            mYawTwist = Nui.z(shoulderR) - Nui.z(shoulderL);
            mYawTwist = Nui.constrain(mYawTwist, .05f, Nui.magnitude(shoulderL - shoulderR), 5f, true);

            mYaw = mYawTwist + mYawLean;
        }

        #region IDeltaInput Members 

        public event Action<IDeltaInput> Change;

        public Vector3 PositionDelta {
            get { return mDelta; }
        }

        public Rotation OrientationDelta {
            get { return new Rotation(mPitchDelta, mYawDelta); }
        }

        public void Init(IInputSource input) { }

        public UserControl ControlPanel {
            get {
                if (mPanel == null)
                    mPanel = new TimespanMovementPanel(this);
                return mPanel;
            }
        }

        public bool Enabled {
            get { return mEnabled; }
            set { 
                mEnabled = value;
                if (EnabledChanged != null)
                    EnabledChanged(this, mEnabled);
            }
        }

        public string Name {
            get { return "Kinect Movement - Timespan Configuration"; }
        }

        public string State {
            get {
                string dump = "----Timespan Config Kinect Input----";
                return ""; 
            }
        }

        public ConfigBase Config {
            get { throw new NotImplementedException(); }
        }

        public void Close() { }

        public void Draw(Perspective perspective, System.Drawing.Graphics graphics) {
            throw new NotImplementedException();
        }

        #endregion

    }
}