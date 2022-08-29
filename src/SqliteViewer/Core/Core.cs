using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Base.Core;
using Fusee.Base.Common;
using Fusee.Engine.Core.ShaderShards;
using Fusee.Engine.Imp.Graphics.Desktop;
using Fusee.ImGuiImp.Desktop;
using Fusee.ImGuiImp.Desktop.Templates;
using Fusee.Math.Core;
using Fusee.PointCloud.Common;
using ImGuiNET;
using System.Numerics;
using System.Threading;

namespace Fusee.Examples.SQLiteViewer.Core
{
    [FuseeApplication(Name = $"SQLite Pointcloud Viewer",
        Description = "Converts SQLite into a viewable pointcloud.")]
    public class Core : RenderCanvas
    {
        #region StaticBindingVars

        private static bool _dockspaceOpen = true;

        private static int _edlNeighbour = 0;
        private static float _edlStrength = 0f;

        private static int _currentPtShape;
        private static int _currentPtSizeMethod;
        private static int _ptSize = 1;

        private static int _currentColorMode = 1;

        private static Vector4 _2DbgColor = new(0, 0, 0, 1);
        private static Vector4 _3DbgColor = new(0, 0, 0, 1);
        private static bool _2DcolorPickerOpen;
        private static bool _3DcolorPickerOpen;
        private static bool _cloud1ColorPickerOpen;
        private static bool _cloud2ColorPickerOpen;
        private static bool _cloud3ColorPickerOpen;
        private static bool _cloud4ColorPickerOpen;
        private static bool _cloud8ColorPickerOpen;
        private static bool _cloud9ColorPickerOpen;

        private static Vector4 _scn1Color = new(1, 1, 1, 1);
        private static Vector4 _scn2Color = new(1, 1, 1, 1);
        private static Vector4 _scn3Color = new(1, 1, 1, 1);
        private static Vector4 _scn4Color = new(1, 1, 1, 1);
        private static Vector4 _scn8Color = new(1, 1, 1, 1);
        private static Vector4 _scn9Color = new(1, 1, 1, 1);

        private static float _currentFootpulse;
        private static int _stepsize = 10;
        private static float _playerspeed = 5f;

        private static bool _isMouseInside3DWindow;
        private static bool _isMouseInside2DWindow;

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

        private ImGuiFilePicker _picker;
        private bool _spawnOpenFilePopup = false;

        private string _currentlyConvertingFile = "";

        private int _numberOfFiles = 0;
        private int _convertedFiles = 0;

        private bool _iniLoaded = false;

        private float _camera2DRenderDistance = 1f;
        private int _camera3DRenderDistance = 100;

        #endregion

        public override async Task InitAsync()
        {
            if (File.Exists(Path.Combine("Assets/MyImGuiSettings.ini")))
            {
                ImGui.LoadIniSettingsFromDisk(Path.Combine("Assets/MyImGuiSettings.ini"));
            }

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

            // File Picker
            _picker = new ImGuiFilePicker(Path.Combine(Environment.CurrentDirectory, ""), false, ".sqlite");
            _picker.OnPicked += (s, file) =>
            {
                if (string.IsNullOrEmpty(file)) return;

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
                PtRenderingParams.Instance.PathToOocFile = new FileInfo(file).Directory.FullName;
                _currentlyConvertingFile = new FileInfo(file).FullName;

                // Thread used for converting following SQLite files.
                Thread t = new Thread(() => ConvertFiles());
                t.Start();
            };

            await base.InitAsync();
        }

        // Convert all SQLite files in the same target directory as the picked file.
        private async Task<bool> ConvertFiles()
        {
            _numberOfFiles = FileManager.GetSqliteFiles().Length;
            Diagnostics.Debug(_currentlyConvertingFile + " " + _numberOfFiles);
            _convertedFiles = 1;
            if (_currentlyConvertingFile != "")
            {
                for (int i = 0; i < _numberOfFiles; i++)
                {
                    Diagnostics.Debug("Converting file no. " + _convertedFiles + " from " + _numberOfFiles);
                    _currentlyConvertingFile = PtRenderingParams.Instance.PathToOocFile + "/" + FileManager.NextFileFromPath(_currentlyConvertingFile);
                    if (File.Exists(_currentlyConvertingFile) && await FileManager.CreateOctreeFromDBAsync(_currentlyConvertingFile))
                    {
                        _convertedFiles++;
                    }
                    Diagnostics.Debug(_convertedFiles + " converted");
                }
            }
            return true;
        }

        private void DrawFilePickerDialog()
        {
            _picker.Draw(ref _spawnOpenFilePopup);
        }

        public override void Update()
        {
            if (_sqliteViewerControl != null)
            {
                _sqliteViewerControl.Update(false);
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
                _sqliteViewerControl?.UpdateOriginalGameWindowDimensions(e.Width, e.Height);
            }

        }

        public override void RenderAFrame()
        {
            // Enable Dockspace
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

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

            if (_sqliteViewerControl != null)
            {
                // Fusee Viewport
                ImGui.Begin("3D Viewport",
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);

                var parentMin3D = ImGui.GetWindowContentRegionMin();
                var parentMax3D = ImGui.GetWindowContentRegionMax();
                var size3D = parentMax3D - parentMin3D;

                // Using a Child allow to fill all the space of the window.
                // It also allows customization
                ImGui.BeginChild("3DGameRender", size3D, true, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

                var fuseeViewportMin3D = ImGui.GetWindowContentRegionMin();
                var fuseeViewportMax3D = ImGui.GetWindowContentRegionMax();
                var fuseeViewportSize3D = fuseeViewportMax3D - fuseeViewportMin3D;
                var fuseeViewportPos3D = ImGui.GetWindowPos();

                _sqliteViewerControl._render2DFrame = false;
                var hndl3D = _sqliteViewerControl.RenderToTexture((int)fuseeViewportSize3D.X, (int)fuseeViewportSize3D.Y);

                ImGui.Image(hndl3D, fuseeViewportSize3D,
                    new Vector2(0, 1),
                    new Vector2(1, 0));

                // check if mouse is inside window, if true, accept update() inputs
                _isMouseInside3DWindow = ImGui.IsWindowFocused();
                _sqliteViewerControl.Controls3D = _isMouseInside3DWindow;

                ImGui.EndChild();

                ImGui.Begin("2D Viewport",
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);

                var parentMin2D = ImGui.GetWindowContentRegionMin();
                var parentMax2D = ImGui.GetWindowContentRegionMax();
                var size2D = parentMax2D - parentMin2D;

                // Using a Child allow to fill all the space of the window.
                // It also allows customization
                ImGui.BeginChild("2DGameRender", size2D, true, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);


                var fuseeViewportMin2D = ImGui.GetWindowContentRegionMin();
                var fuseeViewportMax2D = ImGui.GetWindowContentRegionMax();
                var fuseeViewportSize2D = fuseeViewportMax2D - fuseeViewportMin2D;
                var fuseeViewportPos2D = ImGui.GetWindowPos();

                _sqliteViewerControl.width2d = (int)fuseeViewportSize2D.X;
                _sqliteViewerControl.height2d = (int)fuseeViewportSize2D.Y;

                _sqliteViewerControl._render2DFrame = true;
                var hndl2D = _sqliteViewerControl.RenderToTexture((int)fuseeViewportSize2D.X, (int)fuseeViewportSize2D.Y);

                ImGui.Image(hndl2D, fuseeViewportSize2D,
                    new Vector2(0, 1),
                    new Vector2(1, 0));

                // check if mouse is inside window, if true, accept update() inputs
                _isMouseInside2DWindow = ImGui.IsWindowFocused();
                _sqliteViewerControl.Controls2D = _isMouseInside2DWindow;

                ImGui.EndChild();

                ImGui.End();
            }
            DrawGUI();
            DrawFilePickerDialog();
        }
        internal void DrawGUI()
        {
            int s = 32;  // Image size for buttons.
            int c = 64;


            if (_sqliteViewerControl != null)
            {
                ImGui.Begin("Video Player");

                int hndl1 = ((TextureHandle)_beginningTexture.TextureHandle).TexId;
                if (ImGui.ImageButton(new IntPtr(hndl1), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnBeginningDown();
                }

                ImGui.SameLine();
                int hndl2 = ((TextureHandle)_jumpBackTexture.TextureHandle).TexId;
                if (ImGui.ImageButton(new IntPtr(hndl2), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnBackwardDown(_stepsize);
                }

                ImGui.SameLine();

                if (!_sqliteViewerControl.IsPlaying)
                {
                    int hndl3 = ((TextureHandle)_playTexture.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndl3), new Vector2(s, s)))
                    {
                        _sqliteViewerControl.OnPlayDown();
                    }
                }
                else
                {
                    int hndl4 = ((TextureHandle)_stopTexture.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndl4), new Vector2(s, s)))
                    {
                        _sqliteViewerControl.OnPlayDown();
                    }
                }

                ImGui.SameLine();
                int hndl5 = ((TextureHandle)_jumpForwardTexture.TextureHandle).TexId;
                if (ImGui.ImageButton(new IntPtr(hndl5), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnForwardDown(_stepsize);
                }

                ImGui.SameLine();
                int hndl6 = ((TextureHandle)_endingTexture.TextureHandle).TexId;
                if (ImGui.ImageButton(new IntPtr(hndl6), new Vector2(s, s)))
                {
                    _sqliteViewerControl.OnEndDown();
                }

                ImGui.NewLine();

                // Video Player Settings
                ImGui.BeginGroup();

                ImGui.NewLine();
                _currentFootpulse = _sqliteViewerControl.CurrentFootpulse;
                ImGui.InputFloat("Aktueller footpulse", ref _currentFootpulse, 1, 10, String.Format("{0:0.#}", _currentFootpulse));
                _sqliteViewerControl.CurrentFootpulse = _currentFootpulse;

                ImGui.NewLine();
                ImGui.InputInt("Sprunggröße", ref _stepsize, 1, 10);
                if (_stepsize < 0) _stepsize = 0;

                ImGui.NewLine();
                ImGui.InputFloat("Abspielgeschwindigkeit", ref _playerspeed, 5, 10);
                if (_playerspeed < 1) _playerspeed = 1;
                _sqliteViewerControl.Playerspeed = _playerspeed;


                ImGui.EndGroup();

                ImGui.Begin("Scannereinstellungen");

                ImGui.Text("Scannerkanal ein/ausblenden");

                ImGui.NewLine();
                // HSPA-Master
                if (_sqliteViewerControl.Channel1)
                {
                    int hndlc = ((TextureHandle)_green1.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner1();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red1.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner1();
                    }
                }

                ImGui.SameLine();
                // HSPB-Master
                if (_sqliteViewerControl.Channel3)
                {
                    int hndlc = ((TextureHandle)_green3.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner3();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red3.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner3();
                    }
                }

                ImGui.SameLine();
                // HRS1
                if (_sqliteViewerControl.Channel8)
                {
                    int hndlc = ((TextureHandle)_green8.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner8();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red8.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner8();
                    }
                }

                ImGui.NewLine();
                // HSPA-Slave
                if (_sqliteViewerControl.Channel2)
                {
                    int hndlc = ((TextureHandle)_green2.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner2();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red2.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner2();
                    }
                }

                ImGui.SameLine();
                // HSPB-Slave
                if (_sqliteViewerControl.Channel4)
                {
                    int hndlc = ((TextureHandle)_green4.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner4();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red4.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner4();
                    }
                }

                ImGui.SameLine();
                // HRS2
                if (_sqliteViewerControl.Channel9)
                {
                    int hndlc = ((TextureHandle)_green9.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner9();
                    }
                }
                else
                {
                    int hndlc = ((TextureHandle)_red9.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(hndlc), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleScanner9();
                    }
                }

                ImGui.BeginGroup();
                ImGui.Text("Farbdarstellung");

                ImGui.Combo("Color mode", ref _currentColorMode, new string[] { "Eigene Farbe", "Intensitätskodiert" }, 2);
                PtRenderingParams.Instance.ColorMode = _currentColorMode switch
                {
                    0 => ColorMode.BaseColor,
                    1 => ColorMode.VertexColor0,
                    _ => ColorMode.VertexColor0
                };

                if (_currentColorMode == 0)
                {
                    ImGui.NewLine();
                    if (ImGui.ColorButton("HSPA Master", _scn1Color, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                    {
                        _cloud1ColorPickerOpen = !_cloud1ColorPickerOpen;
                    }

                    if (_cloud1ColorPickerOpen)
                    {
                        ImGui.Begin("HSPA Master", ref _cloud1ColorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                        ImGui.ColorPicker4("Farbe", ref _scn1Color);
                        ImGui.End();
                        _sqliteViewerControl.SetColorPassEfColor(0, _scn1Color.ToFuseeVector());
                    }

                    ImGui.SameLine();
                    if (ImGui.ColorButton("HSPB Master", _scn3Color, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                    {
                        _cloud3ColorPickerOpen = !_cloud3ColorPickerOpen;
                    }

                    if (_cloud3ColorPickerOpen)
                    {
                        ImGui.Begin("HSPA Master", ref _cloud3ColorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                        ImGui.ColorPicker4("Farbe", ref _scn3Color);
                        ImGui.End();
                        _sqliteViewerControl.SetColorPassEfColor(2, _scn3Color.ToFuseeVector());
                    }

                    ImGui.SameLine();
                    if (ImGui.ColorButton("HRS1", _scn8Color, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                    {
                        _cloud8ColorPickerOpen = !_cloud8ColorPickerOpen;
                    }

                    if (_cloud8ColorPickerOpen)
                    {
                        ImGui.Begin("HSPA Master", ref _cloud8ColorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                        ImGui.ColorPicker4("Farbe", ref _scn8Color);
                        ImGui.End();
                        _sqliteViewerControl.SetColorPassEfColor(4, _scn8Color.ToFuseeVector());
                    }

                    ImGui.NewLine();
                    if (ImGui.ColorButton("HSPA Slave", _scn2Color, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                    {
                        _cloud2ColorPickerOpen = !_cloud2ColorPickerOpen;
                    }

                    if (_cloud2ColorPickerOpen)
                    {
                        ImGui.Begin("HSPA Master", ref _cloud2ColorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                        ImGui.ColorPicker4("Farbe", ref _scn2Color);
                        ImGui.End();
                        _sqliteViewerControl.SetColorPassEfColor(1, _scn2Color.ToFuseeVector());
                    }

                    ImGui.SameLine();
                    if (ImGui.ColorButton("HSPB Slave", _scn4Color, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                    {
                        _cloud4ColorPickerOpen = !_cloud4ColorPickerOpen;
                    }

                    if (_cloud4ColorPickerOpen)
                    {
                        ImGui.Begin("HSPA Master", ref _cloud4ColorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                        ImGui.ColorPicker4("Farbe", ref _scn4Color);
                        ImGui.End();
                        _sqliteViewerControl.SetColorPassEfColor(3, _scn4Color.ToFuseeVector());
                    }


                    ImGui.SameLine();
                    if (ImGui.ColorButton("HRS2", _scn9Color, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                    {
                        _cloud9ColorPickerOpen = !_cloud9ColorPickerOpen;
                    }

                    if (_cloud9ColorPickerOpen)
                    {
                        ImGui.Begin("HSPA Master", ref _cloud9ColorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                        ImGui.ColorPicker4("Farbe", ref _scn9Color);
                        ImGui.End();
                        _sqliteViewerControl.SetColorPassEfColor(5, _scn9Color.ToFuseeVector());
                    }
                }

                ImGui.BeginGroup();
                ImGui.NewLine();
                ImGui.Text("Hintergrundfarben");
                ImGui.NewLine();

                ImGui.BeginGroup();
                ImGui.Text("3D Ansicht");
                if (ImGui.ColorButton("3D Hintergrundfarbe", _3DbgColor, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                {
                    _3DcolorPickerOpen = !_3DcolorPickerOpen;
                }

                if (_3DcolorPickerOpen)
                {
                    ImGui.Begin("Color Picker", ref _3DcolorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                    ImGui.ColorPicker4("Color", ref _3DbgColor);
                    ImGui.End();
                    _sqliteViewerControl.Camera1BackgroundColor = _3DbgColor.ToFuseeVector();
                }
                ImGui.EndGroup();

                ImGui.Spacing();

                ImGui.BeginGroup();
                ImGui.Text("2D Ansicht");
                if (ImGui.ColorButton("2D Hintergrundfarbe", _2DbgColor, ImGuiColorEditFlags.DefaultOptions, Vector2.One * 50))
                {
                    _2DcolorPickerOpen = !_2DcolorPickerOpen;
                }

                if (_2DcolorPickerOpen)
                {
                    ImGui.Begin("Color Picker", ref _2DcolorPickerOpen, ImGuiWindowFlags.AlwaysAutoResize);
                    ImGui.ColorPicker4("Color", ref _2DbgColor);
                    ImGui.End();
                    _sqliteViewerControl.Camera2BackgroundColor = _2DbgColor.ToFuseeVector();
                }
                ImGui.EndGroup();

                if (ImGui.Button("Farben angleichen"))
                {
                    _sqliteViewerControl.Camera2BackgroundColor = _sqliteViewerControl.Camera1BackgroundColor;
                }

                ImGui.EndGroup();

                ImGui.Spacing();

                ImGui.Begin("Punktwolkeneinstellungen");
                ImGui.Text($"Application average {1000.0f / ImGui.GetIO().Framerate:0.00} ms/frame ({ImGui.GetIO().Framerate:0} FPS)");

                ImGui.NewLine();
                ImGui.Text($"Aktuelle Datenbank: {Path.GetFileName(PtRenderingParams.Instance.PathToSqliteFile)}");

                ImGui.NewLine();

                DrawProgressBar();

                ImGui.NewLine();
                ImGui.Spacing();

                // ---------------------- VISIBILITY ----------------------
                /*
                ImGui.BeginGroup();
                ImGui.Text("Visibility");
                ImGui.InputInt("Point threshold", ref _threshold, 1000, 10000);
                ImGui.SliderFloat("Min. Projection Size Modifier", ref _fuseeViewportMinProj, 0f, 1f);

                PtRenderingParams.Instance.PointThreshold = _threshold;
                PtRenderingParams.Instance.ProjectedSizeModifier = _fuseeViewportMinProj;

                ImGui.EndGroup();

                ImGui.NewLine();
                ImGui.Spacing();
                */

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

                ImGui.EndGroup();
                ImGui.NewLine();

                ImGui.Text("Hilfslinien für 2D Sichtbereich umschalten");
                if (_sqliteViewerControl.GuideLinesOn)
                {
                    int onhndl = ((TextureHandle)_on.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(onhndl), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleGuidelines();
                    }
                }
                else
                {
                    int onhndl = ((TextureHandle)_off.TextureHandle).TexId;
                    if (ImGui.ImageButton(new IntPtr(onhndl), new Vector2(c, c)))
                    {
                        _sqliteViewerControl.ToggleGuidelines();
                    }
                }

                ImGui.NewLine();
                ImGui.InputFloat("2D Abschnittsgröße", ref _camera2DRenderDistance, .1f, 10, String.Format("{0:0.#}", _camera2DRenderDistance));
                if (_camera2DRenderDistance < 0.1f) _camera2DRenderDistance = 0.1f;
                _sqliteViewerControl.Camera2DRenderDistance = _camera2DRenderDistance;

                ImGui.NewLine();
                ImGui.InputInt("3D Sichtweite", ref _camera3DRenderDistance, 1, 10);
                if (_camera3DRenderDistance < 10) _camera3DRenderDistance = 10;
                _sqliteViewerControl.Camera3DRenderDistance = _camera3DRenderDistance;

                ImGui.NewLine();
                if (ImGui.Button("Fensteranordnung laden"))
                {
                    ImGui.LoadIniSettingsFromDisk(Path.Combine("Assets/MyImGuiSettings.ini"));
                }

                if (ImGui.Button("Fensteranordnung speichern"))
                {
                    ImGui.SaveIniSettingsToDisk(Path.Combine("Assets/MyImGuiSettings.ini"));
                }

                // Load previous ini settings on application start.
                if (!_iniLoaded)
                {
                    ImGui.LoadIniSettingsFromDisk("Assets/MyImGuiSettings.ini");
                    _iniLoaded = true;
                }

                ImGui.NewLine();
                ImGui.Text("Ordner mit konvertierten Punktwolken öffnen");
                if (ImGui.Button("Ordner öffnen"))
                {
                    FileManager.OpenFolderWithExplorer(FileManager.GetConvDir());
                }
                ImGui.End();
            }
        }

        private void DrawProgressBar()
        {
            float yPos = 123 - ImGui.GetScrollY();  // 123px because it looks the best on this position.
            float height = 25;
            float width = ImGui.GetWindowPos().X + ImGui.GetWindowWidth();
            float factor = _convertedFiles / (float)_numberOfFiles;

            Vector2 loadingBarPos = new Vector2(ImGui.GetWindowPos().X, ImGui.GetWindowPos().Y + yPos);

            ImGui.GetWindowDrawList().AddRectFilled(loadingBarPos, new Vector2(width, loadingBarPos.Y - height), (uint)ColorUint.DarkRed.ToRgba());

            ImGui.GetWindowDrawList().AddRectFilled(loadingBarPos, new Vector2((int)(width * factor), loadingBarPos.Y - height), (uint)ColorUint.Green.ToRgba());
            ImGui.TextColored(new Vector4(1, 1, 1, 1), $"Konvertiert: {_convertedFiles} / {_numberOfFiles}");
        }

        internal void DrawMainMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Menu"))
                {
                    if (ImGui.MenuItem("Open"))
                    {
                        _spawnOpenFilePopup = true;
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


        /// <summary>
        /// Place all design/styles inside this method
        /// </summary>
        internal static void SetImGuiDesign()
        {
            var style = ImGui.GetStyle();
            var colors = style.Colors;

            style.WindowRounding = 0.0f;             // Radius of window corners rounding. Set to 0.0f to have rectangular windows
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
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
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
            colors[(int)ImGuiCol.Button] = new Vector4(0.71f, 0.78f, 0.69f, 0.40f);
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