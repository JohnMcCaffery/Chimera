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

namespace Chimera.Plugins 
{
    public class PanoramaPlugin : PluginBase<PanoramaPanel> 
    {
        private ILog Logger = LogManager.GetLogger("Panorama");

        private CoreConfig mConfig = new CoreConfig();
        private Frame mFrame;
        private bool mRunning;

        private Queue<Bitmap> mScreenshots = new Queue<Bitmap>();

        public PanoramaPlugin()
            : base("Panorama", plugin => new PanoramaPanel(plugin as PanoramaPlugin)) 
        {
        }

        public override void Init(Core core) 
        {
            base.Init(core);
            mFrame = mCore.Frames.First();
        }

        public override Config.ConfigBase Config 
        {
            get { return mConfig; }
        }

        public void TakePanorama() 
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

            Thread t = new Thread(ScreenshotProcessor);
            t.Name = "Panorama Image Processor";
            t.Start();

            float offset = 0.95f;
            //float offset = 1.3f;
            //float offset = 2.1f;
            //float offset = 0.63f;
            //float offset = 6.3f;

            
            for (int i = 1; i < 13; i++)
            {
                if (i <= 6)
                {
                    mCore.Update(mCore.Position, Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 6);
                    TakeScreenshot();
                }

                else if (i == 7)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 6);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 8)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 6);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 9)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 6);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 10)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 6);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 11)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 6);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 12)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 6);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }
            }
            


            /*
            for (int i = 1; i < 13; i++)
            {
                if (i <= 6)
                {
                    mCore.Update(mCore.Position, Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS*2);
                    TakeScreenshot();
                }

                else if (i == 7)
                {
                    //offset on z-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X + offset, temp.Y, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    //mCore.Update(new Vector3(temp.X, temp.Y, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 2);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 8)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 2);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 9)
                {
                    //offset on z-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X + offset, temp.Y, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    //mCore.Update(new Vector3(temp.X, temp.Y, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 2);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 10)
                {
                    //offset on x-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y + offset, temp.Z), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 2);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 11)
                {
                    //offset on y-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y, temp.Z + offset), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 2);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }

                else if (i == 12)
                {
                    //offset on y-plane
                    Vector3 temp = mCore.Position;
                    mCore.Update(new Vector3(temp.X, temp.Y, temp.Z + offset), Vector3.Zero, GetRotation(i), Rotation.Zero);
                    Thread.Sleep(mConfig.CaptureDelayMS * 2);
                    TakeScreenshot();
                    mCore.Update(temp, Vector3.Zero, GetRotation(i), Rotation.Zero);
                }
            }
            */
            
            mRunning = false;

            mCore.Update(mCore.Position, Vector3.Zero, r, Rotation.Zero);
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

        private void ScreenshotProcessor() 
        {
            int image = 1;
            while (mRunning || mScreenshots.Count > 0) 
            {
                if (mScreenshots.Count > 0) {
                    Bitmap screenshot = mScreenshots.Dequeue();
                    using (Bitmap resized = new Bitmap(screenshot, new Size(screenshot.Height, screenshot.Height))) 
                    {
                        string file = Path.Combine(mConfig.ScreenshotFolder, GetImageName(image++) + ".png");
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
                case 7: return new Rotation(0.0, 0.0);
                case 8: return new Rotation(0.0, 90);
                case 9: return new Rotation(0.0, 180.0);
                case 10: return new Rotation(0.0, -90);
                case 11: return new Rotation(-90.0, 0.0);
                case 12: return new Rotation(90.0, 0.0);
                default: return new Rotation(0.0, 0.0);
            }
        }

        private string GetImageName(int image) 
        {
            switch (image) 
            {
                case 1: return "Front-L";
                case 2: return "Right-L";
                case 3: return "Back-L";
                case 4: return "Left-L";
                case 5: return "Up-L";
                case 6: return "Down-L";
                case 7: return "Front-R";
                case 8: return "Right-R";
                case 9: return "Back-R";
                case 10: return "Left-R";
                case 11: return "Up-R";
                case 12: return "Down-R";
                default: return "Unknown";
            }
        }
    }
}
