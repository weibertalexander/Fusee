using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Engine.Core.Scene;
using Fusee.Math.Core;
using Fusee.PointCloud.Common;
using Fusee.PointCloud.Core.Scene;
using Fusee.PointCloud.Potree.V2;
using System;
using System.Collections.Generic;

namespace Fusee.Examples.SQLiteViewer.Core
{
    internal class SQLiteViewerControlCore : ImGuiDesktop.Templates.FuseeControlToTexture, IDisposable
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

        private Transform _cameraTransform;
        private Transform _camera2Transform;
        private Transform _camera3Transform;

        private float3 _initialCamTransform;
        private float _mainCamViewportSize = 70;

        private SceneNode _coordinateAxes;
        private Transform _coordinateAxesTransform;

        private float4 _cameraBackgroundColor = (float4)ColorUint.Black;

        public float4 CameraBackgroundColor
        {
            get { return _cameraBackgroundColor; }
            set
            {
                _cameraBackgroundColor = value;
                _camera.BackgroundColor = value;
                _camera2.BackgroundColor = value;
            }
        }

        // Pointcloud related.
        private Potree2Reader[] _readerList;
        private PointCloudComponent[] _pointCloudComponentList;

        private int _startFootpulse;
        private int _endFootpulse;

        public String CurrentFootpulse
        {
            get { return ((int)_cameraTransform.Translation.z + _startFootpulse).ToString(); }
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

        private bool _isMouseDown = false;
        private float2 _lastClickedMousePos;
        private float _speed = 5;
        private float _camera2MouseSensitivity = 0.02f;
        private float _zoom = 1f;
        private float _scannerChannelCooldown = 0f;

        private const float ZNear = 1f;
        private const float ZFar = 1000;

        private int _width;
        private int _height;

        private readonly float _fovy = M.PiOver4;

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
        }

        public override void Init()
        {
            try
            {
                PtRenderingParams.Instance.DepthPassEf = MakePointCloudEffect.ForDepthPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape);
                PtRenderingParams.Instance.ColorPassEf = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);
                PtRenderingParams.Instance.PointThresholdHandler = OnThresholdChanged;
                PtRenderingParams.Instance.ProjectedSizeModifierHandler = OnProjectedSizeModifierChanged;


                _scene = new SceneContainer();

                InitCamera();

                InitCoordAxes();

                //PlaceOriginPoint();

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
        private void InitCamera()
        {
            _initialCamTransform = new float3(0, 5, 0);

            // 3D camera.
            _camera = new Camera(ProjectionMethod.Perspective, 0.1f, 150, M.PiOver4, RenderLayers.Layer01)
            {
                Layer = 1,
                BackgroundColor = _cameraBackgroundColor,
            };

            // 2D camera.
            _camera2 = new Camera(ProjectionMethod.Orthographic, 1f, 2f, M.PiOver4, RenderLayers.Layer01)
            {
                Layer = 1,
                BackgroundColor = _cameraBackgroundColor,

                Scale = 0.05f
            };

            // Axes camera.
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

            // Shows 2D-Cameras viewing area.
            SceneNode camera2Boundaries = new SceneNode
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

            // Set viewports: 2D camera is on top of 3D cameras viewport, axes camera is on the right side of 2D cameras viewport.
            _camera.Viewport = new float4(0, 0, 100, _mainCamViewportSize);
            //_camera.Viewport = new float4(0, 0, 100, 100);
            _camera2.Viewport = new float4(0, _mainCamViewportSize, 100, 100 - _mainCamViewportSize);
            _camera3.Viewport = new float4(_mainCamViewportSize + ((100 - _mainCamViewportSize) / 4), _mainCamViewportSize + ((100 - _mainCamViewportSize) / 4), 15, 15);

            _scene.Children.Add(cam);
            _scene.Children.Add(cam2);
            _scene.Children.Add(cam3);
        }

        // Initialize coordinate axes, which will rotate according to 2D cameras viewing direction.
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
                        PtRenderingParams.Instance.ColorPassEf,
                        _pointCloudComponentList[i]
                    }
                };
                _scene.Children.Add(cloud);
            }
        }

        private WritableTexture RenderTexture;
        private bool disposedValue;

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
                    TexHandle = -1
                };
            }

            if (PtRenderingParams.Instance.EdlStrength != 0f)
            {
                PtRenderingParams.Instance.DepthPassEf.Active = true;
                PtRenderingParams.Instance.ColorPassEf.Active = false;

                _camera.Viewport = new float4(0, 0, 100, 100);  // If Viewport is not "reset", the DepthTex will be rendered to only half of the camera (?????).
                _camera.RenderTexture = PtRenderingParams.Instance.ColorPassEf.DepthTex;

                _sceneRenderer.Render(_rc);

                //_camera.Viewport = new float4(0, 0, 100, _mainCamViewportSize);
                _camera2.Viewport = new float4(0, _mainCamViewportSize, 100, 100 - _mainCamViewportSize);

                _camera.RenderTexture = null;
                //_camera2.RenderTexture = null;

                PtRenderingParams.Instance.DepthPassEf.Active = false;
                PtRenderingParams.Instance.ColorPassEf.Active = true;
            }

            _camera.RenderTexture = RenderTexture;
            _camera2.RenderTexture = RenderTexture;
            _camera3.RenderTexture = RenderTexture;

            _sceneRenderer.Render(_rc);

            ReadyToLoadNewFile = true;
            return RenderTexture.TextureHandle;
        }

        public override void Update(bool allowInput)
        {
            //SetPointcloud();

            // Play button is active.
            if (_isPlaying)
            {
                _cameraTransform.Translate(new float3(0, 0, Time.DeltaTime * _speed));
                _camera2Transform.Translate(new float3(0, 0, Time.DeltaTime * _speed));
            }

            // Controls with left mouse button.
            if (Input.Mouse.LeftButton)
            {
                // Store mouse position when clicked.
                if (!_isMouseDown)
                {
                    _lastClickedMousePos = Input.Mouse.Position;
                    _isMouseDown = true;
                }

                // Mouse is over 2D camera.
                if (_lastClickedMousePos.y <= (_height / 100 * (100 - _mainCamViewportSize)))
                {
                    //Diagnostics.Debug(_lastClickedMousePos.y + "   " + Height / 100 * _mainCamViewportSize);
                    if (allowInput)
                    {
                        _camera2Transform.Translate(new float3(-Input.Mouse.Velocity.x * Time.DeltaTime * _camera2MouseSensitivity, Input.Mouse.Velocity.y * Time.DeltaTime * _camera2MouseSensitivity, 0));
                    }
                }

                // Mouse is over 3D camera.
                else
                {
                    _angleVelHorz = -RotationSpeed * Input.Mouse.XVel * Time.DeltaTime * 0.0005f;
                    _angleVelVert = -RotationSpeed * Input.Mouse.YVel * Time.DeltaTime * 0.0005f;

                    if (allowInput)
                    {
                        _angleHorz += _angleVelHorz;
                        _angleVert += _angleVelVert;
                    }

                    //_coordinateAxesTransform.FpsView(_angleHorz, _angleVert, 0, 0, _speed / 10);
                    _cameraTransform.FpsView(-_angleHorz, -_angleVert, 0, 0, _speed / 10);
                }
            }
            // Mouse is released so if it is clicked and held again the initial mouse position can be stored.
            else
            {
                _isMouseDown = false;
            }

            // Zoom and controls with right mouse button over 2D camera.
            if (Input.Mouse.Position.y <= (_height / 100 * (100 - _mainCamViewportSize)))
            {
                // 2D Rotation with right mouse button.
                if (Input.Mouse.RightButton && allowInput)
                {
                    _camera2Transform.Rotate(new float3(0, Input.Mouse.Velocity.x * Time.DeltaTime * _camera2MouseSensitivity * 0.2f, 0));

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
                _camera2.Scale -= Input.Mouse.WheelVel * Time.DeltaTime * scrollspeed;

                // Set 2D camera zoom/scale restrictions.
                if (_camera2.Scale < .01f) _camera2.Scale = .01f;
                if (_camera2.Scale > 1) _camera2.Scale = 1;


            }
            // Zoom over 3D camera.
            else
            {
                if (allowInput)
                {
                    // Zoom in.
                    float scrollspeed = .1f;
                    _zoom -= Input.Mouse.WheelVel * Time.DeltaTime * scrollspeed;

                    // Set zoom restrictions
                    if (_zoom < .1f) _zoom = .1f;
                    if (_zoom > 1) _zoom = 1;

                    _camera.Fov = _zoom;
                }
            }

            if (allowInput)
            {
                _cameraTransform.FpsView(-_angleHorz, -_angleVert, Input.Keyboard.WSAxis, Input.Keyboard.ADAxis, _speed / 10);
                _camera2Transform.Translation = new float3(_camera2Transform.Translation.x, _camera2Transform.Translation.y, _cameraTransform.Translation.z - _camera2.ClippingPlanes.y / 2);
            }

            /*
           var curDamp = (float)System.Math.Exp(-Damping * DeltaTime);
           _angleVelHorz *= curDamp;
           _angleVelVert *= curDamp;
            */

            ToggleScannerChannel();
        }

        public void ToggleScannerChannel()
        {
            if (_scannerChannelCooldown <= 0)
            {
                _scannerChannelCooldown = 0.15f;
                if (Input.Keyboard.GetKey(KeyCodes.D1))
                {
                    _pointCloudComponentList[0].Active = !_pointCloudComponentList[0].Active;
                }
                if (Input.Keyboard.GetKey(KeyCodes.D2))
                {
                    _pointCloudComponentList[1].Active = !_pointCloudComponentList[1].Active;
                }
                if (Input.Keyboard.GetKey(KeyCodes.D3))
                {
                    _pointCloudComponentList[2].Active = !_pointCloudComponentList[2].Active;
                }
                if (Input.Keyboard.GetKey(KeyCodes.D4))
                {
                    _pointCloudComponentList[3].Active = !_pointCloudComponentList[3].Active;
                }
                if (Input.Keyboard.GetKey(KeyCodes.D8))
                {
                    _pointCloudComponentList[4].Active = !_pointCloudComponentList[4].Active;
                }
                if (Input.Keyboard.GetKey(KeyCodes.D9))
                {
                    _pointCloudComponentList[5].Active = !_pointCloudComponentList[5].Active;
                }
            }
            _scannerChannelCooldown -= Time.DeltaTime;
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

        // Is called when the window was resized
        protected override void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            _width = width;
            _height = height;

            // delete old texture, generate new
            RenderTexture?.Dispose();
            // RenderTexture = WritableMultisampleTexture.CreateAlbedoTex(_rc, width, height, 8);
            RenderTexture = WritableTexture.CreateAlbedoTex(width, height, new ImagePixelFormat(ColorFormat.RGBA));

            if (PtRenderingParams.Instance.EdlStrength == 0f) return;
            PtRenderingParams.Instance.ColorPassEf.DepthTex?.Dispose();
            PtRenderingParams.Instance.ColorPassEf.DepthTex = WritableTexture.CreateDepthTex((int)(_width * (_camera.Viewport.z / 100)), (int)(_height * (_camera.Viewport.w / 100)), new ImagePixelFormat(ColorFormat.Depth24));
        }

        public void ResetCamera()
        {
            _cameraTransform.Translation = _initCameraPos;
            //_cameraTransform.FpsView(_angleHorz, _angleVert, Input.Keyboard.WSAxis, Input.Keyboard.ADAxis, Time.DeltaTimeUpdate * 20);
        }

        public void OnPlayDown()
        {
            _isPlaying = !_isPlaying;
        }

        public void ToggleScanner1()
        {
            _pointCloudComponentList[0].Active = !_pointCloudComponentList[0].Active;
        }

        public void ToggleScanner2()
        {
            _pointCloudComponentList[1].Active = !_pointCloudComponentList[1].Active;
        }

        public void ToggleScanner3()
        {
            _pointCloudComponentList[2].Active = !_pointCloudComponentList[2].Active;
        }

        public void ToggleScanner4()
        {
            _pointCloudComponentList[3].Active = !_pointCloudComponentList[3].Active;
        }

        public void ToggleScanner8()
        {
            _pointCloudComponentList[4].Active = !_pointCloudComponentList[4].Active;
        }

        public void ToggleScanner9()
        {
            _pointCloudComponentList[5].Active = !_pointCloudComponentList[5].Active;
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
            _cameraTransform.Translation = new float3(_cameraTransform.Translation.x, _cameraTransform.Translation.y, _endFootpulse - 10);
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
                    RenderTexture?.Dispose();
                    PtRenderingParams.Instance.ColorPassEf.DepthTex?.Dispose();
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