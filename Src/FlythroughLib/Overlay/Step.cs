﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chimera.Overlay.Features;
using Chimera.Interfaces.Overlay;
using System.Xml;
using System.IO;
using Chimera.Overlay;
using Chimera.Interfaces;
using log4net;
using Chimera.Util;

namespace Chimera.Flythrough.Overlay {
    public class Step : XmlLoader {
        private static TickStatistics sStatistics = null;

        private readonly ILog Logger = LogManager.GetLogger("Flythrough");
        private readonly int mStep;
        private readonly int mSubtitleTimeoutS = 20;

        private readonly OverlayPlugin mManager;
        private readonly IMediaPlayer mPlayer;
        private readonly Text mSubtitlesText;
        private readonly Action mTickListener;
        private readonly string mVoiceoverFile;

        private readonly Dictionary<int, string> mSubtitles = new Dictionary<int, string>();
        private readonly List<IFeature> mFeatures = new List<IFeature>();

        private Queue<int> mSubtitleTimes;

        private DateTime mStarted;
        private DateTime mLastSubtitle;

        public int StepNum {
            get { return mStep; }
        }

        public Step(FlythroughState state, XmlNode node, Text subititlesText, int subtitleTimeoutS, IMediaPlayer player) {
            if (node.Attributes["Step"] == null && !int.TryParse(node.Attributes["Step"].Value, out mStep))
                throw new ArgumentException("Unable to load slideshow step. A valid 'Step' attribute must be supplied.");

            if (sStatistics == null) {
                sStatistics = new TickStatistics();
                StatisticsCollection.AddStatistics(sStatistics, "Flythrough Steps");
            }

            mPlayer = player;
            mManager = state.Manager;
            mStep = GetInt(node, -1, "Step");
            if (mStep == -1)
                throw new ArgumentException("Unable to load step ID. A valid Step attribute is expected.");

            XmlAttribute voiceoverAttribute = node.Attributes["Voiceover"];
            if (voiceoverAttribute != null && File.Exists(voiceoverAttribute.Value)) {
                if (mPlayer != null)
                    mVoiceoverFile = Path.GetFullPath(voiceoverAttribute.Value);
                else
                    Logger.Warn("Unable to load voiceover for flythrough step. No MediaPlayer supplied.");
            }

            mSubtitlesText = subititlesText;

            if (mSubtitlesText != null) {
                mTickListener = new Action(mCoordinator_Tick);
                mSubtitleTimeoutS = subtitleTimeoutS;
                XmlNode subtitlesNode = node.SelectSingleNode("child::Subtitles");
                if (subtitlesNode != null) {
                    foreach (XmlNode child in subtitlesNode.ChildNodes) {
                        if (child is XmlElement) {
                            int time = child.Attributes["Time"] != null ? int.Parse(child.Attributes["Time"].Value) : 0;
                            mSubtitles.Add(time, child.InnerText.Trim('\n', ' ', Environment.NewLine[0]).Replace("  ", ""));
                        }
                    }
                }
            }

            foreach (var featureNode in GetChildrenOfChild(node, "Features")) {
                IFeature feature = mManager.GetFeature(featureNode, "flythrough step " + (mStep + 1), null);
                if (feature != null) {
                    mFeatures.Add(feature);
                    state.AddFeature(feature);
                    feature.Active = false;
                }
            }
        }

        public void Start() {
            mSubtitleTimes = new Queue<int>(mSubtitles.Keys.OrderBy(i=>i));
            mLastSubtitle = DateTime.Now;

            if (mSubtitles.Count > 0) {
                mStarted = DateTime.Now;
                mManager.Core.Tick += mTickListener;
            }

            if (mVoiceoverFile != null)
                mPlayer.PlayAudio(mVoiceoverFile);

            foreach(var feature in mFeatures) {
                feature.Active = true;
                mManager[feature.Frame].ForceRedrawStatic();
            }

            //TODO - play voiceover file
        }

        public void Finish() {
            foreach(var feature in mFeatures) {
                feature.Active = false;
                mManager[feature.Frame].ForceRedrawStatic();
            }

            if (mVoiceoverFile != null)
                mPlayer.StopPlayback();

            if (mSubtitlesText != null)
                mSubtitlesText.TextString = "";
            mManager.Core.Tick -= mTickListener;
        }

        private void mCoordinator_Tick() {
            sStatistics.Begin();
            if (mSubtitleTimes.Count > 0 && DateTime.Now.Subtract(mStarted).TotalSeconds > mSubtitleTimes.Peek()) {
                mSubtitlesText.TextString = mSubtitles[mSubtitleTimes.Dequeue()];
                mLastSubtitle = DateTime.Now;
            } else if (DateTime.Now.Subtract(mLastSubtitle).TotalSeconds > mSubtitleTimeoutS && mSubtitlesText.TextString.Length > 0)
                mSubtitlesText.TextString = "";
            sStatistics.End();
        }

        internal void Prep() {
            foreach (var feature in mFeatures)
                feature.Active = mStep == 0;
        }
    }
}