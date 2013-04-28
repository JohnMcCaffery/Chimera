﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuiLibDotNet;
using C = NuiLibDotNet.Condition;

namespace Chimera.Kinect {
    public static class GlobalConditions {
        private static bool mInit;
        private static Condition sActiveConditionR;
        private static Condition sActiveConditionL;


        private static void Init() {
            mInit = true;
            Vector hipC = Nui.joint(Nui.Hip_Centre);
            Vector handR = Nui.joint(Nui.Hip_Centre);
            Vector handL = Nui.joint(Nui.Hip_Centre);
            Condition heightThresholdR = Nui.y(Nui.joint(Nui.Hand_Right)) > Nui.y(hipC);
            Condition heightThresholdL = Nui.y(Nui.joint(Nui.Hand_Right)) > Nui.y(hipC);
            Condition heightThreshold = C.Or(heightThresholdL, heightThresholdR);

            Scalar dist = Nui.magnitude(Nui.joint(Nui.Shoulder_Centre) - hipC);
            Condition distanceThresholdR = Nui.x(handR - Nui.joint(Nui.Hip_Right)) > dist;
            Condition distanceThresholdL = Nui.x(Nui.joint(Nui.Hip_Left) - handL) > dist;
            //Condition distanceThresholdR = Nui.magnitude(handR - hipR) > dist;
            //Condition distanceThresholdL = Nui.magnitude(hipL - handL) > dist;
            Condition distanceThreshold = C.Or(distanceThresholdL, distanceThresholdR);

            sActiveConditionR = C.Or(heightThresholdR, distanceThresholdR);
            sActiveConditionL = C.Or(heightThresholdL, distanceThresholdL);
        }

        public static Condition ActiveR {
            get {
                if (!mInit)
                    Init();
                return sActiveConditionR;
            }
        }
        public static Condition ActiveL {
            get {
                if (!mInit)
                    Init();
                return sActiveConditionL;
            }
        }
    }
}
