using Fusee.Math.Core;
using System.Numerics;
using Xunit.Abstractions;
using Xunit;

namespace Fusee.Test.Math.Core
{
    

    class VsNumerics
    {
        private readonly ITestOutputHelper output;

        public VsNumerics(ITestOutputHelper output)
        {
            this.output = output;
        }


        private float4x4 matrix1 = new float4x4(new float4(1, 2, 3, 4), new float4(2, 3, 4, 1), new float4(3, 2, 4, 1), new float4(4, 2, 1, 3));
        private float4x4 matrix2 = new float4x4(new float4(4, 3, 2, 1), new float4(3, 2, 1, 4), new float4(2, 3, 1, 4), new float4(1, 3, 4, 2));

        private Matrix4x4 matrix3 = new Matrix4x4(1, 2, 3, 4, 2, 3, 4, 1, 3, 2, 4, 1, 4, 2, 1, 3);
        private Matrix4x4 matrix4 = new Matrix4x4(4, 3, 2, 1, 3, 2, 1, 4, 2, 3, 1, 4, 1, 3, 4, 2);

        private float3 vector1 = new float3(1, 2, 3);
        private float3 vector2 = new float3(3, 2, 1);

        private Vector3 vector3 = new Vector3(1, 2, 3);
        private Vector3 vector4 = new Vector3(3, 2, 1);


        public void IsAddEqualFloat3()
        {
            var expected1 = new Vector3(4,4,4);
            var expected2 = new float3(4, 4, 4);

            var newVector3 = System.Numerics.Vector3.Add(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Add(vector1, vector2);

            Assert.Equal(expected1, newVector3);
            Assert.Equal(expected2, newFloat3);


        }

        public void IsSubEqualFloat3()
        {
            var expected1 = new Vector3(-2, 0, 2);
            var expected2 = new float3(-2, 0, 2);

            var newVector3 = System.Numerics.Vector3.Subtract(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Subtract(vector1, vector2);

            Assert.Equal(expected1, newVector3);
            Assert.Equal(expected2, newFloat3);


        }

        public void IsMultEqualFloat3()
        {
            var expected1 = new Vector3(3,4,3);
            var expected2 = new float3(3, 4, 3);
            var newVector3 = System.Numerics.Vector3.Multiply(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Multiply(vector1, vector2);

            Assert.Equal(expected1, newVector3);
            Assert.Equal(expected2, newFloat3);


        }

        public void IsDivideEqualFloat3()
        {
            var expected1 = new Vector3(1 / 3, 2 / 2, 3 / 1);
            var expected2 = new float3(1 / 3, 2 / 2, 3 / 1);

            var newVector3 = System.Numerics.Vector3.Divide(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Divide(vector1, vector2);

            Assert.Equal(expected1, newVector3);
            Assert.Equal(expected2, newFloat3);


        }
        public void IsMinEqualFloat3()
        {
            var expected1 = new Vector3(1,2,1);
            var expected2 = new float3(1, 2, 1);

            var minVector3 = System.Numerics.Vector3.Min(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Min(vector1, vector2);

            Assert.Equal(expected1, minVector3);
            Assert.Equal(expected2, newFloat3);

        }

        public void IsMaxEqualFloat3()
        {
            var expected1 = new Vector3(3,2,3);
            var expected2 = new float3(3, 2, 3);

            var maxVector3 = System.Numerics.Vector3.Max(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Max(vector1, vector2);

            Assert.Equal(expected1, maxVector3);
            Assert.Equal(expected2, newFloat3);

        }

        public void IsNormalizeEqualFloat3()
        {
            var expected1 = new Vector3 (1 / System.MathF.Sqrt(14), System.MathF.Sqrt(2 / 7), (float)(3 / System.Math.Sqrt(14)));
            var expected2 = new float3(1 / System.MathF.Sqrt(14), System.MathF.Sqrt(2 / 7), (float)(3 / System.Math.Sqrt(14)));

            var normalizedVector3 = System.Numerics.Vector3.Normalize(vector3);
            var newFloat3 = Fusee.Math.Core.float3.Normalize(vector1);

            Assert.Equal(expected1, normalizedVector3);
            Assert.Equal(expected2, newFloat3);

        }

        public void IsDotEqualFloat3()
        {
            var expected = 10;

            var dotVector3 = System.Numerics.Vector3.Dot(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Dot(vector1, vector2);

            Assert.Equal(expected, dotVector3);
            Assert.Equal(expected, newFloat3);

        }

        public void IsCrossEqualFloat3()
        {
            var expected1 = new Vector3(-4, 8, -4);
            var expected2 = new float3(-4, 8, -4);

            var crossVector3 = System.Numerics.Vector3.Cross(vector3, vector4);
            var newFloat3 = Fusee.Math.Core.float3.Cross(vector1, vector2);

            Assert.Equal(expected1, crossVector3);
            Assert.Equal(expected2, newFloat3);

        }
              
        public void IsAddEqualFloat4X4()
        {
            var expected1 = new Matrix4x4(5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5);
            var expected2 = new float4x4(5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5);

            var newMatrix = System.Numerics.Matrix4x4.Add(matrix3, matrix4);
            var newFloat4X4 = Fusee.Math.Core.float4x4.Add(matrix1, matrix2);
            
            Assert.Equal(expected1, newMatrix);
            Assert.Equal(expected2, newFloat4X4);
        }

        public void IsSubEqualFloat4X4()
        {
            var expected1 = new Matrix4x4(-3, -1, 1, 3, -1, 1, 3, -3, 1, -1, 3, -3, 3, -1, -3, 1);
            var expected2 = new float4x4(-3, -1, 1, 3, -1, 1, 3, -3, 1, -1, 3, -3, 3, -1, -3, 1);

            var newMatrix = System.Numerics.Matrix4x4.Subtract(matrix3, matrix4);
            var newFloat4X4 = Fusee.Math.Core.float4x4.Subtract(matrix1, matrix2);

            Assert.Equal(expected1, newMatrix);
            Assert.Equal(expected2, newFloat4X4);
        }

        public void IsMultEqualFloat4X4()
        {
            var expected1 = new Matrix4x4(20, 28, 23, 29, 26, 27, 15, 32, 27, 28, 16, 29, 27, 28, 23, 22);
            var expected2 = new float4x4(20, 28, 23, 29, 26, 27, 15, 32, 27, 28, 16, 29, 27, 28, 23, 22);

            var newMatrix = System.Numerics.Matrix4x4.Multiply(matrix3, matrix4);
            var newFloat4X4 = Fusee.Math.Core.float4x4.Mult(matrix1, matrix2);

            Assert.Equal(expected1, newMatrix);
            Assert.Equal(expected2, newFloat4X4);
        }

        public void IsTransposeEqualFloat4X4()
        {
            var expected1 = new Matrix4x4(1, 2, 3, 4, 2, 3, 2, 2, 3, 4, 4, 1, 4, 1, 1, 3);
            var expected2 = new float4x4(1, 2, 3, 4, 2, 3, 2, 2, 3, 4, 4, 1, 4, 1, 1, 3);

            var newMatrix = System.Numerics.Matrix4x4.Transpose(matrix3);
            var newFloat4X4 = Fusee.Math.Core.float4x4.Transpose(matrix1);

            Assert.Equal(expected1, newMatrix);
            Assert.Equal(expected2, newFloat4X4);
        }

        public void IsTransformEqualFloat3()
        {
            float w = (matrix1.M14 * vector1.x) + (matrix1.M24 * vector1.y) + (matrix1.M34 * vector1.z) + matrix1.M44;
            float v = (matrix3.M14 * vector3.X) + (matrix3.M24 * vector3.Y) + (matrix3.M34 * vector3.Z) + matrix3.M44;

            var expected1 = new float3(((matrix1.M11 * vector1.x) + (matrix1.M21 * vector1.y) + (matrix1.M31 * vector1.z) + matrix1.M41) / w,
                ((matrix1.M12 * vector1.x) + (matrix1.M22 * vector1.y) + (matrix1.M32 * vector1.z) + matrix1.M42) / w,
                ((matrix1.M13 * vector1.x) + (matrix1.M23 * vector1.y) + (matrix1.M33 * vector1.z) + matrix1.M43) / w);

            var expected2 = new Vector3(((matrix3.M11 * vector3.X) + (matrix3.M21 * vector3.Y) + (matrix3.M31 * vector3.Z) + matrix3.M41) / v,
                ((matrix3.M12 * vector3.X) + (matrix3.M22 * vector3.Y) + (matrix3.M32 * vector3.Z) + matrix3.M42) / v,
                ((matrix3.M13 * vector3.X) + (matrix3.M23 * vector3.Y) + (matrix3.M33 * vector3.Z) + matrix3.M43) / v);

            var newVector = System.Numerics.Vector3.Transform(vector3, matrix3);
            var newFloat3 = Fusee.Math.Core.float4x4.Transform(matrix1, vector1);

            Assert.Equal(expected2, newVector);
            Assert.Equal(expected1, newFloat3);

        }

      








    }
}
