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
using PointState = sc.splinemesher.pro.runtime.Structs.PointState;

namespace sc.splinemesher.pro.runtime
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Triangulate : IJob, IDisposable
    {
        private int2 gridSize;
        private float triangleSize;
        private float minTriangleSize;
        
        //Input
        [ReadOnly] private NativeArray<float3> points;
        private int originalPointCount;
        private int pointCount;
        [ReadOnly] private NativeArray<PointState> pointStates;
        [ReadOnly] private NativeArray<int> indexMapping;
        
        [WriteOnly] NativeList<int> triangles;

        public void Setup(int2 gridSize, float triangleSize, NativeArray<float3> m_points, NativeArray<PointState> m_pointStates, NativeArray<int> m_indexMapping)
        {
            this.gridSize = gridSize;
            this.triangleSize = triangleSize;
            this.minTriangleSize = triangleSize * 1.7320f; //Sqrt(3)
            this.points = m_points;
            this.pointStates = m_pointStates;
            this.indexMapping = m_indexMapping;

            //Amount of points in the virtual grid
            originalPointCount = gridSize.x * gridSize.y;
            //Actual points in the filtered grid
            pointCount = points.Length;
            
            triangles = new NativeList<int>(pointCount * 4, Allocator.Persistent);
        }
        
        public void Execute()
        {
            //Iterate over the same grid. Points will have been filtered but the 'indexMapping' array maps indices to the correct vertex
            for (int x = 0; x < gridSize.x - 1; x++)
            {
                for (int z = 0; z < gridSize.y - 1; z++)
                {
                    int currentIndex = x * gridSize.y + z;
                    int rightIndex = x * gridSize.y + (z + 1);
                    int bottomIndex = (x + 1) * gridSize.y + z;
                    int bottomRightIndex = (x + 1) * gridSize.y + (z + 1);
                    
                    //Skip if any index is out of bounds
                    if (currentIndex >= originalPointCount || rightIndex >= originalPointCount ||
                        bottomIndex >= originalPointCount || bottomRightIndex >= originalPointCount)
                        continue;
                    
                    //Remap current coordinate to the actual vertex
                    int p0 = indexMapping[currentIndex];
                    int p1 = indexMapping[rightIndex];
                    int p2 = indexMapping[bottomIndex];
                    int p3 = indexMapping[bottomRightIndex];
                    
                    //Invalid indices, belongs to filtered out vertices
                    if (p0 == -1 || p1 == -1 || p2 == -1 || p3 == -1) continue;
                    
                    //One or more vertices fall outside the curve
                    if (pointStates[p0] == PointState.Outside || pointStates[p1] == PointState.Outside || 
                        pointStates[p2] == PointState.Outside || pointStates[p3] == PointState.Outside) continue;

                    if (z % 2 == 0)
                    {
                        //▲
                        AddTriangle(p0, p1, p2);
                        AddTriangle(p1, p3, p2);
                    }
                    else
                    {
                        //▼ Inverted triangles
                        AddTriangle(p0, p1, p3);
                        AddTriangle(p0, p3, p2);
                    }
                }
            }
        }
        
        void AddTriangle(int v0, int v1, int v2)
        {
            int edgePoints = 0;
            
            edgePoints += pointStates[v0] == PointState.Edge ? 1 : 0;
            edgePoints += pointStates[v1] == PointState.Edge ? 1 : 0;
            edgePoints += pointStates[v2] == PointState.Edge ? 1 : 0;
            //Check if all points are edge points AND form a degenerate or very thin triangle
            if (edgePoints >= 3)
            {
                //Get the positions of the triangle
                float3 posA = points[v0];
                float3 posB = points[v1];
                float3 posC = points[v2];

                //Calculate triangle surface area
                float area = math.length(math.cross(posB - posA, posC - posA)) * 0.5f;

                //Skip if the triangle is smaller than intended
                //Rejecting triangles that are too large solves for triangles forming in concave spline areas (eg. linear knot)
                if (area < minTriangleSize || area > triangleSize)
                {
                    return;
                }
            }

            //Valid triangle at this point, add it
            triangles.Add(v0);
            triangles.Add(v1);
            triangles.Add(v2);
        }

        public NativeList<int> GetTriangles()
        {
            return triangles;
        }
        
        public void Dispose()
        {
            triangles.Dispose();
        }
    }
}