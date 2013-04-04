﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Chimera.Util;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using Chimera.Interfaces;
using System.IO;
using System.Threading;
using Chimera.Overlay;

namespace Chimera {
    public class DeltaUpdateEventArgs : EventArgs {
        /// <summary>
        /// The delta that results in the new position.
        /// </summary>
        public Vector3 positionDelta;
        /// <summary>
        /// The delta that results in the new orientation.
        /// </summary>
        public Rotation rotationDelta;

        /// <param name="positionDelta">The delta that results in the new position.</param>
        /// <param name="rotationDelta">The delta that results in the orientation.</param>
        public DeltaUpdateEventArgs(Vector3 positionDelta, Rotation rotationDelta) {
            this.positionDelta = positionDelta;
            this.rotationDelta = rotationDelta;
        }
    }
    public class CameraUpdateEventArgs : DeltaUpdateEventArgs {
        /// <summary>
        /// The new position for the camera.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The new orientation of the camera.
        /// </summary>
        public Rotation rotation;

        /// <param name="position">The new position for the camera.</param>
        /// <param name="positionDelta">The delta that results in the new position.</param>
        /// <param name="rotation">The new orientation for the camera.</param>
        /// <param name="rotationDelta">The delta that results in the orientation.</param>
        public CameraUpdateEventArgs(Vector3 position, Vector3 positionDelta, Rotation rotation, Rotation rotationDelta) :
            base(positionDelta, rotationDelta) {
            this.position = position;
            this.rotation = rotation;
        }
    }

    /// <summary>
    /// How the camera for the system as a whole is controlled.
    /// </summary>
    public enum ControlMode {
        /// <summary>
        /// Control the system by specifying the precise location and orientation of the camera.
        /// </summary>
        Absolute,
        /// <summary>
        /// Control the system by sending delta events to be applied to camera/avatar position.
        /// </summary>
        Delta
    }

    public class HeightmapChangedEventArgs : EventArgs {
        public float[,] Heights;
        public int StartX;
        public int StartY;
    }

    public class Coordinator : ICrashable, IInputSource {
        /// <summary>
        /// Statistics object which monitors how long ticks are taking.
        /// </summary>
        private readonly TickStatistics mStats = new TickStatistics();
        /// <summary>
        /// The authoratitive orientation of the camera in virtual space. This is read-only. To update it use the 'Update' method.
        /// </summary>
        private readonly Rotation mOrientation;
        /// <summary>
        /// Current orientation delta.
        /// </summary>
        private readonly Rotation mOrientationDelta;
        /// <summary>
        /// The authoratitive position of the camera in virtual space.
        /// </summary>
        private Vector3 mPosition;
        /// <summary>
        /// Current position delta.
        /// </summary>
        private Vector3 mPositionDelta;
        /// <summary>
        /// The position of the eye in the real world, in mm.
        /// </summary>
        private Vector3 mEyePosition;
        /// <summary>
        /// Whether to accept updates to the camera position.
        /// </summary>
        private bool mEnableUpdates = true;
        /// <summary>
        /// Whether the tick thread should continue running.
        /// </summary>
        private bool mAlive;
        /// <summary>
        /// The heightmap for the region. Camera positions below this will be ignored.
        /// </summary>
        private readonly float[,] mHeightmap;
        /// <summary>
        /// The default height below which the camera should go. Used if heightmap data is not available.
        /// </summary>
        private readonly float mDefaultHeight;
        /// <summary>
        /// Lock object to ensure only this object can update the rotation.
        /// </summary>
        private readonly object mRotationLock = new object();
        /// <summary>
        /// The inputs which can control the virtual position and lookAt of the view and the real world eye position to calculate views transition.
        /// </summary>
        private readonly List<ISystemInput> mInputs = new List<ISystemInput>();
        /// <summary>
        /// The windows which define where, in real space, each 'input' onto the virtual space is located.
        /// </summary>
        private readonly List<Window> mWindows = new List<Window>();
        /// <summary>
        /// The state manager which will control what state the overlay is in.
        /// </summary>
        private readonly StateManager mStateManager;

        /// <summary>
        /// File to log information about any crash to.
        /// </summary>
        private string mCrashLogFile;
        /// <summary>
        /// The length of each tick for any input that has an event loop.
        /// </summary>
        private int mTickLength;
        /// <summary>
        /// How the camera is controlled.
        /// </summary>
        private ControlMode mControlMode = ControlMode.Absolute;
        /// <summary>
        /// The object containing the configuration for the system.
        /// </summary>
        private CoordinatorConfig mConfig;

        /// <summary>
        /// Triggered when the camera control mode changes.
        /// </summary>
        public event Action<Coordinator, ControlMode> CameraModeChanged;

        /// <summary>
        /// Triggered whenever a window is added.
        /// </summary>
        public event Action<Window, EventArgs> WindowAdded;

        /// <summary>
        /// Triggered whenever a window is removed.
        /// </summary>
        public event Action<Window, EventArgs> WindowRemoved;

        /// <summary>
        /// Selected whenever the delta controlling where the camera is is updated.
        /// </summary>
        public event Action<Coordinator, DeltaUpdateEventArgs> DeltaUpdated;
        /// <summary>
        /// Selected whenever the virtual camera position/orientation is changed.
        /// </summary>
        public event Action<Coordinator, CameraUpdateEventArgs> CameraUpdated;

        /// <summary>
        /// Selected whenever the location of the eye in real space is updated.
        /// </summary>
        public event Action<Coordinator, EventArgs> EyeUpdated;

        /// <summary>
        /// Triggered whenever a key is pressed or released on the keyboard.
        /// </summary>
        public event Action<Coordinator, KeyEventArgs> KeyDown;

        /// <summary>
        /// Triggered whenever a key is pressed or released on the keyboard.
        /// </summary>
        public event Action<Coordinator, KeyEventArgs> KeyUp;

        /// <summary>
        /// Selected whenever a key is pressed or released on the keyboard.
        /// </summary>
        public event Action<Coordinator, KeyEventArgs> Closed;

        /// <summary>
        /// Triggered whenever the heightmap changes.
        /// </summary>
        public event EventHandler<HeightmapChangedEventArgs> HeightmapChanged;

        /// <summary>
        /// Triggered every tick. Listen for this to keep time across the system.
        /// </summary>
        public event Action Tick;
        /// <summary>
        /// Initialise this input, specifying a collection of inputs to work with.
        /// </summary>
        /// <param name="inputs">The inputs which control the camera through this input.</param>
        public Coordinator(params ISystemInput[] inputs) {
            mConfig = new CoordinatorConfig();
            mStateManager = new StateManager(this);

            mInputs = new List<ISystemInput>(inputs);
            mOrientation = new Rotation(mRotationLock, mConfig.Pitch, mConfig.Yaw);
            mOrientationDelta = new Rotation(mRotationLock);
            mPosition = mConfig.Position;
            mEyePosition = mConfig.EyePosition;
            mCrashLogFile = mConfig.CrashLogFile;
            mTickLength = mConfig.TickLength;
            mDefaultHeight = mConfig.HeightmapDefault;
            mHeightmap = new float[mConfig.XRegions * 256, mConfig.YRegions * 256];
            
            for (int i = 0; i < mHeightmap.GetLength(0); i++) {
                for (int j = 0; j < mHeightmap.GetLength(1); j++) {
                    mHeightmap[i, j] = mDefaultHeight;
                }
            }

            foreach (var input in mInputs)
                input.Init(this);

            Thread tickThread = new Thread(() => {
                mAlive = true;
                while (mAlive) {
                    DateTime tickStart = DateTime.Now;
                    mStats.Begin();
                    if (Tick != null)
                        Tick();
                    mStats.Tick();
                    Thread.Sleep(Math.Max(0, (int) (mTickLength - DateTime.Now.Subtract(tickStart).TotalMilliseconds)));
                }
            });
            tickThread.Name = "Tick Thread";
            tickThread.Start();
        }

        /// <summary>
        /// Initialise this input, specifying a collection of inputs to work with and a collection of windows coordinated by this input.
        /// </summary>
        /// <param name="windows">The windows which are coordinated by this input.</param>
        /// <param name="inputs">The inputs which control the camera through this input.</param>
        public Coordinator(IEnumerable<Window> windows, params ISystemInput[] inputs)
            : this(inputs) {

            foreach (var window in windows)
                AddWindow(window);
        }

        /// <summary>
        /// Look up a window by it's name.
        /// </summary>
        /// <param name="windowName">The name of the window to look up.</param>
        /// <returns>The window called 'windowName'</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no window with the given name.</exception>
        public Window this[string windowName] {
            get { return mWindows.First(w => w.Name.Equals(windowName)); }
        }

        /// <summary>
        /// The heightmap the coordinator is working with. Any attempted input value below the heightmap value will be set to the heightmap value.
        /// Stops the camera going through the floor.
        /// </summary>
        public float[,] Heightmap {
            get { return mHeightmap; }
        }

        /// <summary>
        /// Whether to accept updates to the camera position.
        /// </summary>
        public bool EnableUpdates {
            get { return mEnableUpdates; }
            set { mEnableUpdates = value; }
        }

        /// <summary>
        /// The windows which define where, in real space, each 'input' onto the virtual space is located.
        /// </summary>
        public Window[] Windows {
            get { return mWindows.ToArray() ; }
        }

        /// <summary>
        /// The inputs which can control the virtual position and lookAt of the view and the real world eye position to calculate views transition.
        /// </summary>
        public ISystemInput[] Inputs {
            get { return mInputs.ToArray() ; }
        }

        /// <summary>
        /// The authoratitive position of the camera in virtual space.
        /// </summary>
        public Vector3 Position {
            get { return mPosition ; }
        }

        /// <summary>
        /// The current delta for the camera in virtual space.
        /// </summary>
        public Vector3 PositionDelta {
            get { return mPositionDelta ; }
        }

        /// <summary>
        /// The authoratitive orientation of the camera position in virtual space. This is read-only. To update it use the 'Update' method.
        /// </summary>
        public Rotation Orientation {
            get { return mOrientation ; }
        } 

        /// <summary>
        /// The current delta for the camera orientation in virtual space.
        /// </summary>
        public Rotation OrientationDelta {
            get { return mOrientationDelta ; }
        }

        /// <summary>
        /// The position of the eye in the real world, in mm.
        /// </summary>
        public Vector3 EyePosition {
            get { return mEyePosition ; }
            set { 
                mEyePosition  = value;
                if (EyeUpdated != null)
                    EyeUpdated(this, null);
            }
        }

        /// <summary>
        /// How long every tick should be, in ms, for any input that has a refresh loop.
        /// </summary>
        public int TickLength {
            get { return mTickLength; }
        }

        /// <summary>
        /// How the camera is controlled.
        /// </summary>
        public ControlMode ControlMode {
            get { return mControlMode; }
            set {
                if (value != mControlMode) {
                    mControlMode = value;
                    //TODO - add code here so that when the value is changed the camera updates
                    if (CameraModeChanged != null)
                        CameraModeChanged(this, value);
                }
            }
        }

        /// <summary>
        /// The manager which controls the state of the overlay.
        /// </summary>
        public StateManager StateManager {
            get { return mStateManager; }
        }

        /// <summary>
        /// Object which will supply information about how long ticks are taking.
        /// </summary>
        public TickStatistics Statistics {
            get { return mStats; }
        }

        /// <summary>
        /// Update a section of the heightmap.
        /// </summary>
        public void SetHeightmapSection(float[,] section, int startX, int startY, bool regionCompleted) {
            for (int i = 0; i < section.GetLength(0); i++) {
                for (int j = 0; j < section.GetLength(1); j++)
                    mHeightmap[startX + i, startY + j] = section[i, j];
            }
            //if (regionCompleted && HeightmapChanged != null) {
            if (HeightmapChanged != null) {
                HeightmapChangedEventArgs args = new HeightmapChangedEventArgs();
                args.Heights = section;
                args.StartX = startX;
                args.StartY = startY;
                HeightmapChanged(this, args);
            }
        }

        /// <summary>
        /// Update the position of the camera.
        /// </summary>
        /// <param name="position">The new position for the camera.</param>
        /// <param name="postionDelta">The delta to be applied to the position for interpolation.</param>
        /// <param name="orientation">The look at vector for the camera orientation.</param>
        /// <param name="orientationDelta">The delta to be applied to the look at vector for interpolation.</param>
        public void Update(Vector3 position, Vector3 postionDelta, Rotation orientation, Rotation orientationDelta) {
            Update(position, postionDelta, orientation, orientationDelta, mControlMode);
        }
        /// <summary>
        /// Update the position of the camera.
        /// </summary>
        /// <param name="position">The new position for the camera.</param>
        /// <param name="postionDelta">The delta to be applied to the position for interpolation.</param>
        /// <param name="orientation">The look at vector for the camera orientation.</param>
        /// <param name="orientationDelta">The delta to be applied to the look at vector for interpolation.</param>
        /// <param name="mode">How the camera is to be updated. Can override the current global ControlMode setting.</param>
        public void Update(Vector3 position, Vector3 postionDelta, Rotation orientation, Rotation orientationDelta, ControlMode mode) {
            if (mEnableUpdates) {
                mPositionDelta = postionDelta;
                mOrientationDelta.Update(mRotationLock, orientationDelta);
                if (mode == Chimera.ControlMode.Absolute) {
                    int x = (int)position.X;
                    int y = (int)position.Y;
                    float height =
                        x >= 0 && x < mHeightmap.GetLength(0) &&
                        y >= 0 && y < mHeightmap.GetLength(1) ?
                            mHeightmap[x, y] :
                            mDefaultHeight;
                    height += .5f;
                    if (position.Z < height) {
                        position.Z = height;
                        postionDelta.Z = 0f;
                    }
                    mPosition = position;
                    mOrientation.Update(mRotationLock, orientation);
                    if (CameraUpdated != null && mAlive) {
                        CameraUpdateEventArgs args = new CameraUpdateEventArgs(position, postionDelta, orientation, orientationDelta);
                        CameraUpdated(this, args);
                    }
                } else {
                    if (DeltaUpdated != null && mAlive) {
                        DeltaUpdateEventArgs args = new DeltaUpdateEventArgs(postionDelta, orientationDelta);
                        DeltaUpdated(this, args);
                    }
                }
            }
        }

        /// <summary>
        /// Register a key press or release.
        /// </summary>
        public void TriggerKeyboard(bool down, KeyEventArgs args) {
            if (down && KeyDown != null)
                KeyDown(this, args);
            else if (!down && KeyUp != null)
                KeyUp(this, args);
        }

        /// <summary>
        /// Add a input to the system.
        /// </summary>
        /// <param name="input">The input to add.</param>
        public void AddWindow(Window window) {
            mWindows.Add(window);
            window.Init(this);
            if (WindowAdded != null)
                WindowAdded(window, null);
        }

        /// <summary>
        /// DrawSelected any relevant information about this input onto a diagram.
        /// </summary>
        /// <param name="perspective">The perspective to render along.</param>
        /// <param name="graphics">The graphics object to draw with.</param>
        /// <param name="clip">The bounds of the area being drawn on.</param>
        /// <param name="scale">The value to control how large or small the diagram is rendered.</param>
        /// <param name="origin">The origin on the panel to draw transition.</param>
        public void Draw(Func<Vector3, Point> to2D, Graphics graphics, Rectangle clip, Action redraw) {
            foreach (var input in mInputs)
                input.Draw(to2D, graphics, redraw);

            foreach (var window in mWindows)
                window.Draw(to2D, graphics, clip, redraw);
        }

        /// <summary>
        /// Get a point on the horizontal output input that corresponds to a point in real space.
        /// </summary>
        /// <param name="perspective">The perspective to render along.</param>
        /// <param name="realPoint">The real point to translate into 2D coordinates.</param>
        public Point GetPoint(Func<Vector3, Point> to2D, Vector3 realPoint) {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Called when the input is to be disposed of.
        /// </summary>
        public void Close() {
            mAlive = false;
            foreach (var input in mInputs) {
                input.Enabled = false;
                input.Close();
            }
            foreach (var window in mWindows)
                window.Close();

            if (Closed != null)
                Closed(this, null);
        }

        public bool AutoRestart {
            get { return mConfig.AutoRestart; }
        }

        /// <summary>
        /// Handle a crash event,
        /// </summary>
        public void OnCrash(Exception e) {
            string dump = "Crash: " + DateTime.Now.ToString("u") + Environment.NewLine;
            dump += String.Format("{1}{0}{2}{0}{0}", Environment.NewLine, e.Message, e.StackTrace);
            dump += String.Format("-----------Coordinator-----------{0}", Environment.NewLine);
            dump += "Virtual Position: " + mPosition + Environment.NewLine;
            dump += "Virtual Orientation | Yaw: " + mOrientation.Yaw + ", Pitch: " + mOrientation.Pitch + Environment.NewLine;
            dump += "Eye Position: " + mEyePosition + Environment.NewLine;

            /*
            if (mActiveState != null) {
                dump += Environment.NewLine + "--------------" + mActiveState.Type + " Active-------------------" + Environment.NewLine;
                dump += "Instance: " + mActiveState.Name + Environment.NewLine;
                try {
                    dump += mActiveState.State;
                } catch (Exception ex) {
                    dump += "Unable to get stats for the active menu item. " + ex.Message + Environment.NewLine;
                    dump += ex.StackTrace;
                }
            }
            */

            if (mWindows.Count > 0) {
                dump += String.Format("{0}{0}--------Windows--------{0}", Environment.NewLine);
                foreach (var window in mWindows) {
                    try {
                        dump += Environment.NewLine + window.State;
                    } catch (Exception ex) {
                        dump += "Unable to get stats for input " + window.Name + ". " + ex.Message + Environment.NewLine;
                        dump += ex.StackTrace;
                    }
                }
                dump += Environment.NewLine;
            }

            if (mInputs.Count > 0) {
                dump += String.Format("{0}{0}--------Inputs--------{0}", Environment.NewLine);
                foreach (var input in mInputs) {
                    if (input.Enabled)
                        try {
                            dump += Environment.NewLine + input.State;
                        } catch (Exception ex) {
                            dump += "Unable to get stats for input " + input.Name + ". " + ex.Message + Environment.NewLine;
                            dump += ex.StackTrace;
                        } else
                        dump += Environment.NewLine + "--------" + input.Name + "--------" + Environment.NewLine + "Disabled";
                }
                dump += Environment.NewLine;
            }

            dump += String.Format("{0}{0}------------------------End of Crash Report------------------------{0}{0}", Environment.NewLine);

            File.AppendAllText(mCrashLogFile, dump);

            Close();
        }

        /// <summary>
        /// Get the input instance of the specified type. Throws an ArgumentException if no such input found.
        /// </summary>
        public T GetInput<T> () where T : ISystemInput {
            Type t = typeof(T);
            ISystemInput ret = mInputs.FirstOrDefault(input => input.GetType() == t);
            if (ret == null)
                throw new ArgumentException("Unable to get input. No input of the specified type (" + t.FullName + ") found.");
            return (T)ret;
        }
    }
}
