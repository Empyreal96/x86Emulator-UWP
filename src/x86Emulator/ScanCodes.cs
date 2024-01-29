using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace x86Emulator
{
    public static class ScanCodes
    {
        static Dictionary<VirtualKey[], uint> scanCodes = new Dictionary<VirtualKey[], uint>()
        {
            { new VirtualKey[] { VirtualKey.Insert }, 82 },
            { new VirtualKey[] { VirtualKey.Delete, VirtualKey.GamepadLeftShoulder }, 83 },
            { new VirtualKey[] { VirtualKey.End }, 79 },
            { new VirtualKey[] { VirtualKey.PageDown }, 81 },
            { new VirtualKey[] { VirtualKey.Clear }, 76 },
            { new VirtualKey[] { VirtualKey.Home }, 71 },
            { new VirtualKey[] { VirtualKey.PageUp }, 73 },
            { new VirtualKey[] { VirtualKey.Add }, 78 },
            { new VirtualKey[] { VirtualKey.Subtract }, 74 },
            { new VirtualKey[] { VirtualKey.Multiply }, 55 },
            { new VirtualKey[] { VirtualKey.Divide }, 53 },
            { new VirtualKey[] { VirtualKey.NumberKeyLock }, 69 },

            { new VirtualKey[] { (VirtualKey)192 }, 41 },
            { new VirtualKey[] { VirtualKey.Number1 }, 2 },
            { new VirtualKey[] { VirtualKey.Number2 }, 3 },
            { new VirtualKey[] { VirtualKey.Number3 }, 4 },
            { new VirtualKey[] { VirtualKey.Number4 }, 5 },
            { new VirtualKey[] { VirtualKey.Number5 }, 6 },
            { new VirtualKey[] { VirtualKey.Number6 }, 7 },
            { new VirtualKey[] { VirtualKey.Number7 }, 8 },
            { new VirtualKey[] { VirtualKey.Number8 }, 9 },
            { new VirtualKey[] { VirtualKey.Number9 }, 10 },
            { new VirtualKey[] { VirtualKey.Number0 }, 11 },
            { new VirtualKey[] { (VirtualKey)189 }, 12 },
            { new VirtualKey[] { (VirtualKey)187 }, 13 },
            { new VirtualKey[] { VirtualKey.Back, VirtualKey.GamepadRightShoulder }, 14 },
            { new VirtualKey[] { VirtualKey.Q }, 16 },
            { new VirtualKey[] { VirtualKey.W }, 17 },
            { new VirtualKey[] { VirtualKey.E }, 18 },
            { new VirtualKey[] { VirtualKey.R }, 19 },
            { new VirtualKey[] { VirtualKey.T }, 20 },
            { new VirtualKey[] { VirtualKey.Y }, 21 },
            { new VirtualKey[] { VirtualKey.U }, 22 },
            { new VirtualKey[] { VirtualKey.I }, 23 },
            { new VirtualKey[] { VirtualKey.O }, 24 },
            { new VirtualKey[] { VirtualKey.P }, 25 },
            { new VirtualKey[] { (VirtualKey)219 }, 26 },
            { new VirtualKey[] { (VirtualKey)221 }, 27 },
            { new VirtualKey[] { (VirtualKey)220 }, 43 },
            { new VirtualKey[] { VirtualKey.CapitalLock }, 58 },
            { new VirtualKey[] { VirtualKey.A }, 30 },
            { new VirtualKey[] { VirtualKey.S }, 31 },
            { new VirtualKey[] { VirtualKey.D }, 32 },
            { new VirtualKey[] { VirtualKey.F }, 33 },
            { new VirtualKey[] { VirtualKey.G }, 34 },
            { new VirtualKey[] { VirtualKey.H }, 35 },
            { new VirtualKey[] { VirtualKey.J }, 36 },
            { new VirtualKey[] { VirtualKey.K }, 37 },
            { new VirtualKey[] { VirtualKey.L }, 38 },
            { new VirtualKey[] { (VirtualKey)186 }, 39 },
            { new VirtualKey[] { (VirtualKey)222 }, 40 },
            { new VirtualKey[] { VirtualKey.Enter, VirtualKey.GamepadMenu }, 28 },
            { new VirtualKey[] { VirtualKey.Shift }, 42 },
            { new VirtualKey[] { VirtualKey.Z }, 44 },
            { new VirtualKey[] { VirtualKey.X }, 45 },
            { new VirtualKey[] { VirtualKey.C }, 46 },
            { new VirtualKey[] { VirtualKey.V }, 47 },
            { new VirtualKey[] { VirtualKey.B }, 48 },
            { new VirtualKey[] { VirtualKey.N }, 49 },
            { new VirtualKey[] { VirtualKey.M }, 50 },
            { new VirtualKey[] { (VirtualKey)188 }, 51 },
            { new VirtualKey[] { (VirtualKey)190 }, 52 },
            { new VirtualKey[] { (VirtualKey)191 }, 53 },
            { new VirtualKey[] { VirtualKey.LeftShift }, 42 },
            { new VirtualKey[] { VirtualKey.RightShift }, 42 },
            { new VirtualKey[] { VirtualKey.Control }, 29 },
            { new VirtualKey[] { VirtualKey.LeftWindows }, 91 },
            { new VirtualKey[] { VirtualKey.RightWindows }, 91 },
            { new VirtualKey[] { VirtualKey.Space }, 57 },
            { new VirtualKey[] { VirtualKey.Control }, 29 },
            { new VirtualKey[] { VirtualKey.Left, VirtualKey.GamepadDPadLeft }, 75 },
            { new VirtualKey[] { VirtualKey.Up, VirtualKey.GamepadDPadUp }, 72 },
            { new VirtualKey[] { VirtualKey.Down, VirtualKey.GamepadDPadDown }, 80 },
            { new VirtualKey[] { VirtualKey.Right, VirtualKey.GamepadDPadRight }, 77 },

            { new VirtualKey[] { VirtualKey.Escape, VirtualKey.GamepadB }, 1 },
            { new VirtualKey[] { VirtualKey.F1 }, 59 },
            { new VirtualKey[] { VirtualKey.F2 }, 60 },
            { new VirtualKey[] { VirtualKey.F3, VirtualKey.GamepadX }, 61 },
            { new VirtualKey[] { VirtualKey.F4 }, 62 },
            { new VirtualKey[] { VirtualKey.F5 }, 63 },
            { new VirtualKey[] { VirtualKey.F6 }, 64 },
            { new VirtualKey[] { VirtualKey.F7 }, 65 },
            { new VirtualKey[] { VirtualKey.F8 }, 66 },
            { new VirtualKey[] { VirtualKey.F9 }, 67 },
            { new VirtualKey[] { VirtualKey.F11 }, 87 },
            { new VirtualKey[] { VirtualKey.F12, VirtualKey.GamepadView }, 88 },
        };

        public static uint GetScanCode(VirtualKey key)
        {
            uint scanCode = (uint)key;
            bool foundKey = false;
            foreach (var code in scanCodes.Keys)
            {
                foreach (var value in code)
                {
                    if (value == key)
                    {
                        scanCode = scanCodes[code];
                        foundKey = true;
                        break;
                    }
                }
                if (foundKey)
                {
                    break;
                }
            }

            return scanCode;
        }
    }
}
