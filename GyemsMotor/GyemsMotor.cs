using System;
using System.Collections.Generic;
using System.Text;
using CanInterface;

namespace Gyems
{
    public struct PID
    {
        public byte PositionP;
        public byte PositionI;
        public byte SpeedP;
        public byte SpeedI;
        public byte TorqueP;
        public byte TorqueI;

        public void Print()
        {
            Console.WriteLine("Pos P: {0}", PositionP);
            Console.WriteLine("Pos I: {0}", PositionI);
            Console.WriteLine("Speed P: {0}", SpeedP);
            Console.WriteLine("Speed I: {0}", SpeedI);
            Console.WriteLine("Torque P: {0}", TorqueP);
            Console.WriteLine("Torque I: {0}", TorqueI);
        }
    }

    public class GyemsMotor
    {
        ICanInterface Can;
        int CanId;

        public float Rpm;
        public float Voltage;
        public int Temperature;

        public PID PID;

        public GyemsMotor(ICanInterface can, int can_id)
        {
            Can = can;
            CanId = can_id;
        }

        public void Off()
        {
            Can.SendFrame(
                new Frame() { Id = CanId, Data = new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0 } }
                );
            Can.ReceiveFrame();
        }

        /*
        Defaults(RMD-L-5005 35T):
        Pos P: 100
        Pos I: 100
        Speed P: 40
        Speed I: 14
        Torque P: 30
        Torque I: 30 
        */
        public PID ReadPID()
        {
            PID pid;

            Can.SendFrame(
                new Frame() { Id = CanId, Data = new byte[] { 0x30, 0, 0, 0, 0, 0, 0, 0 } }
                );
            var reply = Can.ReceiveFrame();

            pid.PositionP = reply.Data[2];
            pid.PositionI = reply.Data[3];
            pid.SpeedP = reply.Data[4];
            pid.SpeedI = reply.Data[5];
            pid.TorqueP = reply.Data[6];
            pid.TorqueI = reply.Data[7];

            return pid;
        }

        public void WritePID(PID pid, bool rom = false)
        {
            Can.SendFrame(
                new Frame() { Id = CanId, Data = new byte[] { 0x31, 0, 
                    pid.PositionP, pid.PositionI,
                    pid.SpeedP, pid.SpeedI,
                    pid.TorqueP, pid.TorqueI
                } }
                );
            var reply = Can.ReceiveFrame();
        }

        public void ReadAcceleration()
        {
            Can.SendFrame(
                new Frame() { Id = CanId, Data = new byte[] { 0x33, 0, 0, 0, 0, 0, 0, 0 } }
                );
            var reply = Can.ReceiveFrame();
            int accel = BitConverter.ToInt32(reply.Data, 4);
            Console.WriteLine("Acceleration: {0}", accel);
        }

        public void Current(float current)
        {
            int value = Math.Clamp((int)(current * 2000.0f), -2000, 2000);
            var bytes = BitConverter.GetBytes((short)value);
            Console.WriteLine("{0} {1}", bytes[0], bytes[1]);
            Can.SendFrame(
                new Frame()
                {
                    Id = CanId,
                    Data = new byte[] { 0xA1, 0, 0, 0,
                    bytes[0],
                    bytes[1],
                    0, 0 }
                }
                );
            Can.ReceiveFrame();
        }

        public void Speed(float rpm)
        {
            int value = Math.Clamp((int)(rpm * 360.0f / 60.0f * 100), int.MinValue, int.MaxValue);
            var bytes = BitConverter.GetBytes(value);
            Can.SendFrame(
                new Frame()
                {
                    Id = CanId,
                    Data = new byte[] { 0xA2, 0, 0, 0,
                        bytes[0],
                        bytes[1],
                        bytes[2],
                        bytes[3]
                    }
                }
                );
            Can.ReceiveFrame();
        }

        public void UpdateStatus()
        {
            Can.SendFrame(
                new Frame() { Id = CanId, Data = new byte[] { 0x9C, 0, 0, 0, 0, 0, 0, 0 } }
                );
            var status = Can.ReceiveFrame();

            var dps = BitConverter.ToInt16(status.Data, 4);
            Rpm = ((float)dps / 360 * 60);


            Can.SendFrame(
                new Frame() { Id = CanId, Data = new byte[] { 0x9A, 0, 0, 0, 0, 0, 0, 0 } }
                );
            var status2 = Can.ReceiveFrame();
            Voltage = (float)BitConverter.ToUInt16(status2.Data, 3) * 0.1f;
            Temperature = status2.Data[1];

            Console.WriteLine(Rpm + "rpm, " + Voltage + "V, " + Temperature + "C");

        }
    }

}
