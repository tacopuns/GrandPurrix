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

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace sc.splinemesher.pro.runtime
{
    [BurstCompile]
    public struct Displacement : IJobParallelFor
    {
        private FillMeshSettings.Displacement settings;
        private FillMeshSettings.Conforming conformingSettings;
        private float4x4 worldToLocal;
        
        //Input
        private NativeArray<float3> points;
        [ReadOnly]
        private NativeArray<Structs.PointState> pointStates;
        [ReadOnly]
        private NativeArray<float3> splinePoints;
        private NativeArray<float> distances;
        private NativeArray<RaycastHit> hits;
        private float maxDistance;
        private float averageHeight;

        private bool enableConforming;
        
        public void Setup(FillMeshSettings.Displacement settings, FillMeshSettings.Conforming conforming, Matrix4x4 worldToLocal, NativeArray<float3> points, NativeArray<Structs.PointState> pointStates, 
            NativeArray<float> distances, NativeArray<float3> splinePoints, NativeArray<RaycastHit> hits, float maxDistance, float averageHeight)
        {
            this.settings = settings;
            this.conformingSettings = conforming;
            this.worldToLocal = worldToLocal;

            this.points = points;
            this.pointStates = pointStates;
            this.distances = distances;
            this.splinePoints = splinePoints;
            this.hits = hits;
            enableConforming = hits.Length > 0;

            this.maxDistance = maxDistance;
            this.averageHeight = averageHeight;
        }

        public void Execute(int i)
        {
            float3 position = points[i];
            position.y = averageHeight;

            if (math.any(settings.noise > 0))
            {
                position.y += noise.snoise(new float2(position.x + settings.noiseOffset.x, position.z + settings.noiseOffset.z) * settings.noiseFrequency.y * 0.1f) * settings.noise.y;
            }

            //Distance of position to spline point
            float distance = distances[i];
            float3 splinePoint = splinePoints[i];
            Structs.PointState state = pointStates[i];
            
            //Blend the position of the vertex to the spline point position gradually
            float normalizedDistance = math.saturate(((distance / settings.flatteningFalloff) / maxDistance));
            float blendFactor = (1f-normalizedDistance) * (1f-settings.flattening);
            position.y = math.lerp(position.y, splinePoint.y, blendFactor);
            
            if (enableConforming)
            {
                RaycastHit hit = hits[i];
                if (hit.distance > 0)
                {
                    position = hit.point + (hit.normal * conformingSettings.heightOffset);
                }
            }

            //Bulging
            float signedDist = math.saturate(((distance / settings.bulgeFalloff)));
            position.y += settings.bulge * math.abs(signedDist);

            position.y += settings.offset;

            position = math.transform(worldToLocal, position.xyz).xyz;

            points[i] = position;
        }

        public Bounds GetBounds()
        {
            //Calculate bounds after job completion by iterating through all points
            float3 min = float.MaxValue;
            float3 max = float.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                Structs.PointState state = pointStates[i];

                if (state == Structs.PointState.Inside || state == Structs.PointState.Edge)
                {
                    float3 position = points[i];
                    min = math.min(position, min);
                    max = math.max(position, max);
                }
            }

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }

        public void Dispose()
        {
            
        }
    }
}