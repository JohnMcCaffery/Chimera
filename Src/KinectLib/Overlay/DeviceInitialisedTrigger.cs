/*************************************************************************
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
using Chimera.Interfaces.Overlay;
using NuiLibDotNet;
using Chimera.Overlay;
using System.Xml;
using System.Drawing;
using Chimera.Overlay.Triggers;

namespace Chimera.Kinect.Overlay {
    public class DeviceInitialisedFactory : OverlayXmlLoader, ITriggerFactory {
        public SpecialTrigger Special {
            get { return SpecialTrigger.None; }
        }

        public string Mode {
            get { return OverlayPlugin.HOVER_MODE; }
        }

        public override string Name {
            get { return "DeviceInitialised"; }
        }

        public ITrigger Create(OverlayPlugin manager, XmlNode node) {
            return new DeviceInitialisedTrigger();
        }

        public ITrigger Create(OverlayPlugin manager, XmlNode node, Rectangle clip) {
            return Create(manager, node);
        }
    }
    public class DeviceInitialisedTrigger : TriggerBase, ITrigger {
        private bool mActive;

        public override bool Active {
            get { return mActive; }
            set {
                if (mActive != value) {
                    mActive = value;
                    if (value && Nui.Initialised)
                        Nui_DeviceInitialised();
                }
            }
        }

        public DeviceInitialisedTrigger() {
            GlobalConditions.Initialised += new Action(Nui_DeviceInitialised);
        }

        void Nui_DeviceInitialised() {
            Trigger();
        }
    }
}
