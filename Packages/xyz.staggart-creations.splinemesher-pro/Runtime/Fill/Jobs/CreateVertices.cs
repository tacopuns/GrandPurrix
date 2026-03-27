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
using PointState = sc.splinemesher.pro.runtime.Structs.PointState;

namespace sc.splinemesher.pro.runtime
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct CreateVertices : IJobParallelFor, IDisposable
    {
        [ReadOnly] private NativeSpline spline;
        [ReadOnly] private NativeArray<Structs.SplinePoint> splinePoints;
        private float3 boundsMin;
        private float3 boundsMax;
        
        //Settings
        public float searchIntervalScalar;
        private int splineSearchSampleCount;
        
        private FillMeshSettings.Topology topologySettings;
        private FillMeshSettings.Displacement displacementSettings;
        
        private int2 gridSize;

        //Cached constants
        private float halfTriangleSize;
        private float triangleHeight;
        private float edgeDistanceThreshold;
        private float edgeDistanceThresholdSq;
        private float margin;

        //Output
        [NativeDisableParallelForRestriction]
        private NativeArray<float3> points;
        [NativeDisableParallelForRestriction]
        private NativeArray<PointState> pointStates;
        [NativeDisableParallelForRestriction]
        private NativeArray<float3> splinePositions;
        [NativeDisableParallelForRestriction]
        private NativeArray<float> distances;

        private const float EDGE_DISTANCE_THRESHOLD = 1.7320f; //Sqrt(3)
        
        public void Setup(int2 gridSize, Bounds bounds, NativeSpline nativeSpline, bool clockwise, NativeArray<Structs.SplinePoint> splinePoints, SplineFillMesher mesher)
        {
            this.spline = nativeSpline;
            this.splinePoints = splinePoints;
            boundsMin = bounds.min;
            boundsMax = bounds.max;

            topologySettings = mesher.settings.topology;
            displacementSettings = mesher.settings.displacement;
            
            searchIntervalScalar = 1;
            searchIntervalScalar = topologySettings.accuracy switch
            {
                Structs.Accuracy.BestPerformance => 6f,
                Structs.Accuracy.PreferPerformance => 4f,
                Structs.Accuracy.Balanced => 3f,
                Structs.Accuracy.PreferPrecision => 1f,
                Structs.Accuracy.HighestPrecision => 0.5f,
                _ => searchIntervalScalar
            };
            
            splineSearchSampleCount = (int)math.ceil(spline.GetLength() / (topologySettings.triangleSize * searchIntervalScalar));

            //Pre-calculate constants
            halfTriangleSize = topologySettings.triangleSize * 0.5f;
            triangleHeight = topologySettings.triangleSize * math.sin(math.PI / 3f);
            edgeDistanceThreshold = topologySettings.triangleSize * EDGE_DISTANCE_THRESHOLD;
            edgeDistanceThresholdSq = edgeDistanceThreshold * edgeDistanceThreshold;
            margin = topologySettings.margin;
            if(clockwise) margin = -margin;

            this.gridSize = gridSize;
            int pointCount = gridSize.x * gridSize.y;
            points = new NativeArray<float3>(pointCount, Allocator.Persistent);
            pointStates = new NativeArray<PointState>(pointCount, Allocator.Persistent);
            distances = new NativeArray<float>(pointCount, Allocator.Persistent);
            splinePositions = new NativeArray<float3>(pointCount, Allocator.Persistent);
        }

        public static int2 GetGridSize(float triangleSize, float width, float height)
        {
            int xCount = Mathf.CeilToInt((width / triangleSize)) + 2;
            //Offset by half the width of a triangle
            int zCount = Mathf.CeilToInt((height / (triangleSize * Mathf.Sqrt(3) * 0.5f))) + 1;
            
            //Counts are +1 to ensure the points on the far side can be made edge points
            
            return new int2(xCount, zCount);
        }

        public void Execute(int i)
        {
            float triangleSize = topologySettings.triangleSize;

            float epsilon = 0.02f;
            int x = i / gridSize.y;
            int z = i % gridSize.y;

            //World-space
            float3 position = new float3(
                boundsMin.x + (triangleSize * x) + ((z % 2) * halfTriangleSize),
                0,
                boundsMin.z + (triangleHeight * z)
                );
            position.x -= halfTriangleSize;

            if (math.any(displacementSettings.noise > 0))
            {
                float2 noiseCoords = new float2(position.x + displacementSettings.noise.x, position.z + displacementSettings.noise.z);
                position.x += noise.snoise(noiseCoords * displacementSettings.noiseFrequency.x * 0.1f) * displacementSettings.noise.x;
                position.z += noise.snoise(noiseCoords * displacementSettings.noiseFrequency.z * 0.1f) * displacementSettings.noise.z;
            }

            bool inside = IsInsideSpline(position, out float3 nearestPosition, out float t, out float minDistSq);
            
            if (topologySettings.snapToKnot)
            {
                //Nearest knot
                int knotIndex = (int)math.round(spline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Knot));

                if (knotIndex < spline.Knots.Length) //May be rounded up out of range
                {
                    BezierKnot knot = spline.Knots[knotIndex];

                    //Linear knot
                    if (math.lengthsq(knot.TangentOut + knot.TangentIn) < (epsilon * epsilon))
                    {
                        float knotDist = math.distance(knot.Position, nearestPosition);

                        //Snap to knot
                        if (knotDist < (triangleSize)-0.01f)
                        {
                            //Note: would want to check if the knot has already been snapped to, but this isn't possible in a IJobParallelFor
                            
                            nearestPosition = knot.Position;
                        }
                    }
                }
            }
            
            float distance = math.distance(position.xz, nearestPosition.xz);
            
            bool outside = !inside;
            //Use squared distance for edge check to avoid sqrt
            bool onEdge = (minDistSq < edgeDistanceThresholdSq);

            PointState state = PointState.Outside;
            
            //Test
            //outside = false; onEdge = false;
            
            if (inside)
            {
                state = PointState.Inside;
            }
            if (onEdge && outside)
            {
                state = PointState.Edge;

                //Snap to nearest point on spline
                position.x = nearestPosition.x;
                position.z = nearestPosition.z;
                
                //Point sits on the edge, so distance is 0
                distance = 0;
            }
            if (outside && !onEdge)
            {
                state = PointState.Outside;
            }

            //Test hole cutout
            /*
            if (inside)
            {
                float3 center = (boundsMax + boundsMin) * 0.5f;
                float distToCenter = math.distance(position, center);

                if (distToCenter < 5f)
                {
                    pointStates[i] = PointState.Outside;
                }
            }
            */
            
            points[i] = position;
            splinePositions[i] = nearestPosition;
            distances[i] = distance;
            pointStates[i] = state;
        }
        
        [BurstCompile(FloatPrecision.Medium, FloatMode.Fast, CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        private bool IsInsideSpline(float3 position, out float3 nearestPosition, out float nearestT, out float minDistSq)
        {
            nearestT = 0;
            
            SplineCache.Sample(ref splinePoints, nearestT, out float3 splinePoint, out float3 tangent, out float3 up);
            nearestPosition = splinePoint;
            
            if (margin != 0)
            {
                float3 right = math.cross(math.normalize(tangent), up);
                nearestPosition += right * margin;
            }

            float3 previousPosition = nearestPosition;

            bool isInside = false;
            minDistSq = float.MaxValue;

            int m_samples = splineSearchSampleCount;
            
            //Cache position components and pre-compute 
            float posX = position.x;
            float posZ = position.z;
            //rcp to replace division with multiplication
            float invSamples = 1.0f / m_samples;

            //Skip first iteration since the spline start has already been sampled
            for (int i = 1; i <= m_samples; i++)
            {
                float t = i * invSamples;

                //Use cached positions, at least twice as fast
                SplineCache.Sample(ref splinePoints, t, out splinePoint, out tangent, out up);

                if (margin != 0)
                {
                    float3 right = math.cross(math.normalize(tangent), up);
                    
                    splinePoint += right * margin;
                }

                float3 splinePosition = splinePoint;
                
                //Test raw sampling
                //splinePosition = spline.EvaluatePosition(t);
                
                //Check for distance from spline using squared distance
                //Manual distance squared calculation to avoid float2 temporary allocation
                float dx = splinePosition.x - posX;
                float dz = splinePosition.z - posZ;
                float distSq = math.abs(dx * dx + dz * dz);
                
                //Track nearest point for output
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    nearestPosition = splinePosition;
                    nearestT = t;
                }

                //Ray-edge intersection test using Z axis
                if ((previousPosition.z <= position.z && splinePosition.z > position.z) || 
                    (splinePosition.z <= position.z && previousPosition.z > position.z))
                {
                    float intersectionX = previousPosition.x + 
                                          (position.z - previousPosition.z) * 
                                          (splinePosition.x - previousPosition.x) / 
                                          (splinePosition.z - previousPosition.z);
                    
                    if (intersectionX > position.x)
                    {
                        isInside = !isInside;
                    }
                }

                previousPosition = splinePosition;
            }

            return isInside;
        }

        public NativeArray<float3> GetPoints()
        {
            return points;
        }
        
        public NativeArray<PointState> GetPointStates()
        {
            return pointStates;
        }
        
        public NativeArray<float3> GetNearestSplinePoints()
        {
            return splinePositions;
        }

        public NativeArray<float> GetDistances()
        {
            return distances;
        }

        public void Dispose()
        {
            //points.Dispose();
            //pointStates.Dispose();
            //distances.Dispose();
            //splinePoints.Dispose();
        }
    }
}