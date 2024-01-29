using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace x86Emulator.Devices
{
    [Flags]
    public enum MouseFlags
    {
        OutputBufferFull = 0x1,
        InputBufferFull = 0x2,
        SystemFlag = 0x4,
        CommandFlag = 0x8,
        UnLocked = 0x10,
        MouseData = 0x20,
        Timeout = 0x40,
        ParityError = 0x80
    }

    public class MouseDevice : IDevice, INeedsIRQ
    {
        private int[] portsUsed = { 0x60, 0x64 };

        private bool enabled;
        private byte inputBuffer;
        private byte commandByte;
        private bool setCommandByte;
        private Queue<byte> outputBuffer;
        private MouseFlags statusRegister;

        private const int IrqNumber = 12;

        public event EventHandler IRQ;

        public int[] PortsUsed
        {
            get { return portsUsed; }
        }

        public int IRQNumber
        {
            get { return IrqNumber; }
        }

        public MouseDevice()
        {
            statusRegister |= MouseFlags.UnLocked;
            outputBuffer = new Queue<byte>();
        }

        public void Reset()
        {
            statusRegister |= MouseFlags.UnLocked;
            outputBuffer.Clear();
        }

        public void Update()
        {
            outputBuffer.Enqueue(0);
            statusRegister |= MouseFlags.OutputBufferFull;
            OnIRQ(new EventArgs());
        }

        private void OnIRQ(EventArgs e)
        {
            EventHandler handler = IRQ;
            if (handler != null)
                handler(this, e);
        }


        public void Cycle()
        {
            if (outputBuffer.Count != 0)
                OnIRQ(new EventArgs());
        }

        private void SetStatusCode(byte status)
        {
            statusRegister |= MouseFlags.OutputBufferFull;
            outputBuffer.Enqueue(status);
        }

        private void ProcessCommand()
        {
            if (setCommandByte)
            {
                SetStatusCode(0xfa);
                setCommandByte = false;
            }
            else
            {

                switch (inputBuffer)
                {
                    case 0x60:
                        setCommandByte = true;
                        break;
                    case 0xa8:
                        commandByte &= 0xDF;
                        break;
                    case 0xaa:
                        statusRegister |= MouseFlags.SystemFlag;
                        SetStatusCode(0x55);
                        break;
                    case 0xab:
                        SetStatusCode(0x00);
                        break;
                    case 0xae:
                        enabled = true;
                        break;
                    case 0xf5:
                        SetStatusCode(0xfa);
                        break;
                    case 0xff:
                        SetStatusCode(0xfa);
                        SetStatusCode(0xaa);
                        break;
                    default:
                        break;
                }
            }
        }

        public uint Read(ushort address, int size)
        {
            byte value = 0;
            switch (address)
            {
                case 0x60:
                    if (outputBuffer.Count != 0)
                    {
                        value = setCommandByte ? commandByte : outputBuffer.Dequeue();

                        if (outputBuffer.Count == 0)
                            statusRegister &= ~MouseFlags.OutputBufferFull;
                        setCommandByte = false;
                    }
                    break;
                case 0x64:
                    value = (byte)statusRegister;
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }

            return value;
        }

        public void Write(ushort address, uint value, int size)
        {
            switch (address)
            {
                case 0x60:
                    if (setCommandByte)
                        commandByte = (byte)value;
                    else
                        inputBuffer = (byte)value;

                    statusRegister &= ~MouseFlags.CommandFlag;
                    ProcessCommand();
                    break;
                case 0x64:
                    inputBuffer = (byte)value;
                    statusRegister |= MouseFlags.CommandFlag;
                    ProcessCommand();
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }
        }
    }
}
