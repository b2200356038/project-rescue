using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace Game.Vehicle.Wheel
{
    [System.Flags]
    enum WheelVisualFlags : byte
    {
        None = 0,
        AngularVelocity = 1 << 0,  // 0x01
        SpringLength = 1 << 1,      // 0x02
        SteerAngle = 1 << 2,        // 0x04
        IsGrounded = 1 << 3,        // 0x08
    }

    public struct WheelVisualData : INetworkSerializable
    {
        public byte Flags;
        public short angularVelocity;    
        public byte springLength;     
        public sbyte steerAngle;    

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFlag(WheelVisualFlags flag, bool value)
        {
            if (value)
                Flags |= (byte)flag;
            else
                Flags &= (byte)~flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasFlag(WheelVisualFlags flag)
        {
            return (Flags & (byte)flag) != 0;
        }

        public void SetAngularVelocity(float value)
        {
            angularVelocity = (short)Mathf.Clamp(value * 100f, -32767, 32767);
            SetFlag(WheelVisualFlags.AngularVelocity, true);
        }

        public float GetAngularVelocity()
        {
            return angularVelocity / 100f;
        }

        public void SetSpringLength(float value)
        {
            springLength = (byte)Mathf.Clamp(value * 100f, 0, 255);
            SetFlag(WheelVisualFlags.SpringLength, true);
        }

        public float GetSpringLength()
        {
            return springLength / 100f;
        }

        public void SetSteerAngle(float value)
        {
            steerAngle = (sbyte)Mathf.Clamp(value, -127, 127);
            SetFlag(WheelVisualFlags.SteerAngle, true);
        }

        public float GetSteerAngle()
        {
            return steerAngle;
        }

        public void SetIsGrounded(bool value)
        {
            SetFlag(WheelVisualFlags.IsGrounded, value);
        }

        public bool GetIsGrounded()
        {
            return HasFlag(WheelVisualFlags.IsGrounded);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Flags);

            if (HasFlag(WheelVisualFlags.AngularVelocity))
            {
                serializer.SerializeValue(ref angularVelocity);
            }

            if (HasFlag(WheelVisualFlags.SpringLength))
            {
                serializer.SerializeValue(ref springLength);
            }

            if (HasFlag(WheelVisualFlags.SteerAngle))
            {
                serializer.SerializeValue(ref steerAngle); 
            }
        }
    }
}