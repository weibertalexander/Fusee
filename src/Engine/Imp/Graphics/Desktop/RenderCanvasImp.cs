using Fusee.Base.Core;
using Fusee.Engine.Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Image = OpenTK.Windowing.Common.Input.Image;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

// friend: Fusee.Tests.Render.Desktop
[assembly: InternalsVisibleTo("Fusee.Tests.Render.Desktop")]

namespace Fusee.Engine.Imp.Graphics.Desktop
{

    /// <summary>
    /// This is a default render canvas implementation creating its own rendering window.
    /// </summary>
    public class RenderCanvasImp : RenderCanvasImpBase, IRenderCanvasImp, IDisposable
    {
        #region Fields

        //Some tryptichon related variables.
        private bool _videoWallMode = false;
        private int _videoWallMonitorsHor;
        private int _videoWallMonitorsVert;
        private bool _windowBorderHidden = false;

        /// <summary>
        /// Window handle for the window the engine renders to.
        /// </summary>
        public IWindowHandle WindowHandle { get; }

        /// <summary>
        /// Implementation Tasks: Gets and sets the width(pixel units) of the Canvas.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public int Width
        {
            get => BaseWidth;
            set
            {
                _gameWindow.Size = new OpenTK.Mathematics.Vector2i(value, _gameWindow.Size.Y);
                BaseWidth = value;
                ResizeWindow();
            }
        }

        /// <summary>
        /// Gets and sets the height in pixel units.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
        public int Height
        {
            get => BaseHeight;
            set
            {
                _gameWindow.Size = new OpenTK.Mathematics.Vector2i(_gameWindow.Size.X, value);
                BaseHeight = value;
                ResizeWindow();
            }
        }

        /// <summary>
        /// Gets and sets the caption(title of the window).
        /// </summary>
        /// <value>
        /// The caption.
        /// </value>
        public string Caption
        {
            get => (_gameWindow == null) ? "" : _gameWindow.Title;
            set { if (_gameWindow != null) _gameWindow.Title = value; }
        }

        /// <summary>
        /// Gets the delta time.
        /// The delta time is the time that was required to render the last frame in milliseconds.
        /// This value can be used to determine the frames per second of the application.
        /// </summary>
        /// <value>
        /// The delta time in milliseconds.
        /// </value>
        public float DeltaTime
        {
            get
            {
                if (_gameWindow != null)
                    return _gameWindow.DeltaTime;
                return 0.01f;
            }
        }

        /// <summary>
        /// Gets the delta time.
        /// The delta time is the time that was required to update the last frame in milliseconds.
        /// </summary>
        /// <value>
        /// The delta time in milliseconds.
        /// </value>
        public float DeltaTimeUpdate
        {
            get
            {
                if (_gameWindow != null)
                    return _gameWindow.DeltaTimeUpdate;
                return 0.01f;
            }
        }

        /// <summary>
        /// Gets and sets a value indicating whether [vertical synchronize].
        /// This option is used to reduce "Glitches" during rendering.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [vertical synchronize]; otherwise, <c>false</c>.
        /// </value>
        public bool VerticalSync
        {
            get => (_gameWindow != null) && _gameWindow.VSync == OpenTK.Windowing.Common.VSyncMode.On;
            set { if (_gameWindow != null) _gameWindow.VSync = value ? OpenTK.Windowing.Common.VSyncMode.On : OpenTK.Windowing.Common.VSyncMode.Off; }
        }

        /// <summary>
        /// Gets and sets a value indicating whether [enable blending].
        /// Blending is used to render transparent objects.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable blending]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableBlending
        {
            get => _gameWindow.Blending;
            set => _gameWindow.Blending = value;
        }

        /// <summary>
        /// Gets and sets a value indicating whether [fullscreen] is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [fullscreen]; otherwise, <c>false</c>.
        /// </value>
        public bool Fullscreen
        {
            get => (_gameWindow.WindowState == OpenTK.Windowing.Common.WindowState.Fullscreen);
            set => _gameWindow.WindowState = (value) ? OpenTK.Windowing.Common.WindowState.Fullscreen : OpenTK.Windowing.Common.WindowState.Normal;
        }

        /// <summary>
        /// Gets a value indicating whether [focused].
        /// This property is used to identify if this application is the active window of the user.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [focused]; otherwise, <c>false</c>.
        /// </value>
        public bool Focused => _gameWindow.IsFocused;

        #region Tryptichon related Fields.

        /// <summary>
        /// Activates (true) or deactivates (false) the video wall feature.
        /// </summary>
        public bool VideoWallMode
        {
            get => _videoWallMode;
            set => _videoWallMode = value;
        }

        /// <summary>
        /// This represents the number of the monitors in a vertical column.
        /// </summary>
        public int TryptMonitorSetupVertical
        {
            get => _videoWallMonitorsVert;
            set => _videoWallMonitorsVert = value;
        }

        /// <summary>
        /// This represents the number of the monitors in a horizontal row.
        /// </summary>
        public int TryptMonitorSetupHorizontal
        {
            get => _videoWallMonitorsHor;
            set => _videoWallMonitorsHor = value;
        }

        #endregion

        internal RenderCanvasGameWindow _gameWindow;

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderCanvasImp"/> class.
        /// </summary>
        public RenderCanvasImp() : this(null, false)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderCanvasImp"/> class.
        /// </summary>
        /// <param name="isMultithreaded">If true OpenTk will call run() in a new Thread. The default value is false.</param>
        /// <param name="icon">The window icon to use</param>
        /// <param name="startVisible">Define if window is visible from the start, default: true.</param>
        public RenderCanvasImp(ImageData icon = null, bool isMultithreaded = false, bool startVisible = true)
        {
            //TODO: Select correct monitor
            MonitorInfo mon = Monitors.GetMonitors()[0];

            int width = 1280;
            int height = 720;

            if (mon != null)
            {
                width = System.Math.Min(mon.HorizontalResolution - 100, width);
                height = System.Math.Min(mon.VerticalResolution - 100, height);
            }

            try
            {
                _gameWindow = new RenderCanvasGameWindow(this, width, height, false, isMultithreaded, startVisible);
            }
            catch
            {
                _gameWindow = new RenderCanvasGameWindow(this, width, height, false, isMultithreaded, startVisible);
            }

            WindowHandle = new WindowHandle()
            {
                Handle = _gameWindow.Context.WindowPtr
            };

            _gameWindow.CenterWindow();

            if (_gameWindow.IsMultiThreaded)
                _gameWindow.Context.MakeNoneCurrent();

            // convert icon to OpenTKImage
            if (icon != null)
            {
                var res = new Span<Rgba32>(new Rgba32[width * height]);
                var pxData = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(icon.PixelData, icon.Width, icon.Height);
                pxData.Mutate(x => x.AutoOrient());
                pxData.Mutate(x => x.RotateFlip(RotateMode.None, FlipMode.Vertical));

                pxData.CopyPixelDataTo(res);

                var resBytes = MemoryMarshal.AsBytes<Rgba32>(res.ToArray());
                _gameWindow.Icon = new WindowIcon(new Image[] { new Image(icon.Width, icon.Height, resBytes.ToArray()) });
            }
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="RenderCanvasImp"/> class.
        /// </summary>
        /// <param name="width">The width of the render window.</param>
        /// <param name="height">The height of the render window.</param>
        /// <param name="isMultithreaded">If true OpenTk will call run() in a new Thread. The default value is false.</param>
        /// <remarks>The window created by this constructor is not visible. Should only be used for internal testing.</remarks>
        public RenderCanvasImp(int width, int height, bool isMultithreaded = false)
        {
            try
            {
                _gameWindow = new RenderCanvasGameWindow(this, width, height, true, isMultithreaded);
            }
            catch
            {
                _gameWindow = new RenderCanvasGameWindow(this, width, height, false, isMultithreaded);
            }

            WindowHandle = new WindowHandle()
            {
                Handle = _gameWindow.Context.WindowPtr
            };

            _gameWindow.IsVisible = false;

            if (_gameWindow.IsMultiThreaded)
                _gameWindow.Context.MakeNoneCurrent();
        }

        /// <summary>
        /// Implementation of the Dispose pattern. Disposes of the OpenTK game window.
        /// </summary>
        public void Dispose()
        {
            _gameWindow.Dispose();
        }

        #endregion

        #region Members

        private void ResizeWindow()
        {
            if (!_videoWallMode)
            {
                _gameWindow.WindowBorder = _windowBorderHidden ? OpenTK.Windowing.Common.WindowBorder.Hidden : OpenTK.Windowing.Common.WindowBorder.Resizable;
                _gameWindow.Bounds = new OpenTK.Mathematics.Box2i(BaseLeft, BaseTop, BaseWidth, BaseHeight);
            }
            else
            {
                //TODO: Select correct monitor
                MonitorInfo mon = Monitors.GetMonitors()[0];

                var oneScreenWidth = mon.HorizontalResolution;
                var oneScreenHeight = mon.VerticalResolution;

                var width = oneScreenWidth * _videoWallMonitorsHor;
                var height = oneScreenHeight * _videoWallMonitorsVert;

                _gameWindow.Bounds = new OpenTK.Mathematics.Box2i(0, 0, width, height);

                if (_windowBorderHidden)
                    _gameWindow.WindowBorder = OpenTK.Windowing.Common.WindowBorder.Hidden;
            }
        }

        /// <summary>
        /// Changes the window of the application to video wall mode.
        /// </summary>
        /// <param name="monitorsHor">Number of monitors on horizontal axis.</param>
        /// <param name="monitorsVert">Number of monitors on vertical axis.</param>
        /// <param name="activate">Start the window in activated state-</param>
        /// <param name="borderHidden">Start the window with a hidden windows border.</param>
        public void VideoWall(int monitorsHor = 1, int monitorsVert = 1, bool activate = true, bool borderHidden = false)
        {
            VideoWallMode = activate;
            _videoWallMonitorsHor = monitorsHor > 0 ? monitorsHor : 1;
            _videoWallMonitorsVert = monitorsVert > 0 ? monitorsVert : 1;
            _windowBorderHidden = borderHidden;

            ResizeWindow();
        }

        /// <summary>
        /// Sets the size of the output window for desktop development.
        /// </summary>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        /// <param name="posx">The x position of the window.</param>
        /// <param name="posy">The y position of the window.</param>
        /// <param name="borderHidden">Show the window border or not.</param>
        public void SetWindowSize(int width, int height, int posx = -1, int posy = -1, bool borderHidden = false)
        {
            MonitorInfo mon = Monitors.GetMonitors()[0];

            BaseWidth = width;
            BaseHeight = height;

            BaseLeft = (posx == -1) ? mon.HorizontalResolution / 2 - width / 2 : posx;
            BaseTop = (posy == -1) ? mon.VerticalResolution / 2 - height / 2 : posy;

            _windowBorderHidden = borderHidden;

            // Disable video wall mode for this because it would not make sense.
            _videoWallMode = false;

            ResizeWindow();
        }

        /// <summary>
        /// Closes the GameWindow with a call to OpenTk.
        /// </summary>
        public void CloseGameWindow()
        {
            if (_gameWindow != null)
            {
                NativeWindow.ProcessWindowEvents(true);
                _gameWindow.Close();
                _gameWindow.Dispose();
            }
        }

        /// <summary>
        /// Presents this application instance. Call this function after rendering to show the final image.
        /// After Present is called the render buffers get flushed.
        /// </summary>
        public void Present()
        {
            if (!_gameWindow.IsExiting)
                _gameWindow.SwapBuffers();
        }

        /// <summary>
        /// Set the cursor (the mouse pointer image) to one of the predefined types
        /// </summary>
        /// <param name="cursorType">The type of the cursor to set.</param>
        public void SetCursor(CursorType cursorType)
        {
            // Currently not supported by OpenTK... Too bad.
        }

        /// <summary>
        /// Opens the given URL in the user's standard web browser. The link MUST start with "http://".
        /// </summary>
        /// <param name="link">The URL to open</param>
        public void OpenLink(string link)
        {
            if (link.StartsWith("http://"))
            {
                //UseShellExecute needs to be set to true in .net 3.0. See:https://github.com/dotnet/corefx/issues/33714
                ProcessStartInfo psi = new()
                {
                    FileName = link,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }

        /// <summary>
        /// Implementation Tasks: Runs this application instance. This function should not be called more than once as its only for initialization purposes.
        /// </summary>
        public void Run()
        {
            if (_gameWindow != null)
            {
                _gameWindow.UpdateFrequency = 0;
                _gameWindow.RenderFrequency = 0;

                _gameWindow.Run();
            }
        }

        /// <summary>
        /// Creates a bitmap image from the current frame of the application.
        /// </summary>
        /// <param name="width">The width of the window, and therefore image to render.</param>
        /// <param name="height">The height of the window, and therefore image to render.</param>
        /// <returns></returns>
        public SixLabors.ImageSharp.Image ShootCurrentFrame(int width, int height)
        {
            var mem = new byte[width * height * 4];

            //_gameWindow.Context.MakeCurrent();

            GL.Flush();
            //GL.PixelStore(PixelStoreParameter.PackRowLength, 1);
            GL.ReadPixels(0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, mem);
            //GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Bgra, PixelType.UnsignedByte, mem);

            var img = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(mem, width, height);

            img.Mutate(x => x.AutoOrient());
            img.Mutate(x => x.RotateFlip(RotateMode.None, FlipMode.Vertical));
            return img;
        }

        #endregion
    }

    /// <summary>
    /// OpenTK implementation of RenderCanvas for the window output.
    /// </summary>
    public class RenderCanvasImpBase
    {
        #region Fields

        /// <summary>
        /// The Width
        /// </summary>
        protected internal int BaseWidth;

        /// <summary>
        /// The Height
        /// </summary>
        protected internal int BaseHeight;

        /// <summary>
        /// The Top Position
        /// </summary>
        protected internal int BaseTop;

        /// <summary>
        /// The Left Position
        /// </summary>
        protected internal int BaseLeft;

        #endregion

        #region Events
        /// <summary>
        /// Occurs when [initialize].
        /// </summary>
        public event EventHandler<InitEventArgs> Init;
        /// <summary>
        /// Occurs when [unload].
        /// </summary>
        public event EventHandler<InitEventArgs> UnLoad;
        /// <summary>
        /// Occurs when [update].
        /// </summary>
        public event EventHandler<RenderEventArgs> Update;
        /// <summary>
        /// Occurs when [render].
        /// </summary>
        public event EventHandler<RenderEventArgs> Render;
        /// <summary>
        /// Occurs when [resize].
        /// </summary>
        public event EventHandler<ResizeEventArgs> Resize;

        #endregion

        #region Internal Members

        /// <summary>
        /// Does the initialize of this instance.
        /// </summary>
        protected internal void DoInit()
        {
            Init?.Invoke(this, new InitEventArgs());
        }

        /// <summary>
        /// Does the unload of this instance.
        /// </summary>
        protected internal void DoUnLoad()
        {
            UnLoad?.Invoke(this, new InitEventArgs());
        }

        /// <summary>
        /// Does the update of this instance.
        /// </summary>
        protected internal void DoUpdate()
        {
            Update?.Invoke(this, new RenderEventArgs());
        }

        /// <summary>
        /// Does the render of this instance.
        /// </summary>
        protected internal void DoRender()
        {
            Render?.Invoke(this, new RenderEventArgs());
        }

        /// <summary>
        /// Does the resize on this instance.
        /// </summary>
        protected internal void DoResize(int width, int height)
        {
            Resize?.Invoke(this, new ResizeEventArgs(width, height));
        }

        #endregion
    }

    internal class RenderCanvasGameWindow : GameWindow
    {
        #region Fields

        private readonly RenderCanvasImp _renderCanvasImp;

        /// <summary>
        /// True if the GameWindow/ the application uses multiple threads.
        /// With OpenTK 4.7 we need to use the "new" modifier to hide the GameWindow.IsMultithreaded property, which became obsolete in this version.
        /// </summary>
        public new bool IsMultiThreaded { get; private set; } = false;

        /// <summary>
        /// Gets the delta time.
        /// The delta time is the time that was required to render the last frame in milliseconds.
        /// This value can be used to determine the frames per second of the application.
        /// </summary>
        /// <value>
        /// The delta time in milliseconds.
        /// </value>
        public float DeltaTime { get; private set; }

        /// <summary>
        /// Gets the delta time.
        /// The delta time is the time that was required to update the last frame in milliseconds.
        /// </summary>
        /// <value>
        /// The delta time in milliseconds.
        /// </value>
        public float DeltaTimeUpdate { get; private set; }

        /// <summary>
        /// Gets and sets a value indicating whether [blending].
        /// Blending is used to render transparent objects.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [blending]; otherwise, <c>false</c>.
        /// </value>
        public bool Blending
        {
            get => GL.IsEnabled(EnableCap.Blend);
            set
            {
                if (value)
                {
                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                }
                else
                {
                    GL.Disable(EnableCap.Blend);
                }
            }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderCanvasGameWindow"/> class.
        /// </summary>
        /// <param name="renderCanvasImp">The render canvas implementation.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="antiAliasing">if set to <c>true</c> [anti aliasing] is on.</param>
        /// <param name="isMultithreaded">If true OpenTk will call run() in a new Thread. The default value is false.</param>
        /// <param name="startVisible">Should the window be visible from the start, default: true.</param>
        public RenderCanvasGameWindow(RenderCanvasImp renderCanvasImp, int width, int height, bool antiAliasing, bool isMultithreaded = false, bool startVisible = true)
            : base(new GameWindowSettings(), new NativeWindowSettings
            {
                Size = new OpenTK.Mathematics.Vector2i(width, height),
                Profile = OpenTK.Windowing.Common.ContextProfile.Core,
                Flags = OpenTK.Windowing.Common.ContextFlags.ForwardCompatible,
                StartVisible = startVisible
            })
        {
            IsMultiThreaded = isMultithreaded;
            _renderCanvasImp = renderCanvasImp;
            _renderCanvasImp.BaseWidth = width;
            _renderCanvasImp.BaseHeight = height;
        }

        #endregion

        #region Overrides

        protected override void OnLoad()
        {
            // Check for necessary capabilities
            string version = GL.GetString(StringName.Version);

            int major = version[0];
            // int minor = (int)version[2];

            if (major < 2)
            {
                throw new InvalidOperationException("You need at least OpenGL 2.0 to run this example. GLSL not supported.");
            }

            // Use VSync!
            VSync = OpenTK.Windowing.Common.VSyncMode.On;

            _renderCanvasImp.DoInit();
        }

        protected override void OnUnload()
        {
            _renderCanvasImp.DoUnLoad();
            _renderCanvasImp.Dispose();
        }

        protected override void OnResize(OpenTK.Windowing.Common.ResizeEventArgs e)
        {
            base.OnResize(e);

            if (_renderCanvasImp != null)
            {
                _renderCanvasImp.BaseWidth = e.Width;
                _renderCanvasImp.BaseHeight = e.Height;
                _renderCanvasImp.DoResize(e.Width, e.Height);
            }
        }

        protected override void OnUpdateFrame(OpenTK.Windowing.Common.FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            DeltaTimeUpdate = (float)args.Time;

            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F11))
                WindowState = (WindowState != OpenTK.Windowing.Common.WindowState.Fullscreen) ? OpenTK.Windowing.Common.WindowState.Fullscreen : OpenTK.Windowing.Common.WindowState.Normal;

            _renderCanvasImp?.DoUpdate();
        }

        protected override void OnRenderFrame(OpenTK.Windowing.Common.FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            DeltaTime = (float)args.Time;

            _renderCanvasImp?.DoRender();
        }

        #endregion
    }
}