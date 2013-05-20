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

namespace Chimera.Overlay {
    public class StateTransition {
        /// <summary>
        /// The individual transitions for each window in the system.
        /// </summary>
        private readonly Dictionary<string, IWindowTransition> mWindowTransitions = new Dictionary<string, IWindowTransition>();
        /// <summary>
        /// During a transition, the windows which have completed the transition. The transition as a whole is only complete when all windows have completed.
        /// </summary>
        private readonly HashSet<IWindowTransition> mCompletedWindows = new HashSet<IWindowTransition>();
        /// <summary>
        /// Factory for creating new window transitions when new windows are added to the system.
        /// </summary>
        private IWindowTransitionFactory mWindowTransitionFactory;
        /// <summary>
        /// The manager which this transition works transition.
        /// </summary>
        private OverlayPlugin mManager;
        /// <summary>
        /// The trigger which will start this transition.
        /// </summary>
        private List<ITrigger> mTriggers = new List<ITrigger>();
        /// <summary>
        /// The state the transition starts at.
        /// </summary>
        private State mFrom;
        /// <summary>
        /// The state the transition goes to.
        /// </summary>
        private State mTo;
        /// <summary>
        /// Whether the transition is in progress.
        /// </summary>
        private bool mInProgress;
        /// <summary>
        /// Whether the transition is active and should start when triggered.
        /// </summary>
        private bool mActive;

        /// <summary>
        /// Triggered when the transition has started.
        /// </summary>
        public event Action<StateTransition> Started;
        /// <summary>
        /// Triggered when the transition has finished.
        /// </summary>
        public event Action<StateTransition> Finished;

        /// <param name="manager">The manager this transition works transition.</param>
        public StateTransition(OverlayPlugin manager, State from, State to, ITrigger trigger, IWindowTransitionFactory factory) {
            mManager = manager;
            mFrom = from;
            mTo = to;
            mTriggers.Add(trigger);
            mWindowTransitionFactory = factory;

            mManager.Coordinator.WindowAdded += new Action<Window,EventArgs>(Coordinator_WindowAdded);

            AddTrigger(trigger);

            foreach (var window in mManager.Coordinator.Windows)
                Coordinator_WindowAdded(window, null);
        }

        /// <summary>
        /// Transitions for each window.
        /// </summary>
        public IWindowTransition[] WindowTransitions {
            get { return mWindowTransitions.Values.ToArray(); }
        }

        /// <summary>
        /// The state the transition goes transition.
        /// </summary>
        public State From {
            get { return mFrom; }
        }

        /// <summary>
        /// The state the transition goes to.
        /// </summary>
        public State To {
            get { return mTo; }
        }

        /// <summary>
        /// Trigger which will start the transition.
        /// </summary>
        public List<ITrigger> Triggers {
            get { return mTriggers; }
        }

        /// <summary>
        /// Controller object to notify of the transition.
        /// </summary>
        public OverlayPlugin Manager {
            get { return mManager; }
        }

        /// <summary>
        /// Whether the trigger firing should start this transition.
        /// </summary>
        public bool Active {
            get { return mActive; }
            set { 
                mActive = value;
                foreach (var trigger in mTriggers)
                    trigger.Active = value;
            }
        }

        /// <summary>
        /// Whether the transition is currently in progress.
        /// </summary>
        public bool InProgress {
            get { return mInProgress; }
            set { mInProgress = value; }
        }

        public void AddTrigger(ITrigger trigger) {
            trigger.Triggered += new Action(mTrigger_Triggered);
            mTriggers.Add(trigger);

            if (trigger is IFeature) {
                IFeature feature = trigger as IFeature;
                From.AddFeature(feature);
            }
        }

        public void AddTriggers(IEnumerable<ITrigger> triggers) {
            foreach (var trigger in triggers)
                AddTrigger(trigger);
        }

        /// <summary>
        /// Start the transition.
        /// </summary>
        public void Begin() {
            if (mActive) {
                mFrom.Active = false;
                mTo.TransitionToStart();
                mCompletedWindows.Clear();
                foreach (var windowTrans in mWindowTransitions.Values) {
                    windowTrans.From.Active = false;
                    windowTrans.Manager.CurrentDisplay = windowTrans;
                    windowTrans.Begin();
                }
                if (Started != null)
                    Started(this);
            }
        }

        void mTrigger_Triggered() {
            if (mActive) {
                foreach (var trigger in mTriggers)
                    trigger.Active = false;
                mManager.BeginTransition(this);
            }
        }

        void Coordinator_WindowAdded(Window window, EventArgs args) {
            IWindowTransition transition = mWindowTransitionFactory.Create(this, Manager[window.Name]);
            mWindowTransitions.Add(window.Name, transition);
            transition.Finished += new Action<IWindowTransition>(transition_Finished);
        }

        void transition_Finished(IWindowTransition transition) {
            mCompletedWindows.Add(transition);
            mFrom.TransitionFromFinish();
            transition.To.Active = true;
            transition.Manager.CurrentDisplay = transition.To;
            transition.Manager.ForceRedrawStatic();
            if (mCompletedWindows.Count == mWindowTransitions.Count) {
                mInProgress = false;
                if (Finished != null)
                    Finished(this);
            }
        }

        /// <summary>
        /// Cancel the transition that is currently happening.
        /// </summary>
        public void Cancel() {
            foreach (var windowTransition in mWindowTransitions.Values)
                windowTransition.Cancel();
        }

        public override string ToString() {
            return "Transition " + mFrom.Name + " to " + mTo.Name;
        }
    }
}
