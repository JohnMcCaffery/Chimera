﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Chimera.Overlay {
    public class InvisibleSelection : SelectableArea {
        public override event Action<ISelectable> StaticChanged;

        public InvisibleSelection(ISelectionRenderer renderer, float x, float y, float w, float h)
            : base(renderer, x, y, w, h) {
        }

        public InvisibleSelection(float x, float y, float w, float h)
            : base(x, y, w, h) {
        }

        public override void DrawStatic(Graphics graphics, Rectangle clipRectangle) { }

        public override void Show() {
            Active = false;
        }

        public override void Hide() {
            Active = false;
        }
    }
}