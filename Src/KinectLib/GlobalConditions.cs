﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuiLibDotNet;
using C = NuiLibDotNet.Condition;
using Chimera.Kinect.Axes;
using Chimera.Kinect.GUI;
using log4net;
using System.Threading;

namespace Chimera.Kinect {
    public static class GlobalConditions {
        private static ILog Logger = LogManager.GetLogger("Kinect");
        private static bool mVectorsInitialised;
        private static bool mInitialising;
        private static Condition sActiveConditionR;
        private static Condition sActiveConditionL;

        private static KinectAxisConfig mConfig = new KinectAxisConfig();

        public static event Action Initialised;

        public static KinectAxisConfig Cfg {
            get { return mConfig; }
        }

        public static bool VectorsInitialised {
            get { return mVectorsInitialised; }
        }

        private static bool InitSensor() {
            if (mInitialising || Nui.Initialised)
                return Nui.Initialised;

            mInitialising = true;
            int attempt = 1;
            int wait = mConfig.InitialRetryWait;
            bool initialised = Nui.Init();
            while (!initialised && attempt <= mConfig.RetryAttempts) {
                Logger.Warn(String.Format("NuiLib unable to initialise Kinect after attempt {0}. Reason: {2} Waiting {1}s and retrying.", attempt, (wait / 1000), Nui.State));

                Thread.Sleep(wait);

                attempt++;
                float newWait = wait * mConfig.RetryWaitMultiplier;
                wait = (int) newWait;
                initialised = Nui.Init();
            }

            Logger.Info(initialised ? "Kinect successfully initialised." : "Unable to successfuly initialise NUI after " + (attempt - 1) + " attempts.");
            if (initialised && Initialised != null)
                Initialised();

            mInitialising = false;
            return initialised;
        }
        
        public static bool Init() {
            if (mVectorsInitialised)
                return true;
            
            Nui.DeviceConnected += () => {
                Logger.Info("Kinect Connected.");
                InitSensor();
            }; 
            Nui.DeviceDisconnected += () => {
                Logger.Info("Kinect Disconnected.");
            };

            //if (!InitSensor())
                //return false;
            InitSensor();

            Nui.SetAutoPoll(true);
            mVectorsInitialised = true;
            Vector hipR = Nui.joint(Nui.Hip_Right);
            Vector handR = Nui.joint(Nui.Hand_Right);
            Vector handL = Nui.joint(Nui.Hand_Left);
            Condition heightThresholdR = Nui.y(handR) > Nui.y(hipR);
            Condition heightThresholdL = Nui.y(handL) > Nui.y(hipR);
            Condition heightThreshold = C.Or(heightThresholdL, heightThresholdR);

            Scalar dist = Nui.magnitude(Nui.joint(Nui.Shoulder_Centre) - Nui.joint(Nui.Hip_Centre));
            Condition distanceThresholdR = Nui.x(handR - Nui.joint(Nui.Hip_Right)) > dist;
            Condition distanceThresholdL = Nui.x(Nui.joint(Nui.Hip_Left) - handL) > dist;
            //Condition distanceThresholdR = Nui.magnitude(handR - hipR) > dist;
            //Condition distanceThresholdL = Nui.magnitude(hipL - handL) > dist;
            Condition distanceThreshold = C.Or(distanceThresholdL, distanceThresholdR);

            sActiveConditionR = C.Or(heightThresholdR, distanceThresholdR);
            sActiveConditionL = C.Or(heightThresholdL, distanceThresholdL);

            return true;
        }

        public static Condition ActiveR {
            get {
                if (!mVectorsInitialised)
                    Init();
                return sActiveConditionR;
            }
        }
        public static Condition ActiveL {
            get {
                if (!mVectorsInitialised)
                    Init();
                return sActiveConditionL;
            }
        }
    }
}
