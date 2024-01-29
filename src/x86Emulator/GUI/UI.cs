using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using x86Emulator.Devices;

namespace x86Emulator.GUI
{
    public abstract class UI
    {
        public event EventHandler<UIntEventArgs> KeyDown;
        public event EventHandler<UIntEventArgs> KeyUp;

        protected VGA vgaDevice;

        public UI(VGA device)
        {
            vgaDevice = device;
        }

        public abstract void ResetScreen();

        public virtual void OnKeyDown(uint key)
        {
            EventHandler<UIntEventArgs> keyDown = KeyDown;
            if (keyDown != null)
                keyDown(this, new UIntEventArgs(key));
        }

        public virtual void OnKeyUp(uint key)
        {
            EventHandler<UIntEventArgs> keyUp = KeyUp;
            if (keyUp != null)
                keyUp(this, new UIntEventArgs(key));
        }

        public abstract void Init();
        public abstract Task Cycle();
    }
}
