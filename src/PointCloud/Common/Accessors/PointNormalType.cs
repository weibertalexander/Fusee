﻿using Fusee.Math.Core;

namespace Fusee.PointCloud.Common.Accessors
{
    /// <summary>
    /// Declares valid data types for a point cloud's normal vectors.
    /// </summary>
    public enum PointNormalType
    {
        /// <summary>
        /// A point cloud point without an normal.
        /// </summary>
        None,

        /// <summary>
        /// A point cloud point has a position value of type <see cref="float3"/>.
        /// </summary>
        Float3,
        /// <summary>
        /// A point cloud point has a position value of type <see cref="double3"/>.
        /// </summary>
        Double3
    }
}