using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Overlay;
using Chimera.Flythrough.Overlay;
<<<<<<< HEAD
using Chimera.Overlay.States;
=======
>>>>>>> e08ecbd75910ef1c1f1c2686cee832da3c752bea

namespace Chimera.Launcher {
    public class FlythroughLauncher : Launcher {
        protected override void InitOverlay() {
<<<<<<< HEAD
            State idleFlythrough = new FlythroughState("Flythrough", Coordinator.StateManager, Config.Flythrough);
            State explore = new ExploreState("Explore", Coordinator.StateManager);

            Coordinator.StateManager.AddState(explore);
=======
            State idleFlythrough = new FlythroughState("Flythrough", Coordinator.StateManager, "../Flythroughs/Cathedral5.xml");

>>>>>>> e08ecbd75910ef1c1f1c2686cee832da3c752bea
            Coordinator.StateManager.AddState(idleFlythrough);
        }
    }
}
