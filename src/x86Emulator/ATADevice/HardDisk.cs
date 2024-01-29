﻿using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Text;
using Windows.Storage;
using System.Threading.Tasks;
using x86Emulator.Configuration;
using Windows.Storage.Streams;

namespace x86Emulator.ATADevice
{
    public class HardDisk : ATADrive
    {
        private Stream stream;
        private IRandomAccessStream fileopen;
        private BinaryReader reader;
        private Footer footer;
        private DiskHeader header;
        private ushort[] identifyBuffer;
        private byte lastCommand;

        public ushort Cylinders
        {
            get { return footer.Cylinders; }
        }

        public byte Heads
        {
            get { return footer.Heads; }
        }

        public byte Sectors
        {
            get { return footer.SectorsPerCylinder; }
        }

        public HardDisk()
        {
            identifyBuffer = new ushort[256];

            identifyBuffer[0] = 0x40;   // Fixed drive
            identifyBuffer[5] = 512;    // Bytes per sector
            Util.ByteArrayToUShort(Encoding.ASCII.GetBytes("12345678901234567890"), identifyBuffer, 10, true);
            Util.ByteArrayToUShort(Encoding.ASCII.GetBytes("x86 XS Virtual Hard Drive             "), identifyBuffer, 27, true);

            identifyBuffer[47] = 0x0010; // Max number of sectors
            identifyBuffer[48] = 0x0;    // double word i/o supported (disable for now)
            identifyBuffer[49] = 0x0300; // LBA and DMA supported
            identifyBuffer[51] = 0x0200; // Timing mode
            identifyBuffer[52] = 0x0200; // Timing mode
            identifyBuffer[53] = 0x0007;
            identifyBuffer[63] = 0x0007;
            identifyBuffer[65] = 0x0078;
            identifyBuffer[66] = 0x0078;
            identifyBuffer[67] = 0x0078;
            identifyBuffer[68] = 0x0078;
            identifyBuffer[80] = 0x007e; // ATA 6
            identifyBuffer[82] = 0x4000;
            identifyBuffer[83] = 0x7400;
            identifyBuffer[84] = 0x4000;
            identifyBuffer[85] = 0x4000;
            identifyBuffer[86] = 0x7400;
            identifyBuffer[87] = 0x4000;
            identifyBuffer[88] = 0x003f;
            identifyBuffer[93] = 0x6001;
            identifyBuffer[100] = 0x9c90;
            identifyBuffer[101] = 0x000f;
            identifyBuffer[102] = 0x0000;
        }

        public async Task UnMountImage()
        {
            try
            {
                if (stream != null)
                {
                    stream.Dispose();
                    fileopen.Dispose();
                    stream = null;
                    fileopen = null;
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {

            }
        }
        public override async Task LoadImage(StorageFile filename)
        {
            byte[] buffer;

            if (filename == null)
            {
                Helpers.Logger($"Failed loading image {filename.Name}");
                return;
            }

            await UnMountImage();

            fileopen = await filename.OpenAsync(FileAccessMode.ReadWrite);
            stream = fileopen.AsStream();
            reader = new BinaryReader(stream);
            stream.Seek(-512, SeekOrigin.End);
            buffer = reader.ReadBytes(512);

            footer = Util.ByteArrayToStructureBigEndian<Footer>(buffer);

            stream.Seek(512, SeekOrigin.Begin);
            buffer = reader.ReadBytes(1024);

            header = Util.ByteArrayToStructureBigEndian<DiskHeader>(buffer);

            stream.Seek(0, SeekOrigin.Begin);

            identifyBuffer[1] = identifyBuffer[54] = footer.Cylinders;
            identifyBuffer[3] = identifyBuffer[55] = footer.Heads;
            identifyBuffer[4] = (ushort)(footer.SectorsPerCylinder * 512);
            identifyBuffer[6] = identifyBuffer[56] = footer.SectorsPerCylinder;
            identifyBuffer[57] = identifyBuffer[60] = (ushort)(footer.CurrentSize / 512);
            identifyBuffer[58] = identifyBuffer[61] = (ushort)((footer.CurrentSize / 512) >> 16);
        }

        public override void Reset()
        {
            Error = 1;
            SectorNumber = 1;
            SectorCount = 1;
            CylinderLow = 0;
            CylinderHigh = 0;
            Status |= DeviceStatus.Busy | DeviceStatus.SeekComplete;
        }

        private byte[] ReadSector(long sector)
        {
            SystemConfig.IO_HDDCall();
            long blockNumber = sector / (header.BlockSize / 512);
            uint blockOffset;
            long sectorInBlock;
            byte[] bitmap;

            stream.Seek((long)((long)header.TableOffset + (blockNumber * 4)), SeekOrigin.Begin);
            blockOffset = Util.SwapByteOrder(reader.ReadUInt32());

            if (blockOffset == 0xffffffff)
                return new byte[512];

            stream.Seek(blockOffset * 512, SeekOrigin.Begin);

            bitmap = reader.ReadBytes((int)(header.BlockSize / 512 / 8));
            byte bitmapByte = bitmap[sector / 8];
            byte offset = (byte)(sector % 8);

            if ((bitmapByte & (1 << (7 - offset))) == 0)
                return new byte[512];

            sectorInBlock = sector % (header.BlockSize / 512);
            stream.Seek(sectorInBlock * 512, SeekOrigin.Current);

            return reader.ReadBytes(512);
        }

        private void Read()
        {
            int addr = (Cylinder * footer.Heads + (DriveHead & 0x0f)) * footer.SectorsPerCylinder + (SectorNumber - 1);
            sectorBuffer = new ushort[(SectorCount * 512) / 2];

            for (int i = 0; i < SectorCount; i++)
            {
                Util.ByteArrayToUShort(ReadSector(addr + i), sectorBuffer, i * 256);
            }
        }

        private void WriteSector(long sector, byte[] data)
        {
            SystemConfig.IO_HDDCall();
            long blockNumber = sector / (header.BlockSize / 512);
            uint blockOffset;
            long sectorInBlock;
            byte[] bitmap;
            BinaryWriter writer = new BinaryWriter(stream);

            stream.Seek((long)((long)header.TableOffset + (blockNumber * 4)), SeekOrigin.Begin);
            blockOffset = Util.SwapByteOrder(reader.ReadUInt32());

            if (blockOffset == 0xffffffff)
            {
                // Create new block
                byte[] oldFooter;
                byte[] newBlock = new byte[header.BlockSize];
                byte[] newBitmap = new byte[header.BlockSize / 512 / 8];
                long offsetPosition;

                stream.Seek(-512, SeekOrigin.End);
                offsetPosition = stream.Position;
                oldFooter = reader.ReadBytes(512);
                stream.Seek(-512, SeekOrigin.End);
                writer.Write(newBitmap);
                writer.Write(newBlock);
                writer.Write(oldFooter);

                stream.Seek((long)((long)header.TableOffset + (blockNumber * 4)), SeekOrigin.Begin);
                blockOffset = (uint)(offsetPosition / 512);
                writer.Write(Util.SwapByteOrder(blockOffset));
            }

            stream.Seek(blockOffset * 512, SeekOrigin.Begin);

            bitmap = reader.ReadBytes((int)(header.BlockSize / 512 / 8));
            bitmap[sector / 8] |= (byte)(1 << (byte)(7 - (sector % 8)));
            stream.Seek(blockOffset * 512, SeekOrigin.Begin);
            writer.Write(bitmap);

            sectorInBlock = sector % (header.BlockSize / 512);
            stream.Seek(sectorInBlock * 512, SeekOrigin.Current);
            writer.Write(data);
        }

        private void Write()
        {
            int addr = (Cylinder * footer.Heads + (DriveHead & 0x0f)) * footer.SectorsPerCylinder + (SectorNumber - 1);

            for (int i = 0; i < SectorCount; i++)
            {
                byte[] sector = new byte[512];

                Util.UShortArrayToByte(sectorBuffer, sector, i * 256);
                WriteSector(addr + i, sector);
            }

            stream.Flush();
        }

        public override void FinishCommand()
        {
            switch (lastCommand)
            {
                case 0x30:
                    Write();
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }

        }

        public override void RunCommand(byte command)
        {
            Status |= DeviceStatus.Busy;
            switch (command)
            {
                case 0x20: // Read sector   
                    Read();
                    break;
                case 0x30: // Write Sector
                    sectorBuffer = new ushort[(SectorCount * 512) / 2];
                    break;
                case 0xec: // Identify
                    sectorBuffer = identifyBuffer;
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }
            Status |= DeviceStatus.DataRequest;
            Status &= ~DeviceStatus.Busy;
            bufferIndex = 0;
            lastCommand = command;
        }

        public override void FinishRead()
        {
            SystemConfig.IO_HDDCall();
        }
    }
}
