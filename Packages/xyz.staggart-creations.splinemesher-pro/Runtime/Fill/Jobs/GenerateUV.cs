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

namespace sc.splinemesher.pro.runtime
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct GenerateUV : IJob, IDisposable
    {
        [ReadOnly] private NativeArray<float3> points;
        [ReadOnly] private NativeArray<float> distances;

        private FillMeshSettings.UV settings;
        private bool storeGradientsInUV;
        private float3 boundsSize;
        
        [WriteOnly] private NativeArray<float4> uv0;
        
        public void Setup(NativeArray<float3> points, NativeArray<float> distances, Vector3 boundsSize, FillMeshSettings.UV uvSettings, bool storeGradients)
        {
            this.points = points;
            this.distances = distances;
            this.boundsSize = boundsSize;

            this.settings = uvSettings;
            this.storeGradientsInUV = storeGradients;

            uv0 = new NativeArray<float4>(points.Length, Allocator.Persistent);
        }
        
        public void Execute()
        {
            float maxDistance = float.MinValue;
            for (int i = 0; i < distances.Length; i++)
            {
                maxDistance = math.max(maxDistance, distances[i]);
            }
            
            for (int i = 0; i < points.Length; i++)
            {
                float4 uv = new float4( points[i].x, points[i].z, distances[i], distances[i] / maxDistance);

                if (storeGradientsInUV == false) uv.zw = 0f;
                
                //Center
                uv.x -= boundsSize.x * 0.5f;
                uv.y -= boundsSize.z * 0.5f;
                
                if (settings.fitToMesh)
                {
                    float size = math.max(boundsSize.x, boundsSize.z);
                    uv.xy /= size;
                    
                    //Does not retain aspect ratio
                    //uv.x /= boundsSize.x;
                    //uv.y /= boundsSize.z;
                }
                
                //Classic tiling & offset
                uv.xy *= settings.tiling;
                uv.xy += settings.offset;

                //Rotate 90 degrees
                if (settings.rotate) (uv.x, uv.y) = (uv.y, uv.x);
                
                uv.x = math.select(uv.x, 1f - uv.x, settings.FlipX);
                uv.y = math.select(uv.y, 1f - uv.y, settings.FlipY);

                uv0[i] = uv;
            }
        }

        public NativeArray<float4> GetUV()
        {
            return uv0;
        }

        public void Dispose()
        {
            uv0.Dispose();
        }
    }
}