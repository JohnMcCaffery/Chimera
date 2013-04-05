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
using OpenMetaverse.Packets;
using OpenMetaverse;

namespace Chimera.OpenSim {
    /// <exclude/>
    public sealed class SetCameraPacket: Packet {
        /// <exclude/>
        public sealed class CameraBlock : PacketBlock {
            public Vector3 Position;
            public Vector3 PositionDelta;
            public Vector3 LookAt;
            public Vector3 LookAtDelta;
            public uint TickLength;
            public UUID Source;

            public override int Length {
                get {
                    //Matrix (4x4 floats (4bit) + 4 vector3s (3x floats (4bits)) + (1 xint) UUID
                    return (sizeof(float) * 3 * 4) + sizeof(int) + Source.GetBytes().Length;
                }
            }

            public CameraBlock() { }
            public CameraBlock(byte[] bytes, ref int i) {
                FromBytes(bytes, ref i);
            }

            public override void FromBytes(byte[] bytes, ref int i) {
                try {
                    Position.FromBytes(bytes, i); i += sizeof(float) * 3;
                    PositionDelta.FromBytes(bytes, i); i += sizeof(float) * 3;
                    LookAt.FromBytes(bytes, i); i += sizeof(float) * 3;
                    LookAtDelta.FromBytes(bytes, i); i += sizeof(float) * 3;

                    TickLength = Utils.BytesToUInt(bytes, i); i += sizeof(int);

                    Source.FromBytes(bytes, i);
                } catch (Exception) {
                    throw new MalformedDataException();
                }
            }

            public override void ToBytes(byte[] bytes, ref int i) {
                Position.ToBytes(bytes, i); i += sizeof(float) * 3;
                PositionDelta.ToBytes(bytes, i); i += sizeof(float) * 3;
                LookAt.ToBytes(bytes, i); i += sizeof(float) * 3;
                LookAtDelta.ToBytes(bytes, i); i += sizeof(float) * 3;

                Utils.UIntToBytes(TickLength, bytes, i); i += sizeof(int);

                Source.ToBytes(bytes, i);
            }
        }

        public override int Length {
            get {
                int length = 10;
                length += Camera.Length;
                return length;
            }
        }
        public CameraBlock Camera;

        public SetCameraPacket () {
            HasVariableBlocks = false;
            //Type = PacketType.SetFollowCamProperties;
            Header = new Header();
            Header.Frequency = PacketFrequency.Low;
            Header.ID = 427;
            Header.Reliable = true;
            Camera = new CameraBlock();
        }

        public SetCameraPacket(byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(bytes, ref i, ref packetEnd, null);
        }

        override public void FromBytes(byte[] bytes, ref int i, ref int packetEnd, byte[] zeroBuffer) {
            Header.FromBytes(bytes, ref i, ref packetEnd);
            if (Header.Zerocoded && zeroBuffer != null) {
                packetEnd = Helpers.ZeroDecode(bytes, packetEnd + 1, zeroBuffer) - 1;
                bytes = zeroBuffer;
            }
            Camera.FromBytes(bytes, ref i);
        }

        public SetCameraPacket(Header head, byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(head, bytes, ref i, ref packetEnd);
        }

        override public void FromBytes(Header header, byte[] bytes, ref int i, ref int packetEnd) {
            Header = header;
            Camera.FromBytes(bytes, ref i);
        }

        public override byte[] ToBytes() {
            int length = 10;
            length += Camera.Length;
            if (Header.AckList != null && Header.AckList.Length > 0) { length += Header.AckList.Length * 4 + 1; }
            byte[] bytes = new byte[length];
            int i = 0;
            Header.ToBytes(bytes, ref i);
            Camera.ToBytes(bytes, ref i);
            if (Header.AckList != null && Header.AckList.Length > 0) { Header.AcksToBytes(bytes, ref i); }
            return bytes;
        }

        public override byte[][] ToBytesMultiple() {
            return new byte[][] { ToBytes() };
        }
    }

    /// <exclude/>
    public sealed class ClearCameraPacket : Packet
    {
        /// <exclude/>
        public sealed class ObjectDataBlock : PacketBlock
        {
            public UUID ObjectID;

            public override int Length
            {
                get
                {
                    return 16;
                }
            }

            public ObjectDataBlock() { }
            public ObjectDataBlock(byte[] bytes, ref int i)
            {
                FromBytes(bytes, ref i);
            }

            public override void FromBytes(byte[] bytes, ref int i)
            {
                try
                {
                    ObjectID.FromBytes(bytes, i); i += 16;
                }
                catch (Exception)
                {
                    throw new MalformedDataException();
                }
            }

            public override void ToBytes(byte[] bytes, ref int i)
            {
                ObjectID.ToBytes(bytes, i); i += 16;
            }

        }

        public override int Length
        {
            get
            {
                int length = 10;
                length += ObjectData.Length;
                return length;
            }
        }
        public ObjectDataBlock ObjectData;

        public ClearCameraPacket()
        {
            HasVariableBlocks = false;
            Type = PacketType.ClearFollowCamProperties;
            Header = new Header();
            Header.Frequency = PacketFrequency.Low;
            Header.ID = 428;
            Header.Reliable = true;
            ObjectData = new ObjectDataBlock();
        }

        public ClearCameraPacket(UUID id)
            : this() {
            ObjectData.ObjectID = id;
        }

        public ClearCameraPacket(byte[] bytes, ref int i) : this()
        {
            int packetEnd = bytes.Length - 1;
            FromBytes(bytes, ref i, ref packetEnd, null);
        }

        override public void FromBytes(byte[] bytes, ref int i, ref int packetEnd, byte[] zeroBuffer)
        {
            Header.FromBytes(bytes, ref i, ref packetEnd);
            if (Header.Zerocoded && zeroBuffer != null)
            {
                packetEnd = Helpers.ZeroDecode(bytes, packetEnd + 1, zeroBuffer) - 1;
                bytes = zeroBuffer;
            }
            ObjectData.FromBytes(bytes, ref i);
        }

        public ClearCameraPacket(Header head, byte[] bytes, ref int i): this()
        {
            int packetEnd = bytes.Length - 1;
            FromBytes(head, bytes, ref i, ref packetEnd);
        }

        override public void FromBytes(Header header, byte[] bytes, ref int i, ref int packetEnd)
        {
            Header = header;
            ObjectData.FromBytes(bytes, ref i);
        }

        public override byte[] ToBytes()
        {
            int length = 10;
            length += ObjectData.Length;
            if (Header.AckList != null && Header.AckList.Length > 0) { length += Header.AckList.Length * 4 + 1; }
            byte[] bytes = new byte[length];
            int i = 0;
            Header.ToBytes(bytes, ref i);
            ObjectData.ToBytes(bytes, ref i);
            if (Header.AckList != null && Header.AckList.Length > 0) { Header.AcksToBytes(bytes, ref i); }
            return bytes;
        }

        public override byte[][] ToBytesMultiple()
        {
            return new byte[][] { ToBytes() };
        }
    }







    /// <exclude/>
    public sealed class SetWindowPacket: Packet {
        /// <exclude/>
        public sealed class WindowBlock : PacketBlock {
            public Matrix4 ProjectionMatrix;

            public override int Length {
                get {
                    //Matrix (4x4 floats (4bit) + 4 vector3s (3x floats (4bits)) + (1 xint) UUID
                    return (sizeof(float) * 16);
                }
            }

            public WindowBlock() { }
            public WindowBlock(byte[] bytes, ref int i) {
                FromBytes(bytes, ref i);
            }

            public override void FromBytes(byte[] bytes, ref int i) {
                try {
                    Vector4 r1 = new Vector4(), r2 = new Vector4(), r3 = new Vector4(), r4 = new Vector4();
                    r1.FromBytes(bytes, i); i += sizeof(float) * 4;
                    r2.FromBytes(bytes, i); i += sizeof(float) * 4;
                    r3.FromBytes(bytes, i); i += sizeof(float) * 4;
                    r4.FromBytes(bytes, i); i += sizeof(float) * 4;

                    ProjectionMatrix.M11 = r1.X;
                    ProjectionMatrix.M12 = r1.Y;
                    ProjectionMatrix.M13 = r1.Z;
                    ProjectionMatrix.M14 = r1.W;

                    ProjectionMatrix.M21 = r2.X;
                    ProjectionMatrix.M22 = r2.Y;
                    ProjectionMatrix.M23 = r2.Z;
                    ProjectionMatrix.M24 = r2.W;

                    ProjectionMatrix.M31 = r3.X;
                    ProjectionMatrix.M32 = r3.Y;
                    ProjectionMatrix.M33 = r3.Z;
                    ProjectionMatrix.M34 = r3.W;

                    ProjectionMatrix.M41 = r4.X;
                    ProjectionMatrix.M42 = r4.Y;
                    ProjectionMatrix.M43 = r4.Z;
                    ProjectionMatrix.M44 = r4.W;
                } catch (Exception) {
                    throw new MalformedDataException();
                }
            }

            public override void ToBytes(byte[] bytes, ref int i) {
                //Vector4 r1;
                //ProjectionMatrix.UpAxis
                Utils.FloatToBytes(ProjectionMatrix.M11, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M12, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M13, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M14, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M21, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M22, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M23, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M34, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M31, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M32, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M33, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M34, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M41, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M42, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M43, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(ProjectionMatrix.M44, bytes, i); i += sizeof(float);
            }
        }

        public override int Length {
            get {
                int length = 10;
                length += Window.Length;
                return length;
            }
        }
        public WindowBlock Window;

        public SetWindowPacket () {
            HasVariableBlocks = false;
            //Type = PacketType.SetFollowCamProperties;
            Header = new Header();
            Header.Frequency = PacketFrequency.Low;
            Header.ID = 429;
            Header.Reliable = true;
            Window = new WindowBlock();
        }

        public SetWindowPacket(byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(bytes, ref i, ref packetEnd, null);
        }
        public SetWindowPacket(Matrix4 matrix)
            : this() {

            Window.ProjectionMatrix = matrix;
        }

        override public void FromBytes(byte[] bytes, ref int i, ref int packetEnd, byte[] zeroBuffer) {
            Header.FromBytes(bytes, ref i, ref packetEnd);
            if (Header.Zerocoded && zeroBuffer != null) {
                packetEnd = Helpers.ZeroDecode(bytes, packetEnd + 1, zeroBuffer) - 1;
                bytes = zeroBuffer;
            }
            Window.FromBytes(bytes, ref i);
        }

        public SetWindowPacket(Header head, byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(head, bytes, ref i, ref packetEnd);
        }

        override public void FromBytes(Header header, byte[] bytes, ref int i, ref int packetEnd) {
            Header = header;
            Window.FromBytes(bytes, ref i);
        }

        public override byte[] ToBytes() {
            int length = 10;
            length += Window.Length;
            if (Header.AckList != null && Header.AckList.Length > 0) { length += Header.AckList.Length * 4 + 1; }
            byte[] bytes = new byte[length];
            int i = 0;
            Header.ToBytes(bytes, ref i);
            Window.ToBytes(bytes, ref i);
            if (Header.AckList != null && Header.AckList.Length > 0) { Header.AcksToBytes(bytes, ref i); }
            return bytes;
        }

        public override byte[][] ToBytesMultiple() {
            return new byte[][] { ToBytes() };
        }
    }

    /// <exclude/>
    public sealed class ClearWindowPacket : Packet
    {
        /// <exclude/>
        public sealed class ObjectDataBlock : PacketBlock
        {
            public UUID ObjectID;

            public override int Length
            {
                get
                {
                    return 16;
                }
            }

            public ObjectDataBlock() { }
            public ObjectDataBlock(byte[] bytes, ref int i)
            {
                FromBytes(bytes, ref i);
            }

            public override void FromBytes(byte[] bytes, ref int i)
            {
                try
                {
                    ObjectID.FromBytes(bytes, i); i += 16;
                }
                catch (Exception)
                {
                    throw new MalformedDataException();
                }
            }

            public override void ToBytes(byte[] bytes, ref int i)
            {
                ObjectID.ToBytes(bytes, i); i += 16;
            }

        }

        public override int Length
        {
            get
            {
                int length = 10;
                length += ObjectData.Length;
                return length;
            }
        }
        public ObjectDataBlock ObjectData;

        public ClearWindowPacket()
        {
            HasVariableBlocks = false;
            Type = PacketType.ClearFollowCamProperties;
            Header = new Header();
            Header.Frequency = PacketFrequency.Low;
            Header.ID = 430;
            Header.Reliable = true;
            ObjectData = new ObjectDataBlock();
        }

        public ClearWindowPacket(UUID id)
            : this() {
            ObjectData.ObjectID = id;
        }

        public ClearWindowPacket(byte[] bytes, ref int i) : this()
        {
            int packetEnd = bytes.Length - 1;
            FromBytes(bytes, ref i, ref packetEnd, null);
        }

        override public void FromBytes(byte[] bytes, ref int i, ref int packetEnd, byte[] zeroBuffer)
        {
            Header.FromBytes(bytes, ref i, ref packetEnd);
            if (Header.Zerocoded && zeroBuffer != null)
            {
                packetEnd = Helpers.ZeroDecode(bytes, packetEnd + 1, zeroBuffer) - 1;
                bytes = zeroBuffer;
            }
            ObjectData.FromBytes(bytes, ref i);
        }

        public ClearWindowPacket(Header head, byte[] bytes, ref int i): this()
        {
            int packetEnd = bytes.Length - 1;
            FromBytes(head, bytes, ref i, ref packetEnd);
        }

        override public void FromBytes(Header header, byte[] bytes, ref int i, ref int packetEnd)
        {
            Header = header;
            ObjectData.FromBytes(bytes, ref i);
        }

        public override byte[] ToBytes()
        {
            int length = 10;
            length += ObjectData.Length;
            if (Header.AckList != null && Header.AckList.Length > 0) { length += Header.AckList.Length * 4 + 1; }
            byte[] bytes = new byte[length];
            int i = 0;
            Header.ToBytes(bytes, ref i);
            ObjectData.ToBytes(bytes, ref i);
            if (Header.AckList != null && Header.AckList.Length > 0) { Header.AcksToBytes(bytes, ref i); }
            return bytes;
        }

        public override byte[][] ToBytesMultiple()
        {
            return new byte[][] { ToBytes() };
        }
    }























    /// <exclude/>
    public sealed class RemoteControlPacket : Packet {
        /// <exclude/>
        public sealed class DeltaBlock : PacketBlock {
            public Vector3 Position;
            public float Pitch;
            public float Yaw;

            public override int Length {
                get {
                    return (sizeof(float) * 5);
                }
            }

            public DeltaBlock() { }
            public DeltaBlock(byte[] bytes, ref int i) {
                FromBytes(bytes, ref i);
            }

            public override void FromBytes(byte[] bytes, ref int i) {
                try {
                    Position.FromBytes(bytes, i); i += sizeof(float) * 3;

                    Pitch = Utils.BytesToFloat(bytes, i); i += sizeof(float);
                    Yaw = Utils.BytesToFloat(bytes, i); i += sizeof(float);
                } catch (Exception) {
                    throw new MalformedDataException();
                }
            }

            public override void ToBytes(byte[] bytes, ref int i) {
                Position.ToBytes(bytes, i); i += sizeof(float) * 3;

                Utils.FloatToBytes(Pitch, bytes, i); i += sizeof(float);
                Utils.FloatToBytes(Yaw, bytes, i); i += sizeof(float);
            }
        }

        public override int Length {
            get {
                int length = 10;
                length += Delta.Length;
                return length;
            }
        }
        public DeltaBlock Delta;

        public RemoteControlPacket() {
            HasVariableBlocks = false;
            //Type = PacketType.SetFollowCamProperties;
            Header = new Header();
            Header.Frequency = PacketFrequency.Low;
            Header.ID = 431;
            Header.Reliable = true;
            Delta = new DeltaBlock();
        }

        public RemoteControlPacket(byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(bytes, ref i, ref packetEnd, null);
        }

        override public void FromBytes(byte[] bytes, ref int i, ref int packetEnd, byte[] zeroBuffer) {
            Header.FromBytes(bytes, ref i, ref packetEnd);
            if (Header.Zerocoded && zeroBuffer != null) {
                packetEnd = Helpers.ZeroDecode(bytes, packetEnd + 1, zeroBuffer) - 1;
                bytes = zeroBuffer;
            }
            Delta.FromBytes(bytes, ref i);
        }

        public RemoteControlPacket(Header head, byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(head, bytes, ref i, ref packetEnd);
        }

        override public void FromBytes(Header header, byte[] bytes, ref int i, ref int packetEnd) {
            Header = header;
            Delta.FromBytes(bytes, ref i);
        }

        public override byte[] ToBytes() {
            int length = 10;
            length += Delta.Length;
            if (Header.AckList != null && Header.AckList.Length > 0) { length += Header.AckList.Length * 4 + 1; }
            byte[] bytes = new byte[length];
            int i = 0;
            Header.ToBytes(bytes, ref i);
            Delta.ToBytes(bytes, ref i);
            if (Header.AckList != null && Header.AckList.Length > 0) { Header.AcksToBytes(bytes, ref i); }
            return bytes;
        }

        public override byte[][] ToBytesMultiple() {
            return new byte[][] { ToBytes() };
        }
    }

    /// <exclude/>
    public sealed class ClearRemoteControlPacket : Packet {
        /// <exclude/>
        public sealed class ObjectDataBlock : PacketBlock {
            public UUID ObjectID;

            public override int Length {
                get {
                    return 16;
                }
            }

            public ObjectDataBlock() { }
            public ObjectDataBlock(byte[] bytes, ref int i) {
                FromBytes(bytes, ref i);
            }

            public override void FromBytes(byte[] bytes, ref int i) {
                try {
                    ObjectID.FromBytes(bytes, i); i += 16;
                } catch (Exception) {
                    throw new MalformedDataException();
                }
            }

            public override void ToBytes(byte[] bytes, ref int i) {
                ObjectID.ToBytes(bytes, i); i += 16;
            }

        }

        public override int Length {
            get {
                int length = 10;
                length += ObjectData.Length;
                return length;
            }
        }
        public ObjectDataBlock ObjectData;

        public ClearRemoteControlPacket() {
            HasVariableBlocks = false;
            Type = PacketType.ClearFollowCamProperties;
            Header = new Header();
            Header.Frequency = PacketFrequency.Low;
            Header.ID = 432;
            Header.Reliable = true;
            ObjectData = new ObjectDataBlock();
        }

        public ClearRemoteControlPacket(UUID id)
            : this() {
            ObjectData.ObjectID = id;
        }

        public ClearRemoteControlPacket(byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(bytes, ref i, ref packetEnd, null);
        }

        override public void FromBytes(byte[] bytes, ref int i, ref int packetEnd, byte[] zeroBuffer) {
            Header.FromBytes(bytes, ref i, ref packetEnd);
            if (Header.Zerocoded && zeroBuffer != null) {
                packetEnd = Helpers.ZeroDecode(bytes, packetEnd + 1, zeroBuffer) - 1;
                bytes = zeroBuffer;
            }
            ObjectData.FromBytes(bytes, ref i);
        }

        public ClearRemoteControlPacket(Header head, byte[] bytes, ref int i)
            : this() {
            int packetEnd = bytes.Length - 1;
            FromBytes(head, bytes, ref i, ref packetEnd);
        }

        override public void FromBytes(Header header, byte[] bytes, ref int i, ref int packetEnd) {
            Header = header;
            ObjectData.FromBytes(bytes, ref i);
        }

        public override byte[] ToBytes() {
            int length = 10;
            length += ObjectData.Length;
            if (Header.AckList != null && Header.AckList.Length > 0) { length += Header.AckList.Length * 4 + 1; }
            byte[] bytes = new byte[length];
            int i = 0;
            Header.ToBytes(bytes, ref i);
            ObjectData.ToBytes(bytes, ref i);
            if (Header.AckList != null && Header.AckList.Length > 0) { Header.AcksToBytes(bytes, ref i); }
            return bytes;
        }

        public override byte[][] ToBytesMultiple() {
            return new byte[][] { ToBytes() };
        }
    }
}