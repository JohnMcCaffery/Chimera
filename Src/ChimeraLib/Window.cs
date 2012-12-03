﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using UtilLib;

namespace ChimeraLib {
    public class Window {
        private Rotation rotation = new Rotation(0f, 0f);
        private Vector3 position = Vector3.UnitZ * 400;
        private Vector3 positionOffset = Vector3.Zero;
        private double aspectRatio = 9f / 16f;
        private double mmDiagonal = 19.0 * 25.4;
        
        public static readonly double TOLERANCE = 0.0001;
        //private float height = 720f;

        private void Changed() {
            if (OnChange != null)
                OnChange(this, null);
        }

        private void RotationChanged(object source, EventArgs args) {
            Changed();
        }

        /// <summary>
        /// Triggered whenever any of the properties of the window changes.
        /// </summary>
        public event EventHandler OnChange;


        /// <summary>
        /// The position of the centre screen in real space (mm).
        /// </summary>
        public Vector3 ScreenPosition {
            get { return position; }
            set {
                if (value == position)
                    return;
                position = value;
                Changed();
            }
        }

        /// <summary>
        /// The offset of the origin/eye position for this screen from the centre of the real space (mm).
        /// </summary>
        public Vector3 EyeOffset {
            get { return positionOffset; }
            set {
                if (value == positionOffset)
                    return;
                positionOffset = value;
                Changed();
            }
        }

        /// <summary>
        /// The rotation of the screen from forward in real space.
        /// </summary>
        public Rotation RotationOffset {
            get { return rotation; }
            set {
                if (rotation != null)
                    rotation.OnChange -= RotationChanged;
                rotation = value;
                rotation.OnChange += RotationChanged;
                if (OnChange != null)
                    OnChange(this, null);
            }
        }

        /// <summary>
        /// How wide the screen is in real space (mm). 
        /// Changing this will also change the aspect ratio and the diagonal.
        /// </summary>
        public double Width {
            get { return (Math.Cos(Math.Atan(aspectRatio)) * mmDiagonal); }
            set {
                if (Math.Abs(Width - value) < TOLERANCE || value <= 0.0)
                    return;
                aspectRatio = Height / value;
                mmDiagonal = value / Math.Cos(Math.Atan(aspectRatio));
                Changed();
            }
        }

        /// <summary>
        /// The height of the screen in real space (mm).
        /// Changing this will also change the aspect ration and the diagonal.
        /// </summary>
        public double Height {
            get { return (Math.Sin(Math.Atan(aspectRatio)) * mmDiagonal); }
            set {
                if (Math.Abs(Width - value) < TOLERANCE || value <= 0.0)
                    return;
                aspectRatio = value / Width;
                mmDiagonal = value / Math.Sin(Math.Atan(aspectRatio));
                Changed();
            }
        }

        /// <summary>
        /// The diagonal size of the screen. Specified in inches.
        /// This is included for convenience. Most screens are rated in diagonal inches.
        /// Changing this will change the width and height according to the aspect ratio.
        /// </summary>
        public double Diagonal {
            get { return mmDiagonal; }
            set {
                if (mmDiagonal == value || value <= 0.0)
                    return;
                mmDiagonal = value;
                Changed();
            }
        }

        /// <summary>
        /// The aspect ratio between the height and width of the screen. (h/w).
        /// Changing this will change the width of the screen.
        /// Calculated as height / width.
        /// </summary>
        public double AspectRatio {
            get { return aspectRatio; }
            set {
                if (aspectRatio == value || value <= 0.0)
                    return;
                aspectRatio = value;
                Changed();
            }
        }

        /// <summary>
        /// The field of view the screen shows, in radians. 
        /// Changing this will change the height and width of the screen according to the aspect ratio.
        /// Calculated as the tangent of <code>height / (2 * d)</code> where d is distance to the screen.
        /// </summary>
        public double FieldOfView {
            get {
                if (position.Z == 0)
                    return Math.PI;
                return Math.Atan2(Height, position.Z); 
            }
            set {
                double fov = FieldOfView;
                if (Math.Abs(fov) < TOLERANCE || value <= 0.0)
                    return;
                double ratio = Height / Math.Sin(fov);
                double height = Math.Sin(value) * ratio;
                mmDiagonal =  Math.Sqrt(Math.Pow(height, 2) + Math.Pow(height * aspectRatio, 2));
                Changed();
            }
        }
    }
}
