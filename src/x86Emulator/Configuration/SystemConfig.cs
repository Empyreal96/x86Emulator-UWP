
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace x86Emulator.Configuration
{
    public static class SystemConfig
    {
        public static int MemorySize = 32;
        public static Resources MachineResources;

        //On-screen animated icons
        public static EventHandler IO_Floppy;
        public static EventHandler IO_HDD;
        public static EventHandler IO_CDROM;
        public static EventHandler Notification;
        public static void IO_FloppyCall()
        {
            IO_Floppy.Invoke(null, null);
        }
        public static void IO_CDCall()
        {
            IO_CDROM.Invoke(null, null);
        }
        public static void IO_HDDCall()
        {
            IO_HDD.Invoke(null, null);
        }
        public static void NotificationCall(string message = "")
        {
            Notification?.Invoke(message, null);
        }
    }
}
