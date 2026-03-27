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
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace sc.splinemesher.pro.runtime
{
    [BurstCompile]
    public struct ProcessInput : IJob
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Structs.InputMeshData> meshData;

        private float3 rotation;
        private float3 scale;
        private float3 alignmentOffset;

        [ReadOnly]
        private Mesh.MeshDataArray dataArray;

        public Structs.InputMeshData MeshData => meshData[0];
        public Structs.BoundsData Bounds => MeshData.bounds;
        
        private bool3 invertAxis;
        private Structs.Alignment alignment;
        
        public bool isCreated => meshData.IsCreated;
        
        public void Setup(CurveMeshSettings.InputMesh settings, float3 minSize, float3 offset)
        {
            Mesh mesh = settings.mesh;
            if (settings.shape == CurveMeshSettings.Shape.Cube)
            {
                mesh = ProceduralMesh.Cube.Create(
                    Mathf.Max(minSize.x, settings.cube.scale.x), 
                    Mathf.Max(minSize.y, settings.cube.scale.y), 
                    Mathf.Max(minSize.z, settings.cube.scale.z),
                    settings.cube.edgeLoopDistance,
                    settings.cube.caps,
                    settings.uvTiling,
                    offset,
                    settings.alignment);
            }
            else if (settings.shape == CurveMeshSettings.Shape.Cylinder)
            {
                float r = Mathf.Max(Mathf.Max(minSize.x, minSize.y) * 0.5f, settings.cylinder.outerRadius);
                mesh = ProceduralMesh.Cylinder.Create(r, settings.cylinder.hollow, settings.cylinder.innerRadius, settings.cylinder.caps, settings.cylinder.radialSegments,
                    Mathf.Max(0.1f, minSize.z), settings.cylinder.edgeLoopDistance, settings.uvTiling, offset, settings.alignment);
            }
            else if (settings.shape == CurveMeshSettings.Shape.Plane)
            {
                mesh = ProceduralMesh.Plane.Create(settings, 
                    Mathf.Max(settings.plane.width, minSize.x), settings.plane.widthEdgeDistance, settings.plane.lengthEdgeDistance, 
                    settings.plane.stretchUV, settings.plane.curve, settings.plane.curveScale, minSize, settings.alignment);
            }

            if (settings.shape != CurveMeshSettings.Shape.Custom)
            {
                mesh.UploadMeshData(false);
            }
            
            if (mesh == null)
            {
                //Use dummy mesh. Important so that arrays are still allocated and populated
                mesh = new Mesh();
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            //Using ushort for vertex indices as a performance improvement, so max is 65.535. This is unlikely to ever be exceeded.
            if (mesh.vertexCount >= 65535)
            {
                throw new Exception("[Spline Curve Mesher] Input mesh cannot have more than 65.535 vertices.");
            }
            #endif
            
            dataArray = Mesh.AcquireReadOnlyMeshData(mesh);

            Structs.InputMeshData inputData = new Structs.InputMeshData();
            Mesh.MeshData m_meshData = dataArray[0];
            PopulateMeshData(ref inputData, ref m_meshData);

            this.rotation = settings.rotation;
            this.scale = settings.scale;
            if (settings.shape == CurveMeshSettings.Shape.Custom)
            {
                //this.alignmentOffset = ProceduralMesh.CalculateOffset(settings.alignment, mesh.bounds);
            }

            invertAxis.x = settings.scale.x < 0;
            invertAxis.y = settings.scale.y < 0;
            invertAxis.z = settings.scale.z < 0;
            
            this.alignment = settings.alignment;
            
            meshData = new NativeArray<Structs.InputMeshData>(1, Allocator.Persistent);
            meshData[0] = inputData;
        }

        private void PopulateMeshData(ref Structs.InputMeshData inputData, ref Mesh.MeshData meshData)
        {
            //Note: Not using a Vertex struct for the input mesh, since it's format can vary (ie. multiple UV channels)
            inputData.vertexCount = (ushort)meshData.vertexCount;
            
            //Note: Allocations are persistent because the data will only change if the input mesh is changed.
            
            inputData.positions = new NativeArray<float3>(inputData.vertexCount, Allocator.Persistent);
            inputData.normals = new NativeArray<float3>(inputData.vertexCount, Allocator.Persistent);
            inputData.tangents = new NativeArray<float4>(inputData.vertexCount, Allocator.Persistent);
            inputData.uv = new NativeArray<float2>(inputData.vertexCount, Allocator.Persistent);
            inputData.colors = new NativeArray<float4>(inputData.vertexCount, Allocator.Persistent);
            
            if(meshData.HasVertexAttribute(VertexAttribute.Position)) meshData.GetVertices(inputData.positions.Reinterpret<Vector3>());
            if(meshData.HasVertexAttribute(VertexAttribute.Normal)) meshData.GetNormals(inputData.normals.Reinterpret<Vector3>());
            if(meshData.HasVertexAttribute(VertexAttribute.Tangent)) meshData.GetTangents(inputData.tangents.Reinterpret<Vector4>());
            if(meshData.HasVertexAttribute(VertexAttribute.TexCoord0)) meshData.GetUVs(0, inputData.uv.Reinterpret<Vector2>());
            if(meshData.HasVertexAttribute(VertexAttribute.Color)) meshData.GetColors(inputData.colors.Reinterpret<Color>());

            //Copy to writable array to store reversed triangles
            var sourceIndexData = meshData.GetIndexData<ushort>();
            inputData.sourceTriangles = new NativeArray<ushort>(sourceIndexData.Length, Allocator.Persistent);
            NativeArray<ushort>.Copy(sourceIndexData, inputData.sourceTriangles);
            
            inputData.submeshCount = meshData.subMeshCount;

            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);
            
            //Store the start/end indices of each submesh
            inputData.sourceSubmeshRanges = new NativeArray<int2>(inputData.submeshCount, Allocator.Persistent);
            for (int submeshIndex = 0; submeshIndex < inputData.submeshCount; submeshIndex++)
            {
                SubMeshDescriptor subMeshDescriptor = meshData.GetSubMesh(submeshIndex);
                inputData.sourceSubmeshRanges[submeshIndex] = new int2(subMeshDescriptor.indexStart, subMeshDescriptor.indexCount);

                //Calculate total bounds
                min = math.min(min, subMeshDescriptor.bounds.min);
                max = math.min(max, subMeshDescriptor.bounds.max);
                
                //int triCount = inputData.sourceSubmeshRanges[submeshIndex].y - inputData.sourceSubmeshRanges[submeshIndex].x;
                //if(inputData.submeshCount > 1) Debug.Log($"[Input] Triangles for submesh #{submeshIndex}. Start:{inputData.sourceSubmeshRanges[submeshIndex].x} End:{inputData.sourceSubmeshRanges[submeshIndex].y}. Total:{triCount}");
            }
            
            inputData.bounds = new Structs.BoundsData()
            {
                size = (min - max) * 0.5f,
                min = min,
                max = max,
            };
        }
        
        //Rotate the mesh, etc
        public void Execute()
        {
            var rotationAmount = math.abs(math.length(rotation));
            var scaleAmount = math.abs(math.length(scale));

            Structs.InputMeshData inputData = meshData[0];
            
            float3 boundsMin = new float3(float.MaxValue);
            float3 boundsMax = new float3(float.MinValue);
            
            bool flip = math.any(invertAxis);
            //Operations performed
            if (rotationAmount > 0f || (math.abs(scaleAmount - 1f) > math.EPSILON) || flip)
            {
                for (int i = 0; i < inputData.vertexCount; i++)
                {
                    float3 position = inputData.positions[i];
                    float3 normal = inputData.normals[i];
                    float4 sourceTangent = new float4(inputData.tangents[i]);
                    
                    quaternion meshRotation = quaternion.EulerXYZ(rotation * Mathf.Deg2Rad);
                    position = math.mul(meshRotation, position);
                    normal = math.mul(meshRotation, normal);
                    
                    position *= scale;

                    if (flip)
                    {
                        //Rotate normals
                        float oneEighty = 180f * Mathf.Deg2Rad;
                        
                        //Note: swizzling is intentional
                        quaternion normalRotation = quaternion.EulerXYZ(invertAxis.y ? oneEighty : 0f, invertAxis.x ? oneEighty : 0f, 0f);
                        normal = math.rotate(normalRotation, normal);
                        
                        sourceTangent.w *= -1;
                    }

                    inputData.positions[i] = position;
                    inputData.normals[i] = normal;
                    inputData.tangents[i] = new float4(math.mul(meshRotation, sourceTangent.xyz).xyz, sourceTangent.w);;

                    //Recalculate bounds
                    boundsMin = math.min(position, boundsMin);
                    boundsMax = math.max(position, boundsMax);
                }

                //Reverse triangle order if negatively scaled
                if (flip)
                {
                    var triangleCount = inputData.sourceTriangles.Length / 3;
                    for (int j = 0; j < triangleCount; j++)
                    {
                        (inputData.sourceTriangles[j * 3], inputData.sourceTriangles[j * 3 + 1]) = (
                            inputData.sourceTriangles[j * 3 + 1], inputData.sourceTriangles[j * 3]);
                    }
                }

                inputData.bounds.min = boundsMin;
                inputData.bounds.max = boundsMax;
                inputData.bounds.size = (boundsMax - boundsMin);
            }
            
            this.alignmentOffset = ProceduralMesh.CalculateOffset(alignment, inputData.bounds.min, inputData.bounds.max);
            
            if (math.length(this.alignmentOffset) > 0)
            {
                for (int i = 0; i < inputData.vertexCount; i++)
                {
                    float3 position = inputData.positions[i];
                    
                    position += alignmentOffset;
                    
                    inputData.positions[i] = position;
                }
                
                //Translate bounds. Size remains the same!
                inputData.bounds.min += alignmentOffset;
                inputData.bounds.max += alignmentOffset;
            }
            

            meshData[0] = inputData;
        }
        
        public void Dispose()
        {
            if (meshData.IsCreated)
            {
                meshData[0].Dispose();
                meshData.Dispose();
                dataArray.Dispose();
            }
        }
    }
}