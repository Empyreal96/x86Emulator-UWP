using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace x86Emulator
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        bool isInitialized = false;
        bool isBackPressedReady = false;
        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            MainPage.GoBacCallBack?.Invoke(e, null);
        }
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (!isBackPressedReady)
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
                isBackPressedReady = true;
            }

            InitializeApp(e.PreviousExecutionState, e.PrelaunchActivated, e.Arguments);
        }

        protected async override void OnFileActivated(FileActivatedEventArgs e)
        {
            try
            {
                if (!isBackPressedReady)
                {
                    SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
                    isBackPressedReady = true;
                }

                InitializeApp(e.PreviousExecutionState, false, null);
                var file = (StorageFile)e.Files.First(d => d is IStorageFile);
                try
                {
                    if (file != null)
                    {
                        var fileType = Path.GetExtension(file.Path).ToLower();
                        switch (fileType)
                        {
                            case ".iso":
                                //CD
                                break;

                            case ".img":
                                //Floppy or HDD
                                break;
                            case ".vhd":
                                //HDD
                                break;
                        }
                    }
                }
                catch (Exception exc)
                {
                }
            }
            catch (Exception ex)
            {
            }
        }


        private void InitializeApp(ApplicationExecutionState previousExecutionState, bool prelaunchActivated, string args)
        {
            if (args != null && args.Length > 0)
            {
                if (args.Contains("x86emu::") || args.Contains("x86emu:"))
                {
                    try
                    {
                        var cleanURL = args.Replace("x86emu::", "").Replace("x86emu:", "");
                        //Do something if you want
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            if (isInitialized)
            {
                return;
            }
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (previousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window

                Grid rootGrid = Window.Current.Content as Grid;
                //Frame rootFrame = rootGrid?.Children.Where((c) => c is Frame).Cast<Frame>().FirstOrDefault();

                if (rootGrid == null)
                {
                    rootGrid = new Grid();

                    //var notificationGrid = new Grid();
                    //LocalNotificationManager = new LocalNotificationManager(notificationGrid);

                    rootGrid.Children.Add(rootFrame);
                    //rootGrid.Children.Add(notificationGrid);

                    Window.Current.Content = rootGrid;
                    try
                    {
                        Window.Current.VisibilityChanged += Current_VisibilityChanged; ;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            try
            {
                if (prelaunchActivated == false)
                {
                    if (rootFrame.Content == null)
                    {
                        // When the navigation stack isn't restored navigate to the first page,
                        // configuring the new page by passing required information as a navigation
                        // parameter
                        rootFrame.Navigate(typeof(MainPage), args);
                    }
                    // Ensure the current window is active
                    Window.Current.Activate();
                }
                isInitialized = true;
            }
            catch (Exception ex)
            {

            }
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
