﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Inputs;
using Chimera;
using Chimera.Interfaces;

namespace Joystick {
    public class XBoxControllerDelta : AxisBasedDelta {
        public XBoxControllerDelta()
            : base(
                "XBox Controller",
                new ThumbstickAxis(false, false),
                new ThumbstickAxis(false, true),
                new TriggerAxis(false),
                new ThumbstickAxis(true, true),
                new ThumbstickAxis(true, false)
            ) { }
    }

    public class XBoxControllerInput : DeltaBasedInput {
        public XBoxControllerInput()
            : base(new XBoxControllerDelta()) { }
    }
}
