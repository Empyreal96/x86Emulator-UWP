using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using x86Emulator.Devices;

namespace x86Emulator
{
    public class Resources
    {
        public StackPanel resourcesContainer;

        public BIOS bios;
        public Floppies floppies;
        public HDDs hdds;
        public CDROMs cdrom;


        public Resources(EventHandler floppyCallback, EventHandler hddCallback, EventHandler cdromCallback)
        {
            bios = new BIOS();
            floppies = new Floppies(1, floppyCallback);
            hdds = new HDDs(2, hddCallback);
            cdrom = new CDROMs(1, cdromCallback);

            resourcesContainer = new StackPanel();
            resourcesContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
            resourcesContainer.Margin = new Thickness(0, 5, 0, 0);

            resourcesContainer.Children.Add(bios.resourcesSubContainer);
            resourcesContainer.Children.Add(GetSperator());

            resourcesContainer.Children.Add(floppies.resourcesSubContainer);
            resourcesContainer.Children.Add(GetSperator());

            resourcesContainer.Children.Add(hdds.resourcesSubContainer);
            resourcesContainer.Children.Add(GetSperator());

            resourcesContainer.Children.Add(cdrom.resourcesSubContainer);
        }

        #region Internal 
        private UIElement GetSperator()
        {
            Border border = new Border();
            border.HorizontalAlignment = HorizontalAlignment.Stretch;
            border.Name = $"bios_block_Sperator";
            border.BorderBrush = new SolidColorBrush(Colors.Gray);
            border.Margin = new Thickness(0, 5, 0, 0);
            border.BorderThickness = new Thickness(1);

            return border;
        }
        #endregion
    }
    public class BIOS
    {
        public StackPanel resourcesSubContainer;
        public ResourcesFile BOCHS;
        public ResourcesFile VGA;

        public BIOS()
        {
            resourcesSubContainer = new StackPanel();
            resourcesSubContainer.HorizontalAlignment = HorizontalAlignment.Stretch;

            //Keep latest CRC at index 0
            BOCHS = new ResourcesFile("bios-bochs", @"Bochs:\", null, new string[] { "B0F321CE", "28850B2A", "C78F26B1" });
            //                                                                       ^^^^^^^^^^
            VGA = new ResourcesFile("bios-vga", @"VGA:\", null, new string[] { "BE7AB338", "560A4B7C", "77BEB276", "D825A7C8", "27516A12", "87045754", "719483F3", "24BEF15C" });
            //                                                                 ^^^^^^^^^^
            
            BOCHS.SetBuiltinTarget("BIOS-bochs-latest");
            VGA.SetBuiltinTarget("VGABIOS-lgpl-latest");

            TextBlock header = new TextBlock();
            header.Text = "BIOS";
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.Margin = new Thickness(0, 5, 0, 0);
            header.FontWeight = FontWeights.Bold;

            resourcesSubContainer.Children.Add(header);
            resourcesSubContainer.Children.Add(BOCHS.resourcesBlock);
            resourcesSubContainer.Children.Add(VGA.resourcesBlock);
        }
    }
    public class Floppies
    {
        public StackPanel resourcesSubContainer;
        public ResourcesFile[] Floppy;

        string[] suggestedLetters = new string[] { @"A:\", @"B:\", @"D:\" };

        public Floppies(int count, EventHandler floppyCallback)
        {
            resourcesSubContainer = new StackPanel();
            resourcesSubContainer.HorizontalAlignment = HorizontalAlignment.Stretch;

            Floppy = new ResourcesFile[count];

            TextBlock header = new TextBlock();
            header.Text = "Floppy";
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.Margin = new Thickness(0, 5, 0, 0);
            header.FontWeight = FontWeights.Bold;
            resourcesSubContainer.Children.Add(header);

            var extensions = new string[] { ".img" };
            for (int i = 0; i < count; i++)
            {
                Floppy[i] = new ResourcesFile($"machine-floppy-{(i + 1)}", suggestedLetters[i], extensions);
                Floppy[i].changeCallback = floppyCallback;
                resourcesSubContainer.Children.Add(Floppy[i].resourcesBlock);
            }
        }
    }

    public class HDDs
    {
        public StackPanel resourcesSubContainer;
        public ResourcesFile[] HDD;

        public HDDs(int count, EventHandler hddCallback)
        {
            resourcesSubContainer = new StackPanel();
            resourcesSubContainer.HorizontalAlignment = HorizontalAlignment.Stretch;

            HDD = new ResourcesFile[count];

            TextBlock header = new TextBlock();
            header.Text = "HDD";
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.Margin = new Thickness(0, 5, 0, 0);
            header.FontWeight = FontWeights.Bold;
            resourcesSubContainer.Children.Add(header);

            var extensions = new string[] { ".img", ".vhd" };
            for (int i = 0; i < count; i++)
            {
                HDD[i] = new ResourcesFile($"machine-hdd-{(i + 1)}", $@"HDD {(i + 1)}:\", extensions);
                HDD[i].changeCallback = hddCallback;
                resourcesSubContainer.Children.Add(HDD[i].resourcesBlock);
            }
        }
    }
    public class CDROMs
    {
        public StackPanel resourcesSubContainer;
        public ResourcesFile[] CDROM;

        public CDROMs(int count, EventHandler cdromCallback)
        {
            resourcesSubContainer = new StackPanel();
            resourcesSubContainer.HorizontalAlignment = HorizontalAlignment.Stretch;

            CDROM = new ResourcesFile[count];

            TextBlock header = new TextBlock();
            header.Text = "CD-ROM";
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.Margin = new Thickness(0, 5, 0, 0);
            header.FontWeight = FontWeights.Bold;
            resourcesSubContainer.Children.Add(header);

            var extensions = new string[] { ".iso" };
            for (int i = 0; i < count; i++)
            {
                CDROM[i] = new ResourcesFile($"machine-cdrom-{(i + 1)}", $@"CD-ROM {(i + 1)}:\", extensions);
                CDROM[i].changeCallback = cdromCallback;
                resourcesSubContainer.Children.Add(CDROM[i].resourcesBlock);
            }
        }
    }

    public class ResourcesFile
    {
        public StackPanel resourcesBlock;
        public EventHandler changeCallback;

        StackPanel subStackBlock;
        ProgressBar loadingBar;
        Button selectButton;
        Button resetButton;
        TextBlock noticeText;

        bool builtinSelected = false;
        public string previewName
        {
            get
            {
                string name = $"{resourcesTag} [None]";
                if (resourcesFile != null)
                {
                    if (builtinSelected)
                    {
                        name = $"{resourcesTag} Built-in";
                    }
                    else
                    {
                        name = $"{resourcesTag} {resourcesFile.Name}";
                    }
                }
                return name;
            }
        }
        public StorageFile resourcesFile;
        public int resourcesID;
        string resourcesKey;
        string resourcesKeyGUID; //Modified by the constuctor
        string resourcesTag;
        string builtinTarget;
        string[] resourcesExtensions;
        string[] crcCheck;

        public ResourcesFile(string key, string tag, string[] exts = null, string[] crc = null)
        {
            resourcesKey = key;
            resourcesKeyGUID = $"{resourcesKey}_GUID";
            resourcesTag = tag;
            resourcesExtensions = exts;
            crcCheck = crc;

            resourcesBlock = new StackPanel();
            resourcesBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
            resourcesBlock.Name = $"{resourcesKey}_MainStack";
            resourcesBlock.Margin = new Thickness(0, 10, 0, 0);

            subStackBlock = new StackPanel();
            subStackBlock.Orientation = Orientation.Horizontal;
            subStackBlock.Name = $"{resourcesKey}_SubStack";

            loadingBar = new ProgressBar();
            loadingBar.HorizontalAlignment = HorizontalAlignment.Stretch;
            loadingBar.Name = $"{resourcesKey}_Progress";
            loadingBar.IsIndeterminate = false;
            loadingBar.Visibility = Visibility.Collapsed;

            selectButton = new Button();
            selectButton.HorizontalContentAlignment = HorizontalAlignment.Left;
            selectButton.Name = $"{resourcesKey}_Select";
            selectButton.MaxWidth = 350;
            selectButton.Margin = new Thickness(5, 0, 0, 0);
            selectButton.Content = previewName;
            selectButton.Click += async (sender, args) =>
            {
                await Select();
            };

            resetButton = new Button();
            resetButton.Name = $"{resourcesKey}_Reset";
            resetButton.Content = "Reset";
            resetButton.Click += (sender, args) =>
            {
                Reset();
            };

            noticeText = new TextBlock();
            noticeText.HorizontalAlignment = HorizontalAlignment.Stretch;
            noticeText.Visibility = Visibility.Collapsed;
            noticeText.Foreground = new SolidColorBrush(Colors.Orange);
            noticeText.Margin = new Thickness(0, 0, 0, 5);
            if (crcCheck != null)
            {
                noticeText.Text = $"CRC expected: {crcCheck[0]}";
            }
            else
            {
                noticeText.Text = $"CRC not matched!";
            }

            subStackBlock.Children.Add(resetButton);
            subStackBlock.Children.Add(selectButton);

            resourcesBlock.Children.Add(loadingBar);
            resourcesBlock.Children.Add(noticeText);
            resourcesBlock.Children.Add(subStackBlock);

            //Restore file if selected before based on 'key'
            Restore();
        }

        //Select file and add it to future access list
        public async Task Select(StorageFile force = null)
        {
            try
            {
                FileOpenPicker picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.Downloads;
                if (resourcesExtensions != null && resourcesExtensions.Length > 0)
                {
                    foreach (string extension in resourcesExtensions)
                    {
                        var ext = extension;
                        if (!extension.StartsWith("."))
                        {
                            //Append '.' in case provided without it
                            ext = $".{ext}";
                        }
                        picker.FileTypeFilter.Add(ext);
                    }
                }
                else
                {
                    picker.FileTypeFilter.Add("*");
                }
                var selectedFile = force != null ? force : await picker.PickSingleFileAsync();
                if (selectedFile != null)
                {
                    loadingResources(true);
                    Reset();
                    await addAccess(selectedFile);
                    loadingResources(false);
                }
            }
            catch (Exception ex)
            {
                Helpers.Logger(ex);
            }
        }
        public void SetBuiltinTarget(string target)
        {
            builtinTarget = target;
            Restore();
        }

        //Remove file from future access list
        public async void Reset()
        {
            loadingResources(true);
            var currentToken = GetToken();
            if (validateToken(currentToken))
            {
                await removeAccess(currentToken);
            }
            loadingResources(false);
        }

        //Restore file from future access list
        public async void Restore()
        {
            loadingResources(true);
            var currentToken = GetToken();
            if (validateToken(currentToken))
            {
                var file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(currentToken);
                await getAccess(file);
            }
            else
            {
                if(builtinTarget!=null && builtinTarget.Length > 0)
                {
                    var testBuiltin = await Package.Current.InstalledLocation.TryGetItemAsync(builtinTarget);
                    if (testBuiltin != null)
                    {
                        await getAccess(testBuiltin as StorageFile, true);
                    }
                }
            }
            loadingResources(false);
        }

        public bool isValid()
        {
            return resourcesFile != null;
        }
        #region Internal
        private string GetToken()
        {
            return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(resourcesKey, "");
        }
        private async Task addAccess(StorageFile file)
        {
            builtinSelected = false;
            string token = StorageApplicationPermissions.FutureAccessList.Add(file, resourcesKey);
            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(resourcesKey, token);
            Guid guid = Guid.NewGuid();
            var id = guid.GetHashCode();
            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(resourcesKeyGUID, id);
            resourcesID = id;
            resourcesFile = file;
            InvokeUpdateCallback("Added");
            await updateLayout();
        }
        private async Task removeAccess(string token)
        {
            //StorageApplicationPermissions.FutureAccessList.Remove(token);
            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(resourcesKey, "");
            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(resourcesKeyGUID, 0);
            resourcesFile = null;
            InvokeUpdateCallback("Removed");
            resourcesID = 0;
            await updateLayout();
        }
        private async Task getAccess(StorageFile file, bool builtin = false)
        {
            resourcesFile = file;
            builtinSelected = builtin;
            await updateLayout();
        }
        private void InvokeUpdateCallback(string action)
        {
            string message = $"Device changed [{action}] [Key: {resourcesKey}, ID:{resourcesID}]";
            changeCallback?.Invoke(message, null);
        }
        private bool validateToken(string token)
        {
            return token != null && token.Length > 0 && StorageApplicationPermissions.FutureAccessList.ContainsItem(token);
        }
        private async Task updateLayout()
        {
            selectButton.Content = previewName;
            if (crcCheck != null && crcCheck.Length > 0)
            {
                if (resourcesFile != null)
                {
                    var fileCRC = await Helpers.CRCFile(resourcesFile);
                    bool crcMatch = false;
                    foreach (var crc in crcCheck)
                    {
                        if (fileCRC.ToLower().Equals(crc.ToLower()))
                        {
                            crcMatch = true;
                            break;
                        }
                    }
                    if (crcMatch)
                    {
                        selectButton.Background = new SolidColorBrush(Colors.Green);
                        noticeText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        selectButton.Background = new SolidColorBrush(Colors.Orange);
                        noticeText.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    selectButton.ClearValue(Button.BackgroundProperty);
                    noticeText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (resourcesFile != null)
                {
                    selectButton.Background = new SolidColorBrush(Colors.DodgerBlue);
                    noticeText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    selectButton.ClearValue(Button.BackgroundProperty);
                    noticeText.Visibility = Visibility.Collapsed;
                }
            }
        }
        private void loadingResources(bool state)
        {
            if (state)
            {
                selectButton.IsEnabled = false;
                resetButton.IsEnabled = false;
                loadingBar.IsIndeterminate = true;
                loadingBar.Visibility = Visibility.Visible;
            }
            else
            {
                selectButton.IsEnabled = true;
                resetButton.IsEnabled = true;
                loadingBar.IsIndeterminate = false;
                loadingBar.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
    }
}
