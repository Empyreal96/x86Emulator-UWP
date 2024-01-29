using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using x86Emulator.Configuration;

namespace x86Emulator.Devices
{
    public class Floppy : IDevice, INeedsIRQ, INeedsDMA
    {
        private int IrqNumber = 6;
        private const int DmaChannel = 2;
        private readonly int[] portsUsed = { 0x3f0, 0x3f1, 0x3f2, 0x3f4, 0x3f5, 0x3f7 };

        private readonly byte[][] data = new byte[2][];

        private Stream[] floppyStream = new Stream[2];
        private BinaryReader[] floppyReader = new BinaryReader[2];
        private IRandomAccessStream[] fileopen = new IRandomAccessStream[2];
        private bool primarySelected = true;

        private DORSetting[] digitalOutput = new DORSetting[2];
        private MainStatus[] mainStatus = new MainStatus[2];
        private bool[] inCommand = new bool[2];
        private byte[] paramCount = new byte[2];
        private byte[] resultCount = new byte[2];
        private byte[] paramIdx = new byte[2];
        private byte[] resultIdx = new byte[2];
        private FloppyCommand[] command = new FloppyCommand[2];
        private byte[] statusZero = new byte[2];
        private byte[] headPosition = new byte[2];
        private byte[] currentCyl = new byte[2];
        private bool[] interruptInProgress = new bool[2];

        public event EventHandler IRQ;
        public event EventHandler<ByteArrayEventArgs> DMA;

        public int[] PortsUsed
        {
            get { return portsUsed; }
        }

        public int IRQNumber
        {
            get { return IrqNumber; }
        }

        public int DMAChannel
        {
            get { return DmaChannel; }
        }

        public Floppy()
        {
            mainStatus[0] = MainStatus.RQM;
            mainStatus[1] = MainStatus.RQM;
            data[0] = new byte[16];
            data[1] = new byte[16];
        }

        public void OnDMA(ByteArrayEventArgs e)
        {
            EventHandler<ByteArrayEventArgs> handler = DMA;
            if (handler != null)
                handler(this, e);
        }

        public void OnIRQ(EventArgs e)
        {
            EventHandler handler = IRQ;
            if (handler != null)
                handler(this, e);
        }

        private int GetFloppyIndex(FloppyType type)
        {
            int index = 0;
            if (type != FloppyType.PrimaryFloppy)
            {
                index = 1;
            }
            return index;
        }
        private int GetFloppyIndex()
        {
            int index = 0;
            if (!primarySelected)
            {
                index = 1;
            }
            return index;
        }
        public void UnMountImage(FloppyType type)
        {
            try
            {
                int index = GetFloppyIndex(type);

                if (floppyStream[index] != null)
                {
                    floppyStream[index].Dispose();
                    fileopen[index].Dispose();
                }
            }
            catch (Exception ex)
            {

            }
        }
        public async Task<bool> MountImage(StorageFile floppy, FloppyType type)
        {
            if (floppy == null)
                return false;

            UnMountImage(type);

            int index = GetFloppyIndex(type);

            try
            {
                fileopen[index] = await floppy.OpenAsync(FileAccessMode.ReadWrite);
                floppyStream[index] = fileopen[index].AsStream();
                floppyReader[index] = new BinaryReader(floppyStream[index]);
            }catch(Exception ex)
            {
                Helpers.Logger(ex);
                return false;
            }

            return true;
        }

        private void Reset()
        {
            Helpers.Logger("Reset issued");
            int index = GetFloppyIndex();
            digitalOutput[index] &= ~DORSetting.Reset;
            OnIRQ(new EventArgs());
        }

        private void ReadSector()
        {
            SystemConfig.IO_FloppyCall();
            int index = GetFloppyIndex();
            int addr = (data[index][1] * 2 + data[index][2]) * 18 + (data[index][3] - 1);
            int numSectors = data[index][5] - data[index][3] + 1;

            if (numSectors == -1)
                numSectors = 1;

            if (floppyStream[index] != null)
            {
                floppyStream[index].Seek(addr * 512, SeekOrigin.Begin);
                byte[] sector = floppyReader[index].ReadBytes(512 * numSectors);

                if (Helpers.DebugFile)
                    Helpers.Logger(String.Format("Reading {0} sectors from sector offset {1}", numSectors, addr));

                resultCount[index] = 7;
                resultIdx[index] = 0;
                data[index][0] = 0;
                data[index][1] = 0;
                data[index][2] = 0;
                data[index][3] = 0;
                data[index][4] = 0;
                data[index][5] = 0;
                data[index][6] = 0;

                OnDMA(new ByteArrayEventArgs(sector));
            }
            mainStatus[index] |= MainStatus.DIO;
            statusZero[index] = 0;

            OnIRQ(new EventArgs());
        }
        private void Seek()
        {
            SystemConfig.IO_FloppyCall();
            int index = GetFloppyIndex();
            int addr = (data[index][1] * 2 + data[index][2]) * 18 + (data[index][3] - 1);
            int numSectors = data[index][5] - data[index][3] + 1;
            if (floppyStream[index] != null)
            {
                floppyStream[index].Seek(addr * 512, SeekOrigin.Begin);

                if (Helpers.DebugFile)
                    Helpers.Logger(String.Format("Seek {0} sectors from sector offset {1}", numSectors, addr));

                resultCount[index] = 7;
                resultIdx[index] = 0;
                data[index][0] = 0;
                data[index][1] = 0;
                data[index][2] = 0;
                data[index][3] = 0;
                data[index][4] = 0;
                data[index][5] = 0;
                data[index][6] = 0;
            }
            mainStatus[index] |= MainStatus.DIO;
            statusZero[index] = 0;

            OnIRQ(new EventArgs());
        }

        private void RunCommand()
        {
            int index = GetFloppyIndex();
            switch (command[index])
            {
                case FloppyCommand.Recalibrate:
                    Helpers.Logger("Recalibrate issued");
                   
                    if (floppyReader[index] != null)
                    {
                        floppyStream[index].Seek(0, SeekOrigin.Begin);
                    }
                    headPosition[index] = 0;
                    currentCyl[index] = 0;
                    statusZero[index] = 0x20;
                    interruptInProgress[index] = true;
                    OnIRQ(new EventArgs());
                    break;
                case FloppyCommand.SenseInterrupt:
                    Helpers.Logger("Sense interrupt isssued");
                    if (!interruptInProgress[index])
                        statusZero[index] = 0x80;
                    interruptInProgress[index] = false;
                    mainStatus[index] |= MainStatus.DIO;
                    resultIdx[index] = 0;
                    resultCount[index] = 2;
                    data[index][0] = statusZero[index];
                    data[index][1] = currentCyl[index];
                    break;
                case FloppyCommand.Seek:
                    Seek();
                    break;
                case FloppyCommand.ReadData:
                    ReadSector();
                    break;
                case FloppyCommand.WriteData:
                    resultCount[index] = 7;
                    resultIdx[index] = 0;
                    data[index][0] = 0;
                    data[index][1] = 0x2;
                    data[index][2] = 0;
                    data[index][3] = 0;
                    data[index][4] = 0;
                    data[index][5] = 0;
                    data[index][6] = 0;

                    mainStatus[index] |= MainStatus.DIO;
                    statusZero[index] = 0;

                    OnIRQ(new EventArgs());
                    break;

                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }
        }

        private void ProcessCommandAndArgs(ushort value)
        {
            int index = GetFloppyIndex();
            if (inCommand[index])
            {
                data[index][paramIdx[index]++] = (byte)value;
                if (paramIdx[index] == paramCount[index])
                {
                    RunCommand();
                    inCommand[index] = false;
                }
            }
            else
            {
                inCommand[index] = true;
                paramIdx[index] = 0;
                command[index] = (FloppyCommand)(value & 0x0f);
                switch (command[index])
                {
                    case FloppyCommand.Recalibrate:
                        paramCount[index] = 1;
                        break;
                    case FloppyCommand.SenseInterrupt:
                        paramCount[index] = 0;
                        RunCommand();
                        inCommand[index] = false;
                        break;
                    case FloppyCommand.Seek:
                        paramCount[index] = 8;
                        break;
                    case FloppyCommand.ReadData:
                        paramCount[index] = 8;
                        break;
                    case FloppyCommand.WriteData:
                        paramCount[index] = 8;
                        break;
                    default:
                        System.Diagnostics.Debugger.Break();
                        break;
                }
            }
        }

        #region IDevice Members

        public uint Read(ushort addr, int size)
        {
            SystemConfig.IO_FloppyCall();
            int index = GetFloppyIndex();
            switch (addr)
            {
                case 0x3f2:
                    return (ushort)digitalOutput[index];
                case 0x3f4:
                    return (ushort)mainStatus[index];
                case 0x3f5:
                    if (floppyReader[index] != null)
                    {
                        byte ret = data[index][resultIdx[index]++];
                        if (resultIdx[index] == resultCount[index])
                            mainStatus[index] &= ~MainStatus.DIO;
                        return ret;
                    }
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }
            return 0;
        }

        public void Write(ushort addr, uint value, int size)
        {
            SystemConfig.IO_FloppyCall();
            int index = GetFloppyIndex();

            switch (addr)
            {
                case 0x3f2:
                    if (((digitalOutput[index] & DORSetting.Reset) == 0) && (((DORSetting)value & DORSetting.Reset) == DORSetting.Reset))
                        Reset();

                    digitalOutput[index] = (DORSetting)value;
                    break;
                case 0x3f5:
                    ProcessCommandAndArgs((ushort)value);
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }
        }

        #endregion
    }

    [Flags]
    enum MainStatus
    {
        Drive0Busy = 0x1,
        Drive1Busy = 0x2,
        Drive2Busy = 0x4,
        Drive3Busy = 0x8,
        CommandBusy = 0x10,
        NonDMA = 0x20,
        DIO = 0x40,
        RQM = 0x80
    }

    [Flags]
    enum DORSetting
    {
        Drive = 0x1,
        Reset = 0x4,
        Dma = 0x8,
        Drive0Motor = 0x10,
        Drive1Motor = 0x20,
        Drive2Motor = 0x40,
        Drive3Motor = 0x80
    }

    enum FloppyCommand
    {
        ReadTrack = 2,
        SPECIFY = 3,
        SenseDriveStatus = 4,
        WriteData = 5,
        ReadData = 6,
        Recalibrate = 7,
        SenseInterrupt = 8,
        WriteDeletedData = 9,
        ReadID = 10,
        ReadDeletedData = 12,
        FormatTrack = 13,
        Seek = 15,
        Version = 16,
        ScanEqual = 17,
        PerpendicularMode = 18,
        Configure = 19,
        Lock = 20,
        Verify = 22,
        ScanLowOrEqual = 25,
        ScanHighOrEqual = 29
    };

    public enum FloppyType
    {
        PrimaryFloppy,
        SecondaryFloppy
    }
}