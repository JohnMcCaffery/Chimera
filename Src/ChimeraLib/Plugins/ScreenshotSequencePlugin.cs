using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Config;
using Chimera.GUI.Controls.Plugins;
using OpenMetaverse;
using Chimera.Util;
using System.Drawing;
using System.Threading;
using System.IO;
using log4net;
using System.Windows.Forms;


namespace Chimera.Plugins
{
    public class ScreenshotSequencePlugin : ISystemPlugin
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
        private int mW, mH;
        private int step = 0;
        private String folder;

        private Queue<Bitmap> mScreenshots = new Queue<Bitmap>();

        private ILog Logger = LogManager.GetLogger("Panorama");

        public event Action Started;
        public event Action Stopped;

        public bool Running
        {
            get { return mRunning; }
            set
            {
                if (mRunning != value)
                {
                    mRunning = value;
                    if (value)
                    {
                        mW = mFrame.Monitor.Bounds.Width;
                        mH = mFrame.Monitor.Bounds.Height;

                        mStarted = DateTime.Now;
                        mCore.Tick += mTickListener;
                        //Thread t = new Thread(ScreenshotProcessor);
                        //t.Name = "Screenshot Processor";
                        //t.Start();
                        if (Started != null)
                            Started();
                    }
                    else
                    {
                        mCore.Tick -= mTickListener;
                        if (Started != null)
                            Stopped();
                    }
                }
            }
        }

        public void Init(Core core)
        {
            mCore = core;
            mFrame = mCore.Frames[0];
            mTickListener = new Action(TickListener);
        }

        public void SetForm(Form form)
        {
            mForm = form;
        }

        public event Action<IPlugin, bool> EnabledChanged;

        public Control ControlPanel
        {
            get
            {
                if (mPanel == null)
                    mPanel = new ScreenshotSequencePanel(this);
                return mPanel;
            }
        }

        public bool Enabled
        {
            get { return mEnabled; }
            set
            {
                if (mEnabled != value)
                {
                    mEnabled = value;
                    if (EnabledChanged != null)
                        EnabledChanged(this, value);
                }
            }
        }

        public string Name
        {
            get { return "ScreenshotSequence"; }
        }

        public string State
        {
            get { throw new NotImplementedException(); }
        }

        public ConfigBase Config
        {
            get { return mConfig; }
        }

        public void Close()
        {
        }

        public void Draw(System.Drawing.Graphics graphics, Func<OpenMetaverse.Vector3, System.Drawing.Point> to2D, Action redraw, Perspective perspective) { }

        private void TickListener()
        {
            if (mConfig.StopM < DateTime.Now.Subtract(mStarted).TotalMinutes)
            {
                Running = false;
            }
            else if (mConfig.IntervalMS < DateTime.Now.Subtract(mLastPress).TotalMilliseconds)
            {
                new Thread(TakePanorama).Start();
                mLastPress = DateTime.Now;
            }
        }

        public void TakePanorama()
        {
            step++;
            folder = Path.Combine(mConfig.ScreenshotFolder, "pano"+step);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Rotation r = mCore.Orientation;
            mRunning = true;

            /*
              if (!Directory.Exists(mConfig.ScreenshotFolder))
                Directory.CreateDirectory(mConfig.ScreenshotFolder);
            */
            Thread tp = new Thread(ScreenshotProcessor);
            tp.Name = "Panorama Image Processor";
            tp.Start();

            for (int i = 1; i < 7; i++)
            {
                mCore.Update(mCore.Position, Vector3.Zero, GetRotation(i), Rotation.Zero);
                Thread.Sleep(mConfig.CaptureDelayMS);
                TakeScreenshot();
            }

            mRunning = false;
            mCore.Update(mCore.Position, Vector3.Zero, r, Rotation.Zero);
        }

        private void TakeScreenshot()
        {
            Bitmap screenshot = new Bitmap(mW, mH);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(mFrame.Monitor.Bounds.Location, Point.Empty, mFrame.Monitor.Bounds.Size);
            }
            mScreenshots.Enqueue(screenshot);
        }

        private void ScreenshotProcessor()
        {
            int image = 1;
            while (mRunning || mScreenshots.Count > 0)
            {
                if (mScreenshots.Count > 0)
                {
                    Bitmap screenshot = mScreenshots.Dequeue();
                    using (Bitmap resized = new Bitmap(screenshot, new Size(screenshot.Height, screenshot.Height)))
                    {
                        //string file = Path.Combine(mConfig.ScreenshotFolder, step + GetImageName(image++) + ".png")
                        string file = Path.Combine(folder, GetImageName(image++) + ".png");
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


