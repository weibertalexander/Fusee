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

namespace Fusee.Viewer.Core
{
    [FuseeApplication(Name = "Punktwolkenviewer2D", Description = "Punktwolkenviewer für .sqlite Dateien")]
    public class View2D : RenderCanvas
    {
        private float _angleHorz = 0, _angleVert = 0f;
        private static float _angleVelHorz, _angleVelVert;

        private SceneContainer _guiScene;
        private SceneContainer _scene;

        private SceneRendererForward _sceneRenderer;
        private SceneRendererForward _guiRenderer;

        private Camera _camera;
        private Transform _cameraTransform;
        private float _camZ = 0;
        private Transform _cloudTransform;

        private float _zoom = 1;
        private float _speed = 5;

        private PointCloudComponent _pointCloud;
        private bool _keys;

        private SceneInteractionHandler _sceneInteraction;

        private GuiButton _playButton;
        private GuiButton _forwardButton;
        private GuiButton _backwardButton;
        private GuiButton _beginningButton;
        private GuiButton _endButton;

        private TextureNode _playNode;
        private TextureNode _stopNode;
        private CanvasNode _canvasNode;
        private float _canvasWidth;
        private float _canvasHeight;

        private bool _isPlaying = false;

        public override void Init()
        {
            //PtRenderingParams.Instance.PathToOocFile = Directory.GetDirectories(FileManager.GetConvDir())[0];
            PtRenderingParams.Instance.DepthPassEf = MakePointCloudEffect.ForDepthPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape);
            PtRenderingParams.Instance.ColorPassEf = MakePointCloudEffect.ForColorPass(PtRenderingParams.Instance.Size, PtRenderingParams.Instance.ColorMode, PtRenderingParams.Instance.PtMode, PtRenderingParams.Instance.Shape, PtRenderingParams.Instance.EdlStrength, PtRenderingParams.Instance.EdlNoOfNeighbourPx);

            RC.ClearColor = new float4(1, 1, 1, 1);
            //_filename = Path.GetFileName(FileManager.GetSqliteFiles()[0]);

            _scene = new SceneContainer();
            _guiScene = new SceneContainer();

            //InitCamera();
            InitPointCloud();

            PrepareUI();
            InitButtons();
            DrawControlPanel();

            _sceneRenderer = new SceneRendererForward(_scene);
            _guiRenderer = new SceneRendererForward(_guiScene);

            _sceneInteraction = new SceneInteractionHandler(_guiScene);

            _sceneRenderer.VisitorModules.Add(new PointCloudRenderModule(true));
        }

        public void InitCamera()
        {
            _camera = new Camera(ProjectionMethod.Perspective, 1f, 1000f, M.PiOver4)
            {
                BackgroundColor = (float4)ColorUint.PapayaWhip,
            };
            _cameraTransform = new Transform
            {
                Translation = new float3(20, 2, 0),
            };
            SceneNode cam = new SceneNode
            {
                Name = "Cam",
                Components =
                {
                    _cameraTransform,
                    _camera
                }
            };
            _scene.Children.Add(cam);
        }

        public void InitPointCloud()
        {
            Potree2Reader potreeReader = new();

            _pointCloud = (PointCloudComponent)potreeReader.GetPointCloudComponent(PtRenderingParams.Instance.PathToOocFile);
            _pointCloud.PointCloudImp.MinProjSizeModifier = PtRenderingParams.Instance.ProjectedSizeModifier;
            _pointCloud.PointCloudImp.PointThreshold = PtRenderingParams.Instance.PointThreshold;
            _cloudTransform = new Transform
            {
                Scale = new float3(1, 1, 1),
                Translation = new float3(0, 0, 0),
                Rotation = new float3(0, 0, 0)
            };
            var pointCloudNode = new SceneNode()
            {
                Name = "PointCloud",
                Components = new List<SceneComponent>()
                {
                    _cloudTransform,
                    PtRenderingParams.Instance.DepthPassEf,
                    PtRenderingParams.Instance.ColorPassEf,
                    _pointCloud
                }
            };

            _scene.Children.Add(pointCloudNode);
        }

        public override async Task InitAsync()
        {
            await base.InitAsync();
        }

        public override void Update()
        {
            _sceneInteraction.CheckForInteractiveObjects(RC, Input.Mouse.Position, Width, Height);
            if (_isPlaying)
            {
                _camZ -= Time.DeltaTime * _speed;

            }
        }

        public override void RenderAFrame()
        {
            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            RC.Viewport(0, 0, Width, Height);
            // Create Projection for pointcloud (with zoom factor).
            //RC.Projection = float4x4.CreatePerspectiveFieldOfView(M.PiOver4, (float)Width / Height, 1, 1000);
            float fac = 30;
            RC.Projection = float4x4.CreateOrthographic(Width / fac, Height / fac, 1, 10);

            PtRenderingParams.Instance.DepthPassEf.Active = false;
            PtRenderingParams.Instance.ColorPassEf.Active = true;

            if (Keyboard.WSAxis == 1)
            {
                _camZ -= 1f;
            }
            else if (Keyboard.WSAxis == -1)
            {
                _camZ += 1f;
            }

            // zoom in
            float scrollspeed = .1f;
            _zoom += Mouse.WheelVel * DeltaTime * scrollspeed;

            // Set zoom restrictions
            if (_zoom < .1f) _zoom = .1f;
            if (_zoom > 2) _zoom = 2;

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;

            // Create the camera matrix and set it as the current View transformation
            var mtxRot = float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz);
            var mtxCam = float4x4.LookAt(0, 0, 1, 0, 0, 0, 0, 1, 0) * float4x4.Scale(_zoom);
            RC.View = mtxCam * mtxRot * float4x4.CreateTranslation(-20, -5, _camZ);

            //_camera.Fov = _zoom;
            //_cameraTransform.FpsView(-_angleHorz, -_angleVert, Keyboard.WSAxis, Keyboard.ADAxis, DeltaTimeUpdate * 20);
            //_cameraTransform.Translate(new float3(0, 0, _camZ));

            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.
            
            _sceneRenderer.Render(RC);
            _guiRenderer.Render(RC);

            Present();
        }

        // Set initial canvas values and initialize fontmap
        public void PrepareUI()
        {
            CanvasRenderMode canvasRenderMode = CanvasRenderMode.Screen;
            _canvasWidth = Width / 100f;
            _canvasHeight = Height / 100f;

            Diagnostics.Debug(_canvasWidth + "    " + _canvasHeight);

            _canvasNode = new CanvasNode(
                "Canvas",
                canvasRenderMode,
                new MinMaxRect
                {
                    Min = new float2(0, 0),
                    Max = new float2(_canvasWidth, _canvasHeight)
                }
            );

            Font fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            FontMap fontMap = new FontMap(fontLato, 18);
            _guiScene.Children.Add(_canvasNode);
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
            _camZ -= _speed;
        }

        public void OnBackwardDown(CodeComponent sender)
        {
            Diagnostics.Debug("Backward button");
            _camZ += _speed;
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
    }
}
