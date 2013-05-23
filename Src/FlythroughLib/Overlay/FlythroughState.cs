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
using Chimera.Interfaces.Overlay;
using Chimera.Overlay;
using Chimera.Overlay.States;
using Chimera.Overlay.Triggers;
using Chimera.Overlay.Drawables;
using System.Drawing;
using System.Xml;
using Chimera.Interfaces;

namespace Chimera.Flythrough.Overlay {
    public class FlythroughStateFactory : IStateFactory {
        private IMediaPlayer mPlayer = null;

        public FlythroughStateFactory() { }

        public FlythroughStateFactory(IMediaPlayer player) {
            mPlayer = player;
        }

        #region IFactory<State> Members

        public string Name {
            get { return "Flythrough"; }
        }

        public State Create(OverlayPlugin manager, XmlNode node) {
            return new FlythroughState(manager, node, mPlayer);
        }

        public State Create(OverlayPlugin manager, XmlNode node, Rectangle clip) {
            return Create(manager, node);
        }

        #endregion
    }

    public class FlythroughState : State {
        private static string FONT = "Verdana";
        private static float FONT_SIZE = 20;
        private static Color FONT_COLOUR = Color.Red;
        private static PointF STEP_TEXT_POS = new PointF(.005f, .05f);

        private Dictionary<int, Step> mSteps = new Dictionary<int, Step>();
        private FlythroughPlugin mInput;
        private SlideshowWindow mSlideshow;
        private IImageTransition mSlideshowTransition;
        private List<ITrigger> mStepTriggers = new List<ITrigger>();
        private IMediaPlayer mPlayer;
        private Text mStepText;
        private Text mSubtitlesText;
        private Step mCurrentStep;
        private string mSlideshowWindowName;
        private string mSlideshowFolder;
        private string mFlythrough;
        private bool mStepping = false;
        private int mStep = 1;

        public FlythroughState(string name, OverlayPlugin manager, string flythrough)
            : base(name, manager) {

            mFlythrough = flythrough;
            mInput = manager.Coordinator.GetPlugin<FlythroughPlugin>();
        }

        public FlythroughState(string name, OverlayPlugin manager, string flythrough, params ITrigger[] stepTriggers)
            : this(name, manager, flythrough) {

            mStepping = true;
            Font f = new Font(FONT, FONT_SIZE, FontStyle.Bold);
            mStepText = new StaticText(mStep + "/" + mInput.Count, manager[0], f, FONT_COLOUR, STEP_TEXT_POS);
            AddFeature(mStepText);

            mInput.CurrentEventChange += new Action<FlythroughEvent<Camera>,FlythroughEvent<Camera>>(mInput_CurrentEventChange);

            foreach (var trigger in stepTriggers)
                AddStepTrigger(trigger);
        }

        public FlythroughState(string name, OverlayPlugin manager, string flythrough, string slideshowWindow, string slideshowFolder, IImageTransition slideshowTransition, params ITrigger[] steps)
            : this(name, manager, flythrough, steps) {

            mSlideshowWindowName = slideshowWindow;
            mSlideshowFolder = slideshowFolder;
            mSlideshowTransition = slideshowTransition;
        }

        private void AddStepTrigger(ITrigger trigger) {
            if (trigger == null)
                //TODO - debug?
                return;
            mStepTriggers.Add(trigger);
            trigger.Triggered += new Action(step_Triggered);
            if (trigger is IFeature)
                AddFeature(trigger as IFeature);
        }

        public FlythroughState(OverlayPlugin manager, XmlNode node, IMediaPlayer player)
            : base(GetName(node), manager) {

            mInput = manager.Coordinator.GetPlugin<FlythroughPlugin>();
            bool displaySubtitles = GetBool(node, false, "DisplaySubtitles");
            mFlythrough = GetString(node, null, "File");
            mStepping = GetBool(node, false, "Stepping");

            if (mFlythrough == null)
                throw new ArgumentException("Unable to load flythrough state. No flythrough file specified.");

            // ----------- Anything below here is only relevant in stepping mode ------
            if (!mStepping)
                return;

            mPlayer = player;
            if (mPlayer != null)
                mDefaultWindow = Manager[0];

            if (displaySubtitles) {
                mSubtitlesText = Manager.MakeText(node.SelectSingleNode("child::SubtitleText"));
                AddFeature(mSubtitlesText);
            }
            mStepText = Manager.MakeText(node.SelectSingleNode("child::StepText"));
            AddFeature(mStepText);
            mInput.CurrentEventChange += new Action<FlythroughEvent<Camera>,FlythroughEvent<Camera>>(mInput_CurrentEventChange);
            int subtitleTimeout = GetInt(node, 20, "SubtitleTimeout");

            XmlNode triggersRoot = node.SelectSingleNode("child::Triggers");
            if (triggersRoot != null) {
                foreach (XmlNode child in triggersRoot.ChildNodes)
                    AddStepTrigger(manager.GetTrigger(child, "flythrough step", null));
            }
            XmlNode stepsRoot = node.SelectSingleNode("child::Steps");
            if (stepsRoot != null) {
                foreach (XmlNode child in stepsRoot.ChildNodes) {
                    if (child is XmlElement) {
                        Step step = new Step(this, child, mSubtitlesText, subtitleTimeout, mPlayer);
                        mSteps.Add(step.StepNum, step);
                    }
                }
            }
        }

        void mInput_CurrentEventChange(FlythroughEvent<Camera> old, FlythroughEvent<Camera> n) {
            if (mCurrentStep != null)
                mCurrentStep.Finish();
            mCurrentStep = null;
            if (mSteps.ContainsKey(mStep)) {
                mCurrentStep = mSteps[mStep];
                mCurrentStep.Start();
            }
            mStep++;

            if (mStep == mInput.Count) {
                foreach (var trigger in mStepTriggers)
                    trigger.Active = false;
                foreach (var manager in Manager.OverlayManagers)
                    manager.ForceRedrawStatic();
            }

            mStepText.TextString = mStep + "\\" + mInput.Count;

        }

        void step_Triggered() {
            mInput.Step();
            //foreach (var step in mStepTriggers)
                //step.Active = false;
        }


        public override IWindowState CreateWindowState(WindowOverlayManager manager) {
            if (manager.Name.Equals(mSlideshowWindowName)) {
                mSlideshow = new SlideshowWindow(manager, mSlideshowFolder, mSlideshowTransition);
                return mSlideshow;
            }
            return new WindowState(manager);
        }

        protected override void TransitionToFinish() { }

        protected override void TransitionFromStart() {
            if (mCurrentStep != null)
                mCurrentStep.Finish();
        }

        public override void TransitionToStart() {
            if (mPlayer != null) {
                mDefaultWindow.AddControl(mPlayer.Player, new RectangleF(0f, 0f, 0f, 0f));
            }

            if (mSubtitlesText != null)
                mSubtitlesText.Active = true;

            mStep = 0;
            Manager.Coordinator.ControlMode = ControlMode.Absolute;
            mInput.Enabled = true;
            mInput.Coordinator.EnableUpdates = true;
            mInput.Load(mFlythrough);

            if (mStepping) {
                mInput.AutoStep = false;
                mInput.Loop = false;
            } else {
                mInput.Loop = true;
                mInput.AutoStep = true;
            }

            mInput.Time = 0;
            //mInput.CurrentEventChange += mInput_CurrentEventChange;
            mInput.Play();
        }

        private WindowOverlayManager mDefaultWindow;

        public override void TransitionFromFinish() {
            mInput.Paused = true;
            mInput.Enabled = false;
            if (mPlayer != null)
                mDefaultWindow.RemoveControl(mPlayer.Player);
            if (mSubtitlesText != null)
                mSubtitlesText.Active = false;
        }
    }
}
