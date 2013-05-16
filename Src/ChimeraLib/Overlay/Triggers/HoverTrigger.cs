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
using System.Drawing;
using System.Windows.Forms;
using Chimera.Interfaces.Overlay;
using System.Xml;

namespace Chimera.Overlay.Triggers {
    public abstract class HoverTrigger : ITrigger, IDrawable {
        /// <summary>
        /// How many ms to the hover must be maintened before the selector is triggered.
        /// </summary>
        private readonly float mSelectTimeMS = 1500f;
        /// <summary>
        /// The manager which will supply the cursor position.
        /// </summary>
        private readonly WindowOverlayManager mManager;

        /// <summary>
        /// The render object used to draw a visual representation of how close the selector is to triggering.
        /// </summary>
        private IHoverSelectorRenderer mRenderer;
        /// <summary>
        /// The bounds defining the area which the cursor can hover over to trigger this selector. The bounds are specified as scaled values between 0,0 and 1,1. 0,0 is top left. 1,1 bottom right.
        /// </summary>
        private RectangleF mBounds;
        /// <summary>
        /// The time when the cursor started hovering over the area.
        /// </summary>
        private DateTime mHoverStart;
        /// <summary>
        /// The clip rectangle for the area the trigger will be drawn onto.
        /// </summary>
        private Rectangle mClip;
        /// <summary>
        /// Whether the cursor is currently hovering over the area.
        /// </summary>
        private bool mHovering;
        /// <summary>
        /// Whether the selector has been triggered.
        /// </summary>
        private bool mTriggered;
        /// <summary>
        /// Whether the area is active. If false it will not draw or trigger.
        /// </summary>
        private bool mActive = true;
        /// <summary>
        /// Controls whether there is any change to the hover which needs to be redrawn on the output.
        /// </summary>
        private bool mNeedsRedrawn;

        /// <summary>
        /// Create the trigger. Specifies the position and size of the area the cursor must hover in to trigger this trigger as values between 0 and 1.
        /// 0,0 is top left, 1,1 is bottom right.
        /// </summary>
        /// <param name="manager">The manager which manages the window this trigger is to draw to.</param>
        /// <param name="render">The renderer used to draw this trigger being selected.</param>
        /// <param name="x">The x coordinate for where the image is to be positioned, specified between 0 and 1. 0 is flush to the left, 1 flush to the right.</param>
        /// <param name="y">The y coordinate for where the image is to be positioned, specified between 0 and 1. 0 is flush to the top, 1 flush to the bottom.</param>
        /// <param name="x">The width of the image, specified between 0 and 1. 1 will fill the entire width, 0 will be invisible.</param>
        /// <param name="y">The width of the image, specified between 0 and 1. 1 will fill the entire height, 0 will be invisible.</param>
        public HoverTrigger(WindowOverlayManager manager, IHoverSelectorRenderer renderer, float x, float y, float w, float h)
            : this(manager, renderer, new RectangleF(x, y, w, h)) {
        }

        public HoverTrigger(WindowOverlayManager manager, IHoverSelectorRenderer renderer, RectangleF bounds) {
            mManager = manager;
            mBounds = bounds;
            mRenderer = renderer;

            mManager.Window.Coordinator.Tick += new Action(Coordinator_Tick);
        }

        public HoverTrigger(XmlNode node) {
            //TODO add logic for initialisation
        }

        public string Window {
            get { return mManager.Window.Name; }
        }

        /// <summary>
        /// Whether the cursor is currently hovering within the area.
        /// </summary>
        public virtual bool CurrentlyHovering {
            get { return mHovering; }
        }

        /// <summary>
        /// Whether the selector has been selected.
        /// </summary>
        public virtual bool CurrentlySelected {
            get { return mTriggered; }
        }

        /// <summary>
        /// The bounds defining the area which the cursor can hover over to trigger this selector. The bounds are specified as scaled values between 0,0 and 1,1. 0,0 is top left. 1,1 bottom right.
        /// </summary>
        protected virtual RectangleF Bounds {
            get { return mBounds; }
            set { mBounds = value; }
        }

        /// <summary>
        /// The manager which controls the window this trigger renders on.
        /// </summary>
        protected WindowOverlayManager Manager {
            get { return mManager; }
        }

        /// <summary>
        /// Bounds in relationship to the full clip.
        /// </summary>
        protected virtual Rectangle ScaledBounds {
            get {
                return new Rectangle(
                    (int)(mClip.Width * Bounds.X),
                    (int)(mClip.Height * Bounds.Y),
                    (int)(mClip.Width * Bounds.Width),
                    (int)(mClip.Height * Bounds.Height));
                }
        }

        /// <summary>
        /// Clip boundary of the area on which the selector is being drawn.
        /// </summary>
        public virtual Rectangle Clip {
            get { return mClip; }
            set { mClip = value; }
        }

        private void Coordinator_Tick() {
            if (mActive && Bounds.Contains(mManager.CursorPosition)) {
                if (!mHovering) {
                    mHovering = true;
                    mHoverStart = DateTime.Now;
                }

                if (!mTriggered && DateTime.Now.Subtract(mHoverStart).TotalMilliseconds > mSelectTimeMS) {
                    if (Triggered != null)
                        Triggered();
                    mTriggered = true;
                    mHovering = false;
                    mRenderer.Clear();
                }

                mNeedsRedrawn = true;
            } else if (mHovering || mTriggered) {
                mTriggered = false;
                mHovering = false;
                mNeedsRedrawn = true;
                mManager.ForceRedrawStatic();
                mRenderer.Clear();
            } else
                mNeedsRedrawn = false;
        }

        #region ITrigger Members

        public event Action Triggered;

        public virtual bool Active {
            get { return mActive; }
            set { mActive = value; }
        }

        #endregion

        #region IDrawable

        public virtual bool NeedsRedrawn {
            get { return mNeedsRedrawn; }
        }

        /// <summary>
        /// Draw the elements of the drawable that change more frequently than when the drawing area is resized.
        /// </summary>
        /// <param name="graphics">The object with which to draw the elements.</param>
        public virtual void DrawDynamic(Graphics graphics) {
            if (mTriggered)
                mRenderer.DrawSelected(graphics, ScaledBounds);
            else if (mHovering) {
                mRenderer.DrawHover(graphics, ScaledBounds, DateTime.Now.Subtract(mHoverStart).TotalMilliseconds / mSelectTimeMS);
            }
        }

        /// <summary>
        /// Notify the drawable that the area on which it is to draw has changed. Should draw any elements which only change when the area is resized to the supplied graphics object.
        /// </summary>
        /// <param name="clip">The area in which this drawable will be drawn.</param>
        /// <param name="graphics">The object with which to to draw any elements which only change when the area is resized.</param>
        public virtual void DrawStatic(Graphics graphics) { }

        #endregion    
    }
}
