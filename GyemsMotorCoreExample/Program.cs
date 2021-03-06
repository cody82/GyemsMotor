﻿using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using Gyems;
using CanInterface;

namespace GyemsMotorCoreExample
{
    class Program
    {

        static void Main(string[] args)
        {
            //ICanInterface port = new UsbCanAnalyzer.UsbCan("COM3");
            ICanInterface port = new PeakCan.PCanDevice();
            port.Open(CanInterface.Baudrate.Baudrate1000000);
            var motor = new GyemsMotor(port, 0x141);

            port.ReceiveFrame(false);
            motor.ReadAcceleration();
            var pid = motor.ReadPID();
            pid.Print();
            pid.SpeedP = 10;
            pid.SpeedI = 3;
            motor.WritePID(pid);

            int speed = 0;
            float current = 0;

            while (true)
            {
                motor.UpdateStatus();
                while (Console.KeyAvailable)
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case 'w':
                            speed += 1;
                            current = Math.Clamp(current + 0.001f, -1, 1);
                            break;
                        case 's':
                            speed -= 1;
                            current = Math.Clamp(current - 0.001f, -1, 1);
                            break;
                        case 'x':
                            speed = 0;
                            current = 0;
                            break;
                        case 'q':
                            goto exit;
                    }
                }
                Console.WriteLine("Current: {0}, Velocity: {1}", speed, (int)(motor.Rpm * 60 * Math.PI * 0.065 * 0.001));
                motor.Current(current);
                //motor.Speed(speed);
                Thread.Sleep(100);
            }
        exit:

            motor.Off();
        }
    }
}
