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
using Chimera.Interfaces.Overlay;

namespace Chimera.Overlay.Triggers {
    public class CustomTriggerTrigger : ITrigger {
        private bool mActive;
        private string mKey;

        public event Action Triggered;

        public bool Active {
            get { return mActive; }
            set { mActive = value; }
        }

        public CustomTriggerTrigger(StateManager stateManager, string key) {
            mKey = key;
            stateManager.CustomTrigger += new Action<string>(stateManager_CustomTrigger);
        }

        private void stateManager_CustomTrigger(string key) {
            if (mActive && Triggered != null && key.Equals(this.mKey))
                Triggered();
        }
    }
}