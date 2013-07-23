﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Util;
using System.Threading;
using log4net;

namespace Chimera.OpenSim {
    public class ViewerController : ProcessController {
        private ILog Logger;
        private string mToggleHudKey = "^%{F1}";

        public ViewerController(string exe, string workingDir, string args, string name)
            : base(exe, workingDir, args) {

            Logger = LogManager.GetLogger("OpenSim." + name + "Viewer");
        }

        public ViewerController(string toggleHUDKey, string name) {
            mToggleHudKey = toggleHUDKey;

            Logger = LogManager.GetLogger("OpenSim." + name + "Viewer");
        }

        public void Close(bool blocking) {
            if (!Started)
                return;

            ThreadStart close = () => {
                Logger.Debug("Closing");
                bool closed = false;
                object closeLock = new object();
                Action closeListener = () => {
                    closed = true;
                    lock (closeLock)
                        System.Threading.Monitor.PulseAll(closeLock);
                };
                Exited += closeListener;

                for (int i = 0; !closed && Started && i < 5; i++) {
                    PressKey("q", true, false, false);
                    lock (closeLock)
                        System.Threading.Monitor.Wait(closeLock, (i + 1) * 5000);
                }

                Logger.Info("Closed");
                Exited -= closeListener;
            };

            if (blocking)
                close();
            else
                new Thread(close).Start();
        }

        public void ToggleHUD() {
            PressKey(mToggleHudKey);
        }
    }
}
