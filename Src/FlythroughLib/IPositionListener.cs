﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.FlythroughLib;
using OpenMetaverse;

namespace FlythroughLib {
    public interface IPositionListener {
        /// <summary>
        /// Create a link to the sequence of positions this listener wishes to track.
        /// </summary>
        /// <param name="positions">The positions this listener will query.</param>
        void Init(EventSequence<Vector3> positions);
    }
}
