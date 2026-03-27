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
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using SplinePoint = sc.splinemesher.pro.runtime.Structs.SplinePoint;

namespace sc.splinemesher.pro.runtime
{
	[BurstCompile]
    //Cache pre-evaluated spline points for much faster (repeated) sampling
    public struct SplineCache : IJobParallelFor
    {
        private readonly int sampleCount;
        private readonly NativeSpline spline;
        private readonly bool closed;

        private NativeArray<SplinePoint> points;
        public NativeArray<SplinePoint> Points => points;

        public SplineCache(NativeSpline nativeSpline, float sampleDistance, Structs.Accuracy accuracy = Structs.Accuracy.PreferPrecision)
        {
            float searchIntervalScalar = 1;
            searchIntervalScalar = accuracy switch
            {
                Structs.Accuracy.BestPerformance => 6f,
                Structs.Accuracy.PreferPerformance => 4f,
                Structs.Accuracy.Balanced => 3f,
                Structs.Accuracy.PreferPrecision => 1f,
                Structs.Accuracy.HighestPrecision => 0.5f,
                _ => searchIntervalScalar
            };
            
            this.spline = nativeSpline;
            this.closed = nativeSpline.Closed;
            
            sampleCount = Mathf.CeilToInt(nativeSpline.GetLength() / (sampleDistance * searchIntervalScalar));
            sampleCount = Mathf.Max(4, sampleCount);
            
            //Add 1 extra position for closed splines to store the closing point
            int arraySize = closed ? sampleCount + 1 : sampleCount;
            points = new NativeArray<SplinePoint>(arraySize, Allocator.TempJob);

            //Schedule jobs for all positions (including the extra closing point for closed splines)
            int jobCount = points.Length;

            JobHandle jobHandle = this.Schedule(jobCount, 64);
            jobHandle.Complete();
        }
        
        public void Execute(int i)
        {
            float t = i / (float)(sampleCount-1);
            
            if (closed && i == sampleCount) t = 0f;
            
            spline.Evaluate(t, out float3 position, out float3 tangent, out float3 up);

            if (i == 0)
            {
                //At the very start of the spline the tangent is often not valid. Sample up ahead a fraction
                spline.Evaluate(Utilities.MIN_T_VALUE, out _ , out tangent, out up);
            }

            if (i == sampleCount)
            {
                spline.Evaluate(Utilities.MAX_T_VALUE, out _ , out tangent, out up);
            }
            
            points[i] = new SplinePoint(position, tangent, up);
        }

        public void Dispose()
        {
            if (points.IsCreated) points.Dispose();
        }

        [BurstCompile]
        public static void Sample(ref NativeArray<SplinePoint> points, float t, out float3 position, out float3 tangent, out float3 up)
        {
            int pointCount = points.Length;
            
            if (pointCount == 0)
            {
                position = default;
                tangent = default;
                up = default;
                return;
            }
            
            float i = t * (float)(pointCount - 1);

            int prev = (int)math.floor(i);
            int next = (int)math.floor(i + 1);

            if (next >= pointCount)
            {
                position = points[pointCount - 1].position;
                tangent = points[pointCount - 1].tangent;
                up = points[pointCount - 1].up;
                return;
            }

            if (prev < 0)
            {
                position = points[0].position;
                tangent = points[0].tangent;
                up = points[0].up;
                return;
            }

            SplinePoint a = points[prev];
            SplinePoint b = points[next];

            t = i - prev;
            
            position = math.lerp(a.position, b.position, t);
            tangent = math.lerp(a.tangent, b.tangent, t);
            up = math.lerp(a.up, b.up, t);
        }
    }
}