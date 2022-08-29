using Fusee.Engine.Core.Effects;
using Fusee.Engine.Core.ShaderShards;
using Fusee.PointCloud.Common;
using System;

namespace Fusee.Examples.SQLiteViewer.Core
{
    public delegate void PointThresholdHandler(int val);
    public delegate void ProjectedSizeModifierHandler(float val);

    public sealed class PtRenderingParams : IDisposable
    {
        public static PtRenderingParams Instance { get; private set; } = new();

        public PointThresholdHandler PointThresholdHandler;
        public ProjectedSizeModifierHandler ProjectedSizeModifierHandler;

        // Currently depending on my system, will be changed later.
        //public string PathToOocFile = @"C:\Praktikum\datenbanken\potree\220202002-0000";
        public string PathToOocFile = "";

        //public string PathToSqliteFile = @"C:\Praktikum\datenbanken\220202002-0000.sqlite";
        public string PathToSqliteFile = "";


        public ShaderEffect DepthPassEf;
        public SurfaceEffectPointCloud ColorPassEf1;
        public SurfaceEffectPointCloud ColorPassEf2;
        public SurfaceEffectPointCloud ColorPassEf3;
        public SurfaceEffectPointCloud ColorPassEf4;
        public SurfaceEffectPointCloud ColorPassEf8;
        public SurfaceEffectPointCloud ColorPassEf9;

        private PointShape _shape = PointShape.Paraboloid;
        public PointShape Shape
        {
            get { return _shape; }
            set
            {
                _shape = value;
                ColorPassEf1.PointShape = (int)_shape;
                ColorPassEf2.PointShape = (int)_shape;
                ColorPassEf3.PointShape = (int)_shape;
                ColorPassEf4.PointShape = (int)_shape;
                ColorPassEf8.PointShape = (int)_shape;
                ColorPassEf9.PointShape = (int)_shape;
                DepthPassEf.SetFxParam(UniformNameDeclarations.PointShapeHash, (int)Shape);
            }
        }

        private PointSizeMode _ptMode = PointSizeMode.FixedWorldSize;
        public PointSizeMode PtMode
        {
            get { return _ptMode; }
            set
            {
                _ptMode = value;
                ColorPassEf1.PointSizeMode = (int)_ptMode;
                ColorPassEf2.PointSizeMode = (int)_ptMode;
                ColorPassEf3.PointSizeMode = (int)_ptMode;
                ColorPassEf4.PointSizeMode = (int)_ptMode;
                ColorPassEf8.PointSizeMode = (int)_ptMode;
                ColorPassEf9.PointSizeMode = (int)_ptMode;
                DepthPassEf.SetFxParam(UniformNameDeclarations.PointSizeModeHash, (int)_ptMode);
            }
        }

        private ColorMode _colorMode = ColorMode.VertexColor0;

        public ColorMode ColorMode
        {
            get { return _colorMode; }
            set
            {
                _colorMode = value;
                ColorPassEf1.ColorMode = (int)_colorMode;
                ColorPassEf2.ColorMode = (int)_colorMode;
                ColorPassEf3.ColorMode = (int)_colorMode;
                ColorPassEf4.ColorMode = (int)_colorMode;
                ColorPassEf8.ColorMode = (int)_colorMode;
                ColorPassEf9.ColorMode = (int)_colorMode;
            }
        }

        private int _size = 6;
        public int Size
        {
            get { return _size; }
            set
            {
                _size = value;
                DepthPassEf.SetFxParam(UniformNameDeclarations.PointSizeHash, Size);
                ColorPassEf1.PointSize = _size;
                ColorPassEf2.PointSize = _size;
                ColorPassEf3.PointSize = _size;
                ColorPassEf4.PointSize = _size;
                ColorPassEf8.PointSize = _size;
                ColorPassEf9.PointSize = _size;
            }
        }

        private int _edlNoOfNeighbourPx = 1;
        public int EdlNoOfNeighbourPx
        {
            get { return _edlNoOfNeighbourPx; }
            set
            {
                _edlNoOfNeighbourPx = value;
                ColorPassEf1.EDLNeighbourPixels = _edlNoOfNeighbourPx;
                ColorPassEf2.EDLNeighbourPixels = _edlNoOfNeighbourPx;
                ColorPassEf3.EDLNeighbourPixels = _edlNoOfNeighbourPx;
                ColorPassEf4.EDLNeighbourPixels = _edlNoOfNeighbourPx;
                ColorPassEf8.EDLNeighbourPixels = _edlNoOfNeighbourPx;
                ColorPassEf9.EDLNeighbourPixels = _edlNoOfNeighbourPx;
            }
        }

        private float _edlStrength = 0f;
        public float EdlStrength
        {
            get { return _edlStrength; }
            set
            {
                _edlStrength = value;
                ColorPassEf1.EDLStrength = _edlStrength;
                ColorPassEf2.EDLStrength = _edlStrength;
                ColorPassEf3.EDLStrength = _edlStrength;
                ColorPassEf4.EDLStrength = _edlStrength;
                ColorPassEf8.EDLStrength = _edlStrength;
                ColorPassEf9.EDLStrength = _edlStrength;
            }
        }

        private float _projSizeMod = 0f;
        public float ProjectedSizeModifier
        {
            get { return _projSizeMod; }
            set
            {
                _projSizeMod = value;
                ProjectedSizeModifierHandler(_projSizeMod);
            }
        }

        private int _ptThreshold = 40000000;

        public int PointThreshold
        {
            get { return _ptThreshold; }
            set
            {
                _ptThreshold = value;
                PointThresholdHandler(_ptThreshold);
            }
        }

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static PtRenderingParams()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                if (Instance != null)
                {
                    Instance = null;
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
        ~PtRenderingParams()
        {
            Dispose(false);
        }
    }
}