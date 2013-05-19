﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse.Packets;
using OpenMetaverse;

namespace Chimera.OpenSim {
    internal class BackwardCompatibleController : ProxyControllerBase {
        internal BackwardCompatibleController(Window window)
            : base(window) {
        }
            SetFollowCamPropertiesPacket cameraPacket = new SetFollowCamPropertiesPacket();
            cameraPacket.CameraProperty = new SetFollowCamPropertiesPacket.CameraPropertyBlock[22];
            for (int i = 0; i < 22; i++) {
                cameraPacket.CameraProperty[i] = new SetFollowCamPropertiesPacket.CameraPropertyBlock();
                cameraPacket.CameraProperty[i].Type = i + 1;
            }

            Vector3 focus = Window.Coordinator.Position + Window.Coordinator.Orientation.LookAtVector;
            cameraPacket.CameraProperty[0].Value = 0;
            cameraPacket.CameraProperty[1].Value = 0f;
            cameraPacket.CameraProperty[2].Value = 0f;
            cameraPacket.CameraProperty[3].Value = 0f;
            cameraPacket.CameraProperty[4].Value = 0f;
            cameraPacket.CameraProperty[5].Value = 0f;
            cameraPacket.CameraProperty[6].Value = 0f;
            cameraPacket.CameraProperty[7].Value = 0f;
            cameraPacket.CameraProperty[8].Value = 0f;
            cameraPacket.CameraProperty[9].Value = 0f;
            cameraPacket.CameraProperty[10].Value = 0f;
            cameraPacket.CameraProperty[11].Value = enable ? 1f : 0f; //enable
            cameraPacket.CameraProperty[12].Value = 0f;
            cameraPacket.CameraProperty[13].Value = Window.Coordinator.Position.X;
            cameraPacket.CameraProperty[14].Value = Window.Coordinator.Position.Y;
            cameraPacket.CameraProperty[15].Value = Window.Coordinator.Position.Z;
            cameraPacket.CameraProperty[16].Value = 0f;
            cameraPacket.CameraProperty[17].Value = focus.X;
            cameraPacket.CameraProperty[18].Value = focus.Y;
            cameraPacket.CameraProperty[19].Value = focus.Z;
            cameraPacket.CameraProperty[20].Value = 1f;
            cameraPacket.CameraProperty[21].Value = 1f;
            return cameraPacket;
        }
        public override void SetCamera() {
            InjectPacket(MakePacket(true));
        }

        public override void SetCamera(OpenMetaverse.Vector3 positionDelta, Util.Rotation orientationDelta) {
            InjectPacket(MakePacket(true));
        }

        public override void SetFrustum(bool setPosition) {
            //throw new NotImplementedException();
        }

        public override void Move(OpenMetaverse.Vector3 positionDelta, Util.Rotation orientationDelta, float scale) {
            InjectPacket(MakePacket(true));
        }

        public override void ClearCamera() {
            //throw new NotImplementedException();
            InjectPacket(MakePacket(false));
        }

        public override void ClearFrustum() {
            //throw new NotImplementedException();
        }

        public override void ClearMovement() {
            //throw new NotImplementedException();
        }
    }
}