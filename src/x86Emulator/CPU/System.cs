﻿using x86Disasm;

namespace x86Emulator.CPU
{
    public partial class CPU
    {
        [CPUFunction(OpCode = 0xf4)]
        public void Halt()
        {
            Halted = true;
        }

        [CPUFunction(OpCode = 0x90)]
        public void Nop()
        {
            // Litterly does nothing ;)
        }

        [CPUFunction(OpCode = 0x0f0103)]
        public void LoadIDT(Operand dest)
        {
            if (opSize == 16)
            {
                idtRegister.Limit = SegReadWord(dest.Memory.Segment, dest.Memory.Address);
                idtRegister.Base = SegReadDWord(dest.Memory.Segment, dest.Memory.Address + 2) & 0x00ffffff;
            }
            else
            {
                idtRegister.Limit = SegReadWord(dest.Memory.Segment, dest.Memory.Address);
                idtRegister.Base = SegReadDWord(dest.Memory.Segment, dest.Memory.Address + 2);
            }
        }

        [CPUFunction(OpCode = 0x0f0101)]
        public void StoreIDT(Operand dest)
        {
            if (opSize == 16)
            {
                SegWriteWord(dest.Memory.Segment, dest.Memory.Address, idtRegister.Limit);
                SegWriteDWord(dest.Memory.Segment, dest.Memory.Address + 2, idtRegister.Base & 0x00ffffff);
            }
            else
            {
                SegWriteWord(dest.Memory.Segment, dest.Memory.Address, idtRegister.Limit);
                SegWriteDWord(dest.Memory.Segment, dest.Memory.Address + 2, idtRegister.Base);
            }
        }

        [CPUFunction(OpCode = 0x0f0102)]
        public void LoadGDT(Operand dest)
        {
            if (opSize == 16)
            {
                gdtRegister.Limit = SegReadWord(dest.Memory.Segment, dest.Memory.Address);
                gdtRegister.Base = SegReadDWord(dest.Memory.Segment, dest.Memory.Address + 2) & 0x00ffffff;
            }
            else
            {
                gdtRegister.Limit = SegReadWord(dest.Memory.Segment, dest.Memory.Address);
                gdtRegister.Base = SegReadDWord(dest.Memory.Segment, dest.Memory.Address + 2);
            }
        }

        [CPUFunction(OpCode = 0x0f0100)]
        public void StoreGDT(Operand dest)
        {
            if (opSize == 16)
            {
                SegWriteWord(dest.Memory.Segment, dest.Memory.Address, gdtRegister.Limit);
                SegWriteDWord(dest.Memory.Segment, dest.Memory.Address + 2, gdtRegister.Base & 0x00ffffff);
            }
            else
            {
                SegWriteWord(dest.Memory.Segment, dest.Memory.Address, gdtRegister.Limit);
                SegWriteDWord(dest.Memory.Segment, dest.Memory.Address + 2, gdtRegister.Base);
            }
        }

        [CPUFunction(OpCode = 0x0f0002)]
        public void LoadLDT(Operand dest)
        {
        }

        [CPUFunction(OpCode = 0x0f0003)]
        public void LoadTaskRegister(Operand dest)
        {
        }

        [CPUFunction(OpCode = 0x0f09)]
        public void WriteBackInvalidate()
        {
            // ?!?!?!
        }

        [CPUFunction(OpCode = 0x0f0104)]
        public void StoreMachineStatusWord(Operand dest)
        {
            dest.Value = (ushort)CR0;
            WriteOperand(dest);
        }

        [CPUFunction(OpCode = 0x0f0106)]
        public void LoadMachineStatusWord(Operand dest)
        {
            CR0 = (CR0 & 0xffff0000) + dest.Value;
        }
    }
}
