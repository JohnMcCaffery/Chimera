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

namespace Chimera.Overlay {
    public class MainMenu : IOverlay {
        /// <summary>
        /// The areas which represent the choices on the main menu.
        /// </summary>
        private readonly Dictionary<string, WindowOverlay> mWindowOverlays = new Dictionary<string, WindowOverlay>();
        private Coordinator mCoordinator;
        private MainMenuItem[] mItems;
        private double mMainMenuSelectableSize = .1;

        /// <summary>
        /// The state which has been selected.
        /// </summary>
        private IState mSelectedState;
        /// <summary>
        /// True if the main menu active. False if some other state has been entered.
        /// </summary>
        private bool mMenuActive = true;
        /// <summary>
        /// True if the main menu is currently being minimised.
        /// </summary>
        private bool mMinimizing;
        /// <summary>
        /// True if the main menu is currently being maximised.
        /// </summary>
        private bool mMaximising;
        /// <summary>
        /// The current step that has been reached in terms if minimizing or maximising.
        /// </summary>
        private double mCurrentStep;
        /// <summary>
        /// The total number of steps to do whilst minimizing or maximising.
        /// </summary>
        private int mSteps = 30;

        public MainMenu(params MainMenuItem[] items) {
            mItems = items;
        }

        public double MainMenuSelectableSize {
            get { return mMainMenuSelectableSize; }
        }

        #region IOverlay Members

        public event Action<IState> StateSelected;

        public IState SelectedState {
            get { return mSelectedState; }
        }

        public void Init(Coordinator coordinator) {
            mCoordinator = coordinator;
            foreach (var window in mCoordinator.Windows) {
                WindowOverlay overlay = new WindowOverlay(mItems.Where(i => i.WindowName.Equals(window.Name)));
                mWindowOverlays.Add(window.Name, overlay);
                overlay.Init(this, window);
            }
        }

        public void Draw(Graphics graphics, Rectangle clipRectangle, Window window) {
            if (mMenuActive)
                mWindowOverlays[window.Name].DrawMenu(graphics, clipRectangle);
            else if (mMinimizing || mMaximising) {
                mCurrentStep += mMinimizing ? -1 : 1;
                double scale = (1.0 - mMainMenuSelectableSize) * (mCurrentStep / mSteps) + mMainMenuSelectableSize;
                mWindowOverlays[window.Name].DrawInBetween(mSelectedState, scale, graphics, clipRectangle);

                if (mMinimizing && mCurrentStep == 0) {
                    mMinimizing = false;
                    mSelectedState.Activate();
                }
                if (mMaximising && mCurrentStep == mSteps) {
                    mMaximising = false;
                    mMenuActive = true;
                    if (MainMenuSelected != null)
                        MainMenuSelected();
                }
            } else
                mWindowOverlays[window.Name].DrawState(mSelectedState, graphics, clipRectangle);

            mWindowOverlays[window.Name].DrawCursor(graphics, clipRectangle);
        }

        public void SelectState(IState newState) {
            mMenuActive = false;
            mMinimizing = true;
            mCurrentStep = mSteps;
            mSelectedState = newState;
            if (StateSelected != null)
                StateSelected(newState);
        }

        public void SelectMainMenu() {
            mSelectedState.Deactivated += new Action<IState>(mSelectedState_Deactivated);
            mSelectedState.Deactivate();
        }

        private void mSelectedState_Deactivated(IState state) {
            mMaximising = true;
            mCurrentStep = 0;
            mSelectedState.Deactivated -= new Action<IState>(mSelectedState_Deactivated);
        }


        #endregion

        #region OverlayState Members


        public bool Active {
            get { return mMenuActive; }
            set { mMenuActive = value; }
        }

        public void Init(IOverlay coordinator) { }

        public event Action MainMenuSelected;

        #endregion
    }
}
