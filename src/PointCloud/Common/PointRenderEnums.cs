﻿namespace Fusee.PointCloud.Common
{
    /// <summary>
    /// Available point shapes.
    /// </summary>
    public enum PointShape
    {
        /// <summary>
        /// Every point appears as a rectangle.
        /// </summary>
        Rect = 0,
        /// <summary>
        /// Every point appears as a circle.
        /// </summary>
        Circle = 1,
        /// <summary>
        /// Every point appears as a rectangle with weighted Z-Buffer values to generate a paraboloid.
        /// </summary>
        Paraboloid = 2
    }

    /// <summary>
    /// Available color modes.
    /// </summary>
    public enum PointColorMode
    {
        /// <summary>
        /// RGB value of the point.
        /// </summary>
        VertexColor0,
        /// <summary>
        /// RGB value of the point.
        /// </summary>
        VertexColor1,
        /// <summary>
        /// RGB value of the point.
        /// </summary>
        VertexColor2,
        /// <summary>
        /// RGB value of the point.
        /// </summary>
        Single,
    }

    /// <summary>
    /// Available point size modes.
    /// </summary>
    public enum PointSizeMode
    {
        /// <summary>
        /// Given point size is interpreted as diameter of the point in px
        /// </summary>
        FixedPixelSize = 0,

        /// <summary>
        /// Given point size is interpreted as diameter of the point in px for the initial camera position.
        /// If the camera gets closer, the point size in px will increase.
        /// </summary>
        FixedWorldSize = 1
    }

    /// <summary>
    /// Available Lighting methods.
    /// </summary>
    public enum PointCloudLighting
    {
        /// <summary>
        /// Albedo only - no lighting is calculated.
        /// </summary>
        Unlit = 0,

        /// <summary>
        /// Eye dome lighting
        /// </summary>
        Edl = 1
    }
}