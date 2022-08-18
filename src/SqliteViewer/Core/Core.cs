using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Base.Core;
using Fusee.Engine.Core.ShaderShards;
using Fusee.Engine.Imp.Graphics.Desktop;
using Fusee.ImGuiDesktop;
using Fusee.ImGuiDesktop.Templates;
using Fusee.Math.Core;
using Fusee.PointCloud.Common;
using ImGuiNET;
using System.Numerics;

namespace Fusee.Examples.SQLiteViewer.Core
{
    [FuseeApplication(Name = "SQLite Pointcloud Viewer",
        Description = "Converts SQLite into a viewable pointcloud.")]
    public class Core : RenderCanvas
    {
        #region StaticBindingVars

        private static bool _dockspaceOpen = true;

        private static int _threshold = 1000000;
        private static float _fuseeViewportMinProj;

        private static int _edlNeighbour = 0;
        private static float _edlStrength = .5f;

        private static int _currentPtShape;
        private static int _currentPtSizeMethod;
        private static int _ptSize = 1;

        private static int _currentColorMode = 1;

        private static Vector4 _ptColor = new(0, 0, 0, 1);
        private static bool _colorPickerOpen;

        private static float _currentFootpulse;
        private static int _stepsize = 10;
        private static float _playerspeed = 5f;

        private static bool _isMouseInsideFuControl;

        // 2D-Camera guiding lines textures.
        private ExposedTexture _on;
        private ExposedTexture _off;

        // "Video player" textures.
        private ExposedTexture _beginningTexture;
        private ExposedTexture _jumpBackTexture;
        private ExposedTexture _playTexture;
        private ExposedTexture _stopTexture;
        private ExposedTexture _jumpForwardTexture;
        private ExposedTexture _endingTexture;

        // Scanner channel button textures.
        private ExposedTexture _green1;
        private ExposedTexture _red1;
        private ExposedTexture _green2;
        private ExposedTexture _red2;
        private ExposedTexture _green3;
        private ExposedTexture _red3;
        private ExposedTexture _green4;
        private ExposedTexture _red4;
        private ExposedTexture _green8;
        private ExposedTexture _red8;
        private ExposedTexture _green9;
        private ExposedTexture _red9;


        private SQLiteViewerControlCore _sqliteViewerControl = null;
        private bool _warned = false;

        #endregion

        public override async Task InitAsync()
        {
            SetImGuiDesign();

            if (!String.IsNullOrEmpty(PtRenderingParams.Instance.PathToSqliteFile))
            {
                Diagnostics.Warn("true");
                _sqliteViewerControl = new SQLiteViewerControlCore(RC);
            }

            ImageData on = await AssetStorage.GetAsync<ImageData>("on.png");
            _on = new ExposedTexture(on);
            RC.RegisterTexture(_on);

            ImageData off = await AssetStorage.GetAsync<ImageData>("off.png");
            _off = new ExposedTexture(off);
            RC.RegisterTexture(_off);

            // "Video player" textures.
            ImageData img = await AssetStorage.GetAsync<ImageData>("beginning1.png");
            _beginningTexture = new ExposedTexture(img);
            RC.RegisterTexture(_beginningTexture);

            ImageData img2 = await AssetStorage.GetAsync<ImageData>("backward1.png");
            _jumpBackTexture = new ExposedTexture(img2);
            RC.RegisterTexture(_jumpBackTexture);

            ImageData img3 = await AssetStorage.GetAsync<ImageData>("play1.png");
            _playTexture = new ExposedTexture(img3);
            RC.RegisterTexture(_playTexture);

            ImageData img4 = await AssetStorage.GetAsync<ImageData>("stop1.png");
            _stopTexture = new ExposedTexture(img4);
            RC.RegisterTexture(_stopTexture);

            ImageData img5 = await AssetStorage.GetAsync<ImageData>("forward1.png");
            _jumpForwardTexture = new ExposedTexture(img5);
            RC.RegisterTexture(_jumpForwardTexture);

            ImageData img6 = await AssetStorage.GetAsync<ImageData>("end1.png");
            _endingTexture = new ExposedTexture(img6);
            RC.RegisterTexture(_endingTexture);

            // Scanner button textures.
            ImageData green1 = await AssetStorage.GetAsync<ImageData>("1_g.png");
            _green1 = new ExposedTexture(green1);
            RC.RegisterTexture(_green1);

            ImageData red1 = await AssetStorage.GetAsync<ImageData>("1_r.png");
            _red1 = new ExposedTexture(red1);
            RC.RegisterTexture(_red1);

            ImageData green2 = await AssetStorage.GetAsync<ImageData>("2_g.png");
            _green2 = new ExposedTexture(green2);
            RC.RegisterTexture(_green2);

            ImageData red2 = await AssetStorage.GetAsync<ImageData>("2_r.png");
            _red2 = new ExposedTexture(red2);
            RC.RegisterTexture(_red2);

            ImageData green3 = await AssetStorage.GetAsync<ImageData>("3_g.png");
            _green3 = new ExposedTexture(green3);
            RC.RegisterTexture(_green3);

            ImageData red3 = await AssetStorage.GetAsync<ImageData>("3_r.png");
            _red3 = new ExposedTexture(red3);
            RC.RegisterTexture(_red3);

            ImageData green4 = await AssetStorage.GetAsync<ImageData>("4_g.png");
            _green4 = new ExposedTexture(green4);
            RC.RegisterTexture(_green4);

            ImageData red4 = await AssetStorage.GetAsync<ImageData>("4_r.png");
            _red4 = new ExposedTexture(red4);
            RC.RegisterTexture(_red4);

            ImageData green8 = await AssetStorage.GetAsync<ImageData>("8_g.png");
            _green8 = new ExposedTexture(green8);
            RC.RegisterTexture(_green8);

            ImageData red8 = await AssetStorage.GetAsync<ImageData>("8_r.png");
            _red8 = new ExposedTexture(red8);
            RC.RegisterTexture(_red8);

            ImageData green9 = await AssetStorage.GetAsync<ImageData>("9_g.png");
            _green9 = new ExposedTexture(green9);
            RC.RegisterTexture(_green9);

            ImageData red9 = await AssetStorage.GetAsync<ImageData>("9_r.png");
            _red9 = new ExposedTexture(red9);
            RC.RegisterTexture(_red9);

            await base.InitAsync();

        }


        public override void Update()
        {
            if (_sqliteViewerControl != null)
            {
                _sqliteViewerControl.Update(_isMouseInsideFuControl);
                if (_sqliteViewerControl.CurrentFootpulse >= _sqliteViewerControl.EndFootpulse)
                {
                    if (File.Exists(FileManager.GetDBDir() + FileManager.NextFile))
                    {
                        Diagnostics.Warn(FileManager.GetDBDir() + FileManager.NextFile + " exists");
                        PtRenderingParams.Instance.PathToOocFile = FileManager.ConvertedDirectory + "/" + Path.GetFileNameWithoutExtension(FileManager.NextFile);
                        PtRenderingParams.Instance.PathToSqliteFile = FileManager.GetDBDir() + FileManager.NextFile;

                        _sqliteViewerControl.Dispose();
                        _sqliteViewerControl = new SQLiteViewerControlCore(RC);
                        _sqliteViewerControl.UpdateOriginalGameWindowDimensions(Width, Height);

                    }
                    else
                    {
                        if (!_warned)
                        {
                            _warned = true;
                            Diagnostics.Warn("Next sqlite file not found.");
                        }
                    }
                }
            }
        }

        public override void Resize(ResizeEventArgs e)
        {
            if (_sqliteViewerControl != null)
            {
                _sqliteViewerControl.UpdateOriginalGameWindowDimensions(e.Width, e.Height);
            }

        }

        public override void RenderAFrame()
        {
            // Set Window flags for Dockspace
            var wndDockspaceFlags =
                    ImGuiWindowFlags.NoDocking
                    | ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoCollapse
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoMove
                    | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoFocusOnAppearing;

            var dockspaceFlags = ImGuiDockNodeFlags.PassthruCentralNode /*| ImGuiDockNodeFlags.AutoHideTabBar*/;

            var viewport = ImGui.GetMainViewport();

            // Set the parent window's position, size, and viewport to match that of the main viewport. This is so the parent window
            // completely covers the main viewport, giving it a "full-screen" feel.
            ImGui.SetNextWindowPos(viewport.WorkPos);
            ImGui.SetNextWindowSize(viewport.WorkSize);
            ImGui.SetNextWindowViewport(viewport.ID);

            // Set the parent window's styles to match that of the main viewport:
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f); // No corner rounding on the window
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f); // No border around the window
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            // Create Dockspace
            ImGui.Begin("DockSpace", ref _dockspaceOpen, wndDockspaceFlags);

            var dockspace_id = ImGui.GetID("DockSpace");
            ImGui.DockSpace(dockspace_id, Vector2.Zero, dockspaceFlags);

            ImGui.PopStyleVar(3);

            // Titlebar
            DrawMainMenuBar();

            // Fusee Viewport
            ImGui.Begin("Viewport",
              ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);

            var parentMin = ImGui.GetWindowContentRegionMin();
            var parentMax = ImGui.GetWindowContentRegionMax();
            var size = parentMax - parentMin;

            // Using a Child allow to fill all the space of the window.
            // It also allows customization
            ImGui.BeginChild("GameRender", size, true, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

            var fuseeViewportMin = ImGui.GetWindowContentRegionMin();
            var fuseeViewportMax = ImGui.GetWindowContentRegionMax();
            var fuseeViewportSize = fuseeViewportMax - fuseeViewportMin;
            var fuseeViewportPos = ImGui.GetWindowPos();

            if (_sqliteViewerControl != null)
            {
                var hndl = _sqliteViewerControl.RenderToTexture((int)fuseeViewportSize.X, (int)fuseeViewportSize.Y);


                ImGui.Image(hndl, fuseeViewportSize,
                    new Vector2(0, 1),
                    new Vector2(1, 0));
            }

            // check if mouse is inside window, if true, accept update() inputs
            _isMouseInsideFuControl = ImGui.IsItemHovered();

            ImGui.EndChild();
            ImGui.End();

            DrawGUI();
            DrawFilePickerDialog();
        }


        internal void DrawGUI()
        {
            int s = 30;  // Image size for buttons.
            int c = 64;
            ImGui.Begin("Controls");

            ImGui.NewLine();
            if (_sqliteViewerControl != null)
            {

                int hndl1 = ((TextureHandle)_beginningTexture.TextureHandle).TexHandle;
                if (ImGui.ImageButton(new IntPtr(hndl1), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnBeginningDown();
                }

                ImGui.SameLine();
                int hndl2 = ((TextureHandle)_jumpBackTexture.TextureHandle).TexHandle;
                if (ImGui.ImageButton(new IntPtr(hndl2), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnBackwardDown(_stepsize);
                }

                ImGui.SameLine();

                if (!_sqliteViewerControl.IsPlaying)
                {
                    int hndl3 = ((TextureHandle)_playTexture.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndl3), new Vector2(s, s)))
                    {
                        _sqliteViewerControl.OnPlayDown();
                    }
                }
                else
                {
                    int hndl4 = ((TextureHandle)_stopTexture.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndl4), new Vector2(s, s)))
                    {
                        _sqliteViewerControl.OnPlayDown();
                    }
                }

                ImGui.SameLine();
                int hndl5 = ((TextureHandle)_jumpForwardTexture.TextureHandle).TexHandle;
                if (ImGui.ImageButton(new IntPtr(hndl5), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnForwardDown(_stepsize);
                }

                ImGui.SameLine();
                int hndl6 = ((TextureHandle)_endingTexture.TextureHandle).TexHandle;
                if (ImGui.ImageButton(new IntPtr(hndl6), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnEndDown();
                }

                ImGui.NewLine();
                ImGui.InputInt("Step size", ref _stepsize, 1, 10);
                if (_stepsize < 0) _stepsize = 0;

                ImGui.NewLine();
                ImGui.InputFloat("Player speed", ref _playerspeed, 5, 10);
                if (_playerspeed < 1) _playerspeed = 1;
                _sqliteViewerControl.Playerspeed = _playerspeed;

                ImGui.NewLine();
                _currentFootpulse = _sqliteViewerControl.CurrentFootpulse;
                ImGui.InputFloat("Footpulse", ref _currentFootpulse, 1, 10, String.Format("{0:0.#}", _currentFootpulse));
                _sqliteViewerControl.CurrentFootpulse = _currentFootpulse;

                ImGui.NewLine();
                ImGui.Text($"Current DB: {Path.GetFileName(PtRenderingParams.Instance.PathToSqliteFile)}");
                
                ImGui.NewLine();
                if (ImGui.Button("Open File"))
                {
                    spwanOpenFilePopup = true;
                }

                ImGui.NewLine();

                ImGui.Text("Toggle scanner channel");

                ImGui.NewLine();
                if (_sqliteViewerControl.Channel1)
                {
                    int hndlc = ((TextureHandle)_green1.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner1();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red1.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner1();
                    }
                }

                ImGui.SameLine();
                if (_sqliteViewerControl.Channel2)
                {
                    int hndlc = ((TextureHandle)_green2.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner2();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red2.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner2();
                    }
                }

                ImGui.SameLine();
                if (_sqliteViewerControl.Channel3)
                {
                    int hndlc = ((TextureHandle)_green3.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner3();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red3.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner3();
                    }
                }

                ImGui.NewLine();
                if (_sqliteViewerControl.Channel4)
                {
                    int hndlc = ((TextureHandle)_green4.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner4();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red4.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner4();
                    }
                }

                ImGui.SameLine();
                if (_sqliteViewerControl.Channel8)
                {
                    int hndlc = ((TextureHandle)_green8.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner8();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red8.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner8();
                    }
                }

                ImGui.SameLine();
                if (_sqliteViewerControl.Channel9)
                {
                    int hndlc = ((TextureHandle)_green9.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner9();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red9.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner9();
                    }
                }

                ImGui.Begin("Settings");
                ImGui.Text("Fusee PointCloud Rendering");
                ImGui.Text($"Application average {1000.0f / ImGui.GetIO().Framerate:0.00} ms/frame ({ImGui.GetIO().Framerate:0} FPS)");

                ImGui.NewLine();
                ImGui.Spacing();

                ImGui.BeginGroup();
                ImGui.Text("Visibility");
                ImGui.InputInt("Point threshold", ref _threshold, 1000, 10000);
                ImGui.SliderFloat("Min. Projection Size Modifier", ref _fuseeViewportMinProj, 0f, 1f);

                PtRenderingParams.Instance.PointThreshold = _threshold;
                PtRenderingParams.Instance.ProjectedSizeModifier = _fuseeViewportMinProj;

                ImGui.EndGroup();

                ImGui.NewLine();
                ImGui.Spacing();
                ImGui.BeginGroup();
                ImGui.Text("Lighting");
                ImGui.SliderInt("EDL Neighbor Px", ref _edlNeighbour, 0, 5);
                ImGui.SliderFloat("EDL Strength", ref _edlStrength, 0f, 5f);

                PtRenderingParams.Instance.EdlStrength = _edlStrength;
                PtRenderingParams.Instance.EdlNoOfNeighbourPx = _edlNeighbour;

                ImGui.EndGroup();

                ImGui.NewLine();
                ImGui.Spacing();
                ImGui.BeginGroup();
                ImGui.Text("Point Shape");
                ImGui.Combo("PointShape", ref _currentPtShape, new string[] { "Paraboloid", "Rect", "Circle" }, 3);

                PtRenderingParams.Instance.Shape = _currentPtShape switch
                {
                    0 => PointShape.Paraboloid,
                    1 => PointShape.Rect,
                    2 => PointShape.Circle,
                    _ => PointShape.Paraboloid
                };

                ImGui.EndGroup();

                ImGui.NewLine();
                ImGui.Spacing();
                ImGui.BeginGroup();
                ImGui.Text("Point Size Method");
                ImGui.Combo("Point Size Method", ref _currentPtSizeMethod, new string[] { "FixedPixelSize", "FixedWorldSize" }, 2);
                ImGui.SliderInt("Point Size", ref _ptSize, 1, 20);

                PtRenderingParams.Instance.Size = _ptSize;
                PtRenderingParams.Instance.PtMode = _currentPtSizeMethod switch
                {
                    0 => PointCloud.Common.PointSizeMode.FixedPixelSize,
                    1 => PointCloud.Common.PointSizeMode.FixedWorldSize,
                    _ => PointCloud.Common.PointSizeMode.FixedPixelSize
                };

                ImGui.EndGroup();

                ImGui.NewLine();
                ImGui.Spacing();
                ImGui.BeginGroup();
                ImGui.Text("Color Mode");

                ImGui.Combo("Color mode", ref _currentColorMode, new string[] { "BaseColor", "VertexColor0", "VertexColor1", "VertexColor2" }, 4);

                PtRenderingParams.Instance.ColorMode = _currentColorMode switch
                {
                    0 => ColorMode.BaseColor,
                    1 => ColorMode.VertexColor0,
                    2 => ColorMode.VertexColor1,
                    3 => ColorMode.VertexColor2,
                    _ => ColorMode.VertexColor0
                };

                ImGui.Spacing();
                ImGui.BeginGroup();
                ImGui.Text("Background Color");

                if (ImGui.ColorButton("Toggle Color Picker", _ptColor, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                {
                    _colorPickerOpen = !_colorPickerOpen;
                }
                if (_colorPickerOpen)
                {
                    ImGui.Begin("Color Picker", ref _colorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                    ImGui.ColorPicker4("Color", ref _ptColor);
                    ImGui.End();
                    ImGui.GetStyle().Colors[(int)ImGuiCol.ChildBg] = _ptColor;
                    if (_sqliteViewerControl != null)
                    {
                        _sqliteViewerControl.CameraBackgroundColor = _ptColor.ToFuseeVector();
                    }
                    //PtRenderingParams.Instance.ColorPassEf.SurfaceInput.Albedo = _ptColor.ToFuseeVector();
                }
                ImGui.EndGroup();

                ImGui.EndGroup();
                ImGui.NewLine();

                ImGui.Text("Toggle 2D Camera guides");
                if (_sqliteViewerControl.GuideLinesOn)
                {
                    int onhndl = ((TextureHandle)_on.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(onhndl), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleGuidelines();
                    }
                }
                else
                {
                    int onhndl = ((TextureHandle)_off.TextureHandle).TexHandle;
                    if (ImGui.ImageButton(new IntPtr(onhndl), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleGuidelines();
                    }
                }
                ImGui.End();
            }
        }

        internal void DrawMainMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Menu"))
                {
                    if (ImGui.MenuItem("Open"))
                    {
                        spwanOpenFilePopup = true;
                    }
                    if (ImGui.MenuItem("Exit"))
                    {
                        Environment.Exit(0);
                    }
                    ImGui.EndMenu();
                }
            }
            ImGui.EndMainMenuBar();
        }

        bool filePickerOpen = true;
        bool spwanOpenFilePopup = false;

        private void DrawFilePickerDialog()
        {
            if (spwanOpenFilePopup)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(700, 555), ImGui.GetWindowViewport().Size);

                ImGui.OpenPopup("open-file");
                spwanOpenFilePopup = false;
            }

            if (ImGui.BeginPopupModal("open-file", ref filePickerOpen, ImGuiWindowFlags.NoTitleBar))
            {
                var picker = ImGuiFileDialog.GetFilePicker(this, Path.Combine(Environment.CurrentDirectory, ""), new float4(30, 180, 30, 255), ".sqlite");
                if (picker.Draw())
                {
                    if (string.IsNullOrWhiteSpace(picker.SelectedFile)) return;

                    var file = picker.SelectedFile;
                    Path.GetFileNameWithoutExtension(file);

                    PtRenderingParams.Instance.PathToOocFile = FileManager.ConvertedDirectory + "/" + Path.GetFileNameWithoutExtension(file);
                    PtRenderingParams.Instance.PathToSqliteFile = file;

                    if (_sqliteViewerControl != null)
                    {
                        _sqliteViewerControl.Dispose();

                    }
                    _sqliteViewerControl = new SQLiteViewerControlCore(RC);
                    _sqliteViewerControl.UpdateOriginalGameWindowDimensions(Width, Height);

                    // reset color picker
                    _currentColorMode = 1;
                    ImGuiFileDialog.RemoveFilePicker(this);
                }
                ImGui.EndPopup();
            }
        }

        /// <summary>
        /// Place all design/styles inside this method
        /// </summary>
        internal static void SetImGuiDesign()
        {
            var style = ImGui.GetStyle();
            var colors = style.Colors;

            style.WindowRounding = 2.0f;             // Radius of window corners rounding. Set to 0.0f to have rectangular windows
            style.ScrollbarRounding = 3.0f;             // Radius of grab corners rounding for scrollbar
            style.GrabRounding = 2.0f;             // Radius of grabs corners rounding. Set to 0.0f to have rectangular slider grabs.
            style.AntiAliasedLines = true;
            style.AntiAliasedFill = true;
            style.WindowRounding = 2;
            style.ChildRounding = 2;
            style.ScrollbarSize = 16;
            style.ScrollbarRounding = 3;
            style.GrabRounding = 2;
            style.ItemSpacing.X = 10;
            style.ItemSpacing.Y = 4;
            style.IndentSpacing = 22;
            style.FramePadding.X = 6;
            style.FramePadding.Y = 4;
            style.Alpha = 1.0f;
            style.FrameRounding = 3.0f;

            colors[(int)ImGuiCol.Text] = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
            //color(int)s[ImGuiCol_ChildWindowBg]         = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.95f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.93f, 0.93f, 0.93f, 0.98f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.71f, 0.71f, 0.71f, 0.08f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.04f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.71f, 0.71f, 0.71f, 0.55f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.94f, 0.94f, 0.94f, 0.55f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.71f, 0.78f, 0.69f, 0.98f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.85f, 0.85f, 0.85f, 1.00f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.82f, 0.78f, 0.78f, 0.51f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.78f, 0.78f, 0.78f, 1.00f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.20f, 0.25f, 0.30f, 0.61f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.90f, 0.90f, 0.90f, 0.30f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.92f, 0.92f, 0.92f, 0.78f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.184f, 0.407f, 0.193f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.26f, 0.59f, 0.98f, 0.78f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.71f, 0.78f, 0.69f, 0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.725f, 0.805f, 0.702f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.793f, 0.900f, 0.836f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.71f, 0.78f, 0.69f, 0.31f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.71f, 0.78f, 0.69f, 0.80f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.71f, 0.78f, 0.69f, 1.00f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.78f);
            colors[(int)ImGuiCol.TabActive] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.Separator] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.14f, 0.44f, 0.80f, 0.78f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.14f, 0.44f, 0.80f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(1.00f, 1.00f, 1.00f, 0.00f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.45f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.78f);
            colors[(int)ImGuiCol.PlotLines] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
            //colors[(int)ImGuiCol.ModalWindowDarkening] = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);
            colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
            colors[(int)ImGuiCol.NavHighlight] = colors[(int)ImGuiCol.HeaderHovered];
            colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(0.70f, 0.70f, 0.70f, 0.70f);
        }
    }
}