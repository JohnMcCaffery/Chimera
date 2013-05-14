using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Overlay;
using Chimera.Flythrough.Overlay;
using Chimera.Overlay.States;

namespace Chimera.Launcher {
    public class FlythroughLauncher : Launcher {
        protected override void InitOverlay() {
            State idleFlythrough = new FlythroughState("Flythrough", Coordinator.StateManager, Config.Flythrough);
            State explore = new ExploreState("Explore", Coordinator.StateManager);

            Coordinator.StateManager.AddState(explore);
            Coordinator.StateManager.AddState(idleFlythrough);
        }
    }
}
