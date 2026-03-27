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
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace sc.splinemesher.pro.runtime
{
    public static class Utilities
    {
        public const float MIN_T_VALUE = 0.00001f;
        public const float MAX_T_VALUE = 0.99999f;
        
        public static quaternion LockRotationAngle(quaternion neutralRotation, quaternion targetRotation, bool3 angles)
        {
            math.RotationOrder rotationOrder = math.RotationOrder.ZXY;
            
            float3 prevEuler = math.Euler(neutralRotation, rotationOrder);
            float3 newEuler = math.Euler(targetRotation, rotationOrder);
                
            //Note: Angles are in radians
            
            if (angles.x)
            {
                newEuler.x = prevEuler.x;
            }
            if (angles.y)
            {
                newEuler.y = prevEuler.y;
            }
            if (angles.z)
            {
                newEuler.z = prevEuler.z;
            }
                
            quaternion newRotation = quaternion.Euler(newEuler, rotationOrder);

            return newRotation;
        }
        
        [BurstCompile]
        public static float EaseInOut(float t)
        {
            float eased = 2f * t * t;
            if (t > 0.5f) eased = 4f * t - eased - 1f;
                            
            return eased;
        }
        
        [BurstCompile]
        public static float CalculateDistanceWeight(float position, float surfaceLength, float startDistance, float startFalloff, float endDistance, float endFalloff, bool invert, bool easeInOut = false)
        {
            float start = math.saturate(((startDistance) - (position - (startDistance + startFalloff))) / (math.max(startFalloff, 0.00001f)));
            float end = math.saturate(((surfaceLength - endDistance) - (position + endDistance)) / (math.max(endFalloff, 0.00001f)));

            //Patch when falloff is 0
            //if(endFalloff == 0f && (position - surfaceLength) <= 0f) end = 1f;
            
            float gradient = math.max(start, 1f- end);

            if (easeInOut)
            {
                gradient = EaseInOut(gradient);
            }
            
            if(invert) gradient = 1f-gradient;
            
            return gradient;
        }
        
        [BurstCompile]
        public static float EdgeDistanceMask(float position, float maxWidth, float distance, float falloff, bool invert = false)
        {
            falloff = math.max(falloff, 0.00001f);
            
            float start = math.saturate(((distance + falloff) - (position - distance)) / falloff);
            float end = math.saturate(((maxWidth - distance) - (position + distance)) / falloff);

            float gradient = math.max(start, 1f- end);
            
            if(invert) gradient = 1f-gradient;
            
            return gradient;
        }
        
        //In megabytes
        public static float GetMemorySize(Mesh mesh)
        {
            float size = 0;
            
            if (mesh)
            {
                int vertexCount = mesh.vertexCount;
                
                //Get actual byte size per vertex
                for (int stream = 0; stream < mesh.vertexBufferCount; stream++)
                {
                    int stride = mesh.GetVertexBufferStride(stream);
                    size += vertexCount * stride;
                }
                
                //Index data
                long indexCount = 0;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    indexCount += mesh.GetIndexCount(submesh);

                bool use32Bit = mesh.indexFormat == IndexFormat.UInt32;
                size += indexCount * (use32Bit ? sizeof(uint) : sizeof(ushort));
                
                return size / (1024f * 1024f);
            }

            return size;
        }
        
        public static string FormatMemorySize(float size)
        {
            //Convert to bytes
            float sizeInBytes = size * 1024 * 1024;

            if (sizeInBytes < 1024f)
            {
                return sizeInBytes.ToString("F0") + " bytes";
            }
            else if (sizeInBytes < 1024f * 1024f)
            {
                float sizeInKB = sizeInBytes / 1024f;
                return sizeInKB.ToString("F2") + " KB";
            }
            else
            {
                return size.ToString("F2") + " MB";
            }
        }
    }
}