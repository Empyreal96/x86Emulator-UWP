using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using x86Emulator.Configuration;
using x86Emulator.Devices;
using x86Emulator.UWP.Native;

namespace x86Emulator.GUI.WIN2D
{
    public class WIN2D : UI
    {
        private byte[] Memory;
        private static int Width = 640;
        private static int Height = 400;

        public static bool InterpolationLinear = false;
        public static bool FitScreen = false;
        public static bool DumpFrames = false;
        public static RowDefinition PanelRow;
        private static Color fillColor = Colors.Black;
        public static Color FillColor
        {
            get
            {
                return fillColor;
            }
            set
            {
                fillColor = value;
                FillDisplay(fillColor);
            }
        }

        #region Render Manager
        private static CanvasAnimatedControl renderPanel;
        public CanvasAnimatedControl RenderPanel
        {
            get => renderPanel;
            set
            {
                if (renderPanel == value)
                {
                    return;
                }

                Dispose();

                if (renderPanel != null)
                {
                    renderPanel.Update -= RenderPanelUpdate;
                    renderPanel.Draw -= RenderPanelDraw;
                    renderPanel.GameLoopStopped -= RenderPanelLoopStopping;
                }

                renderPanel = value;
                if (renderPanel != null)
                {
                    RenderPanel.ClearColor = fillColor;
                    renderPanel.Update += RenderPanelUpdate;
                    renderPanel.Draw += RenderPanelDraw;
                    renderPanel.GameLoopStopped += RenderPanelLoopStopping;
                }
            }
        }

        private const uint RenderTargetMinSize = 1024;
        public CanvasBitmap RenderTarget { get; set; } = null;
        private Rect RenderTargetViewport = new Rect();

        //This may be different from viewport's width/haight.
        public float RenderTargetAspectRatio { get; set; } = 1.0f;

        private GameGeometry currentGeometry;
        public GameGeometry CurrentGeometry
        {
            get => currentGeometry;
            set
            {
                currentGeometry = value;
                RenderTargetAspectRatio = currentGeometry.AspectRatio;
                if (RenderTargetAspectRatio < 0.1f)
                {
                    RenderTargetAspectRatio = (float)(currentGeometry.BaseWidth) / currentGeometry.BaseHeight;
                }
            }
        }
        public Rotations CurrentRotation { get; set; }

        public void Dispose()
        {
            try
            {
                RenderTarget?.Dispose();
                RenderTarget = null;
            }
            catch (Exception e)
            {

            }
        }

        public Matrix3x2 transformMatrix;
        public float aspectRatio = 1;
        public Size destinationSize;
        public void Render(CanvasDrawingSession drawingSession, ICanvasAnimatedControl canvas)
        {
            try
            {
                var canvasSize = canvas.Size;

                UpdateRenderTargetSize(drawingSession);

                drawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
                drawingSession.TextAntialiasing = CanvasTextAntialiasing.Auto;

                var viewportWidth = RenderTargetViewport.Width;
                var viewportHeight = RenderTargetViewport.Height;
                aspectRatio = RenderTargetAspectRatio;
                if (RenderTarget == null || viewportWidth <= 0 || viewportHeight <= 0)
                    return;

                var rotAngle = 0.0;
                switch (CurrentRotation)
                {
                    case Rotations.CCW90:
                        rotAngle = -0.5 * Math.PI;
                        aspectRatio = 1.0f / aspectRatio;
                        break;
                    case Rotations.CCW180:
                        rotAngle = -Math.PI;
                        break;
                    case Rotations.CCW270:
                        rotAngle = -1.5 * Math.PI;
                        aspectRatio = 1.0f / aspectRatio;
                        break;
                }

                destinationSize = ComputeBestFittingSize(canvasSize, aspectRatio);
                var scaleMatrix = Matrix3x2.CreateScale((float)(destinationSize.Width), (float)(destinationSize.Height));
                var rotMatrix = Matrix3x2.CreateRotation((float)rotAngle);
                var transMatrix = Matrix3x2.CreateTranslation((float)(0.5 * canvasSize.Width), (float)(0.5f * canvasSize.Height));
                transformMatrix = rotMatrix * scaleMatrix * transMatrix;

                drawingSession.Transform = transformMatrix;
                var interpolation = InterpolationLinear ? CanvasImageInterpolation.Linear : CanvasImageInterpolation.NearestNeighbor;
                drawingSession.DrawImage(RenderTarget, new Rect(-0.5, -0.5, 1, 1), RenderTargetViewport, 1.0f, interpolation);
                drawingSession.Transform = Matrix3x2.Identity;
            }
            catch (Exception e)
            {

            }
        }
        int trueWidth = 0;
        int trueHeight = 0;
        StorageFolder dumpFramesFolder;
        public async Task UpdateOutput(Color[] data)
        {
            var device = renderPanel.Device;
            try
            {
                RenderTargetViewport.Width = Width;
                RenderTargetViewport.Height = Height;

                RenderTarget = CanvasBitmap.CreateFromColors(renderPanel, data, (int)Width, (int)Height, 96, CanvasAlphaMode.Ignore);
                if (DumpFrames)
                {
                    if (dumpFramesFolder == null)
                    {
                        var root = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("DumpFrames", CreationCollisionOption.OpenIfExists);
                        var time = DateTime.Now.ToString().Replace("/", "_").Replace("\\", "_").Replace(":", "_").Replace(" ", "_");

                        dumpFramesFolder = await root.CreateFolderAsync(time, CreationCollisionOption.ReplaceExisting);
                    }

                    StorageFile tempFile = null;
                    tempFile = await dumpFramesFolder.CreateFileAsync("x86Emulator.png", CreationCollisionOption.GenerateUniqueName);

                    using (var saveStream = (await tempFile.OpenStreamForWriteAsync()).AsRandomAccessStream())
                    {
                        await RenderTarget.SaveAsync(saveStream, CanvasBitmapFileFormat.Png);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        private void RenderPanelDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            Render(args.DrawingSession, sender);
        }
        private void RenderPanelUpdate(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {

        }
        private void RenderPanelLoopStopping(ICanvasAnimatedControl sender, object args)
        {
            callResizeTimer();
        }

        private void UpdateRenderTargetSize(CanvasDrawingSession drawingSession)
        {
            if (RenderTarget != null)
            {
                try
                {
                    var currentSize = RenderTarget.Size;
                    if (currentSize.Width >= CurrentGeometry.MaxWidth && currentSize.Height >= CurrentGeometry.MaxHeight)
                    {
                        return;
                    }
                }
                catch
                {
                    return;
                }
            }
            try
            {
                var size = Math.Max(Math.Max(CurrentGeometry.MaxWidth, CurrentGeometry.MaxHeight), RenderTargetMinSize);
                size = ClosestGreaterPowerTwo(size);

                RenderTarget?.Dispose();
                RenderTarget = BitmapMap.CreateMappableBitmap(drawingSession, size, size);
            }
            catch (Exception e)
            {

            }
        }

        private static Size ComputeBestFittingSize(Size viewportSize, float aspectRatio)
        {
            try
            {
                var candidateWidth = Math.Floor(viewportSize.Height * aspectRatio);
                var size = new Size(candidateWidth, viewportSize.Height);
                if (viewportSize.Width < candidateWidth)
                {
                    var height = viewportSize.Width / aspectRatio;
                    size = new Size(viewportSize.Width, height);
                }

                return size;
            }
            catch (Exception e)
            {
                return viewportSize;
            }
        }

        private static uint ClosestGreaterPowerTwo(uint value)
        {
            uint output = 1;
            while (output < value)
            {
                output *= 2;
            }

            return output;
        }

        #endregion

        #region Main
        public WIN2D(CanvasAnimatedControl panel, VGA device) : base(device)
        {
            dumpFramesFolder = null;
            RenderPanel = panel;
            Memory = new byte[Width * Height * 4]; // BGRA

            CurrentGeometry = new GameGeometry()
            {
                BaseHeight = (uint)Height,
                MaxHeight = (uint)Height,
                BaseWidth = (uint)Width,
                MaxWidth = (uint)Width,
                AspectRatio = 1.6f
            };
            callResizeTimer(true);

            ResetScreen();
        }


        public override void Init()
        {
            if (renderPanel != null)
            {
                RenderPanel.ClearColor = fillColor;
            }
        }

        public override void ResetScreen()
        {
            RenderPanel.ClearColor = fillColor;
        }

        private static void FillDisplay(Color color)
        {
            if (renderPanel != null)
            {
                renderPanel.ClearColor = color;
            }
        }

        public override async Task Cycle()
        {
            var fontBuffer = new byte[0x2000];
            var displayBuffer = new byte[0xfa0];
            Color[] data = new Color[Width * Height];

            x86Emulator.Memory.BlockRead(0xa0000, fontBuffer, fontBuffer.Length);
            x86Emulator.Memory.BlockRead(0xb8000, displayBuffer, displayBuffer.Length);

            for (var i = 0; i < displayBuffer.Length; i += 2)
            {
                int currChar = displayBuffer[i];
                int fontOffset = currChar * 32;
                byte attribute = displayBuffer[i + 1];
                int y = i / 160 * 16; // height

                Color foreColour = vgaDevice.GetColour(attribute & 0xf);
                Color backColour = vgaDevice.GetColour((attribute >> 4) & 0xf);

                for (var f = fontOffset; f < fontOffset + 16; f++)
                {
                    int x = ((i % 160) / 2) * 8; // width

                    for (var j = 7; j >= 0; j--)
                    {
                        if (((fontBuffer[f] >> j) & 0x1) != 0)
                            data[y * Width + x] = foreColour;
                        else
                            data[y * Width + x] = backColour;
                        x++;
                    }
                    y++;
                }
            }
            await UpdateOutput(data);
        }

        #endregion

        #region Canvas Resolver
        public static int[] ASR = new int[] { 4, 3 };
        private Timer ResolveTimer;
        bool timerState = false;
        private void callResizeTimer(bool startState = false)
        {
            try
            {
                ResolveTimer?.Dispose();
                timerState = false;
                if (startState)
                {
                    ResolveTimer = new Timer(async delegate
                    {
                        if (!timerState)
                        {
                            await ResolveCanvasSize();
                        }
                    }, null, 0, 1100);
                }
            }
            catch (Exception e)
            {

            }
        }
        private async Task ResolveCanvasSize()
        {
            try
            {
                timerState = true;

                await Task.Delay(300);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    try
                    {
                        var width = 0d;
                        var height = 0d;
                        var currentHeight = 0d;
                        var currentWidth = 0d;

                        if (FitScreen)
                        {
                            if (renderPanel.VerticalAlignment != VerticalAlignment.Center)
                            {
                                renderPanel.VerticalAlignment = VerticalAlignment.Center;
                            }
                            width = (double)Width;
                            height = (double)Height;
                            currentHeight = PanelRow.ActualHeight;
                            currentWidth = Window.Current.CoreWindow.Bounds.Width;
                        }
                        else
                        {
                            if (renderPanel.VerticalAlignment != VerticalAlignment.Top)
                            {
                                renderPanel.VerticalAlignment = VerticalAlignment.Top;
                            }
                            width = (double)Width;
                            height = (double)Height;

                            currentHeight = PanelRow.ActualHeight;
                            currentWidth = Window.Current.CoreWindow.Bounds.Width;
                        }

                        if (width > 0 && height > 0)
                        {
                            try
                            {
                                double aspectRatio_X = ASR[0];
                                double aspectRatio_Y = ASR[1];

                                double targetHeight = height;
                                if (aspectRatio_X == 0 && aspectRatio_Y == 0)
                                {
                                    //get core aspect
                                    targetHeight = Convert.ToDouble(width) / currentGeometry.AspectRatio;
                                }
                                else
                                {
                                    targetHeight = Convert.ToDouble(width) / (aspectRatio_X / aspectRatio_Y);
                                }
                                height = targetHeight;
                            }
                            catch (Exception ex)
                            {

                            }

                            float ratioX = (float)currentWidth / (float)width;
                            float ratioY = (float)currentHeight / (float)height;
                            float ratio = Math.Min(ratioX, ratioY);

                            float sourceRatio = (float)width / (float)height;

                            // New width and height based on aspect ratio
                            int newWidth = (int)(width * ratio);
                            int newHeight = (int)(height * ratio);

                            if (renderPanel.Height != newHeight)
                            {
                                renderPanel.Height = newHeight;
                            }
                            if (renderPanel.Width != newWidth)
                            {
                                renderPanel.Width = newWidth;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            renderPanel.VerticalAlignment = VerticalAlignment.Stretch;
                            renderPanel.Width = Double.NaN;
                            renderPanel.Height = Double.NaN;
                        }
                        catch (Exception ecx)
                        {

                        }
                    }

                    timerState = false;
                });
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
    }
}
