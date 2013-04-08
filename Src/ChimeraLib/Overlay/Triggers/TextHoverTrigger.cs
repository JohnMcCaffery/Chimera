﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Overlay.Drawables;
using System.Drawing;
using Chimera.Interfaces.Overlay;

namespace Chimera.Overlay.Triggers {
    public class TextHoverTrigger : HoverTrigger, IDrawable {
        private Text mText;
        private bool mActive;

        private static RectangleF GetBounds(Text text) {
            using (Bitmap b = new Bitmap(1, 1)) {
                using (Graphics g = Graphics.FromImage(b)) {
                    SizeF size = g.MeasureString(text.TextString, text.Font);
                    return new RectangleF(text.Position, size);
                }
            }
        }

        public TextHoverTrigger(WindowOverlayManager manager, IHoverSelectorRenderer renderer, Text text)
            : base(manager, renderer, GetBounds(text)) {
                mText = text;
        }

        protected override RectangleF Bounds {
            get { return GetBounds(mText); }
            set { }
        }

        #region IDrawable Members

        public override bool Active {
            get { return mActive; }
            set { 
                mActive = value;
                mText.Active = value;
            }
        }

        public override bool NeedsRedrawn {
            get { return mText.NeedsRedrawn || base.NeedsRedrawn; }
        }

        string IDrawable.Window {
            get { return mText.Window; }
        }

        void IDrawable.RedrawStatic(Rectangle clip, Graphics graphics) {
            mText.RedrawStatic(clip, graphics);
            base.RedrawStatic(clip, graphics);
        }

        void IDrawable.DrawDynamic(Graphics graphics) {
            mText.DrawDynamic(graphics);
            base.DrawDynamic(graphics);
        }

        #endregion
    }
}
