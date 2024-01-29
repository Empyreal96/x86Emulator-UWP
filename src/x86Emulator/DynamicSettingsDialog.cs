/**
  Copyright (c) Bashar Astifan
  https://github.com/basharast
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Data;

namespace WUT
{
    public static class DefaultSettings
    {
        public static bool SettingsSwitchVariable;
        public static int SettingsComboVariable;
    }
    public class DynamicSettingsDialog
    {
        DynamicSettingsMap dynamicSettingsMap;

        public DynamicSettingsDialog()
        {
            dynamicSettingsMap = new DynamicSettingsMap();

            //Switch
            dynamicSettingsMap.AddItem(
            new DynamicSettingsMapSwitchItem()
            {
                group = "Switch",
                title = "Switch Variable",
                variable = "SettingsSwitchVariable",
                key = "SettingsSwitchVariableKey",
                desc = "Toggle Switch",
                onreset = false, //Value when reset
                callback = (s, e) =>
                {
                    //Do something
                    bool current = (bool)s;
                },
            });

            //Combo
            dynamicSettingsMap.AddItem(
            new DynamicSettingsMapComboItem()
            {
                group = "Combo",
                title = "Combo Variable",
                variable = "SettingsComboVariable",
                key = "SettingsComboVariableKey",
                desc = "ComboBox",
                min = 0,
                max = 3,
                value = 2,
                step = 1,
                cast = new string[] { "Value 1", "Value 2", "Value 3", "Value 4" },
                callback = (s, e) =>
                {
                    //Do something
                    int current = (int)s;
                },
            });

        }

        public DynamicSettingsDialog(List<DynamicSettingsMapItem> items)
        {
            dynamicSettingsMap = new DynamicSettingsMap(items);
        }
        
        public DynamicSettingsDialog(List<DynamicSettingsMapItem> items, Type parent)
        {
            dynamicSettingsMap = new DynamicSettingsMap(items, parent);
        }

        #region Management
        public async void Show(string title = "Settings", string description = "Settings will be saved automatically")
        {
            dynamicSettingsMap.RestoreSettings();
            DynamicDialog dynamicDialog = new DynamicDialog(
                settingsList: dynamicSettingsMap.GetSettingsItems(),
                dialogTitle: title,
                dialogNote: description,
                onReset: (s, e) =>
                {
                    //Do something on reset
                    Show(title, description);
                }
            );
            await dynamicDialog.Show();
        }

        public void Restore()
        {
            dynamicSettingsMap.RestoreSettings();
        }
        #endregion
    }

    #region Dynamic Dialog
    public class DynamicDialog
    {
        ContentDialog contentDialog = new ContentDialog();
        List<DynamicSettingsItem> settingsList;
        EventHandler onReset;

        public DynamicDialog(List<DynamicSettingsItem> settingsList, string dialogTitle, string dialogNote, EventHandler onReset)
        {
            this.settingsList = settingsList;
            this.onReset = onReset;

            contentDialog.Title = dialogTitle;
            contentDialog.PrimaryButtonText = "Reset";
            contentDialog.SecondaryButtonText = "Close";
            contentDialog.IsPrimaryButtonEnabled = true;
            contentDialog.IsSecondaryButtonEnabled = true;
            StackPanel ConfigBlock = new StackPanel();
            ConfigBlock.Orientation = Orientation.Vertical;

            List<string> settingsGroups = new List<string>();
            foreach (var sItem in this.settingsList)
            {
                TextBlock header = new TextBlock();
                header.FontWeight = FontWeights.Bold;
                header.Text = sItem.Name.ToUpper();
                header.Foreground = new SolidColorBrush(Colors.DodgerBlue);

                TextBlock info = new TextBlock();
                info.Text = sItem.Desc;

                settingsGroups.Add(sItem.Group);

                StackPanel settingsContainer = new StackPanel();
                settingsContainer.Children.Add(header);
                settingsContainer.Children.Add(info);
                if (sItem.IsCombo)
                {
                    sItem.SettingComboItem.Margin = new Thickness(0, 0, 0, 5);
                    settingsContainer.Children.Add(sItem.SettingComboItem);
                }
                else
                {
                    settingsContainer.Children.Add(sItem.SettingSwitchItem);
                }
                settingsContainer.Tag = sItem.Group;

                ConfigBlock.Children.Add(settingsContainer);
            }

            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.Content = ConfigBlock;
            scrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
            scrollViewer.MaxHeight = 300;
            scrollViewer.Margin = new Thickness(3, 7, 0, 7);
            scrollViewer.Padding = new Thickness(0, 5, 0, 5);
            scrollViewer.BorderThickness = new Thickness(0, 1, 0, 1);
            scrollViewer.BorderBrush = new SolidColorBrush(Colors.Gray);

            TextBlock notice = new TextBlock();
            notice.HorizontalAlignment = HorizontalAlignment.Stretch;
            notice.Foreground = new SolidColorBrush(Colors.Green);
            notice.Text = dialogNote;
            notice.FontSize = 12;
            notice.Margin = new Thickness(3, 5, 0, 0);


            Border border1 = new Border();
            border1.BorderThickness = new Thickness(0, 1, 0, 0);
            border1.BorderBrush = new SolidColorBrush(Colors.Gray);

            settingsGroups = settingsGroups.Distinct().ToList();
            settingsGroups = settingsGroups.Distinct().ToList();
            settingsGroups.Insert(0, "All");
            ComboBox groupsFilter = new ComboBox();
            groupsFilter.HorizontalAlignment = HorizontalAlignment.Stretch;
            groupsFilter.ItemsSource = settingsGroups;
            bool onBuild = true;
            groupsFilter.SelectionChanged += (s, e) =>
            {
                if (!onBuild)
                {
                    try
                    {
                        var targetGroup = settingsGroups[groupsFilter.SelectedIndex].ToLower();
                        foreach (var cItem in ConfigBlock.Children)
                        {
                            var targetElement = (StackPanel)cItem;
                            if (targetGroup.Equals("all") || ((string)targetElement.Tag).ToLower().Equals(targetGroup))
                            {
                                targetElement.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                targetElement.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            };
            groupsFilter.SelectedIndex = 0;
            onBuild = false;

            StackPanel stackPanel = new StackPanel();
            stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.Children.Add(groupsFilter);
            stackPanel.Children.Add(scrollViewer);
            stackPanel.Children.Add(notice);

            contentDialog.Content = stackPanel;

        }
        public async Task Show()
        {
            var result = await contentDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                //Reset settings
                foreach (var sItem in settingsList)
                {
                    sItem.ResetSetting();
                }
                onReset?.Invoke(null, null);
            }
        }
    }
    #endregion

    #region Dynamic Map
    public class DynamicSettingsMap
    {
        Type parentClass;
        List<DynamicSettingsMapItem> dynamicSettingsMapItems;
        public DynamicSettingsMap(List<DynamicSettingsMapItem> items = null, Type parent = null)
        {
            dynamicSettingsMapItems = items != null ? items : new List<DynamicSettingsMapItem>();
            parentClass = parent;
        }
        public void AddItem(DynamicSettingsMapItem item)
        {
            dynamicSettingsMapItems.Add(item);
        }
        public void AddItem(DynamicSettingsMapSwitchItem item)
        {
            dynamicSettingsMapItems.Add(item);
        }
        public void AddItem(DynamicSettingsMapComboItem item)
        {
            dynamicSettingsMapItems.Add(item);
        }
        public void RestoreSettings()
        {
            foreach (var item in dynamicSettingsMapItems)
            {
                if (item is DynamicSettingsMapSwitchItem)
                {
                    item.restore(parentClass);
                }
                else
                {
                    item.restore(parentClass);
                }
            }
        }
        public List<DynamicSettingsItem> GetSettingsItems()
        {
            List<DynamicSettingsItem> settingsItems = new List<DynamicSettingsItem>();
            foreach (var item in dynamicSettingsMapItems)
            {
                var parent = ((item.parent == null ? (parentClass == null ? typeof(DefaultSettings) : parentClass) : item.parent));

                if (item is DynamicSettingsMapSwitchItem)
                {
                    //Toggle Switch
                    var switchItem = (DynamicSettingsMapSwitchItem)item;
                    new DynamicSettingsItem(ref settingsItems,
                           group: switchItem.group,
                           name: switchItem.title,
                           desc: switchItem.desc,
                           icon: switchItem.icon, //Not in use
                           preferencesKey: switchItem.key, //Save settings ID (used for restore)
                           defaultValueKey: switchItem.variable, //Variable real name (static) 
                           onReset: switchItem.onreset, //Value on reset
                           switchItem.reversed, //Reverse bool value
                           parent, //Variable class (static)
                           (s, e) => //Callback on change
                           {
                               //'s' is the selected value (bool)
                               switchItem.callback?.Invoke(s, e);
                           });
                }
                else
                {
                    //Combo
                    var comboItem = (DynamicSettingsMapComboItem)item;
                    new DynamicSettingsItem(ref settingsItems,
                           group: comboItem.group,
                           name: comboItem.title,
                           desc: comboItem.desc,
                           icon: comboItem.icon, //Not in use
                           preferencesKey: comboItem.key, //Save settings ID (used for restore)
                           defaultValueKey: comboItem.variable, //Variable real name (static) 
                           onReset: false, //Value on reset [NOT FOR COMBO]
                           false, //Reverse bool value [NOT FOR COMBO]
                           parent, //Variable class (static)
                           (s, e) => //Callback on change
                           {
                               //'s' is the selected value (int)
                               comboItem.callback?.Invoke(s, e);
                           },
                           true, //Is combo 
                           comboItem.min, //Min value (int)
                           comboItem.max, //Max value (int)
                           comboItem.step, //Step (int)
                           comboItem.value, //Default (int)
                           comboItem.cast //Values string cast (optional)
                           );
                }
            }
            return settingsItems;
        }
    }
    public abstract class DynamicSettingsMapItem
    {
        public string group;
        public string title;
        public string desc;
        public string key;
        public string variable;
        public string icon;
        public EventHandler callback;
        public Type parent;
        public abstract void restore(Type parentClass);
    }
    public class DynamicSettingsMapSwitchItem : DynamicSettingsMapItem
    {
        public bool onreset;
        public bool reversed;

        public override void restore(Type parentClass = null)
        {
            var parentc = ((parent == null ? (parentClass == null ? typeof(DefaultSettings) : parentClass) : parent));
            var defaultValue = (bool)parentc.GetField(variable).GetValue(parent);
            bool savedValue = Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue);
            parentc.GetField(variable).SetValue(parentc, savedValue);
        }
    }
    public class DynamicSettingsMapComboItem : DynamicSettingsMapItem
    {
        public int max;
        public int min;
        public int step;
        public int value;
        public string[] cast;
        public override void restore(Type parentClass = null)
        {
            var parentc = ((parent == null ? (parentClass == null ? typeof(DefaultSettings) : parentClass) : parent));
            var defaultValue = (int)parentc.GetField(variable).GetValue(parent);
            int savedValue = Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue);
            parentc.GetField(variable).SetValue(parentc, savedValue);
        }
    }
    #endregion

    #region Dynamic Item
    public class DynamicSettingsItem
    {
        public string Group;
        public string Name;
        public string Desc;
        public string Icon;
        public string PreferencesKey;
        public string DefaultValueKey;
        public EventHandler ExtraAction;
        public ToggleSwitch SettingSwitchItem;
        public ComboBox SettingComboItem;
        public Type ParentClass;
        public bool OnReset = false;
        public bool IsCombo = false;
        public int DefaultInt = 0;
        public DynamicSettingsItem(ref List<DynamicSettingsItem> settingsList, string group, string name, string desc, string icon, string preferencesKey, string defaultValueKey, bool onReset = false, bool reversedValue = false, Type parentClass = null, EventHandler extraAction = null, bool isCombo = false, int min = 0, int max = 0, int step = 1, int defaultInt = 0, string[] cast = null)
        {
            Group = group;
            Name = name;
            Desc = desc;
            Icon = $"ms-appx:///Assets/Icons/{icon}.png";
            IsCombo = isCombo;
            DefaultInt = defaultInt;
            try
            {
                PreferencesKey = preferencesKey;
                DefaultValueKey = defaultValueKey;
                ExtraAction = extraAction;
                OnReset = onReset;
                ParentClass = parentClass;
                if (!isCombo)
                {
                    SettingSwitchItem = new ToggleSwitch();
                    SettingSwitchItem.OnContent = "ON";
                    SettingSwitchItem.OffContent = "OFF";
                    SettingSwitchItem.Margin = new Thickness(0, 0, 0, 3);
                    Type myType = parentClass != null ? parentClass : typeof(DefaultSettings);
                    var fields = myType.GetFields();
                    var probs = myType.GetProperties();
                    bool defaultValue = false;
                    try
                    {
                        defaultValue = (bool)myType.GetField(defaultValueKey).GetValue(myType);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            defaultValue = (bool)myType.GetProperty(defaultValueKey).GetValue(myType);
                        }
                        catch (Exception exx)
                        {

                        }
                    }

                    SettingSwitchItem.IsOn = reversedValue ? !defaultValue : defaultValue;
                    SettingSwitchItem.Toggled += (s, e) =>
                    {
                        try
                        {
                            defaultValue = !(bool)myType.GetField(defaultValueKey).GetValue(myType);
                            myType.GetField(defaultValueKey).SetValue(myType, defaultValue);
                            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(preferencesKey, defaultValue);
                            if (extraAction != null)
                            {
                                extraAction.Invoke(defaultValue, null);
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    };
                }
                else
                {
                    SettingComboItem = new ComboBox();
                    SettingComboItem.Margin = new Thickness(0, 0, 0, 3);
                    SettingComboItem.HorizontalAlignment = HorizontalAlignment.Stretch;
                    for (var i = min; i <= max;)
                    {
                        ComboBoxItem cbItem = new ComboBoxItem();
                        if (cast != null)
                        {
                            try
                            {
                                cbItem.Content = cast[i];
                            }
                            catch (Exception ex)
                            {
                                cbItem.Content = i.ToString();
                            }
                        }
                        else
                        {
                            cbItem.Content = i.ToString();
                        }
                        SettingComboItem.Items.Add(cbItem);
                        i += step;
                    }

                    Type myType = parentClass != null ? parentClass : typeof(DefaultSettings);
                    var fields = myType.GetFields();
                    var probs = myType.GetProperties();
                    int defaultValue = 0;
                    try
                    {
                        defaultValue = (int)myType.GetField(defaultValueKey).GetValue(myType);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            defaultValue = (int)myType.GetProperty(defaultValueKey).GetValue(myType);
                        }
                        catch (Exception exx)
                        {

                        }
                    }
                    SettingComboItem.SelectedIndex = (defaultValue);

                    SettingComboItem.SelectionChanged += (s, e) =>
                    {

                        try
                        {
                            defaultValue = SettingComboItem.SelectedIndex;
                            myType.GetField(defaultValueKey).SetValue(myType, defaultValue);
                            Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(preferencesKey, defaultValue);
                            if (extraAction != null)
                            {
                                extraAction.Invoke(defaultValue, null);
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    };
                }
            }
            catch (Exception ex)
            {
            }
            settingsList.Add(this);
        }
        public void ResetSetting()
        {
            try
            {
                if (!IsCombo)
                {
                    Type myType = ParentClass != null ? ParentClass : typeof(DefaultSettings);
                    var defaultValue = OnReset;
                    myType.GetField(DefaultValueKey).SetValue(myType, defaultValue);
                    Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(PreferencesKey, defaultValue);
                    if (ExtraAction != null)
                    {
                        ExtraAction.Invoke(defaultValue, null);
                    }
                }
                else
                {
                    Type myType = ParentClass != null ? ParentClass : typeof(DefaultSettings);
                    var defaultValue = DefaultInt;
                    myType.GetField(DefaultValueKey).SetValue(myType, defaultValue);
                    Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(PreferencesKey, defaultValue);
                    if (ExtraAction != null)
                    {
                        ExtraAction.Invoke(defaultValue, null);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
    #endregion
}
