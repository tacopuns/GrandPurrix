using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace sc.splinemesher.pro.runtime
{
    public partial struct CurveToMesh
    {
        //Output vertex layout
        //Note: The order is important and what Unity excepts
        //16-bit precision is used for positions and UVs
        //8-bit precision is used for normals, tangents and vertex colors. As these values never exceed -1/+1
        public struct Vertex
        {
            public float3 position;
            public float4 normal;
            public float4 tangent;
            public float4 color;
            public float4 uv0;
            
            //Creates a new vertex in the packed format
            public Vertex(float3 inputPosition, float3 inputNormal, float4 inputTangent, float4 inputColor, float4 inputUv0)
            {
                //position = new half4((half3)inputPosition.xyz, (half)0);
                //normal = new Structs.sbyte4((sbyte)(inputNormal.x * 127), (sbyte)(inputNormal.y * 127), (sbyte)(inputNormal.z * 127), 0);
                //tangent = new Structs.sbyte4((sbyte)(inputTangent.x * 127), (sbyte)(inputTangent.y * 127), (sbyte)(inputTangent.z * 127), (sbyte)(inputTangent.w * 127));
                //color = new Structs.sbyte4((sbyte)(inputColor.x * 127), (sbyte)(inputColor.y * 127), (sbyte)(inputColor.z * 127), (sbyte)(inputColor.w * 127));
                position = new float3(inputPosition.xyz);
                normal = new half4((half3)inputNormal, (half)0);
                tangent = new half4(inputTangent);
                color = new half4(inputColor);
                uv0 = (half4)inputUv0;
            }
        }
        
        //Note, order is important, as it is what Unity expects
        private static readonly VertexAttributeDescriptor[] attributes = new VertexAttributeDescriptor[]
        {
            //Note: Position cannot have a dimension higher than 3, will crash the lightmap UV generation!
            //32-bit precision needs to be used for the same reason. A workaround needs to be found
            
            new (VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new (VertexAttribute.Normal, VertexAttributeFormat.Float32, 4),
            new (VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
            new (VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new (VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
        };
        
        public bool HasInvalidBounds()
        {
            return float.IsNaN(boundsMinMax[0].x) || boundsMinMax[0].x >= float.PositiveInfinity;
        }
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static readonly string kMeshName = "SplineMesh";
        public static readonly string kColliderSuffix = "Collider";
#endif
        
        public Mesh CreateMesh(ref Mesh mesh, int index, bool readable, bool validateIndices = false)
        {
            /*
            //Old slower API
            mesh.indexFormat = vertexCount >= 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices, 0, vertexCount);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.SetNormals(normals, 0, vertexCount);
            mesh.SetTangents(tangents, 0, vertexCount);
            mesh.SetUVs(0, uv0);

            return mesh;
            */

            mesh.SetVertexBufferParams(vertexCount, attributes);

            var noValidation = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds;

            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0, noValidation);
            
            //Triangles
            var triangleValidation = noValidation;
            
            #if UNITY_EDITOR
            //Gizmo mesh drawing requires validated indices
            if(validateIndices) triangleValidation &= MeshUpdateFlags.DontValidateIndices;
            #endif
            
            //Set proper index format based on number of vertices added.
            //Meshes with a higher vertex count that this will need to suffer a performance hit, as the conversion to an int format is needed.
            //TODO investigate if using direct pointers avoids the overhead of accessing native array items in managed code
            if (vertexCount > 65535)
            {
                int indexCount = indices.Length;
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);

                NativeArray<int> indices32 = new NativeArray<int>(indexCount, Allocator.Temp);
                for (int i = 0; i < indexCount; i++)
                {
                    indices32[i] = (int)indices[i];
                }
                mesh.SetIndexBufferData(indices32, 0, 0, indexCount, triangleValidation);
                indices32.Dispose();
            }
            else
            {
                int indexCount = indices.Length;
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
                mesh.SetIndexBufferData(indices, 0, 0, indexCount, triangleValidation);
            }
            
            //Debug.Log($"Creating mesh segment #{index}: Vertices:{vertexCount}. Tris:{triangleCount}. Submeshes:{submeshCount}. Format:{mesh.indexFormat}: Tiles:{tileCount}");
            
            mesh.subMeshCount = submeshCount;
            int currentIndexOffset = 0;
            for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
            {
                //Start and end indices for this submesh
                int start = sourceSubmeshRanges[submeshIndex].x;
                int count = sourceSubmeshRanges[submeshIndex].y;
                
                int sourceIndexCount = sourceSubmeshRanges[submeshIndex].y;
                int totalIndexCount = sourceIndexCount * tileCount;
        
                SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor
                {
                    indexStart = currentIndexOffset,
                    indexCount = totalIndexCount,
                    topology = MeshTopology.Triangles,
                    firstVertex = 0,
                    baseVertex = 0,
                    vertexCount = vertexCount
                };
                
                //if(submeshCount > 1) Debug.Log($"[SplineToMesh] Submesh #{submeshIndex}. Start:{currentIndexOffset}. End:{totalIndexCount}. Vertices:{subMeshDescriptor.vertexCount}. Max triangle index: {indices[^1]}.");
                
                mesh.SetSubMesh(submeshIndex, subMeshDescriptor, noValidation);
                
                currentIndexOffset += totalIndexCount;
            }
            
            Bounds bounds = mesh.bounds;
            bounds.SetMinMax(boundsMinMax[0], boundsMinMax[1]);
            mesh.bounds = bounds;
            
            if(readable) mesh.UploadMeshData(false);
            
            return mesh;
        }

        public Mesh CreateCollider(ref Mesh mesh, string name = "SplineMesh")
        {
            if (colliderVertexCount == 0)
            {
                //throw new Exception("Could not create a collider mesh with 0 vertices...");
            }
            
            mesh.SetVertexBufferParams(colliderVertexCount, attributes);

            var noValidation = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds;
            var validation = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
            
            mesh.SetVertexBufferData(colliderVertices, 0, 0, colliderVertexCount, 0, validation);
            
            //Triangles
            var triangleValidation = MeshUpdateFlags.DontRecalculateBounds;

            int indexCount = colliderIndices.Length;
            //Set proper index format based on number of vertices added.
            //Meshes with a higher vertex count that this will need to suffer a performance hit, as the conversion to an int format is needed.
            if (colliderVertexCount > 65535)
            {
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);

                NativeArray<int> indices32 = new NativeArray<int>(indexCount, Allocator.Temp);
                for (int i = 0; i < indexCount; i++)
                {
                    indices32[i] = (int)colliderIndices[i];
                }
                mesh.SetIndexBufferData(indices32, 0, 0, indexCount, triangleValidation);
                indices32.Dispose();
            }
            else
            {
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
                mesh.SetIndexBufferData(colliderIndices, 0, 0, indexCount, triangleValidation);
            }
            
            //Debug.Log($"Creating spline mesh: Vertices:{vertexCount}. Tris:{triangleCount}. Submeshes:{submeshCount}. Format:{mesh.indexFormat}");
            
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount), noValidation);

            Bounds bounds = mesh.bounds;
            bounds.SetMinMax(boundsMinMax[0], boundsMinMax[1]);
            mesh.bounds = bounds;

            //Collider needs to be kept readable, so a MeshCollider can post-process in a build
            mesh.UploadMeshData(false);

            return mesh;
        }
    }
}