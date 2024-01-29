﻿using System.Threading.Tasks;
using Windows.Storage;

namespace x86Emulator.ATADevice
{
    public abstract class ATADrive
    {
        public byte Error { get; set; }
        public byte SectorCount { get; set; }
        public byte SectorNumber { get; set; }
        public byte CylinderLow { get; set; }
        public byte CylinderHigh { get; set; }
        public byte DriveHead { get; set; }
        public DeviceStatus Status { get; set; }
        protected ushort[] sectorBuffer;
        protected int bufferIndex;

        public ushort Cylinder
        {
            get
            {
                return (ushort)((CylinderHigh << 8) + CylinderLow);
            }
            set
            {
                CylinderLow = (byte)value;
                CylinderHigh = (byte)(value >> 8);
            }
        }

        public ushort SectorBuffer
        {
            get
            {
                ushort ret = sectorBuffer[bufferIndex++];

                if (Cylinder > 0 && (bufferIndex * 2) >= Cylinder)
                {
                    Status &= ~DeviceStatus.DataRequest;
                    FinishRead();

                    Cylinder = (ushort)((sectorBuffer.Length - bufferIndex) * 2);
                }

                if (bufferIndex >= sectorBuffer.Length)
                {
                    Status &= ~DeviceStatus.DataRequest;
                    FinishRead();
                }

                return ret;
            }
            set
            {
                sectorBuffer[bufferIndex++] = value;

                if (bufferIndex >= sectorBuffer.Length)
                {
                    Status &= ~DeviceStatus.DataRequest;
                    FinishCommand();
                }
            }
        }

        public abstract Task LoadImage(StorageFile filename);
        public abstract void Reset();
        public abstract void RunCommand(byte command);
        public abstract void FinishCommand();
        public abstract void FinishRead();
    }
}
