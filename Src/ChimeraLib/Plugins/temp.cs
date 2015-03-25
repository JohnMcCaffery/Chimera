using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Config;
using System.Windows.Forms;
using Chimera.GUI.Controls.Plugins;
using OpenMetaverse;
using Chimera.Util;
using System.Drawing;
using System.Threading;
using System.IO;
using log4net;

namespace Chimera.Plugins {
    public class PanoramaPlugin : PluginBase<PanoramaPanel> 
	{
        private CoreConfig mConfig = new CoreConfig();
        private Core mCore;
        private ScreenshotSequencePanel mPanel;
        private Form mForm = null;
        private bool mEnabled = true;
        private bool mRunning;
        private DateTime mStarted;
        private DateTime mLastPress;
        private Action mTickListener;
		private Frame mFrame;

        private Queue<Bitmap> mScreenshots = new Queue<Bitmap>();

        public event Action Started;
        public event Action Stopped;
		
		private int sequence = 1;
        private ILog Logger = LogManager.GetLogger("Panorama");

			

        public bool Running {
            get { return mRunning; }
            set {
                if (mRunning != value) {
                    mRunning = value;
                    if (value) {
                        mStarted = DateTime.Now;
                        mCore.Tick += mTickListener;
                        Thread t = new Thread(ScreenshotProcessor);
                        t.Name = "Periodic Panorama Processor";
                        t.Start();
                        if (Started != null)
                            Started();
                    } else {
                        mCore.Tick -= mTickListener;
                        if (Started != null)
                            Stopped();
                    }
                }
            }
        }

        public void Init(Core core) {
            mCore = core;
	    mFrame = mCore.Frames[0];
            mTickListener = new Action(TickListener);
        }

        public void SetForm(Form form) {
            mForm = form;
        }

        public event Action<IPlugin, bool> EnabledChanged;

        public Control ControlPanel {
            get {
                if (mPanel == null)
                    mPanel = new ScreenshotSequencePanel(this);
                return mPanel;
            }
        }

        public bool Enabled {
            get { return mEnabled; }
            set {
                if (mEnabled != value) {
                    mEnabled = value;
                    if (EnabledChanged != null)
                        EnabledChanged(this, value);
                }
            }
        }

        public string Name {
            get { return "ScreenshotSequence"; }
        }

        public string State {
            get { throw new NotImplementedException(); }
        }

        public ConfigBase Config {
            get { return mConfig; }
        }

        public void Close() {
        }

        public void Draw(System.Drawing.Graphics graphics, Func<OpenMetaverse.Vector3, System.Drawing.Point> to2D, Action redraw, Perspective perspective) { }

        private void TickListener() {
            if (mConfig.StopM < DateTime.Now.Subtract(mStarted).TotalMinutes) {
                Running = false;
            } else if (mConfig.IntervalMS < DateTime.Now.Subtract(mLastPress).TotalMilliseconds) {
                TakePanorama();
                //TakeScreenshot();
                mLastPress = DateTime.Now;
            }
        }

        private void ScreenshotProcessor() 
        {
            int image = 1;
            while (mRunning || mScreenshots.Count > 0) 
            {
                if (mScreenshots.Count > 0) {
                    Bitmap screenshot = mScreenshots.Dequeue();
                    using (Bitmap resized = new Bitmap(screenshot, new Size(screenshot.Height, screenshot.Height))) 
                    {
						//string file = Path.Combine(mConfig.ScreenshotFolder, GetImageName(image++) + ".png");
                        string file = Path.Combine(mConfig.ScreenshotFolder, GetImageName(image++) + sequence++ + ".png");
                        Logger.Info("Writing Panorama image to: " + file + ".");
                        resized.Save(file);
                    }
                    screenshot.Dispose();
                }
                Thread.Sleep(5);
            }
            if (mConfig.AutoShutdown)
                mForm.Close();
        }

        private void TakeScreenshot() 
        {
            Bitmap screenshot = new Bitmap(mFrame.Monitor.Bounds.Width, mFrame.Monitor.Bounds.Height);
            using (Graphics g = Graphics.FromImage(screenshot)) 
            {
                g.CopyFromScreen(mFrame.Monitor.Bounds.Location, Point.Empty, mFrame.Monitor.Bounds.Size);
            }
            mScreenshots.Enqueue(screenshot);
        }
		
		private void TakePanorama() 
        {
            Rotation r = mCore.Orientation;
            for (int i = 1; i < 13; i++)
            {
                mCore.Update(mCore.Position, Vector3.Zero, GetRotation(i), Rotation.Zero);
                Thread.Sleep(500);
            }
            
            mRunning = true;

            if (!Directory.Exists(mConfig.ScreenshotFolder))
                Directory.CreateDirectory(mConfig.ScreenshotFolder);

            Thread tp = new Thread(ScreenshotProcessor);
            tp.Name = "Panorama Image Processor";
            tp.Start();

            for (int i = 1; i < 7; i++)
            {
                mCore.Update(mCore.Position, Vector3.Zero, GetRotation(i), Rotation.Zero);
                Thread.Sleep(mConfig.CaptureDelayMS * 6);
                TakeScreenshot();
            }
       
            mRunning = false;
            mCore.Update(mCore.Position, Vector3.Zero, r, Rotation.Zero);
        }
		
		private Rotation GetRotation(int image) 
        {
            switch (image) 
            {
                case 1: return new Rotation(0.0, 0.0);
                case 2: return new Rotation(0.0, 90);
                case 3: return new Rotation(0.0, 180.0);
                case 4: return new Rotation(0.0, -90);
                case 5: return new Rotation(-90.0, 0.0);
                case 6: return new Rotation(90.0, 0.0);
                default: return new Rotation(0.0, 0.0);
            }
        }

        private string GetImageName(int image) 
        {
            switch (image) 
            {
                case 1: return "front";
                case 2: return "right";
                case 3: return "back";
                case 4: return "left";
                case 5: return "up";
                case 6: return "down";
                default: return "Unknown";
            }
        }
    }
}


