// Spline Mesher Pro © Staggart Creations (http://staggart.xyz)
// COPYRIGHT PROTECTED UNDER THE UNITY ASSET STORE EULA (https://unity.com/legal/as-terms)
//
// ⚠️ WARNING: UNAUTHORIZED USE OR DISTRIBUTION IS STRICTLY PROHIBITED
// • Copying, referencing, or reverse-engineering this source code for the creation of new Asset Store or derivative products,
//   or any other publicly distributed content is strictly forbidden and will result in legal action.
// • Studying this file for the purpose of reproducing its functionality in your own assets or tools is not permitted.
// • If you are viewing this file as a reference, please close it immediately to avoid unintentional design influence or potential EULA violations.
// • Uploading this file or any derivative of it to a public GitHub or similar repository will trigger an automated DMCA takedown request.
// • Studying to understand for personal, educational or integration purposes is allowed, studying to reproduce is not.

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace sc.splinemesher.pro.runtime
{
    public static class Structs
    {
        public struct InputMeshData
        {
            [ReadOnly]
            public NativeArray<float3> positions;
            public ushort vertexCount;
            [ReadOnly]
            public NativeArray<float3> normals;
            [ReadOnly]
            public NativeArray<float4> tangents;
            [ReadOnly]
            public NativeArray<float2> uv;
            public NativeArray<float4> colors;
            
            //Array may be written to if triangle order is reversed
            public NativeArray<ushort> sourceTriangles;
            public int submeshCount;
            [ReadOnly]
            public NativeArray<int2> sourceSubmeshRanges;
            
            public BoundsData bounds;
            
            public bool IsCreated => positions.IsCreated;
            
            public void Dispose()
            {
                if (!IsCreated)
                {
                    //Debug.LogWarning("Mesh data was already disposed");
                    return;
                }
                
                positions.Dispose();
                normals.Dispose();
                tangents.Dispose();
                uv.Dispose();
                colors.Dispose();
                
                sourceTriangles.Dispose();
                sourceSubmeshRanges.Dispose();
            }
        }

        //[BurstCompile]
        public struct SplinePoint
        {
            public float3 position;
            public float3 tangent;
            public float3 up;
            
            public SplinePoint(float3 position, float3 tangent, float3 up)
            {
                this.position = position;
                this.tangent = tangent;
                this.up = up;
            }

            [BurstCompile]
            public static void Lerp(in SplinePoint a, in SplinePoint b, float t, out SplinePoint result)
            {
                result = new SplinePoint(
                    math.lerp(a.position, b.position, t),
                    math.lerp(a.tangent, b.tangent, t),
                    math.lerp(a.up, b.up, t)
                );
            }
        }
      
        public struct BoundsData
        {
            public float3 size;
            public float3 min;
            public float3 max;
            
            public float3 CalculatedCenter => (min + max) * 0.5f;
        }
        
        //Helper to pack 4 signed bytes
        public struct sbyte4
        {
            public sbyte x, y, z, w;

            public sbyte4(sbyte x, sbyte y, sbyte z, sbyte w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }
        }
        
        public struct SegmentInfo
        {
            public int tileCount;
            public float tileLength;
            public float meshLength;
        }
        
        public enum PointState
        {
            Inside,
            Edge,
            Outside
        }
        
        public enum Accuracy
        {
            BestPerformance,
            PreferPerformance,
            Balanced,
            PreferPrecision,
            HighestPrecision
        }
        
        public enum VertexColorChannel
        {
            Red,
            Green,
            Blue,
            Alpha
        }
        
        public enum Alignment
        {
            PivotPoint,
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

    }
}