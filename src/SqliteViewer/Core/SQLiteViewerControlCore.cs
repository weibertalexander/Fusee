using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Engine.Core.Scene;
using Fusee.Math.Core;
using Fusee.ImGuiImp.Desktop.Templates;
using Fusee.PointCloud.Common;
using Fusee.PointCloud.Core.Scene;
using Fusee.PointCloud.Potree.V2;
using Fusee.Engine.Core.Effects;
using System;
using System.Collections.Generic;

namespace Fusee.Examples.SQLiteViewer.Core
{
    internal class SQLiteViewerControlCore : FuseeControlToTexture, IDisposable
    {
        public bool UseWPF { get; set; }
        public bool ReadyToLoadNewFile { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsAlive { get; private set; }

        // angle variables
        private static float _angleHorz, _angleVert, _angleVelHorz, _angleVelVert;
        private const float RotationSpeed = 7;

        private SceneContainer _scene;

        // Rendering related.
        private SceneRendererForward _sceneRenderer;
        private RenderLayer _pointCloudLayer = new RenderLayer { Layer = RenderLayers.Layer01 };
        private RenderLayer _coordinateLayer = new RenderLayer { Layer = RenderLayers.Layer02 };

        // Camera related.
        private Camera _camera;
        private Camera _camera2;
        private Camera _camera3;

        private bool _seperate = false;

        private Transform _cameraTransform;
        private Transform _camera2Transform;
        private Transform _camera3Transform;

        private float3 _initialCamTransform;

        private SceneNode _coordinateAxes;
        private Transform _coordinateAxesTransform;

        private bool _controls3D = false;
        private bool _controls2D = false;

        public bool Controls3D
        {
            set
            {
                _controls3D = value;
                _controls2D = !value;
            }
        }

        public bool Controls2D
        {
            set
            {
                _controls2D = value;
                _controls3D = !value;
            }
        }

        public bool SeperateCameras
        {
            set
            {
                _seperate = !_seperate;
                if (!_seperate)
                {
                    _camera2Transform.Translation = new float3(_camera2Transform.Translation.x, _camera2Transform.Translation.y, _cameraTransform.Translation.z);
                }
            }
            get { return _seperate; }
        }

        private float4 _cameraBackgroundColor = (float4)ColorUint.Black;
        private float4 _camera2BackgroundColor = (float4)ColorUint.Black;

        private int _cameraRenderDistance = 100;

        private int _camera2RenderStart = 1;
        private float _camera2RenderDistance = 1f;

        public float Camera2DRenderDistance
        {
            set
            {
                _camera2RenderDistance = value;

                _camera2.ClippingPlanes.y = _camera2RenderStart + value;

                _guidelinesNearTransform.Translation = new float3(0, _guidelinesNearTransform.Translation.y, _camera2.ClippingPlanes.x);

                _guidelinesFarTransform.Translation = new float3(0, _guidelinesFarTransform.Translation.y, _camera2.ClippingPlanes.y);
            }
        }
        public int Camera3DRenderDistance
        {
            set
            {
                _cameraRenderDistance = value;
                _camera.ClippingPlanes.y = _cameraRenderDistance;
            }
        }

        public float4 Camera1BackgroundColor
        {
            get { return _cameraBackgroundColor; }
            set
            {
                _cameraBackgroundColor = value;
                _camera.BackgroundColor = value;
            }
        }

        public float4 Camera2BackgroundColor
        {
            get { return _camera2BackgroundColor; }
            set
            {
                _camera2BackgroundColor = value;
                _camera2.BackgroundColor = value;
            }
        }


        // Pointcloud related.
        private Potree2Reader[] _readerList;
        private PointCloudComponent[] _pointCloudComponentList;
        private SurfaceEffectPointCloud[] _colorPassEfs;
        public void SetColorPassEfColor(int i, float4 color)
        {
            _colorPassEfs[i].SurfaceInput.Albedo = color;
        }

        private int _startFootpulse;
        private int _endFootpulse;

        public float CurrentFootpulse
        {
            get { return (float)(_cameraTransform.Translation.z + _startFootpulse); }
            set { _cameraTransform.Translation = new float3(_cameraTransform.Translation.x, _cameraTransform.Translation.y, value - _startFootpulse); }
        }

        public int EndFootpulse
        {
            get { return _endFootpulse; }
        }

        private bool _keys;

        // Control related.
        private bool _isPlaying = false;
        public bool IsPlaying
        {
            get { return _isPlaying; }
        }

        private bool _guideLinesOn = true;
        public bool GuideLinesOn
        {
            get { return _guideLinesOn; }
        }

        private SceneNode _guidelines;
        private Transform _guidelinesNearTransform;
        private Transform _guidelinesFarTransform;

        private bool _isMouseDown = false;
        private float2 _lastClickedMousePos;
        private float _playerspeed = 5;
        public float Playerspeed
        {
            set { _playerspeed = value; }
        }

        private float _camera2MouseSensitivity = 0.02f;
        private float _zoom = 1f;

        private int _width;
        private int _height;

        private bool _channel1 = true;
        private bool _channel2 = true;
        private bool _channel3 = true;
        private bool _channel4 = true;
        private bool _channel8 = true;
        private bool _channel9 = true;

        public bool Channel1
        {
            get { return _channel1; }
        }
        public bool Channel2
        {
            get { return _channel2; }
        }

        public bool Channel3
        {
            get { return _channel3; }
        }
        public bool Channel4
        {
            get { return _channel4; }
        }
        public bool Channel8
        {
            get { return _channel8; }
        }
        public bool Channel9
        {
            get { return _channel9; }
        }

        public bool ClosingRequested
        {
            get => _closingRequested;
            set => _closingRequested = value;
        }
        private bool _closingRequested;

        private float3 _initCameraPos;

        private PointCloudComponent? _pointCloud;
        private RenderContext _rc;

        public SQLiteViewerControlCore(RenderContext rc) : base(rc)
        {
            _rc = rc;
            Init();
        }

        public override void Init()
        {
            try
            {
                PtRenderingParams.Instance.DepthPassEf = MakePointCloudEffect.ForDepthPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape);

                PtRenderingParams.Instance.ColorPassEf1 = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
                PtRenderingParams.Instance.ColorPassEf2 = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
                PtRenderingParams.Instance.ColorPassEf3 = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
                PtRenderingParams.Instance.ColorPassEf4 = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
                PtRenderingParams.Instance.ColorPassEf8 = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
                PtRenderingParams.Instance.ColorPassEf9 = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);

                _colorPassEfs = new SurfaceEffectPointCloud[] { PtRenderingParams.Instance.ColorPassEf1, PtRenderingParams.Instance.ColorPassEf2, PtRenderingParams.Instance.ColorPassEf3, PtRenderingParams.Instance.ColorPassEf4, PtRenderingParams.Instance.ColorPassEf8, PtRenderingParams.Instance.ColorPassEf9 };

                PtRenderingParams.Instance.PointThresholdHandler = OnThresholdChanged;
                PtRenderingParams.Instance.ProjectedSizeModifierHandler = OnProjectedSizeModifierChanged;

                _scene = new SceneContainer();

                InitCameras();
                InitCoordAxes();

                FileManager.CreateDirectories();

                FileManager.CreateOctreeFromDB(PtRenderingParams.Instance.PathToSqliteFile);

                _startFootpulse = FileManager.FootpulseStart;
                _endFootpulse = FileManager.FootpulseEnd;

                InitPointClouds();

                _sceneRenderer = new SceneRendererForward(_scene);

                PointCloudRenderModule module = new PointCloudRenderModule(true);

                _sceneRenderer.VisitorModules.Add(module);

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Diagnostics.Error(ex);
                _sceneRenderer = new SceneRendererForward(new SceneContainer());
            }
        }

        // Initialize 3 cameras: Main 3D camera, 2D camera and a camera for coordinate axes.
        private void InitCameras()
        {
            _initialCamTransform = new float3(0, 5, 0);

            // 3D camera.
            _camera = new Camera(ProjectionMethod.Perspective, 0.1f, _cameraRenderDistance, M.PiOver4, RenderLayers.Layer01)
            {
                Layer = 1,
                BackgroundColor = _cameraBackgroundColor,
            };

            // 2D camera.
            _camera2 = new Camera(ProjectionMethod.Orthographic, _camera2RenderStart, _camera2RenderStart + _camera2RenderDistance, M.PiOver4, RenderLayers.Layer01)
            {
                Layer = 1,
                BackgroundColor = _cameraBackgroundColor,

                Scale = 0.075f
            };

            // Axes camera.
            _camera3 = new Camera(ProjectionMethod.Perspective, 0.1f, 50, M.PiOver4, RenderLayers.Layer02)
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
                Translation = new float3(1, 0, -6),
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

            _guidelinesNearTransform = new Transform
            {
                Translation = new float3(0, -5, _camera2Transform.Translation.z + _camera2.ClippingPlanes.x),
            };

            _guidelinesFarTransform = new Transform
            {
                Translation = new float3(0, -5, _camera2Transform.Translation.z + _camera2.ClippingPlanes.y),
            };

            // Shows 2D-Cameras viewing area.
            _guidelines = new SceneNode
            {
                Name = "Camera2Boundaries",
                Components =
                {
                    _pointCloudLayer,
                },
                Children =
                {
                    new SceneNode
                    {
                        Components =
                        {
                            _guidelinesNearTransform,
                            MakeEffect.FromUnlit(new float4(0, 0, 255, 0.14f)),
                            SimpleMeshes.CreateCuboid(new float3(100, 0.02f, 0.02f)),
                        },
                    },
                    new SceneNode
                    {
                        Components =
                        {
                            _guidelinesFarTransform,
                            MakeEffect.FromUnlit(new float4(255, 0, 0, 0.14f)),
                            SimpleMeshes.CreateCuboid(new float3(100, 0.02f, 0.02f)),
                        }
                    }
                }
            };
            cam2.Children.Add(_guidelines);
            /*
            // Set viewports: 2D camera is on top of 3D cameras viewport, axes camera is on the right side of 2D cameras viewport.
            _camera.Viewport = new float4(0, 0, 100, _mainCamViewportSize);
            _camera2.Viewport = new float4(0, _mainCamViewportSize, 100, 100 - _mainCamViewportSize);
            _camera3.Viewport = new float4(_mainCamViewportSize + ((100 - _mainCamViewportSize) / 4), _mainCamViewportSize + ((100 - _mainCamViewportSize) / 4), 15, 15);
            */
            _camera.Viewport = new float4(0, 0, 100, 100);
            _camera2.Viewport = new float4(0, 0, 100, 100);
            _camera3.Viewport = new float4(0, 0, 50, 20);


            _scene.Children.Add(cam);
            _scene.Children.Add(cam2);
            _scene.Children.Add(cam3);
        }

        // Initialize coordinate axes, which will rotate according to 2D cameras viewing direction.
        private async void InitCoordAxes()
        {

            _coordinateAxesTransform = new Transform
            {
                Translation = new float3(0, -2f, -2f),
                Rotation = new float3(0, 0, 0),
            };
            SceneContainer coordsScene = AssetStorage.Get<SceneContainer>("coords.fus");
            _coordinateAxes = coordsScene.Children[0];
            _coordinateAxes.Components.Add(_coordinateLayer);
            _coordinateAxes.Components.Add(_coordinateAxesTransform);

            _scene.Children.Add(_coordinateAxes);
        }

        // Initialize 6 pointclouds from one SQLite file, one for each scanner channel.
        private void InitPointClouds()
        {
            string[] directories = Directory.GetDirectories(PtRenderingParams.Instance.PathToOocFile);
            _readerList = new Potree2Reader[6];
            _pointCloudComponentList = new PointCloudComponent[6];
            for (int i = 0; i < directories.Length; i++)
            {
                _readerList[i] = new Potree2Reader();
                _pointCloudComponentList[i] = (PointCloudComponent)_readerList[i].GetPointCloudComponent(directories[i]);
                _pointCloudComponentList[i].PointCloudImp.MinProjSizeModifier = PtRenderingParams.Instance.ProjectedSizeModifier;
                _pointCloudComponentList[i].PointCloudImp.PointThreshold = PtRenderingParams.Instance.PointThreshold;

                _pointCloudComponentList[i].Camera = _camera;

                Transform cloudTransform = new Transform()
                {
                    Scale = float3.One,
                    Translation = new float3((float)_readerList[i].GetPointCloudOffset().x, (float)_readerList[i].GetPointCloudOffset().y, (float)_readerList[i].GetPointCloudOffset().z),
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
                        _colorPassEfs[i],
                        _pointCloudComponentList[i]
                    }
                };
                _scene.Children.Add(cloud);
            }
        }

        private WritableTexture RenderTexture3D;
        private WritableTexture RenderTexture2D;

        private bool disposedValue;

        public bool _render2DFrame = false;
        public bool Render2DFrame
        {
            set { _render2DFrame = value; }
        }
        // RenderAFrame is called once a frame
        protected override ITextureHandle RenderAFrame()
        {

            ReadyToLoadNewFile = false;

            if (_closingRequested)
            {
                ReadyToLoadNewFile = true;

                return new Engine.Imp.Graphics.Desktop.TextureHandle
                {
                    DepthRenderBufferHandle = -1,
                    FrameBufferHandle = -1,
                    //TexHandle = -1
                };
            }

            if (_render2DFrame)
            {
                _camera2.RenderTexture = RenderTexture2D;
                _camera3.RenderTexture = RenderTexture2D;

                _sceneRenderer.Render(_rc);
                ReadyToLoadNewFile = true;

                return RenderTexture2D?.TextureHandle;

            }
            else
            {

            }
            //_camera.RenderTexture = null;
            _camera.RenderTexture = RenderTexture3D;
            //_camera2.RenderTexture = RenderTexture2D;
            //_camera3.RenderTexture = RenderTexture2D;

            _sceneRenderer.Render(_rc);

            ReadyToLoadNewFile = true;

            return RenderTexture3D?.TextureHandle;


        }

        public override void Update(bool allowInput)
        {
            // Play button is active.
            if (_isPlaying)
            {
                _cameraTransform.Translate(new float3(0, 0, Time.DeltaTime * _playerspeed));
                if (!_seperate) _camera2Transform.Translate(new float3(0, 0, Time.DeltaTime * _playerspeed));
            }

            // 2D camera controls.
            if (_controls2D)
            {
                // Controls with left mouse button.
                if (Input.Mouse.LeftButton)
                {
                    // Store mouse position when clicked.
                    if (!_isMouseDown)
                    {
                        _lastClickedMousePos = Input.Mouse.Position;
                        _isMouseDown = true;
                    }
                    _camera2Transform.Translate(new float3(-Input.Mouse.Velocity.x * Time.DeltaTime * _camera2MouseSensitivity, Input.Mouse.Velocity.y * Time.DeltaTime * _camera2MouseSensitivity, 0));
                }

                // Mouse is released so if it is clicked and held again the initial mouse position can be stored.
                else
                {
                    _isMouseDown = false;
                }

                // 2D Rotation with right mouse button.
                if (Input.Mouse.RightButton)
                {
                    _camera2Transform.Rotate(new float3(0, Input.Mouse.Velocity.x * Time.DeltaTime * _camera2MouseSensitivity * 0.2f, 0));

                    // Clamp camera angle at  mod90 degree angles.
                    if ((_camera2Transform.Rotation.y % M.PiOver2) > -0.05f && (_camera2Transform.Rotation.y % M.PiOver2) < 0.05)
                    {
                        float rounded = (float)System.Math.Round(_camera2Transform.Rotation.y / M.PiOver2) * M.PiOver2;

                        _camera2Transform.Rotation = new float3(0, rounded, 0);
                    }

                    // Rotate coordinate axes based on 2D view.
                    _coordinateAxesTransform.Rotation = new float3(0, 0, -_camera2Transform.Rotation.y);
                }

                // Zoom in.
                float scrollspeed = .001f;
                _camera2.Scale -= Input.Mouse.WheelVel * Time.DeltaTime * scrollspeed;

                // Set 2D camera zoom/scale restrictions.
                if (_camera2.Scale < .001f) _camera2.Scale = .001f;
                if (_camera2.Scale > 1) _camera2.Scale = 1;

            }

            // 3D camera controls.
            if (_controls3D)
            {
                // Camera rotation with left mouse button.
                if (Input.Mouse.LeftButton)
                {

                    _angleVelHorz = -RotationSpeed * Input.Mouse.XVel * Time.DeltaTime * 0.0005f;
                    _angleVelVert = -RotationSpeed * Input.Mouse.YVel * Time.DeltaTime * 0.0005f;

                    _angleHorz += _angleVelHorz;
                    _angleVert += _angleVelVert;

                    _cameraTransform.FpsView(-_angleHorz, -_angleVert, 0, 0, 0);
                }

                // General movement with keyboard.
                _cameraTransform.FpsView(-_angleHorz, -_angleVert, Input.Keyboard.WSAxis, Input.Keyboard.ADAxis, _playerspeed / 20);
                if (!_seperate) _camera2Transform.Translation = new float3(_camera2Transform.Translation.x, _camera2Transform.Translation.y, _cameraTransform.Translation.z);

                // Zoom in.
                float scrollspeed = .1f;
                _zoom -= Input.Mouse.WheelVel * Time.DeltaTime * scrollspeed;

                // Set zoom restrictions
                if (_zoom < .1f) _zoom = .1f;
                if (_zoom > 1) _zoom = 1;

                _camera.Fov = _zoom;
            }
        }

        private void OnThresholdChanged(int newValue)
        {
            if (_pointCloud != null)
                _pointCloud.PointCloudImp.PointThreshold = newValue;
        }

        private void OnProjectedSizeModifierChanged(float newValue)
        {
            if (_pointCloud != null)
                _pointCloud.PointCloudImp.MinProjSizeModifier = newValue;
        }

        public int width2d = 10;
        public int height2d = 10;

        // Is called when the window was resized
        protected override void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            _width = width;
            _height = height;

            // delete old texture, generate new
            if (_render2DFrame)
            {
                RenderTexture2D?.Dispose();
                RenderTexture2D = WritableTexture.CreateAlbedoTex(width2d, height2d, new ImagePixelFormat(ColorFormat.RGBA));
            }
            else
            {
                RenderTexture3D?.Dispose();
                RenderTexture3D = WritableTexture.CreateAlbedoTex(width, height, new ImagePixelFormat(ColorFormat.RGBA));

            }

            if (PtRenderingParams.Instance.EdlStrength == 0f) return;
            foreach (var ef in _colorPassEfs)
            {
                ef.DepthTex?.Dispose();
                ef.DepthTex = WritableTexture.CreateDepthTex((int)(width), (int)(height), new ImagePixelFormat(ColorFormat.Depth24));
            }
            _camera.RenderTexture = RenderTexture3D;
            _camera2.RenderTexture = RenderTexture2D;
            _camera3.RenderTexture = RenderTexture2D;
            //PtRenderingParams.Instance.ColorPassEf.DepthTex = WritableTexture.CreateDepthTex((int)(_width * (_camera.Viewport.z / 100)), (int)(_height * (_camera.Viewport.w / 100)), new ImagePixelFormat(ColorFormat.Depth24));
        }

        public void ResetCamera()
        {
            _cameraTransform.Translation = _initCameraPos;
        }

        public void ToggleGuidelines()
        {
            _guideLinesOn = !_guideLinesOn;
            _guidelines.EnumChildren.ToList().ForEach(child => child.EnumComponents.ToList().ForEach(component => component.Active = _guideLinesOn));
        }

        public void OnPlayDown()
        {
            _isPlaying = !_isPlaying;
        }

        public void ToggleScanner1()
        {
            _pointCloudComponentList[0].Active = !_pointCloudComponentList[0].Active;
            _channel1 = _pointCloudComponentList[0].Active;
        }

        public void ToggleScanner2()
        {
            _pointCloudComponentList[1].Active = !_pointCloudComponentList[1].Active;
            _channel2 = _pointCloudComponentList[1].Active;
        }

        public void ToggleScanner3()
        {
            _pointCloudComponentList[2].Active = !_pointCloudComponentList[2].Active;
            _channel3 = _pointCloudComponentList[2].Active;
        }

        public void ToggleScanner4()
        {
            _pointCloudComponentList[3].Active = !_pointCloudComponentList[3].Active;
            _channel4 = _pointCloudComponentList[3].Active;
        }

        public void ToggleScanner8()
        {
            _pointCloudComponentList[4].Active = !_pointCloudComponentList[4].Active;
            _channel8 = _pointCloudComponentList[4].Active;
        }

        public void ToggleScanner9()
        {
            _pointCloudComponentList[5].Active = !_pointCloudComponentList[5].Active;
            _channel9 = _pointCloudComponentList[5].Active;
        }

        public void OnForwardDown(int value)
        {
            _cameraTransform.Translate(new float3(0, 0, value));
            //_camera2Transform.Translate(new float3(0, 0, value));
        }

        public void OnBackwardDown(int value)
        {
            _cameraTransform.Translate(new float3(0, 0, -value));
            //_camera2Transform.Translate(new float3(0, 0, -value));
        }

        public void OnEndDown()
        {
            _cameraTransform.Translation = new float3(_cameraTransform.Translation.x, _cameraTransform.Translation.y, FileManager.FootpulseAmount);
        }

        public void OnBeginningDown()
        {
            _cameraTransform.Translation = _initialCamTransform;
            _camera2Transform.Translation = _initialCamTransform;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RenderTexture3D?.Dispose();
                    RenderTexture2D?.Dispose();
                    foreach (var ef in _colorPassEfs)
                    {
                        ef.DepthTex?.Dispose();

                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}