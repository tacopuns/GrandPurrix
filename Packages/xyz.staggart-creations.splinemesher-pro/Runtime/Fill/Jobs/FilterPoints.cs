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
using PointState = sc.splinemesher.pro.runtime.Structs.PointState;

namespace sc.splinemesher.pro.runtime
{
    //Removes any vertices marked as 'outside' of the spline.
    //Creates an index mapping array so original coordinates map to the new vertex
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct FilterPoints : IJob, IDisposable
    {
        //Input
        [ReadOnly] private NativeArray<float3> oldPoints;
        [ReadOnly] private NativeArray<PointState> oldPointStates;
        [ReadOnly] private NativeArray<float3> oldSplinePoints;
        [ReadOnly] private NativeArray<float> oldDistances;
        
        //Output
        [WriteOnly] public NativeArray<float3> newPoints;
        [WriteOnly] public NativeArray<PointState> newStates;
        [WriteOnly] public NativeArray<float> newDistances;
        [WriteOnly] public NativeArray<float3> newSplinePoints;
        [WriteOnly] public NativeArray<int> indexMapping;
        
        public void Setup(NativeArray<float3> points, NativeArray<PointState> pointStates, NativeArray<float3> splinePoints, NativeArray<float> distances)
        {
            this.oldPoints = points;
            this.oldPointStates = pointStates;
            this.oldSplinePoints = splinePoints;
            this.oldDistances = distances;

            int validCount = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (pointStates[i] != PointState.Outside) validCount++;
            }
            
            newPoints =  new NativeArray<float3>(validCount, Allocator.Persistent);
            newStates =  new NativeArray<PointState>(validCount, Allocator.Persistent);
            newSplinePoints =  new NativeArray<float3>(validCount, Allocator.Persistent);
            newDistances =  new NativeArray<float>(validCount, Allocator.Persistent);
            
            indexMapping = new NativeArray<int>(points.Length, Allocator.Persistent);
        }
        
        public void Execute()
        {
            int newIndex = 0;
            for (int i = 0; i < oldPoints.Length; i++)
            {
                if (oldPointStates[i] != PointState.Outside)
                {
                    newPoints[newIndex] = oldPoints[i];
                    newStates[newIndex] = oldPointStates[i];
                    newSplinePoints[newIndex] = oldSplinePoints[i];
                    newDistances[newIndex] = oldDistances[i];
                    indexMapping[i] = newIndex;
                    
                    newIndex++;
                }
                else
                {
                    indexMapping[i] = -1; //Mark as removed
                }
            }

        }

        public void OnCompleted()
        {
            oldPoints.Dispose();
            oldPointStates.Dispose();
            oldSplinePoints.Dispose();
            oldDistances.Dispose();
        }
        
        public void Dispose()
        {
            newPoints.Dispose();
            newStates.Dispose();
            newDistances.Dispose();
            newSplinePoints.Dispose();
            indexMapping.Dispose();
        }
    }
}