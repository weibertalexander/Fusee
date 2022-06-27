using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Engine.Core.Scene;
using Fusee.Engine.Gui;
using Fusee.Math.Core;
using System.Threading.Tasks;
using static Fusee.Engine.Core.Input;
using static Fusee.Engine.Core.Time;
using System.IO;
using System;
using Microsoft.Data.Sqlite;


namespace FuseeApp
{
    [FuseeApplication(Name = "Punktwolkenviewer", Description = "Punktwolkenviewer für .sqlite Dateien")]
    public class FullView : RenderCanvas
    {
        // Horizontal and vertical rotation Angles for the displayed object 
        private static float _angleHorz = 0, _angleVert = -0.2f;

        // Horizontal and vertical angular speed
        private static float _angleVelHorz, _angleVelVert;

        // Overall speed factor. Change this to adjust how fast the rotation reacts to input
        private const float RotationSpeed = 4;

        // Damping factor 
        private const float Damping = 0.8f;


        private SceneContainer _scene;
        private SceneRendererForward _sceneRenderer;

        private CanvasRenderMode _canvasRenderMode;
        private float _canvasWidth;
        private float _canvasHeight;

        private CanvasNode _canvasNode;
        private FontMap _fontMap;

        //private SqliteData _sqliteData;

        private bool _keys;

        public string _filename;

        private float _zoom = 0f;
        private float _camZ = 0f;
        private float _stepSize = 1;
        private bool _isPlaying = false;

        private SceneInteractionHandler _sceneInteraction;
        private GuiButton _playButton;
        private GuiButton _forwardButton;
        private GuiButton _backwardButton;
        private GuiButton _beginningButton;
        private GuiButton _endButton;

        private TextureNode _playNode;
        private TextureNode _stopNode;


        // Init is called on startup. 
        public override void Init()
        {
            Diagnostics.Debug("test");
            RC.ClearColor = new float4(0, 0, 0, 0); // Black Background
            //RC.ClearColor = new float4(1, 1, 1, 1);
            //RC.ClearColor = (float4)ColorUint.Green;
            _filename = Path.GetFileName(FileManager.GetSqliteFiles()[0]);

            _scene = new SceneContainer();

            PlaceOriginPoint();
            //InitCubes();

            FileManager.CreateDirectories();
            FileManager.CreateOctreeFromDB(FileManager.GetSqliteFiles()[0]);

            //Potree2Reader reader = new Potree2Reader();
            //reader.GetPointCloudComponent($"{FileManager.GetPTDir()}/{Path.GetFileNameWithoutExtension(_filename)}");

            // Render UI after points to prevent overlap.
            PrepareUI();
            InitButtons();
            DrawFilename();
            DrawControlPanel();

            _sceneInteraction = new SceneInteractionHandler(_scene);
            _sceneRenderer = new SceneRendererForward(_scene);
        }
        public override async Task InitAsync()
        {
            await base.InitAsync();
        }

        // Place a cube at worlds center of origin (0, 0, 0)
        public void PlaceOriginPoint()
        {
            Transform transform = new Transform
            {
                Translation = new float3(0, 0, 0)
            };

            Fusee.Engine.Core.Effects.SurfaceEffect shader = MakeEffect.FromUnlit((float4)ColorUint.Red);
            float size = 0.05f;
            Mesh mesh = SimpleMeshes.CreateCuboid(new float3(size, size, size));

            // Assemble SceneNode object
            SceneNode zero = new SceneNode();
            zero.Components.Add(transform);
            zero.Components.Add(shader);
            zero.Components.Add(mesh);

            _scene.Children.Add(zero);
        }

        // Generate cube for each data point
        public void InitCubes()
        {
            SqliteConnection connection = new("Data Source=" + FileManager.GetSqliteFiles()[0]);
            connection.Open();

            // Create sqlite commands
            SqliteCommand data = connection.CreateCommand();
            data.CommandText = "SELECT data_points FROM Lichtraum";
            SqliteDataReader data_reader = data.ExecuteReader();

            SqliteCommand nop = connection.CreateCommand();  // number of points
            nop.CommandText = "SELECT sum(number_of_points) FROM Lichtraum";
            SqliteDataReader nop_reader = nop.ExecuteReader();

            SqliteCommand intensity = connection.CreateCommand();
            intensity.CommandText = "SELECT intensity FROM Lichtraum";
            SqliteDataReader intensity_reader = intensity.ExecuteReader();

            SqliteCommand scannerid = connection.CreateCommand();
            scannerid.CommandText = "SELECT scanner_id FROM Lichtraum";
            SqliteDataReader scannerid_reader = scannerid.ExecuteReader();

            nop_reader.Read();
            int rowcounter = 0;
            //data_reader.Read();
            while (data_reader.Read() && intensity_reader.Read() && scannerid_reader.Read() && rowcounter < 1)
            {
                byte[] datablob = (byte[])data_reader.GetValue(0);
                byte[] intensityblob = (byte[])intensity_reader.GetValue(0);
                byte[] scanneridblob = (byte[])scannerid_reader.GetValue(0);

                //Diagnostics.Debug(datablob.Length);  // number of points * 12, as 1 point aka 3 floats (3 * 4 byte) equals 12

                // Store the point coordinate components
                float[] values = new float[datablob.Length / 4];
                Diagnostics.Debug($"Number of components: {values.Length}\tNumber of points: {values.Length / 3}");

                // Limit the number of cubes displayed
                int maxcubes = (datablob.Length / 12);

                for (int i = 0; i < maxcubes; i += 2 ^ 30)  // 2^(every nth point)
                {
                    // Calculate floats from bytes
                    float x = BitConverter.ToSingle(datablob, i * 12 + 0);  // 4 bytes
                    float y = BitConverter.ToSingle(datablob, i * 12 + 4);
                    float fp = BitConverter.ToSingle(datablob, i * 12 + 8);
                    UInt16 inty = BitConverter.ToUInt16(intensityblob, i * 2);  // 2 bytes
                    byte sid = scanneridblob[i];  // 1 byte

                    if (x < -2f || x > 2.5f || inty > 35000) continue;

                    // Add do scene container
                    //Point p = new Point(x, y, fp, inty, sid);
                    //Diagnostics.Debug(p.ToString());
                    //_scene.Children.Add(p.PointToScenenode());
                };
                rowcounter++;
            }
            connection.Close();
        }

        // Update is called 60 times a second
        public override void Update()
        {
            _sceneInteraction.CheckForInteractiveObjects(RC, Input.Mouse.Position, Width, Height);
        }

        // RenderAFrame is called once per frame
        public override void RenderAFrame()
        {
            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
            // + (int)_zoom
            RC.Viewport(0, 0, Width, Height);

            _canvasWidth = Width / 100f;
            _canvasHeight = Height / 100f;

            //Diagnostics.Debug(_canvasWidth + "    " + _canvasHeight);

            // Mouse and keyboard movement
            if (Keyboard.LeftRightAxis != 0 || Keyboard.UpDownAxis != 0)
            {
                _keys = true;
            }

            if (Mouse.LeftButton)
            {
                _keys = false;
                _angleVelHorz = -RotationSpeed * Mouse.XVel * DeltaTime * 0.0005f;
                _angleVelVert = -RotationSpeed * Mouse.YVel * DeltaTime * 0.0005f;
            }
            else
            {
                if (_keys)
                {
                    _angleVelHorz = -RotationSpeed * Keyboard.LeftRightAxis * DeltaTime;
                    _angleVelVert = -RotationSpeed * Keyboard.UpDownAxis * DeltaTime;
                }
                else
                {
                    var curDamp = (float)System.Math.Exp(-Damping * DeltaTime);
                    _angleVelHorz *= curDamp;
                    _angleVelVert *= curDamp;
                }
            }

            if (Keyboard.WSAxis == 1)
            {
                _camZ -= Time.DeltaTime * 2f;
            }
            else if (Keyboard.WSAxis == -1)
            {
                _camZ += Time.DeltaTime * 2f;
            }


            // zoom in
            float scrollspeed = .5f;
            _zoom += Mouse.WheelVel * DeltaTime * scrollspeed;

            // Set zoom restrictions
            if (_zoom < 0) _zoom = 0;
            if (_zoom > 10) _zoom = 10;

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;

            // Create the camera matrix and set it as the current View transformation
            var mtxRot = float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz) * float4x4.CreateTranslation(0, 0, _camZ);
            var mtxCam = float4x4.LookAt(0, 0, -8, 0, 1, 0, 0, 1, 0) * float4x4.CreateScale(1 + _zoom);

            RC.View = mtxCam * mtxRot;
            RC.Projection = float4x4.CreatePerspectiveFieldOfView(M.PiOver4, (float)Width / Height, 1, 1000);

            // Tick any animations and Render the scene loaded in Init()
            _sceneRenderer.Render(RC);

            if (_isPlaying)
            {
                _camZ -= Time.DeltaTime * _stepSize;
            }

            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.
            Present();
        }

        #region UserInterface
        // Set initial canvas values and initialize fontmap
        public void PrepareUI()
        {
            _canvasRenderMode = CanvasRenderMode.Screen;
            _canvasWidth = Width / 100f;
            _canvasHeight = Height / 100f;

            Diagnostics.Debug(_canvasWidth + "    " + _canvasHeight);


            _canvasNode = new CanvasNode(
                "Canvas",
                _canvasRenderMode,
                new MinMaxRect
                {/*
                    Min = new float2(0, 0),
                    Max = new float2(0.2f, 0.1f)
                    Min = new float2(-_canvasWidth / 2, -_canvasHeight / 2f),
                    Max = new float2(_canvasWidth / 2, _canvasHeight / 2f)
                */
                    Min = new float2(0, 0),
                    Max = new float2(_canvasWidth, _canvasHeight)

                }
            );

            Font fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            _fontMap = new FontMap(fontLato, 18);
            _scene.Children.Add(_canvasNode);
        }
        public void DrawFilename()
        {
            /*
            // Text Container
            TextureNode t = TextureNode.Create(
                "Textcontainer",
                new Texture(AssetStorage.Get<ImageData>("test.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.StretchAll),
                GuiElementPosition.CalcOffsets(AnchorPos.StretchAll, new float2(0, 0), _canvasHeight, _canvasWidth, new float2(4.5f, .25f)),
                new float2(1, 1)
            );
            */
            TextNode text = TextNode.Create(
                _filename,
                "FilenameText",
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),
                //GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft, new float2(0, 0), _canvasHeight, _canvasWidth, new float2(.1f, .1f)),
                new MinMaxRect
                {
                    Min = new float2(0, 0),
                    Max = new float2(.5f, .5f)
                },
                _fontMap,
                (float4)ColorUint.Greenery,
                HorizontalTextAlignment.Left,
                VerticalTextAlignment.Center
            );

            //t.Children.Add(text);
            //_canvasNode.Children.Add(t);
            Transform tr = new Transform
            {
                Translation = new float3(-11300, -6200, 900),  // ?????????? it works tho
            };
            text.AddComponent(tr);
            _canvasNode.Children.Add(text);

        }

        // Generate TextureNodes and add buttons.
        public void DrawControlPanel()
        {
            float2 guisize = new float2(.4f, .4f);
            float xpos = _canvasWidth / 2 - guisize.x / 2;
            float ypos = 0f;
            
            _playNode = TextureNode.Create(
                "Play",  // Name
                new Texture(AssetStorage.Get<ImageData>("/play1.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),  // Anchor
                GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft,  // Offset
                    new float2(xpos, ypos),  // Position on parent
                    _canvasHeight,  // Parent height
                    _canvasWidth,  // Parent width
                    guisize  // Gui dimensions
                ),
                float2.One  // Tiling
            );

            //_playNode.Components.Add(_playButton);
            /*

            _stopNode = TextureNode.Create(
                "Play",  // Name
                new Texture(AssetStorage.Get<ImageData>("stop1.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),  // Anchor
                GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft,  // Offset
                    new float2(xpos, ypos),  // Position on parent
                    _canvasHeight,  // Parent height
                    _canvasWidth,  // Parent width
                    guisize  // Gui dimensions
                ),
                float2.One  // Tiling
            );
            _stopNode.Components.Add(_playButton);


            TextureNode forward = TextureNode.Create(
                "Forward",  // Name
                new Texture(AssetStorage.Get<ImageData>("forward1.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),  // Anchor
                GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft,  // Offset
                    new float2(xpos + guisize.x, ypos),  // Position on parent
                    _canvasHeight,  // Parent height
                    _canvasWidth,  // Parent width
                    guisize  // Gui dimensions
                ),
                float2.One  // Tiling
            );
            forward.Components.Add(_forwardButton);

            TextureNode backward = TextureNode.Create(
                "Backward",  // Name
                new Texture(AssetStorage.Get<ImageData>("backward1.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),  // Anchor
                GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft,  // Offset
                    new float2(xpos - guisize.x, ypos),  // Position on parent
                    _canvasHeight,  // Parent height
                    _canvasWidth,  // Parent width
                     guisize  // Gui dimensions
                ),
                float2.One  // Tiling
            );
            backward.Components.Add(_backwardButton);

            TextureNode end = TextureNode.Create(
                "End",  // Name
                new Texture(AssetStorage.Get<ImageData>("end1.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),  // Anchor
                GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft,  // Offset
                    new float2(xpos + 2 * guisize.x, ypos),  // Position on parent
                    _canvasHeight,  // Parent height
                    _canvasWidth,  // Parent width
                    guisize  // Gui dimensions
                ),
                float2.One  // Tiling
            );
            end.Components.Add(_endButton);

            TextureNode beginning = TextureNode.Create(
                "Beginning",  // Name
                new Texture(AssetStorage.Get<ImageData>("beginning1.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),  // Anchor
                GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft,  // Offset
                    new float2(xpos - 2 * guisize.x, ypos),  // Position on parent
                    _canvasHeight,  // Parent height
                    _canvasWidth,  // Parent width
                    guisize  // Gui dimensions
                ),
                float2.One  // Tiling
            );
            beginning.Components.Add(_beginningButton);

            */
            ImageData a = AssetStorage.Get<ImageData>("play1.png");
            Diagnostics.Debug(a);
            //_canvasNode.Children.Add(_playNode);
            
            /*_canvasNode.Children.Add(forward);
            _canvasNode.Children.Add(backward);
            _canvasNode.Children.Add(end);
            _canvasNode.Children.Add(beginning);
            */
        }
        #endregion UserInferface
        #region Interactions

        // Give buttons their functionality.
        public void InitButtons()
        {
            _playButton = new GuiButton
            {
                Name = "StartButton"
            };
            _playButton.OnMouseUp += OnPlayDown;

            _forwardButton = new GuiButton
            {
                Name = "ForwardButton"
            };
            _forwardButton.OnMouseUp += OnForwardDown;

            _backwardButton = new GuiButton
            {
                Name = "BackwardButton"
            };
            _backwardButton.OnMouseUp += OnBackwardDown;

            _endButton = new GuiButton
            {
                Name = "EndButton"
            };
            _endButton.OnMouseUp += OnEndDown;

            _beginningButton = new GuiButton
            {
                Name = "BeginningButton"
            };
            _beginningButton.OnMouseUp += OnBeginningDown;

        }
        public void OnPlayDown(CodeComponent sender)
        {
            Diagnostics.Debug("Start Button");
            _isPlaying = !_isPlaying;
            if (_isPlaying)
            {
                _canvasNode.Children.Remove(_playNode);
                _canvasNode.Children.Add(_stopNode);
            }
            else
            {
                _canvasNode.Children.Remove(_stopNode);
                _canvasNode.Children.Add(_playNode);
            }
        }

        public void OnForwardDown(CodeComponent sender)
        {
            Diagnostics.Debug("Forward button");
            _camZ -= _stepSize;
        }

        public void OnBackwardDown(CodeComponent sender)
        {
            Diagnostics.Debug("Backward button");
            _camZ += _stepSize;
        }

        public void OnEndDown(CodeComponent sender)
        {
            Diagnostics.Debug("End button");
        }

        public void OnBeginningDown(CodeComponent sender)
        {
            Diagnostics.Debug("Beginning button");
            _camZ = 0;
        }
        #endregion Interactions
    }
}