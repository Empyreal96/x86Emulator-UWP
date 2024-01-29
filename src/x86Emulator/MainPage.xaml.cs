using Microsoft.Graphics.Canvas;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.ViewManagement.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WUT;
using x86Emulator.Configuration;
using x86Emulator.GUI.WIN2D;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace x86Emulator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // UI Management
        #region UI
        private void UpdateBindings()
        {
            this.Bindings.Update();
        }

        private async void ShowNotification(bool state, string text = "", string title = "IMPORTANT!")
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, () =>
            {
                notificationProgress.IsActive = state;
                notificationText.Text = text;
                notificationTitle.Text = title;
                emuNotification.Visibility = state ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void SystemIsLoading(bool state)
        {
            ShowNotification(state, "Machine loading..", "PLEASE WAIT");
        }
        private async void ShowReady(bool state)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, () =>
            {
                emuMachineReady.Visibility = state ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void EnableButtons(bool state)
        {
            StartMachine.IsEnabled = state;
            Restart.IsEnabled = state;
            Keyboard.IsEnabled = state;
            Floppy.IsEnabled = state;
            EnabledSettings(state);
        }
        private void EnabledSettings(bool state)
        {
            Settings.IsEnabled = state;
        }
        private void RestoreDefaults()
        {
#if _M_ARM
            WIN2D.FitScreen = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("FitScreen", false);
#else
            WIN2D.FitScreen = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("FitScreen", true);
#endif
            WIN2D.InterpolationLinear = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("InterpolationLinear", false);
            WIN2D.DumpFrames = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("DumpFrames", false);

            var fillColor = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("FillColor", false);
            if (fillColor)
            {
                WIN2D.FillColor = Colors.Blue;
            }
            else
            {
                WIN2D.FillColor = Colors.Black;
            }

            var aspectRatio = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("AspectRatio", false);
            if (aspectRatio)
            {
                WIN2D.ASR[0] = 16;
                WIN2D.ASR[1] = 9;
            }
            else
            {
                WIN2D.ASR[0] = 0;
                WIN2D.ASR[1] = 0;
            }

            Helpers.DebugLog = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("DebugLog", false);
            Helpers.DebugFile = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("DebugFile", false);
            debugInfo = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("DebugOutput", false);
            UpdateBindings();
        }
        private async void BuildFloppyOptions()
        {
            await Task.Delay(1500);
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, async () =>
            {
                try
                {
                    FloppyOption.Items.Clear();
                    foreach (var fItem in StorageApplicationPermissions.FutureAccessList.Entries)
                    {
                        if (fItem.Metadata != null && fItem.Metadata.Contains("floppy"))
                        {
                            var floppyFile = await StorageApplicationPermissions.FutureAccessList.GetItemAsync(fItem.Token);
                            if (floppyFile != null)
                            {
                                MenuFlyoutItem floppyItem = new MenuFlyoutItem();
                                floppyItem.Text = floppyFile.Name;
                                floppyItem.Click += async (sender, args) =>
                                {
                                    await SystemConfig.MachineResources.floppies.Floppy[0].Select((StorageFile)floppyFile);
                                };
                                if (SystemConfig.MachineResources.floppies.Floppy[0].resourcesFile != null && SystemConfig.MachineResources.floppies.Floppy[0].resourcesFile.Path.Equals(floppyFile.Path))
                                {
                                    floppyItem.Foreground = new SolidColorBrush(Colors.DodgerBlue);
                                    FloppyOption.Items.Add(floppyItem);
                                    MenuFlyoutSeparator itemSeprator = new MenuFlyoutSeparator();
                                    FloppyOption.Items.Add(itemSeprator);
                                }
                                else
                                {
                                    FloppyOption.Items.Add(floppyItem);
                                }
                            }
                        }
                    }
                    if (FloppyOption.Items.Count > 0)
                    {
                        MenuFlyoutItem floppyChooseFile = new MenuFlyoutItem();
                        floppyChooseFile.Text = "Choose file";
                        floppyChooseFile.Foreground = new SolidColorBrush(Colors.Green);
                        floppyChooseFile.Click += async (sender, args) =>
                        {
                            await SystemConfig.MachineResources.floppies.Floppy[0].Select();
                        };
                        FloppyOption.Items.Add(floppyChooseFile);

                        MenuFlyoutItem floppyClearItems = new MenuFlyoutItem();
                        floppyClearItems.Text = "Clear all";
                        floppyClearItems.Foreground = new SolidColorBrush(Colors.Orange);
                        floppyClearItems.Click += async (sender, args) =>
                        {
                            ShowNotification(true, "Cleaning floppy history..", "PLEASE WAIT");
                            foreach (var fItem in StorageApplicationPermissions.FutureAccessList.Entries)
                            {
                                if (fItem.Metadata != null && fItem.Metadata.Contains("floppy"))
                                {
                                    var floppyFile = await StorageApplicationPermissions.FutureAccessList.GetItemAsync(fItem.Token);
                                    if (floppyFile != null)
                                    {
                                        if (SystemConfig.MachineResources.floppies.Floppy[0].resourcesFile == null || !SystemConfig.MachineResources.floppies.Floppy[0].resourcesFile.Path.Equals(floppyFile.Path))
                                        {
                                            StorageApplicationPermissions.FutureAccessList.Remove(fItem.Token);
                                        }
                                    }
                                    else
                                    {
                                        StorageApplicationPermissions.FutureAccessList.Remove(fItem.Token);
                                    }
                                }
                            }
                            ShowNotification(false);
                            BuildFloppyOptions();
                        };

                        Floppy.Visibility = Visibility.Visible;
                        MenuFlyoutSeparator itemSeprator = new MenuFlyoutSeparator();
                        FloppyOption.Items.Add(itemSeprator);
                        FloppyOption.Items.Add(floppyClearItems);
                    }
                    else
                    {
                        Floppy.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    Helpers.Logger(ex);
                }
                taskCompletionSource.SetResult(true);
            });
            await taskCompletionSource.Task;
        }
        #endregion

        //Settings Management
        #region Settings
        bool debugInfo = true;
        bool showSettings = false;
        bool ShowSettings
        {
            get
            {
                return showSettings;
            }
            set
            {
                showSettings = value;
                _ = CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, () =>
                 {
                     if (value)
                     {
                         emuSettings.Visibility = Visibility.Visible;
                     }
                     else
                     {
                         emuSettings.Visibility = Visibility.Collapsed;
                     }
                 });
            }
        }
        public static EventHandler GoBacCallBack;
        bool DebugInfo
        {
            get
            {
                return debugInfo;
            }
            set
            {
                debugInfo = value;
                if (debugInfo)
                {
                    debugData.Visibility = Visibility.Visible;
                }
                else
                {
                    debugData.Visibility = Visibility.Collapsed;
                }
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("DebugOutput", value);
                UpdateBindings();
            }
        }
        bool DebugLog
        {
            get
            {
                return Helpers.DebugLog;
            }
            set
            {
                Helpers.DebugLog = value;
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("DebugLog", value);
            }
        }
        bool DebugFile
        {
            get
            {
                return Helpers.DebugFile;
            }
            set
            {
                Helpers.DebugFile = value;
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("DebugFile", value);
            }
        }
        bool FitScreen
        {
            get
            {
                return WIN2D.FitScreen;
            }
            set
            {
                WIN2D.FitScreen = value;
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("FitScreen", value);
            }
        }
        bool InterpolationLinear
        {
            get
            {
                return WIN2D.InterpolationLinear;
            }
            set
            {
                WIN2D.InterpolationLinear = value;
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("InterpolationLinear", value);
            }
        }  
        bool DumpFrames
        {
            get
            {
                return WIN2D.DumpFrames;
            }
            set
            {
                WIN2D.DumpFrames = value;
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("DumpFrames", value);
            }
        }
        bool FillColor
        {
            get
            {
                return WIN2D.FillColor == Colors.Blue;
            }
            set
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("FillColor", value);
                if (value)
                {
                    WIN2D.FillColor = Colors.Blue;
                }
                else
                {
                    WIN2D.FillColor = Colors.Black;
                }
            }
        }
        bool AspectRatio
        {
            get
            {
                return WIN2D.ASR[0] == 16;
            }
            set
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("AspectRatio", value);
                if (value)
                {
                    WIN2D.ASR[0] = 16;
                    WIN2D.ASR[1] = 9;
                }
                else
                {
                    WIN2D.ASR[0] = 0;
                    WIN2D.ASR[1] = 0;
                }
            }
        }

        private void LinkLogs()
        {
            x86DisasmUWP.Helpers.ErrorLog = (sender, args) =>
            {
                if (Helpers.DebugFile)
                    Helpers.Logger(new Exception((string)sender));
            };
            x86DisasmUWP.Helpers.DebugLog = (sender, args) =>
            {
                if (Helpers.DebugLog)
                    Helpers.LoggerDebug((string)sender);
            };
            x86DisasmUWP.Helpers.NormalLog = (sender, args) =>
            {
                if (Helpers.DebugFile)
                    Helpers.Logger((string)sender);
            };
        }
        #endregion

        #region Special Handlers
        private void LinkHandlers()
        {
            GoBacCallBack = async (sender, args) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, async () =>
                {
                    if (ShowSettings)
                    {
                        ShowSettings = false;
                        UpdateBindings();
                    }
                    else
                    {
                        var messageDialog = new MessageDialog("Do you want to exit?");
                        messageDialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(this.CommandInvokedHandler)));
                        messageDialog.Commands.Add(new UICommand("No"));
                        await messageDialog.ShowAsync();
                    }
                });
            };
        }
        private void CommandInvokedHandler(IUICommand command)
        {
            CoreApplication.Exit();
        }
        #endregion

        //Resources Management
        #region Resources
        int MachineMemorySize
        {
            get
            {
                return SystemConfig.MemorySize;
            }
        }
        private void BuildResources()
        {
            EventHandler floppyCallback = async (sender, args) =>
            {
                Helpers.Logger((string)sender);
                BuildFloppyOptions();
                await machine.SyncFloppies();
            };
            EventHandler hddCallback = async (sender, args) =>
            {
                Helpers.Logger((string)sender);
                await machine.SyncHDDs();
            };
            EventHandler cdCallback = async (sender, args) =>
            {
                Helpers.Logger((string)sender);
                await machine.SyncCDROMs();
            };
            SystemConfig.MachineResources = new Resources(floppyCallback, hddCallback, cdCallback);
            resourcesContainerBlock.Children.Add(SystemConfig.MachineResources.resourcesContainer);
            BuildFloppyOptions();
        }
        private void GetMemorySize()
        {
            var storedMemory = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("MachineMemorySize", 32);
            SystemConfig.MemorySize = storedMemory;
            UpdateBindings();
        }
        private void SetMemorySize(int memorySize)
        {
            if (AppStarted)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("MachineMemorySize", memorySize);
                SystemConfig.MemorySize = memorySize;
            }
        }
        private async Task PrepareLogs()
        {
            await Helpers.PrepareLogs();
        }

        bool FloppyIconInProgress = false;
        bool CDIconInProgress = false;
        bool HDDIconInProgress = false;
        private void LinkIOIcons()
        {
            SystemConfig.IO_Floppy += async (sender, args) =>
            {
                if (FloppyIconInProgress)
                {
                    return;
                }
                await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, async () =>
                {
                    try
                    {
                        FloppyIconInProgress = true;
                        ReadWrite_Floppy.Visibility = Visibility.Visible;
                        await Task.Delay(100);
                        ReadWrite_Floppy.Visibility = Visibility.Collapsed;
                        FloppyIconInProgress = false;
                    }
                    catch (Exception ex)
                    {
                        ReadWrite_Floppy.Visibility = Visibility.Collapsed;
                        FloppyIconInProgress = false;
                    }
                });
            };
            SystemConfig.IO_CDROM += async (sender, args) =>
            {
                if (CDIconInProgress)
                {
                    return;
                }
                await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, async () =>
                {
                    try
                    {
                        CDIconInProgress = true;
                        ReadWrite_CD.Visibility = Visibility.Visible;
                        await Task.Delay(100);
                        ReadWrite_CD.Visibility = Visibility.Collapsed;
                        CDIconInProgress = false;
                    }
                    catch (Exception ex)
                    {
                        ReadWrite_CD.Visibility = Visibility.Collapsed;
                        CDIconInProgress = false;
                    }
                });
            };
            SystemConfig.IO_HDD += async (sender, args) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, async () =>
                {
                    if (HDDIconInProgress)
                    {
                        return;
                    }
                    try
                    {
                        HDDIconInProgress = true;
                        ReadWrite_HDD.Visibility = Visibility.Visible;
                        await Task.Delay(100);
                        ReadWrite_HDD.Visibility = Visibility.Collapsed;
                        HDDIconInProgress = false;
                    }
                    catch (Exception ex)
                    {
                        ReadWrite_HDD.Visibility = Visibility.Collapsed;
                        HDDIconInProgress = false;
                    }
                });
            };
        }
        private void LinkNotification()
        {
            SystemConfig.Notification += (sender, args) =>
            {
                string message = (string)sender;
                ShowNotification(message.Length > 0, message);
            };
        }
        #endregion

        #region Main
        bool AppStarted = false;
        Machine machine;
        double frequency = 100000.0f;
        ulong timerTicks;
        bool running;

        public MainPage()
        {
            InitializeComponent();

            try
            {
                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
            catch (Exception e)
            {
            }


            EnableButtons(false);
            CleanTemp();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;

            PrepareMachine();
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (AppStarted && running && !ShowSettings)
            {
                machine.mouse.Update();
            }
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Escape && ShowSettings)
            {
                ShowSettings = false;
                UpdateBindings();
                args.Handled = true;
                Helpers.Logger(Keys);
            }
            else
            {
                uint scanCode = ScanCodes.GetScanCode(args.VirtualKey);
                Machine.gui?.OnKeyUp(scanCode);
                if (running)
                {
                    args.Handled = true;
                }
                args.Handled = true;
            }
        }

        string Keys = "";
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Escape && ShowSettings)
            {
                args.Handled = true;
                //Nothing
            }
            else
            {
                uint scanCode = ScanCodes.GetScanCode(args.VirtualKey);
                Machine.gui?.OnKeyDown(scanCode);
                if (running)
                {
                    args.Handled = true;
                }

            }
        }

        private async void SendKey(VirtualKey key)
        {
            uint scanCode = ScanCodes.GetScanCode(key);
            Machine.gui?.OnKeyDown(scanCode);
            await Task.Delay(10);
            Machine.gui?.OnKeyUp(scanCode);
        }


        //Machine initial
        public async void PrepareMachine()
        {
            await InitialMachine(true);
        }
        public async Task InitialMachine(bool startup = false)
        {
            SystemIsLoading(true);

            if (startup)
            {
                WIN2D.PanelRow = PanelRow;
                await PrepareLogs();
                LinkIOIcons();
                LinkNotification();
                LinkLogs();
                LinkHandlers();
                BuildResources();
            }

            GetMemorySize();
            RestoreDefaults();

            await Task.Delay(1000);

            AppStarted = true;

            if (!SystemConfig.MachineResources.bios.VGA.isValid() || !SystemConfig.MachineResources.bios.BOCHS.isValid())
            {
                //Check built-in BIOS before showing message

                ShowNotification(true, "Please setup BIOS from settings", "BIOS REQUIRED!");
                EnabledSettings(true);
                while (!SystemConfig.MachineResources.bios.VGA.isValid() || !SystemConfig.MachineResources.bios.BOCHS.isValid())
                {
                    await Task.Delay(1500);
                }
                ShowNotification(false);
            }

            machine = new Machine(MonitorCanvas);
            while (machine.systemIsLoading)
            {
                await Task.Delay(100);
            }
            PrintRegisters();

            running = true;

            await Task.Run(() =>
            {
                RunMachine();
            });

            machine.Start();
            SetCPULabel(machine.CPU.InstructionText);
            PrintRegisters();

            EnableButtons(true);
            SystemIsLoading(false);
            ShowReady(true);
        }



        private async void RunMachine()
        {
            var stopwatch = new Stopwatch();
            double lastSeconds = 0;

            while (running)
            {
                if (!machine.Running)
                {
                    await Task.Delay(100);
                    continue;
                }

                if (debugInfo)
                {
                    if (!stopwatch.IsRunning)
                    {
                        stopwatch.Start();
                    }
                    ++timerTicks;

                    if (timerTicks % 50000 == 0)
                    {
                        frequency = 50000 / (stopwatch.Elapsed.TotalSeconds - lastSeconds);
                        lastSeconds = stopwatch.Elapsed.TotalSeconds;
                        await CoreApplication.MainView.CoreWindow.Dispatcher.TryRunAsync(CoreDispatcherPriority.High, () =>
                        {

                            SetCPULabel(machine.CPU.InstructionText);
                            tpsLabel.Text = frequency.ToString("n") + "TPS";
                            PrintRegisters();
                        });
                    }
                }
                else
                {
                    if (stopwatch.IsRunning)
                    {
                        stopwatch.Stop();
                        lastSeconds = 0;
                        timerTicks = 0;
                    }
                }

                machine.RunCycle(Helpers.DebugLog, false);
            }
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
        }
        #endregion

        #region Events
        private void StartMachine_Click(object sender, RoutedEventArgs e)
        {
            ShowReady(false);

            machine.ClearTempBreakpoints();

            if (machine.Running)
            {
                StartMachine.Icon = new SymbolIcon(Symbol.Play);
                machine.Running = false;
            }
            else
            {
                if (!running)
                {
                    running = true;
                    _ = Task.Run(() =>
                    {
                        RunMachine();
                    });
                    machine.Start();
                    SetCPULabel(machine.CPU.InstructionText);
                    PrintRegisters();
                }
                StartMachine.Icon = new SymbolIcon(Symbol.Pause);
                machine.Running = true;
            }
        }

        private async void Keyboard_Click(object sender, RoutedEventArgs e)
        {
            ActionsBar.IsEnabled = false;
            MonitorCanvas.Focus(FocusState.Programmatic);
            if (InputPane.GetForCurrentView().Visible)
            {
                InputPane.GetForCurrentView().TryHide();
            }
            else
            {
                InputPane.GetForCurrentView().TryShow();
            }
            await Task.Delay(100);
            ActionsBar.IsEnabled = true;
        }

        private async void Restart_Click(object sender, RoutedEventArgs e)
        {
            SystemIsLoading(true);
            EnableButtons(false);
            if (machine.Running)
            {
                StartMachine.Icon = new SymbolIcon(Symbol.Play);
                machine.Running = false;
                running = false;
            }

            machine.Stop();
            machine.ResetScreen();
            await Task.Delay(2000);
            machine = null;
            await InitialMachine();
            await Task.Delay(1000);

            EnableButtons(true);
            SystemIsLoading(false);
        }
        private void MemorySizeValue_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                var memorySize = (int)Math.Round(e.NewValue);
                SetMemorySize(memorySize);
            }
            catch (Exception ex)
            {

            }
        }
        private void Keyboard_Key_Click(object sender, RoutedEventArgs e)
        {
            var target = (AppBarButton)sender;
            var key = target.Tag;
            var values = Enum.GetValues(typeof(VirtualKey));
            foreach (var vik in values)
            {
                if (((VirtualKey)vik).ToString().ToLower().Equals(key.ToString().ToLower()))
                {
                    SendKey((VirtualKey)vik);
                    break;
                }
            }
        }
        private void Viewbox_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StartMachine_Click(null, null);
        }
        #endregion

        #region Extra
        private async void CleanTemp()
        {
            var files = await ApplicationData.Current.TemporaryFolder.GetFilesAsync();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    await file.DeleteAsync();
                }
            }
        }
        #endregion

        #region Debug
        private void SetCPULabel(string text)
        {
            cpuLabel.Text = String.Format("{0:X}:{1:X} {2}", machine.CPU.CS, machine.CPU.EIP, text);
        }
        private void PrintRegisters()
        {
            CPU.CPU cpu = machine.CPU;
            List<string> registers = new List<string>();
            List<string> segments = new List<string>();
            List<string> flags = new List<string>();

            registers.Add(cpu.EAX.ToString("X8"));
            registers.Add(cpu.EBX.ToString("X8"));
            registers.Add(cpu.ECX.ToString("X8"));
            registers.Add(cpu.EDX.ToString("X8"));
            registers.Add(cpu.ESI.ToString("X8"));
            registers.Add(cpu.EDI.ToString("X8"));
            registers.Add(cpu.EBP.ToString("X8"));
            registers.Add(cpu.ESP.ToString("X8"));
            Registers.Text = string.Join(",", registers);

            segments.Add(cpu.CS.ToString("X4"));
            segments.Add(cpu.DS.ToString("X4"));
            segments.Add(cpu.ES.ToString("X4"));
            segments.Add(cpu.FS.ToString("X4"));
            segments.Add(cpu.GS.ToString("X4"));
            segments.Add(cpu.SS.ToString("X4"));
            Segments.Text = string.Join(",", segments);

            flags.Add(cpu.CF ? "CF" : "cf");
            flags.Add(cpu.PF ? "PF" : "pf");
            flags.Add(cpu.AF ? "AF" : "af");
            flags.Add(cpu.ZF ? "ZF" : "zf");
            flags.Add(cpu.SF ? "SF" : "sf");
            flags.Add(cpu.TF ? "TF" : "tf");
            flags.Add(cpu.IF ? "IF" : "if");
            flags.Add(cpu.DF ? "DF" : "df");
            flags.Add(cpu.OF ? "OF" : "of");
            flags.Add(cpu.IOPL.ToString("X2"));
            flags.Add(cpu.AC ? "AC" : "ac");
            flags.Add(cpu.NT ? "NT" : "nt");
            flags.Add(cpu.RF ? "RF" : "rf");
            flags.Add(cpu.VM ? "VM" : "vm");
            flags.Add(cpu.VIF ? "VIF" : "vif");
            flags.Add(cpu.VIP ? "VIP" : "vip");
            Flags.Text = string.Join(",", flags);
        }
        #endregion


    }
}
