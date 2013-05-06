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
using Chimera.Overlay.Triggers;
using System.Drawing;
using Chimera.Interfaces.Overlay;
using Chimera.Overlay;
using Chimera.Overlay.States;
using Chimera.Kinect.Overlay;
using Chimera.Flythrough.Overlay;
using Chimera.Overlay.Transitions;
using Chimera.Overlay.Drawables;
using Chimera.OpenSim;
using Chimera.Kinect;
using Chimera.Plugins;
using Joystick;
using Chimera.Kinect.GUI;
using Chimera.Flythrough;
using Touchscreen.Overlay;

namespace Chimera.Launcher {
    public class TimespanLauncher : Launcher{
        protected override void InitOverlay() {
            Window mainWindow = Coordinator["MainWindow"];

            Font font = new Font("Verdana", 52, FontStyle.Bold);
            Rectangle clip = new Rectangle(0, 0, 1920, 1080);

            ITrigger slideshowNext = ImgTrigger(mainWindow, "NextTrans", .85f, .85f);
            ITrigger slideshowPrev = ImgTrigger(mainWindow, "PrevTrans", .85f, .05f);
            ITrigger flythroughNext = ImgTrigger(mainWindow, "Next", .85f, .85f);

            IImageTransitionFactory fade = new FadeFactory();
            CutWindowTransitionFactory cutTransition = new CutWindowTransitionFactory();
            BitmapFadeTransitionFactory fadeTransition = new BitmapFadeTransitionFactory(fade, 1500.0);
            OpacityFadeOutTransitionFactory fadeOutTransition = new OpacityFadeOutTransitionFactory(1500.0);
            OpacityFadeInTransitionFactory fadeInTransition = new OpacityFadeInTransitionFactory(1500.0);

            State splash = new ImageBGState("Splash", Coordinator.StateManager, "../Images/Caen/MenuBGs/Caen-Splash.png");
            State controlAvatar;
            State controlFlycam;
            State helpAvatar = null;
            State helpFlycam = null;
            if (Config.UseClicks) {
                controlAvatar = new TouchscreenState("TouchscreenAvatar", true, Coordinator);
                controlFlycam = new TouchscreenState("TouchscreenFlycam", false, Coordinator);
            } else {
                controlAvatar = new KinectControlState("KinectControlAvatar", Coordinator.StateManager, true);
                controlFlycam = new KinectControlState("KinectControlFlycam", Coordinator.StateManager, false);
                helpAvatar = new KinectHelpState("KinectHelpAvatar", Coordinator.StateManager, mainWindow.Name, mainWindow.Name);
                helpFlycam = new KinectHelpState("KinectHelpFlycam", Coordinator.StateManager, mainWindow.Name, mainWindow.Name);
            }
            State idleFlythrough = new FlythroughState("Idle", Coordinator.StateManager, "../Flythroughs/Caen-long.xml");
            State slideshow = new SlideshowState("Slideshow", Coordinator.StateManager, "../Images/Caen/TodaySlideshow", slideshowNext, slideshowPrev, fade, 1500);
            State structuredFlythrough = new FlythroughState("StructuredFlythrough", Coordinator.StateManager, "../Flythroughs/Caen-Guided.xml", flythroughNext);

            State confirmDog = new ImageBGState("Story1Confirm", Coordinator.StateManager, "../Images/Caen/StorytellingVideos/ConfirmDogs.png");
            State confirmGartymore = new ImageBGState("Story2Confirm", Coordinator.StateManager, "../Images/Caen/StorytellingVideos/ConfirmGartymore.png");
            State confirmFrakkok = new ImageBGState("Story3Confirm", Coordinator.StateManager, "../Images/Caen/StorytellingVideos/ConfirmFrakkok.png");
            State confirmFisherman = new ImageBGState("Story4Confirm", Coordinator.StateManager, "../Images/Caen/StorytellingVideos/ConfirmFisherman.png");
            State confirmWolf = new ImageBGState("Story5Confirm", Coordinator.StateManager, "../Images/Caen/StorytellingVideos/ConfirmWolf.png");

            State storyDog = new OverlayVideoState("Story1", mainWindow.OverlayManager, "../../Videos/dogs.m4v", splash, fadeTransition);
            State storyGartymore = new OverlayVideoState("Story2", mainWindow.OverlayManager, "../../Videos/garyore.mp4", splash, fadeTransition);
            State storyFrakkok = new OverlayVideoState("Story3", mainWindow.OverlayManager, "../../Videos/orkney.mp4", splash, fadeTransition);
            State storyFisherman = new OverlayVideoState("Story4", mainWindow.OverlayManager, "../../Videos/fisherman.mp4", splash, fadeTransition);
            State storyWolf = new OverlayVideoState("Story5", mainWindow.OverlayManager, "../../Videos/wolf.mp4", splash, fadeTransition);

            if (Config.UseClicks) {
                ImgTrans(controlAvatar, splash, "MainMenu", .85f, .80f, .1f, mainWindow, fadeTransition);
                ImgTrans(controlFlycam, splash, "MainMenu", .85f, .80f, .1f, mainWindow, fadeTransition);
            } else {
                ImgTrans(helpAvatar, controlAvatar, "HelpToWorld", .85f, .15f, .1f, mainWindow, fadeOutTransition);
                ImgTrans(helpFlycam, controlFlycam, "HelpToWorld", .85f, .15f, .1f, mainWindow, fadeOutTransition);
                ImgTrans(helpAvatar, splash, "MainMenu", .85f, .35f, .1f, mainWindow, fadeTransition);
                ImgTrans(helpFlycam, splash, "MainMenu", .85f, .35f, .1f, mainWindow, fadeTransition);
            }

            ImgTrans(structuredFlythrough,  splash,         "Back",                 .05f, .85f, mainWindow, fadeTransition);
            ImgTrans(slideshow,  splash,                    "BackTrans",            .45f, .85f, mainWindow, fadeTransition);

            InvisTrans(splash, confirmDog,                new Point(115,395), new Point(835,485), clip, mainWindow, fadeTransition);
            InvisTrans(splash, confirmGartymore,          new Point(115,510), new Point(835,600), clip, mainWindow, fadeTransition);
            InvisTrans(splash, confirmFrakkok,            new Point(115,625), new Point(835,720), clip, mainWindow, fadeTransition);
            InvisTrans(splash, confirmFisherman,          new Point(115,745), new Point(835,825), clip, mainWindow, fadeTransition);
            InvisTrans(splash, confirmWolf,               new Point(115,860), new Point(835,955), clip, mainWindow, fadeTransition);
            InvisTrans(confirmDog, splash,               new Point(320,380), new Point(770,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmDog, storyDog,            new Point(960,380), new Point(1410,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmGartymore, splash,         new Point(320,380), new Point(770,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmGartymore, storyGartymore, new Point(960,380), new Point(1410,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmFrakkok, splash,           new Point(320,380), new Point(770,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmFrakkok, storyFrakkok,     new Point(960,380), new Point(1410,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmFisherman, splash,         new Point(320,380), new Point(770,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmFisherman, storyFisherman, new Point(960,380), new Point(1410,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmWolf, splash,              new Point(320,380), new Point(770,665), clip, mainWindow, fadeTransition);
            InvisTrans(confirmWolf, storyWolf,           new Point(960,380), new Point(1410,665), clip, mainWindow, fadeTransition);

            InvisTrans(splash, structuredFlythrough,    new Point(1080,415), new Point(1755,500), clip, mainWindow, fadeOutTransition);
            InvisTrans(splash, controlAvatar,            new Point(1080,535), new Point(1755,620), clip, mainWindow, fadeOutTransition);
            InvisTrans(splash, slideshow,               new Point(1080,645), new Point(1755,735), clip, mainWindow, fadeTransition);
            InvisTrans(splash, controlFlycam,            new Point(1080,765), new Point(1755,865), clip, mainWindow, fadeOutTransition);

            ITrigger customTriggerHelp = new CustomTriggerTrigger(Coordinator.StateManager, "Help");

            if (!Config.UseClicks) {
                StateTransition kinectHelpAvatarTransition = new StateTransition(Coordinator.StateManager, controlAvatar, helpAvatar, customTriggerHelp, fadeInTransition);
                StateTransition kinectHelpFlycamTransition = new StateTransition(Coordinator.StateManager, controlFlycam, helpFlycam, customTriggerHelp, fadeInTransition);
                controlAvatar.AddTransition(kinectHelpAvatarTransition);
                controlFlycam.AddTransition(kinectHelpFlycamTransition);

                SkeletonFeature splashSkeleton = new SkeletonFeature(940, 1470, 1000, 100f, mainWindow.Name, clip);
                splash.AddFeature(splashSkeleton);

                FadingText raiseHandText = new FadingText("Stretch your arm above your head for help", 30000, 30000, mainWindow.Name, Color.Red, font, 150, 30, clip);
                controlAvatar.AddFeature(raiseHandText);
                controlFlycam.AddFeature(raiseHandText);
            }

            Coordinator.StateManager.AddState(splash);
            Coordinator.StateManager.AddState(controlAvatar);
            Coordinator.StateManager.AddState(controlFlycam);
            if (!Config.UseClicks) {
                Coordinator.StateManager.AddState(helpAvatar);
                Coordinator.StateManager.AddState(helpFlycam);
            }
            Coordinator.StateManager.AddState(slideshow);
            Coordinator.StateManager.AddState(structuredFlythrough);
            Coordinator.StateManager.AddState(idleFlythrough);
            Coordinator.StateManager.AddState(storyDog);
            Coordinator.StateManager.AddState(storyGartymore);
            Coordinator.StateManager.AddState(storyFrakkok);
            Coordinator.StateManager.AddState(storyFisherman);
            Coordinator.StateManager.AddState(storyWolf);
        }
    }
}