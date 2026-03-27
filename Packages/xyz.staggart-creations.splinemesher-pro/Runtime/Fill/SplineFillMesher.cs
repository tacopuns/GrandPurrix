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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using PointState = sc.splinemesher.pro.runtime.Structs.PointState;

namespace sc.splinemesher.pro.runtime
{
    [ExecuteAlways]
    [SelectionBase]
    [AddComponentMenu("Splines/Spline Fill Mesher")]
    [Icon(SplineMesher.kPackageRoot + "/Editor/Resources/Components/spline-fill-mesher-icon-64px.psd")]
    public class SplineFillMesher : SplineMesher
    {
        public static readonly List<SplineFillMesher> Instances = new List<SplineFillMesher>();
        
        public FillMeshSettings settings = new FillMeshSettings();
        
        #if SM_DEV
        public bool gizmos;
        #endif
        
        //Vertex positions
        private NativeArray<float3> positions;
        //Keeps track of the state of each point (inside, edge or outside)
        private NativeArray<PointState> pointStates;
        //Distances from point to spline, for vertex colors
        private NativeArray<float> distances;
        //Points on spline nearest to each vertex
        private NativeArray<float3> splinePoints;
        //Holds the indices of the points
        private NativeArray<int> indexMapping;
        private NativeList<int> triangles;
        
        private NativeArray<RaycastHit> hits;
        
        private void OnEnable()
        {
            Instances.Add(this);
            SubscribeCallbacks();
            
            //Rebuild();
        }
        
        public void Reset()
        {
            root = this.transform;
            splineContainer = GetComponentInParent<SplineContainer>();
            settings.renderer.SetDefaults();
        }

        public override void Rebuild(int splineIndex = -1)
        {
            if (!splineContainer) return;
            
            base.Rebuild();
            
            TriggerPreRebuildEvent(this);
            
            stopWatch.Restart();

            if (splineIndex >= 0)
            {
                RebuildSpline(splineIndex);
            }
            else
            {
                for (int i = 0; i < splineCount; i++)
                {
                    RebuildSpline(i);
                }
            }

            stopWatch.Stop();
            
            TriggerPostRebuildEvent(this);
        }

        //Note: Spline knots need to be transformed to world space before checking the winding order
        private bool IsSplineClockwise(ISpline spline)
        {
            //Right vector of the first knot
            float3 outwardDirection = math.mul(spline[0].Rotation, math.right());
            //Direction to the center of the spline
            float3 dirToCenter = math.normalize((float3)splineContainer.transform.position - spline[0].Position);

            bool clockwise = math.dot(outwardDirection, dirToCenter) < 0;

            return clockwise;
        }
        
        private void RebuildSpline(int splineIndex)
        {
            NativeSpline spline = nativeSplines[splineIndex];
            
            //Invalid
            if(spline.Count < 2 || spline.GetLength() < 1f) return;
            
            //If the spline winding order is counter-clockwise, the right vector should be flipped
            bool clockwise = IsSplineClockwise(spline);
            
            Bounds bounds = spline.GetBounds(float4x4.identity); //Already in world-space now

            Vector3 offset = new Vector3(settings.topology.margin, 0f, settings.topology.margin);
            
            bounds.min -= offset;
            bounds.max += offset;
            bounds.SetMinMax(bounds.min, bounds.max);
            
            SplineMeshContainer container = Containers[splineIndex];
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            
            container.PrepareSegments(1);

            SplineMeshSegment segment = container.GetSegment(0);
            segment.transform.localPosition = Vector3.zero;
            segment.container = container;
            
            float m_triangleSize = Mathf.Max(settings.topology.triangleSize, 0.05f);
            
            SplineCache splinePointsJob = new SplineCache(spline, m_triangleSize, settings.topology.accuracy);

            var createMesh = !(settings.collision.enable && settings.collision.colliderOnly);
            
            if (createMesh)
            {
                segment.SetMaterial(settings.renderer.material);
                
                //Avoid conforming to the collider
                segment.SetColliderEnabled(false);
                segment.EnsureUniqueMesh();
                
                Mesh mesh = segment.mesh;
                GenerateMesh(spline, splinePointsJob.Points, clockwise, bounds, m_triangleSize, settings.output.keepReadable, ref mesh);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                mesh.name = this.name + " Spline Fill Mesh";
#endif
                segment.mesh = mesh;
                
                segment.SetMaterial(settings.renderer.material);
                segment.SetRendererParameters(settings.renderer.shadowCastingMode, settings.renderer.lightProbeUsage, settings.renderer.reflectionProbeUsage, settings.renderer.renderingLayerMask, settings.output.forceMeshLod, settings.output.lodSelectionBias);
            }
            else
            {
                segment.mesh = null;
            }
            segment.SetMeshCollider(settings.collision.enable);
            
            if (settings.collision.enable)
            {
                Mesh collisionMesh = segment.collisionMesh;
                GenerateMesh(spline, splinePointsJob.Points, clockwise, bounds, Mathf.Max(m_triangleSize, 0.05f), true, ref collisionMesh);
                
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                collisionMesh.name = this.name + " Collider";
#endif
                
                segment.SetColliderEnabled(true);
                segment.gameObject.layer = settings.collision.layer;
                //segment.SetColliderSettings(settings.collision.layer, false, false, false, false);
                segment.collisionMesh = collisionMesh;
            }
            
            splinePointsJob.Dispose();
        }

        public override void AssignMaterials(Material[] materials)
        {
            SetMaterial(materials[0]);
        }

        public void SetMaterial(Material target)
        {
            this.settings.renderer.material = target;
            foreach (var container in containers)
            {
                foreach (var segment in container.Segments)
                {
                    segment.SetMaterial(target);
                }
            }
        }
        
        struct Vertex
        {
            public float3 position;
            public float4 normal;
            public float4 tangent;
            public float4 color;
            public float4 uv0;
        }

        private void GenerateMesh(NativeSpline spline, NativeArray<Structs.SplinePoint> splineCachePoints, bool clockwise, Bounds bounds, float m_triangleSize, bool readable, ref Mesh mesh)
        {
            Profiler.BeginSample("Spline Fill Mesher: Vertices");
            
            float3 size = bounds.size;
            int2 gridSize = CreateVertices.GetGridSize(m_triangleSize, size.x, size.z);
            
            CreateVertices gridJob = new CreateVertices();
            gridJob.Setup(gridSize, bounds, spline, clockwise, splineCachePoints, this);

            JobHandle gridJobHandle = gridJob.Schedule(gridSize.x * gridSize.y, 128);
            gridJobHandle.Complete();
            
            //Grab resulting arrays
            positions = gridJob.GetPoints();
            pointStates = gridJob.GetPointStates();
            distances = gridJob.GetDistances();
            splinePoints = gridJob.GetNearestSplinePoints();
            
            int vertexCount = positions.Length;

            //Calculate maximum distance value, so a normalized value can be derived
            float averageHeight = 0;
            float maxDistance = float.MinValue;
            for (int j = 0; j < vertexCount; j++)
            {
                //Sum the height so that vertices can be positioned at the average (works better than the bounds center)
                averageHeight += splinePoints[j].y;
                maxDistance = math.max(maxDistance, distances[j]);
            }
            averageHeight /= vertexCount;
            
            Profiler.EndSample();
            
            Profiler.BeginSample("Spline Fill Mesher: Filtering");
            
            FilterPoints filterJob = new FilterPoints();
            filterJob.Setup(positions, pointStates, splinePoints, distances);

            JobHandle filterJobHandle = filterJob.Schedule(gridJobHandle);
            filterJobHandle.Complete();
            
            //Adopt filtered arrays
            positions = filterJob.newPoints;
            pointStates = filterJob.newStates;
            distances = filterJob.newDistances;
            splinePoints = filterJob.newSplinePoints;
            
            indexMapping = filterJob.indexMapping;
            
            filterJob.OnCompleted();
            
            Profiler.EndSample();

            vertexCount = positions.Length;

            if (settings.conforming.enable)
            {
                NativeArray<Structs.SplinePoint> splinePoints =
                    new NativeArray<Structs.SplinePoint>(positions.Length, Allocator.Temp);

                for (int i = 0; i < positions.Length; i++)
                {
                    splinePoints[i] = new Structs.SplinePoint(positions[i], default, math.up());
                }
                
                ConformRaycaster conformRaycaster = new ConformRaycaster();
                conformRaycaster.Raycast(splinePoints, settings.conforming.layerMask, settings.conforming.seekDistance, false);
                splinePoints.Dispose();
                
                hits = conformRaycaster.Hits;
            }
            else
            {
                hits = new NativeArray<RaycastHit>(0, Allocator.TempJob);
            }

            Profiler.BeginSample("Spline Fill Mesher: Displacement");
            
            Displacement displacementJob = new Displacement();
            displacementJob.Setup(this.settings.displacement, this.settings.conforming, root ? root.transform.worldToLocalMatrix : Matrix4x4.identity, 
                positions, pointStates, distances, splinePoints, hits, maxDistance, averageHeight);

            JobHandle displacementJobHandle = displacementJob.Schedule(vertexCount, 64);
            displacementJobHandle.Complete();
            
            Profiler.EndSample();
            
            Profiler.BeginSample("Spline Fill Mesher: Triangulation");
            
            //Triangles
            Triangulate triangulateJob = new Triangulate();
            triangulateJob.Setup(gridSize, m_triangleSize, positions, pointStates, indexMapping);

            JobHandle triangulateJobHandle = triangulateJob.Schedule(gridJobHandle);
            triangulateJobHandle.Complete();
            
            triangles = triangulateJob.GetTriangles();
            
            Profiler.EndSample();

            Profiler.BeginSample("Spline Fill Mesher: UVs");

            //UV coordinates
            GenerateUV uvJob = new GenerateUV();
            uvJob.Setup(positions, distances, size, settings.uv, settings.output.storeGradientsInUV);

            JobHandle uvJobHandle = uvJob.Schedule();
            uvJobHandle.Complete();

            NativeArray<float4> uv0 = uvJob.GetUV();
            
            Profiler.EndSample();
            
            Profiler.BeginSample("Spline Fill Mesher: Calculate Normals & Tangents");
            
            //Normals & tangents
            CalculateNormals normalsJob = new CalculateNormals();
            normalsJob.Setup(positions, uv0, triangles);

            JobHandle normalsJobHandle = normalsJob.Schedule();
            normalsJobHandle.Complete();

            NativeArray<float3> normals = normalsJob.GetNormals();
            NativeArray<float4> tangents = normalsJob.GetTangents();
            
            Profiler.EndSample();
            
            Profiler.BeginSample("Spline Fill Mesher: Create Mesh");
            {
                //Populate vertex data
                NativeArray<Vertex> vertices = new NativeArray<Vertex>(vertexCount, Allocator.Temp);

                half zero = (half)0;
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i] = new Vertex()
                    {
                        position = positions[i],
                        normal = new half4((half3)normals[i].xyz, zero),
                        tangent = (half4)tangents[i],
                        color = new half4((half)distances[i], zero, zero, zero),
                        uv0 = uv0[i],
                    };
                }
                
                CreateMesh(vertices, displacementJob.GetBounds(), readable, ref mesh);
                
                vertices.Dispose();
            }
            Profiler.EndSample();
            
            triangulateJob.Dispose();
            displacementJob.Dispose();
            normalsJob.Dispose();
            uvJob.Dispose();
                        
            normals.Dispose();
            tangents.Dispose();

            hits.Dispose();
            
            #if SM_DEV
            //TODO: Currently needed still for debugging the points
            if (gizmos == false)
            {
                filterJob.Dispose();
                gridJob.Dispose();
            }
            #endif
        }
        
        private readonly VertexAttributeDescriptor[] attributes = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
        };

        private void CreateMesh(NativeArray<Vertex> vertices, Bounds bounds, bool readable, ref Mesh mesh)
        {
            int vertexCount = vertices.Length;
            mesh.SetVertexBufferParams(vertexCount, attributes);

            var validation = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds;
            
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0, validation);
            
            var triangleValidation = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds;
            
            #if UNITY_EDITOR
            //Gizmo mesh drawing requires validated indices
            if(drawWireFrame) triangleValidation &= MeshUpdateFlags.DontValidateIndices;
            #endif
            
            int indexCount = triangles.Length;
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData(triangles.AsArray(), 0, 0, indexCount, triangleValidation);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new  SubMeshDescriptor(0, indexCount), triangleValidation);
            
            mesh.UploadMeshData(!readable);
            mesh.MarkModified();

            mesh.bounds = bounds;
        }

        public override void DrawComponentGizmos()
        {
            #if SM_DEV
            if (gizmos && positions.IsCreated && pointStates.IsCreated)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.matrix = Gizmos.matrix;

                float r = settings.topology.triangleSize * 0.25f / this.transform.lossyScale.x;
                for (int i = 0; i < positions.Length; i++)
                {
                    if (pointStates[i] == PointState.Outside) Gizmos.color = Color.red;
                    if (pointStates[i] == PointState.Inside) Gizmos.color = Color.green;
                    if (pointStates[i] == PointState.Edge) Gizmos.color = Color.yellow;

                    Gizmos.DrawSphere(positions[i], r);
                }
                for (int i = 0; i < positions.Length; i++)
                {
                    UnityEditor.Handles.zTest = CompareFunction.Always;
                    UnityEditor.Handles.color = Color.black;
                    UnityEditor.Handles.Label(positions[i] + math.up() * r * 4f, i.ToString(), UnityEditor.EditorStyles.boldLabel);
                }
#endif
            }
            #endif
        }

        private void OnDisable()
        {
            Instances.Remove(this);
            UnsubscribeCallbacks();
            
            Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
        
        public override void GetLightmapUVParameters(out float angleThreshold, out float packingMargin)
        {
            angleThreshold = settings.output.lightmapUVAngleThreshold;
            packingMargin = settings.output.lightmapUVMarginMultiplier;
        }

        public override int GetLODCount()
        {
            return settings.output.maxLodCount;
        }
    }
}