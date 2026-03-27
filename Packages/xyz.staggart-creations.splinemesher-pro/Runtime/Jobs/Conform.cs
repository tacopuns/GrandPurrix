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

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#if !UNITY_2022_3_OR_NEWER
//#error "Asset imported into a version older than 2022.3, which is not compatible"
#endif

namespace sc.splinemesher.pro.runtime
{
    public struct ConformRaycaster
    {
        private NativeArray<RaycastCommand> raycastCommands;
        private NativeArray<RaycastHit> raycastHits;
        
        public NativeArray<RaycastHit> Hits => raycastHits;

        private Vector3 origin, direction;
        
        public void Raycast(NativeArray<Structs.SplinePoint> splinePoints, int layerMask, float seekDistance, bool useSplineDirection)
        {
            int sampleCount = splinePoints.Length;
            
            //Debug.Log($"Raycasting {sampleCount} samples");
            
            raycastCommands = new NativeArray<RaycastCommand>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            raycastHits = new NativeArray<RaycastHit>(sampleCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

#if UNITY_2022_3_OR_NEWER
            QueryParameters queryParams = new QueryParameters
            {
                hitBackfaces = true,
                layerMask = layerMask,
                hitTriggers = QueryTriggerInteraction.Ignore,
            };
            
            PhysicsScene physicsScene = Physics.defaultPhysicsScene;

            //Working with a loop of 2 arrays with the length of `sampleCount`. So use unsafe pointers to avoid get/set overhead
            unsafe
            {
                RaycastCommand* commandsPtr = (RaycastCommand*)raycastCommands.GetUnsafePtr();
                Structs.SplinePoint* ptr = (Structs.SplinePoint*)splinePoints.GetUnsafeReadOnlyPtr();
                
                for (var i = 0; i < sampleCount; ++i)
                {
                    origin = ptr[i].position;
                    direction = useSplineDirection ? -ptr[i].up : -Vector3.up;
                    
                    float maxDepth = seekDistance * 2f;

                    Vector3 rayDir = direction;
                    Vector3 rayPos = origin - (rayDir * seekDistance);
                    
                    commandsPtr[i] = new RaycastCommand(physicsScene, rayPos, rayDir.normalized, queryParams, maxDepth);
                }
            }

            JobHandle raycastJobHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 16, 1, default(JobHandle));
            raycastJobHandle.Complete();
#endif
            
            raycastCommands.Dispose();
        }

        public void Dispose()
        {
            raycastHits.Dispose();
        }
    }
}