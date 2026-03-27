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

namespace sc.splinemesher.pro.runtime
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, CompileSynchronously = true)]
    public struct CalculateNormals : IJob, IDisposable
    {
        private int vertexCount;
        [ReadOnly] private NativeArray<float3> vertices;
        [ReadOnly] private NativeList<int> triangles;
        [ReadOnly] private NativeArray<float4> uvs;
        
        private NativeArray<float3> normals;
        [WriteOnly] private NativeArray<float4> tangents;

        private NativeArray<float3> tan1, tan2;
        
        public void Setup(NativeArray<float3> vertices, NativeArray<float4> uvs, NativeList<int> triangles)
        {
            this.vertices = vertices;
            this.uvs = uvs;
            this.triangles = triangles;

            vertexCount = vertices.Length;

            normals = new NativeArray<float3>(vertexCount, Allocator.Persistent);
            tangents = new NativeArray<float4>(vertexCount, Allocator.Persistent);
            
            //Temporary arrays for tangent calculation
            tan1 = new NativeArray<float3>(vertexCount, Allocator.TempJob);
            tan2 = new NativeArray<float3>(vertexCount, Allocator.TempJob);
        }
        
        public void Execute()
        {
            //Initialize normals to zero
            float3 up = math.up();
            for (int i = 0; i < vertexCount; i++)
            {
                normals[i] = up;
                tangents[i] = new float4(0, 0, 0, 1);
            }

            //Calculate face normals and tangents
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                float3 v0 = vertices[i0];
                float3 v1 = vertices[i1];
                float3 v2 = vertices[i2];
                
                float3 edge1 = v1 - v0;
                float3 edge2 = v2 - v0;
                float3 faceNormal = math.cross(edge1, edge2);

                //Accumulate face normal to each vertex
                normals[i0] += faceNormal;
                normals[i1] += faceNormal;
                normals[i2] += faceNormal;

                //Calculate tangent using UV coordinates. UV may be flipped or rotated, which affects the tangent's sign
                float2 uv0 = uvs[i0].xy;
                float2 uv1 = uvs[i1].xy;
                float2 uv2 = uvs[i2].xy;

                float2 deltaUV1 = uv1 - uv0;
                float2 deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x + 0.0001f);
                float3 tangent = (edge1 * deltaUV2.y - edge2 * deltaUV1.y) * r;
                float3 bitangent = (edge2 * deltaUV1.x - edge1 * deltaUV2.x) * r;

                tan1[i0] += tangent;
                tan1[i1] += tangent;
                tan1[i2] += tangent;

                tan2[i0] += bitangent;
                tan2[i1] += bitangent;
                tan2[i2] += bitangent;
            }

            //Normalize normals and calculate final tangents
            for (int i = 0; i < vertices.Length; i++)
            {
                float3 n = normals[i];
                float3 t = tan1[i];

                //Normalize normal
                if (math.lengthsq(n) > 0.0001f)
                {
                    normals[i] = math.normalize(n);
                }
                else
                {
                    //Fallback
                    normals[i] = up;
                }

                float3 normal = normals[i];
                //Gram-Schmidt orthogonalize tangent
                float3 tangent = math.normalize(t - normal * math.dot(normal, t));

                //Calculate handedness w-component
                float w = (math.dot(math.cross(normal, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;

                if (math.lengthsq(tangent) > 0.0001f)
                {
                    tangents[i] = new float4(tangent.x, tangent.y, tangent.z, w);
                }
                else
                {
                    tangents[i] = new float4(1, 0, 0, 1);
                }
            }
        }
        
        public NativeArray<float3> GetNormals()
        {
            return normals;
        }

        public NativeArray<float4> GetTangents()
        {
            return tangents;
        }

        public void Dispose()
        {
            tan1.Dispose();
            tan2.Dispose();
        }
    }
}