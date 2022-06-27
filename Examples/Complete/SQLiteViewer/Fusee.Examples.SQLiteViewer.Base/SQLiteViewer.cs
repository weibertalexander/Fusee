using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Engine.Core.Scene;
using Fusee.Engine.Gui;
using Fusee.Math.Core;
using Fusee.PointCloud.Core.Scene;
using Fusee.PointCloud.Potree.V2;
using static Fusee.Engine.Core.Input;
using static Fusee.Engine.Core.Time;
using Fusee.PointCloud.Common;

namespace Fusee.Examples.SQLiteViewer.NoGui
{
    [FuseeApplication(Name = "Punktwolkenviewer", Description = "Punktwolkenviewer für .sqlite Dateien")]
    public class SQLiteViewer : RenderCanvas
    {
        // Horizontal and vertical rotation Angles for the displayed object 
        private static float _angleHorz = 0, _angleVert = 0f;

        // Horizontal and vertical angular speed
        private static float _angleVelHorz, _angleVelVert;

        // Overall speed factor. Change this to adjust how fast the rotation reacts to input
        private const float RotationSpeed = 4;

        // Damping factor 
        private const float Damping = 4f;

        //private bool _initialized = false;

        private SceneContainer _scene;  // Contains Pointclouds and coordinate axes.
        public SceneContainer Scene
        {
            get { return _scene; }
        }

        private SceneContainer _guiScene;  // Contains GUI.

        private SceneRendererForward _sceneRenderer;
        public SceneRendererForward SceneRenderer
        {
            get { return _sceneRenderer; }
        }
        private SceneRendererForward _guiRenderer;

        private CanvasRenderMode _canvasRenderMode;
        private float _canvasWidth;
        private float _canvasHeight;

        private CanvasNode _canvasNode;
        private FontMap _fontMap;

        private Camera _camera;
        private Camera _camera2;
        private Camera _camera3;

        private Transform _cameraTransform;
        private Transform _camera2Transform;
        private Transform _camera3Transform;

        private float3 _initialCamTransform;

        //private bool _keys;

        public string _filename;

        private float _zoom = 1f;
        private float _camZ = 0f;
        private float _speed = 5;
        private bool _isPlaying = false;

        private SceneInteractionHandler _sceneInteraction;
        private GuiButton _playButton;
        private GuiButton _forwardButton;
        private GuiButton _backwardButton;
        private GuiButton _beginningButton;
        private GuiButton _endButton;
        private TextNode _footpulseTextNode;
        private string _footpulseText = "footpulse: 0";
        private TextureNode _playNode;
        private TextureNode _stopNode;

        private int _currentIndex = 0;

        private Potree2Reader _reader = new(); // Used with 1 large pointcloud.
        private PointCloudComponent _pointCloudComponent;
        private SceneNode _cloud;
        private Transform _cloudTransform;

        private Potree2Reader _reader1 = new();  // This and following are used with multiple pointclouds.
        private Potree2Reader _reader2 = new();
        private Potree2Reader _reader3 = new();
        private Potree2Reader _reader4 = new();
        private Potree2Reader _reader8 = new();
        private Potree2Reader _reader9 = new();


        private PointCloudComponent[] _pointCloudComponentList;

        private ScenePicker _scenePicker;

        private float _scannerChannelCooldown = 0f;

        //private static int _maxclouds = 10;
        private string[] _octrees = new string[3];

        private SceneNode _coordinateAxes;
        private Transform _coordinateAxesTransform;

        private RenderLayer _pointCloudLayer = new RenderLayer { Layer = RenderLayers.Layer01 };
        private RenderLayer _coordinateLayer = new RenderLayer { Layer = RenderLayers.Layer02 };

        private float _mainCamViewportSize = 70;
        private float _camera2MouseSensitivity = 0.02f;

        private bool _isMouseDown = false;
        private float2 _lastClickedMousePos;


        // Init is called on startup. 
        public override void Init()
        {
            PtRenderingParams.Instance.DepthPassEf = MakePointCloudEffect.ForDepthPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape);
            PtRenderingParams.Instance.ColorPassEf = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
            PtRenderingParams.Instance.PointThresholdHandler = OnThresholdChanged;
            PtRenderingParams.Instance.ProjectedSizeModifierHandler = OnProjectedSizeModifierChanged;

            _filename = Path.GetFileName(FileManager.GetSqliteFiles()[0]);

            _scene = new SceneContainer();
            _guiScene = new SceneContainer();

            InitCamera();

            InitCoordAxes();

            PlaceOriginPoint();

            //FileManager.CreateDirectories();

            FileManager.CreateOctreeFromDB(FileManager.GetSqliteFiles()[0]);
            PtRenderingParams.Instance.PathToOocFile = Directory.GetDirectories(FileManager.GetConvDir())[0];

            InitPointClouds();

            //PrepareUI();
            //InitButtons();
            //DrawFilename();
            //DrawFootpulse();
            //DrawControlPanel();

            _sceneRenderer = new SceneRendererForward(_scene);
            PointCloudRenderModule module = new PointCloudRenderModule(true);

            _sceneInteraction = new SceneInteractionHandler(_guiScene);
            _guiRenderer = new SceneRendererForward(_guiScene);


            _sceneRenderer.VisitorModules.Add(module);

            _scenePicker = new ScenePicker(_scene);
        }
        public override async Task InitAsync()
        {
            await base.InitAsync();
        }

        private void InitCamera()
        {
            _initialCamTransform = new float3(0, 5, 0);

            _camera = new Camera(ProjectionMethod.Perspective, 0.1f, 300, M.PiOver4, RenderLayers.Layer01)
            {
                Layer = 1,
                BackgroundColor = (float4)ColorUint.Black,
            };

            _camera2 = new Camera(ProjectionMethod.Orthographic, 1f, 2f, M.PiOver4, RenderLayers.Layer01)
            {
                Layer = 1,
                BackgroundColor = (float4)ColorUint.Black,

                Scale = 0.05f
            };

            _camera3 = new Camera(ProjectionMethod.Perspective, 0.1f, 11.1f, M.PiOver4, RenderLayers.Layer02)
            {
                Layer = 2,
                ClearColor = false,
            };

            _cameraTransform = new Transform
            {
                Translation = new float3(0, 5, 0),
            };

            _camera2Transform = new Transform
            {
                Translation = new float3(0, 5, 0),
            };

            _camera3Transform = new Transform
            {
                Translation = new float3(0, 0, -6),
            };

            SceneNode cam = new SceneNode
            {
                Name = "MainCam",
                Components =
                {
                    _cameraTransform,
                    _camera,
                }
            };

            SceneNode cam2 = new SceneNode
            {
                Name = "2D-Cam",
                Components =
                {
                    _camera2Transform,
                    _camera2,
                }
            };

            SceneNode cam3 = new SceneNode
            {
                Name = "Axes-Cam",
                Components =
                {
                    _camera3Transform,
                    _camera3,
                }
            };

            SceneNode camera2Boundaries = new SceneNode
            {
                Name = "Camera2Boundaries",
                Components =
                {
                    _coordinateLayer,
                },
                Children =
                {
                    new SceneNode
                    {
                        Components =
                        {
                            new Transform
                            {
                                Translation = new float3(0, -_camera2Transform.Translation.y, _camera2.ClippingPlanes.x),
                            },
                            MakeEffect.FromUnlit((float4)ColorUint.White),
                            SimpleMeshes.CreateCuboid(new float3(100, 0.01f, 0.01f)),
                        },
                    },
                    new SceneNode
                    {
                        Components =
                        {
                            new Transform
                            {
                                Translation = new float3(0, -_camera2Transform.Translation.y, _camera2.ClippingPlanes.y),
                            },
                            MakeEffect.FromUnlit((float4)ColorUint.White),
                            SimpleMeshes.CreateCuboid(new float3(100, 0.01f, 0.01f)),
                        }
                    }
                }
            };

            cam2.Children.Add(camera2Boundaries);

            _camera.Viewport = new float4(0, 0, 100, _mainCamViewportSize);
            _camera2.Viewport = new float4(0, _mainCamViewportSize, 100, 100 - _mainCamViewportSize);
            _camera3.Viewport = new float4(_mainCamViewportSize + ((100 - _mainCamViewportSize) / 4), _mainCamViewportSize + ((100 - _mainCamViewportSize) / 4), 15, 15);


            _scene.Children.Add(cam);
            _scene.Children.Add(cam2);
            _scene.Children.Add(cam3);
        }

        private void InitCoordAxes()
        {
            float axisThickness = 0.15f;
            float axisLength = 2.5f;

            _coordinateAxesTransform = new Transform
            {
                Translation = new float3(0, 0, 0),
            };

            _coordinateAxes = new SceneNode
            {
                Name = "Coordinate Axes",
                Components =
                {
                    _coordinateLayer,
                    _coordinateAxesTransform,

                },
                Children =
                {

                    new SceneNode
                    {
                        Name = "xAxis",
                        Components =
                        {
                            new Transform
                            {
                                Translation = new float3(axisLength / 3, 0, 0),
                            },
                            MakeEffect.FromUnlit((float4)ColorUint.Red),
                            SimpleMeshes.CreateCuboid(new float3(axisLength, axisThickness, axisThickness)),
                        }
                    },
                    new SceneNode
                    {
                        Name = "yAxis",
                        Components =
                        {
                            new Transform
                            {
                                Translation = new float3(0, axisLength / 3, 0),
                            },
                            MakeEffect.FromUnlit((float4)ColorUint.Green),
                            SimpleMeshes.CreateCuboid(new float3(axisThickness, axisLength, axisThickness)),
                        }
                    },
                    new SceneNode
                    {
                        Name = "zAxis",
                        Components =
                        {
                            new Transform
                            {
                                Translation = new float3(0, 0, axisLength / 3),
                            },
                            MakeEffect.FromUnlit((float4)ColorUint.Blue),
                            SimpleMeshes.CreateCuboid(new float3(axisThickness, axisThickness, axisLength)),
                        }
                    },
                }
            };

            _scene.Children.Add(_coordinateAxes);
        }

        private void InitPointCloud()
        {
            _pointCloudComponent = (PointCloudComponent)_reader.GetPointCloudComponent(PtRenderingParams.Instance.PathToOocFile);
            _pointCloudComponent.PointCloudImp.MinProjSizeModifier = PtRenderingParams.Instance.ProjectedSizeModifier;
            _pointCloudComponent.PointCloudImp.PointThreshold = PtRenderingParams.Instance.PointThreshold;

            _cloudTransform = new Transform()
            {
                Scale = float3.One,
                Translation = new float3(0, 0, 0),
                Rotation = new float3(0, 0, 0)
            };

            _cloud = new SceneNode()
            {
                Name = "PointCloud",
                Components = new List<SceneComponent>()
                {
                    _cloudTransform,
                    PtRenderingParams.Instance.DepthPassEf,
                    PtRenderingParams.Instance.ColorPassEf,
                    _pointCloudComponent,
                    _pointCloudLayer
                }
            };
            _scene.Children.Add(_cloud);
        }

        private void InitPointClouds()
        {
            string[] directories = Directory.GetDirectories(PtRenderingParams.Instance.PathToOocFile);
            Potree2Reader[] readerList = { _reader1, _reader2, _reader3, _reader4, _reader8, _reader9 };
            _pointCloudComponentList = new PointCloudComponent[6];
            for (int i = 0; i < directories.Length; i++)
            {
                _pointCloudComponentList[i] = (PointCloudComponent)readerList[i].GetPointCloudComponent(directories[i]);
                _pointCloudComponentList[i].PointCloudImp.MinProjSizeModifier = PtRenderingParams.Instance.ProjectedSizeModifier;
                _pointCloudComponentList[i].PointCloudImp.PointThreshold = PtRenderingParams.Instance.PointThreshold;

                _pointCloudComponentList[i].Camera = _camera;

                Diagnostics.Warn(readerList[i].GetPointCloudOffset());

                Transform cloudTransform = new Transform()
                {
                    Scale = float3.One,
                    Translation = new float3((float)readerList[i].GetPointCloudOffset().x, (float)readerList[i].GetPointCloudOffset().y, (float)readerList[i].GetPointCloudOffset().z),
                    Rotation = new float3(0, 0, 0)
                };

                SceneNode cloud = new SceneNode()
                {
                    Name = Path.GetFileNameWithoutExtension(directories[i]),
                    Components = new List<SceneComponent>()
                    {
                        _pointCloudLayer,
                        cloudTransform,
                        PtRenderingParams.Instance.DepthPassEf,
                        PtRenderingParams.Instance.ColorPassEf,
                        _pointCloudComponentList[i]
                    }
                };

                _scene.Children.Add(cloud);
            }
        }

        // Attempt with multiple pointclouds: Add pointcloud to scene based on z position. Add or remove scenenodes
        // using this basis.
        private void PointcloudsToScene()
        {
            string[] newfiles = FileManager.ReturnFiles((int)_camZ, 3);
            SceneNode[] childnodes = _scene.Children.ToArray();
            var set = new HashSet<string>(newfiles);
            var equals = set.SetEquals(_octrees);
            if (!equals)
            {
                _octrees = newfiles;
                for (int i = 0; i < childnodes.Length; i++)
                {
                    // Remove pointcloud from scenecontainer.
                    if (!newfiles.Contains(childnodes[i].Name))
                    {
                        _scene.Children.Remove(childnodes[i]);
                    }
                    // Replace childnode name in newfiles with empty string. 
                    else
                    {
                        for (int j = 0; j < newfiles.Length; j++)
                        {
                            {
                                if (newfiles[j] == childnodes[i].Name)
                                {
                                    Diagnostics.Debug($"Removed {newfiles[j]}");
                                    newfiles[j] = "";
                                    break;
                                }
                            }
                        }
                    }
                }

                Potree2Reader potreeReader = new();

                for (int i = 0; i < newfiles.Length; i++)
                {
                    if (newfiles[i] != "")
                    {
                        _pointCloudComponent = (PointCloudComponent)potreeReader.GetPointCloudComponent(FileManager.GetConvDir() + "/" + newfiles[i] + "/");
                        _pointCloudComponent.PointCloudImp.MinProjSizeModifier = PtRenderingParams.Instance.ProjectedSizeModifier;
                        _pointCloudComponent.PointCloudImp.PointThreshold = PtRenderingParams.Instance.PointThreshold;
                        var pointCloudNode = new SceneNode()
                        {
                            Name = newfiles[i],
                            Components = new List<SceneComponent>()
                            {
                                new Transform()
                                {
                                    Scale = float3.One,
                                    Translation = new float3(-0, 0, 0),
                                    Rotation = new float3(0, 0, 0)
                                },
                                PtRenderingParams.Instance.DepthPassEf,
                                PtRenderingParams.Instance.ColorPassEf,
                                _pointCloudComponent
                            }
                        };
                        _scene.Children.Add(pointCloudNode);
                    }
                }
            }
        }

        // Add a pointcloud to the scene based on current z / footpulse position.
        public void SetPointcloud()
        {
            int index = (int)_camZ / (Constants.FootpulseAmount * 10);

            if (index != _currentIndex)
            {
                //Diagnostics.Debug(_scene.Children.Remove(_cloud));

                string pathToFile = $"{FileManager.GetConvDir()}/220202002-0000_{index}";

                Diagnostics.Debug($"Changing pointcloud to {pathToFile}");
                PtRenderingParams.Instance.PathToOocFile = pathToFile;
                //_reader.FileDataInstance = null;
                _cloud.RemoveComponent<PointCloudComponent>();

                _reader = new();

                _pointCloudComponent = (PointCloudComponent)_reader.GetPointCloudComponent(pathToFile);

                _cloud.Components.Add(_pointCloudComponent);
                _cloudTransform.Translation = new float3(0, 0, 10 * index * Constants.FootpulseAmount);
                /*
                _cloud = new SceneNode()
                {
                    Name = index + "",
                    Components = new List<SceneComponent>()
                        {
                            new Transform()
                            {
                                Scale = float3.One,
                                Translation = new float3(0, 0, index * Constants.FootpulseAmount * 10),
                                Rotation = new float3(0, 0, 0)
                            },
                            PtRenderingParams.Instance.DepthPassEf,
                            PtRenderingParams.Instance.ColorPassEf,
                            _pointCloudComponent
                        }
                };
                */
                //_scene.Children.Add(_cloud);
                _currentIndex = index;
            }
        }

        // Place a cube at worlds center of origin (0, 0, 0)
        public void PlaceOriginPoint()
        {
            Transform transform = new Transform
            {
                Translation = new float3(0, 0, 0)
            };

            Engine.Core.Effects.SurfaceEffect shader = MakeEffect.FromUnlit((float4)ColorUint.Red);
            float size = 0.05f;
            Mesh mesh = SimpleMeshes.CreateCuboid(new float3(size, size, size));

            // Assemble SceneNode object
            SceneNode zero = new SceneNode();
            zero.Name = "Zero";
            zero.Components.Add(transform);
            zero.Components.Add(shader);
            zero.Components.Add(mesh);

            _scene.Children.Add(zero);
        }

        // Update is called 60 times a second
        public override void Update()
        {

            //SetPointcloud();
            _sceneInteraction.CheckForInteractiveObjects(RC, Input.Mouse.Position, Width, Height);

            // Play button is active.
            if (_isPlaying)
            {
                _cameraTransform.Translate(new float3(0, 0, Time.DeltaTime * _speed));
                _camera2Transform.Translate(new float3(0, 0, Time.DeltaTime * _speed));
            }

            if (Input.Mouse.LeftButton)
            {
                // Store mouse position when clicked.
                if (!_isMouseDown)
                {
                    _lastClickedMousePos = Input.Mouse.Position;
                    _isMouseDown = true;
                }

                // Mouse is over 2D camera.
                if (_lastClickedMousePos.y <= (Height / 100 * (100 - _mainCamViewportSize)))
                {
                    //Diagnostics.Debug(_lastClickedMousePos.y + "   " + Height / 100 * _mainCamViewportSize);
                    _camera2Transform.Translate(new float3(-Input.Mouse.Velocity.x * Time.DeltaTime * _camera2MouseSensitivity, Input.Mouse.Velocity.y * Time.DeltaTime * _camera2MouseSensitivity, 0));
                }

                // Mouse is over 3D camera.
                else
                {
                    _angleVelHorz = -RotationSpeed * Mouse.XVel * DeltaTime * 0.0005f;
                    _angleVelVert = -RotationSpeed * Mouse.YVel * DeltaTime * 0.0005f;

                    _angleHorz += _angleVelHorz;
                    _angleVert += _angleVelVert;

                    //_coordinateAxesTransform.FpsView(_angleHorz, _angleVert, 0, 0, _speed / 10);
                    _cameraTransform.FpsView(-_angleHorz, -_angleVert, 0, 0, _speed / 10);
                }
            }
            // Mouse is released so if it is clicked and held again the initial mouse position can be stored.
            else
            {
                _isMouseDown = false;
            }

            // Mouse is over 2D camera.
            if (Input.Mouse.Position.y <= (Height / 100 * (100 - _mainCamViewportSize)))
            {
                // 2D Rotation with right mouse button.
                if (Input.Mouse.RightButton)
                {
                    _camera2Transform.Rotate(new float3(0, Input.Mouse.Velocity.x * Time.DeltaTime * _camera2MouseSensitivity * 0.2f, 0));

                        Diagnostics.Debug(_camera2Transform.Rotation.y);
                    // Clamp camera angle.
                    if ((_camera2Transform.Rotation.y % M.PiOver2) > -0.05f && (_camera2Transform.Rotation.y % M.PiOver2) < 0.05)
                    {
                        float rounded = (float)System.Math.Round(_camera2Transform.Rotation.y / M.PiOver2) * M.PiOver2;

                        _camera2Transform.Rotation = new float3(0, rounded, 0);
                    }

                    // Rotate coordinate axes based on 2D view.
                    _coordinateAxesTransform.Rotation = new float3(_camera2Transform.Rotation.x, -_camera2Transform.Rotation.y, _camera2Transform.Rotation.z);
                }

                // Zoom in.
                float scrollspeed = .001f;
                _camera2.Scale -= Mouse.WheelVel * Time.DeltaTime * scrollspeed;

                // Set 2D camera zoom/scale restrictions.
                if (_camera2.Scale < .01f) _camera2.Scale = .01f;
                if (_camera2.Scale > 1) _camera2.Scale = 1;


            }
            // Mouse is over 3D camera.
            else
            {
                // Zoom in.
                float scrollspeed = .1f;
                _zoom -= Mouse.WheelVel * DeltaTime * scrollspeed;

                // Set zoom restrictions
                if (_zoom < .1f) _zoom = .1f;
                if (_zoom > 1) _zoom = 1;

                _camera.Fov = _zoom;

            }

            _cameraTransform.FpsView(-_angleHorz, -_angleVert, Keyboard.WSAxis, Keyboard.ADAxis, _speed / 10);
            _camera2Transform.Translation = new float3(_camera2Transform.Translation.x, _camera2Transform.Translation.y, _cameraTransform.Translation.z - _camera2.ClippingPlanes.y / 2);

            /*
           var curDamp = (float)System.Math.Exp(-Damping * DeltaTime);
           _angleVelHorz *= curDamp;
           _angleVelVert *= curDamp;
            */

            ToggleScannerChannel();
            //ChangeColorMode();
        }

        // RenderAFrame is called once per frame
        public override void RenderAFrame()
        {

            //Diagnostics.Warn(FramesPerSecond);
            //_footpulseText = "footpulse: " + _cameraTransform.Translation.z.ToString("f2");
            //UpdateGuiFootpulse();

            /*
            // Picking. Not working with pointclouds currently (is it even possible?).
            if (Input.Mouse.LeftButton)
            {
                float2 pickPosClip = Mouse.Position * new float2(2.0f / Width, -2.0f / Height) + new float2(-1, 1);
                PickResult newPick = _scenePicker.Pick(RC, pickPosClip).ToList().OrderBy(pr => pr.ClipPos.z).FirstOrDefault()!;
                if (newPick != null)
                {
                    Diagnostics.Debug($"Object {newPick.Node.Name} picked.");
                }
            }
            */

            if (PtRenderingParams.Instance.EdlStrength != 0f)
            {
                PtRenderingParams.Instance.DepthPassEf.Active = true;
                PtRenderingParams.Instance.ColorPassEf.Active = false;

                _camera.Viewport = new float4(0, 0, 100, 100);  // If Viewport is not "reset", the DepthTex will be rendered to only half of the camera (?????).
                _camera.RenderTexture = PtRenderingParams.Instance.ColorPassEf.DepthTex;
                //_camera2.RenderTexture = PtRenderingParams.Instance.ColorPassEf.DepthTex;
                _camera2.RenderTexture = null;

                _sceneRenderer.Render(RC);

                _camera.Viewport = new float4(0, 0, 100, _mainCamViewportSize);
                //_camera2.Viewport = new float4(0, _mainCamViewportSize, 100, 100 - _mainCamViewportSize);
                _camera.RenderTexture = null;
                _camera2.RenderTexture = null;

                PtRenderingParams.Instance.DepthPassEf.Active = false;
                PtRenderingParams.Instance.ColorPassEf.Active = true;
            }

            _sceneRenderer.Render(RC);

            //_canvasWidth = Width / 100f;
            //_canvasHeight = Height / 100f;


            //_guiRenderer.Render(RC);
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
                {
                    Min = new float2(0, 0),
                    Max = new float2(_canvasWidth, _canvasHeight)
                }
            );
            Font fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            _fontMap = new FontMap(fontLato, 18);
            _guiScene.Children.Add(_canvasNode);
        }

        // Draw the filename of the currently viewed sqlite file in the bottom left corner.
        public void DrawFilename()
        {
            TextNode text = TextNode.Create(
                _filename,
                "FilenameText",
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),
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

            Transform tr = new Transform
            {
                Translation = new float3(-11300, -6200, 900),  // ?????????? it works tho
            };
            text.AddComponent(tr);
            _canvasNode.Children.Add(text);
        }

        // Initialize textnode for displaying the current footpulse in the top left corner.
        public void DrawFootpulse()
        {
            _footpulseTextNode = TextNode.Create(
                _footpulseText,
                "Footpulse",
                GuiElementPosition.GetAnchors(AnchorPos.TopTopLeft),
                new MinMaxRect
                {
                    Min = new float2(0.0f, 0.0f),
                    Max = new float2(0.5f, 0.5f),
                },
                _fontMap,
                (float4)ColorUint.Greenery,
                HorizontalTextAlignment.Left,
                VerticalTextAlignment.Center
            );
            Transform tr = new Transform
            {
                Translation = new float3(-11300, 6200, 900),
            };
            _footpulseTextNode.AddComponent(tr);
            _canvasNode.Children.Add(_footpulseTextNode);
        }

        // Set text to current footpulse.
        public void UpdateGuiFootpulse()
        {
            _footpulseTextNode.Children[0].GetComponent<GuiText>().Text = _footpulseText;
        }

        // Generate TextureNodes and add buttons.
        public void DrawControlPanel()
        {
            float2 guisize = new float2(.4f, .4f);
            float xpos = _canvasWidth / 2 - guisize.x / 2;
            float ypos = 0f;

            _playNode = TextureNode.Create(
                "Play",  // Name
                new Texture(AssetStorage.Get<ImageData>("play1.png"), false, TextureFilterMode.Linear),
                GuiElementPosition.GetAnchors(AnchorPos.DownDownLeft),  // Anchor
                GuiElementPosition.CalcOffsets(AnchorPos.DownDownLeft,  // Offset
                    new float2(xpos, ypos),  // Position on parent
                    _canvasHeight,  // Parent height
                    _canvasWidth,  // Parent width
                    guisize  // Gui dimensions
                ),
                float2.One  // Tiling
            );

            _playNode.Components.Add(_playButton);

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

            _canvasNode.Children.Add(_playNode);

            _canvasNode.Children.Add(forward);
            _canvasNode.Children.Add(backward);
            _canvasNode.Children.Add(end);
            _canvasNode.Children.Add(beginning);

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
            _cameraTransform.Translate(new float3(0, 0, _speed));
            _camera2Transform.Translate(new float3(0, 0, _speed));
        }

        public void OnBackwardDown(CodeComponent sender)
        {
            Diagnostics.Debug("Backward button");
            _cameraTransform.Translate(new float3(0, 0, -_speed));
            _camera2Transform.Translate(new float3(0, 0, -_speed));
        }

        public void OnEndDown(CodeComponent sender)
        {
            Diagnostics.Debug("End button");
        }

        public void OnBeginningDown(CodeComponent sender)
        {
            Diagnostics.Debug("Beginning button");
            _cameraTransform.Translation = _initialCamTransform;
            _camera2Transform.Translation = _initialCamTransform;
        }

        public void ToggleScannerChannel()
        {
            if (_scannerChannelCooldown <= 0)
            {
                _scannerChannelCooldown = 0.15f;
                if (Keyboard.GetKey(KeyCodes.D1))
                {
                    _pointCloudComponentList[0].Active = !_pointCloudComponentList[0].Active;
                }
                if (Keyboard.GetKey(KeyCodes.D2))
                {
                    _pointCloudComponentList[1].Active = !_pointCloudComponentList[1].Active;
                }
                if (Keyboard.GetKey(KeyCodes.D3))
                {
                    _pointCloudComponentList[2].Active = !_pointCloudComponentList[2].Active;
                }
                if (Keyboard.GetKey(KeyCodes.D4))
                {
                    _pointCloudComponentList[3].Active = !_pointCloudComponentList[3].Active;
                }
                if (Keyboard.GetKey(KeyCodes.D8))
                {
                    _pointCloudComponentList[4].Active = !_pointCloudComponentList[4].Active;
                }
                if (Keyboard.GetKey(KeyCodes.D9))
                {
                    _pointCloudComponentList[5].Active = !_pointCloudComponentList[5].Active;
                }
            }
            _scannerChannelCooldown -= Time.DeltaTime;
        }

        // Change the colormode of the pointcloud. Not working yet.
        private void ChangeColorMode()
        {
            if (Keyboard.GetKey(KeyCodes.Up))
            {
                //PtRenderingParams.Instance.ColorMode = PointColorMode.Single;
                PtRenderingParams.Instance.ColorPassEf = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
            }
            if (Keyboard.GetKey(KeyCodes.Down))
            {
                //PtRenderingParams.Instance.ColorMode = PointColorMode.VertexColor0;
                PtRenderingParams.Instance.ColorPassEf = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
            }
        }

        #endregion Interactions
        private void OnThresholdChanged(int newValue)
        {
            _pointCloudComponent.PointCloudImp.PointThreshold = newValue;
        }

        private void OnProjectedSizeModifierChanged(float newValue)
        {
            _pointCloudComponent.PointCloudImp.MinProjSizeModifier = newValue;
        }
        public override void Resize(ResizeEventArgs e)
        {

            if (PtRenderingParams.Instance.EdlStrength == 0f) return;
            Diagnostics.Warn(_camera.Viewport + "  " + Width * (_camera.Viewport.z / 100) + "   " + Height * (_camera.Viewport.w / 100));
            PtRenderingParams.Instance.ColorPassEf.DepthTex = WritableTexture.CreateDepthTex((int)(Width * (_camera.Viewport.z / 100)), (int)(Height * (_camera.Viewport.w / 100)), new ImagePixelFormat(ColorFormat.Depth24));
            //PtRenderingParams.Instance.ColorPassEf.DepthTex = WritableTexture.CreateDepthTex((int)(Width), (int)(Height), new ImagePixelFormat(ColorFormat.Depth24));
        }
    }

}