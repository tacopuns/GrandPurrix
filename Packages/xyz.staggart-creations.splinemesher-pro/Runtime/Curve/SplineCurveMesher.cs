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
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Splines;

namespace sc.splinemesher.pro.runtime
{
    [ExecuteAlways]
    [SelectionBase]
    [AddComponentMenu("Splines/Spline Curve Mesher")]
    [Icon(SplineMesher.kPackageRoot + "/Editor/Resources/Components/spline-curve-mesher-icon-64px.psd")]
    public partial class SplineCurveMesher : SplineMesher, IDisposable
    {
        private const float SPLINE_CACHE_SAMPLE_DISTANCE = 0.1f;
		//Splines shorter than this are considered invalid
        private const float MIN_SPLINE_LENGTH = 0.01f;
		//Prevent processing tiny meshes, as this can result in thousands of tiles
        private const float MIN_MESH_LENGTH = 0.02f;
        
        public static readonly List<SplineCurveMesher> Instances = new List<SplineCurveMesher>();
        
        /// <summary>
        /// Settings instance, see various sub-classes
        /// </summary>
        public CurveMeshSettings settings = new CurveMeshSettings();
        [NonSerialized]
        public bool drawSegments = false;
        
        public void SetInputMesh(Mesh mesh)
        {
            settings.input.shape = CurveMeshSettings.Shape.Custom;
            settings.input.mesh = mesh;
        }

        public override void AssignMaterials(Material[] materials)
        {
            this.settings.renderer.materials = materials;
            foreach (var container in containers)
            {
                foreach (var segment in container.Segments)
                {
                    segment.SetMaterials(materials);
                }
            }
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

        private ProcessInput inputMeshProcessJob = new ProcessInput();
        private ProcessInput inputColliderProcessJob = new ProcessInput();
        private JobHandle inputProcessHandle;
        private ConformRaycaster raycaster;
        private NativeArray<RaycastHit> hits;
        
        private NativeArray<Structs.SplinePoint> splinePoints;
        Structs.InputMeshData meshData;
        Structs.InputMeshData colliderMeshData;
        
        private CurveToMesh[] curveToMeshJobs = Array.Empty<CurveToMesh>();

        private void RebuildSpline(int splineIndex)
        {
            NativeSpline nativeSpline = nativeSplines[splineIndex];
            float splineLength = nativeSpline.GetLength();
            
            //Invalid spline
            if (splineLength < MIN_SPLINE_LENGTH || nativeSpline.Count < 2) return;
            
            Profiler.BeginSample($"Spline Curve Mesher: Spline ({splineIndex}) Point Caching");
            
            SplineCache splinePointsJob = new SplineCache(nativeSpline, SPLINE_CACHE_SAMPLE_DISTANCE);
            splinePoints = splinePointsJob.Points;
            
            Profiler.EndSample();
            
            SplineMeshContainer container = Containers[splineIndex];
            container.splineIndex = splineIndex;
            container.owner = this;
            
            SetCapColliderStates(false, false, out var startCapDisabled, out var endCapDisabled);
            
            //Function has a profiler marker inside
            hits = GetRaycasts(splinePoints, container);

            Profiler.BeginSample("Spline Curve Mesher: Input Mesh Processing");
            
            inputMeshProcessJob.Dispose();
            
            var createMesh = !(settings.collision.enable && settings.collision.colliderOnly);
            if (settings.input.shape == CurveMeshSettings.Shape.Custom && !settings.input.mesh) createMesh = false;
            if (createMesh)
            {
                ProcessInput(settings.input, new float3(MIN_MESH_LENGTH), new float3(0), ref inputMeshProcessJob);
                meshData = inputMeshProcessJob.MeshData;
            }
            else
            {
                meshData = default;
            }

            float meshLength = meshData.bounds.size.z;
            if (createMesh)
            {
#if SM_DEV
                if (meshLength < MIN_MESH_LENGTH)
                    Debug.LogError($"Spline #{splineIndex} in {splineContainer.name} has a mesh with a near zero size ({meshLength}).",
                        this);
#endif

                if (meshLength > splineLength)
                {
                    //Debug.LogWarning($"Failed to generate mesh for spline #{splineIndex} in {splineContainer.name}. The mesh is too large ({meshLength}) for the spline length ({splineLength}).", this);
                }
            }
            
            bool meshCreated = createMesh && meshLength >= MIN_MESH_LENGTH && meshLength < splineLength;
            
            var createCollider = settings.collision.enable;

            inputColliderProcessJob.Dispose();
            if (createCollider)
            {
                //Need to use this value as the minimum size for the collider mesh (if it is a procedural shape)
                float3 meshSize = meshCreated ? meshData.bounds.size : 1f;
                float3 meshCenter = meshCreated ? meshData.bounds.CalculatedCenter : float3.zero;
                
                if (settings.collision.inputMesh.shape == CurveMeshSettings.Shape.Custom)
                {
                    //Assign render mesh as input if missing
                    if (!settings.collision.inputMesh.mesh && createMesh)
                    {
                        settings.collision.inputMesh.mesh = settings.input.mesh;
                    }
                    
                    //Normally the collider is based of the render mesh size. But if missing, adopt the bounds of the custom collider
                    if (settings.collision.colliderOnly && !createMesh)
                    {
                        Bounds bounds = settings.collision.inputMesh.mesh.bounds;
                        
                        meshSize = bounds.size;
                        meshCenter = bounds.center;
                    }

                    settings.collision.inputMesh.scale = settings.input.scale;
                    settings.collision.inputMesh.alignment = settings.input.alignment;
                    settings.collision.inputMesh.rotation = settings.input.rotation;
                    settings.collision.inputMesh.alignment = settings.input.alignment;
                }
                else
                {
                    //Avoid double scaling
                    settings.collision.inputMesh.scale = Vector3.one;
                    settings.collision.inputMesh.rotation = Vector3.zero;
                    settings.collision.inputMesh.alignment = Structs.Alignment.PivotPoint;
                }
                

                ProcessInput(settings.collision.inputMesh, meshSize, meshCenter, ref inputColliderProcessJob);
                colliderMeshData = inputColliderProcessJob.MeshData;

                if (!createMesh)
                {
                    meshData = colliderMeshData;
                }
                
                //Debug.Log($"Created collider input ({settings.collision.inputMesh.shape}) with min size: {meshSize}. Pivot: {meshCenter}. {colliderMeshData.IsCreated}");
            }
            else
            {
                colliderMeshData = default;
            }
            
            Profiler.EndSample();
            
            Profiler.BeginSample("Spline Curve Mesher: Spline Data Conversion");
            
            SplineCurveMeshData splineMeshData = new SplineCurveMeshData();
            splineMeshData.Setup(nativeSpline, GetScaleData(splineIndex),
                GetRollData(splineIndex),
                GetColorData(splineIndex, 0),
                GetColorData(splineIndex, 1),
                GetColorData(splineIndex, 2),
                GetColorData(splineIndex, 3),
                GetConformingData(splineIndex)
            );
            
            //Debug.Log($"Generating mesh with data: {splineMeshData.dataTypes}", this);
            
            Profiler.EndSample();
            
            //Debug.Log($"GenerateGeometry with meshData: {meshData.bounds.size}", this);

            GenerateGeometry(nativeSpline, splineIndex, splineMeshData, meshData, createMesh && meshCreated, colliderMeshData, createCollider && colliderMeshData.IsCreated);

            UpdateCaps(nativeSpline, splineIndex, splineMeshData, settings);
            SetCapColliderStates(startCapDisabled, endCapDisabled, out var _, out var _);
            
            foreach (var segment in container.Segments)
            {
                segment.SetColliderEnabled(createCollider);
                segment.container = container;
                
                if (createCollider)
                {
                    segment.SetColliderSettings(settings.collision.layer, settings.collision.isKinematic, settings.collision.convex, 
                        settings.collision.isTrigger, settings.collision.provideContacts);
                }
            }
            
            inputMeshProcessJob.Dispose();
            inputColliderProcessJob.Dispose();

            splinePointsJob.Dispose();
            hits.Dispose();
        }
        
        private int CalculateTileCount(float splineLength, float meshLength, bool scaleToFit, bool closed)
        {
            int segmentCount = settings.distribution.tiles;

            if (settings.distribution.autoTileCount)
            {
                //Seems to need one extra segment to full close the loop
                //if (closed) splineLength += 0.001f;
                
                if (scaleToFit)
                {
                    return (int)math.ceil((splineLength / meshLength));
                }
                
                if (settings.distribution.evenOnly)
                {
                    return (int)math.floor((splineLength / meshLength));
                }
                else
                {
                    return (int)math.ceil((splineLength / meshLength));
                }
            }

            return segmentCount;
        }
        
        /// <summary>
        /// Convert the input mesh (or creates a procedural one first) and converts it to a mesh data structure that can be used in Jobs.
        /// If any transforms are needed, these are applied as well
        /// </summary>
        /// <param name="input"></param>
        /// <param name="minSize"></param>
        /// <param name="offset"></param>
        /// <param name="job"></param>
        /// <exception cref="Exception"></exception>
        private void ProcessInput(CurveMeshSettings.InputMesh input, float3 minSize, float3 offset, ref ProcessInput job)
        {
            if (Application.isPlaying && input.shape == CurveMeshSettings.Shape.Custom && input.mesh && input.mesh.isReadable == false)
            {
                throw new Exception($"[Spline Curve Mesher] To use this at runtime, the mesh \"{input.mesh.name}\" requires the Read/Write option enabled in its import settings. For procedurally created geometry, use \"Mesh.UploadMeshData(false)\"");
            }
            job.Setup(input, minSize, offset);

            inputProcessHandle = job.Schedule();
            inputProcessHandle.Complete();
        }
        
        private void GenerateGeometry(NativeSpline nativeSpline, int splineIndex, SplineCurveMeshData splineMeshData, Structs.InputMeshData meshData, bool createMesh, Structs.InputMeshData colliderData, bool createCollider)
        {
            float splineLength = nativeSpline.GetLength();

            //Value are distances in meters. Y-component represents the trimming from the end of the spline
            float2 trimRange = new float2((settings.distribution.trimStart), (splineLength - settings.distribution.trimEnd));
            
            float trimmedLength = settings.distribution.trimStart + settings.distribution.trimEnd;
            //Subtract trimmed length from spline length
            float trimmedSplineLength = splineLength - trimmedLength;
            
            Profiler.BeginSample("Spline Curve Mesher: Calculate tile count");
            
            float zScale = settings.scale.scale.z;
            float meshLength = MIN_MESH_LENGTH;
            float meshOccupiedLength = 0;
            
            void CalculateMeshLength(ref float meshLength, ref float meshOccupiedLength)
            {
                meshLength = math.max(MIN_MESH_LENGTH, meshData.bounds.size.z * zScale);
                meshOccupiedLength = meshLength + settings.distribution.spacing;
            }
            CalculateMeshLength(ref meshLength, ref meshOccupiedLength);

            var scaleToFit = settings.distribution.scaleToFit;
            if (meshLength > trimmedSplineLength)
            {
                //Debug.LogError($"Spline #{splineIndex} in {splineContainer.name} is too short ({trimmedSplineLength})");
                scaleToFit = true;
            }
            
            int RecalculateTiles()
            {
                //Spline length needs a tiny bit of padding, otherwise its possible that it miscalculates the count by 1 to few
                return CalculateTileCount(trimmedSplineLength, meshOccupiedLength, scaleToFit, nativeSpline.Closed);
            }
            int tiles = RecalculateTiles();
            
            float totalMeshLength = tiles * meshOccupiedLength;
            
            //Scale each segment by the right amount so that they all evenly fit along the spline
            if (scaleToFit)
            {
                //Scale value needed for each individual segment to achieve full coverage
                float zScaleDelta = trimmedSplineLength / totalMeshLength;
                    
                zScale *= zScaleDelta;

                //Factor in new scale
                CalculateMeshLength(ref meshLength, ref meshOccupiedLength);
                    
                //Recalculate
                tiles = RecalculateTiles();
                    
                //Debug.Log($"Segments:{tiles} Segment length: {segmentLength} - Total mesh length: {tiles * segmentLength} Spline length {trimmedSplineLength}. Delta:{zScaleDelta}. Z-scale: {zScale} (new mesh length:{tiles * bounds.size.z * zScale})");
            }
            
            if (tiles == 0)
            {
                Debug.LogWarning("Spline Curve Mesher: No tiles generated. Check the settings and make sure the spline is long enough to generate at least one tile.", this);
                return;
            }
            
            totalMeshLength = tiles * meshOccupiedLength;
            
            Profiler.EndSample();
            
            //Ensure a segment is never shorter than the input mesh
            float m_maxSegmentLength = Mathf.Max(settings.output.maxSegmentLength, meshOccupiedLength);
            
            //Calculate how many segments this mesh needs to be split up into
            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(totalMeshLength / m_maxSegmentLength));
            
            //Debug.Log($"Generating Spline mesh with {tiles} {meshOccupiedLength}m tiles, divided into {segmentCount} segments.");
            
            if (curveToMeshJobs.Length != segmentCount)
            {
                curveToMeshJobs = new CurveToMesh[segmentCount];
            }
            
            SplineMeshContainer container = containers[splineIndex];
            container.PrepareSegments(segmentCount);
            
            Profiler.BeginSample("Spline Curve Mesher: Setup");

            //Values are in meters
            float segmentLength;
            Vector2 curveRange; 
            
            NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(segmentCount, Allocator.Temp);

            for (int i = 0; i < segmentCount; i++)
            {
                SplineMeshSegment segment = container.GetSegment(i);

                //Calculate how many tiles this segment should contain
                int tilesPerSegment = Mathf.FloorToInt((float)tiles / segmentCount);
                
                //Distribute remainder tiles across segments
                if (i < (tiles % segmentCount))
                {
                    tilesPerSegment += 1;
                }
                
                //The actual length this segment occupies
                segmentLength = tilesPerSegment * meshOccupiedLength;
            
                void CalculateSegmentLength()
                {
                    //Calculate spline range based on accumulated tile count
                    int tilesBeforeThisSegment = 0;
                    for (int j = 0; j < i; j++)
                    {
                        int prevSegmentTiles = Mathf.FloorToInt((float)tiles / segmentCount);
                        if (j < (tiles % segmentCount))
                        {
                            prevSegmentTiles += 1;
                        }
                        tilesBeforeThisSegment += prevSegmentTiles;
                    }
                    
                    float startLength = tilesBeforeThisSegment * meshOccupiedLength;
                    float endLength = startLength + segmentLength;
                    
                    //When spacing is negative, the final tile extends beyond the calculated segment end by the actual mesh length
                    if (settings.distribution.spacing < 0)
                    {
                        //Add the portion of the last tile that extends beyond due to negative spacing
                        //Prevents vertices from being squished at segment boundaries
                        endLength += Mathf.Abs(settings.distribution.spacing);
                    }
                    
                    float start = trimRange.x + (startLength);
                    float end = trimRange.x + (endLength);

                    //Min and max distances along spline that this segment covers
                    curveRange = new Vector2(start, end);
                }
                CalculateSegmentLength();

                //Calculations for each individual segment, to determine the tile count
                Structs.SegmentInfo info = new Structs.SegmentInfo
                {
                    tileCount = tilesPerSegment,
                    meshLength = meshLength,
                    tileLength = meshOccupiedLength
                };
                
                curveToMeshJobs[i].Setup(meshData, colliderData, nativeSpline, splinePoints, info, curveRange, splineLength, 
                    segment.transform.worldToLocalMatrix, settings, splineMeshData, hits);
                
                jobHandles[i] = curveToMeshJobs[i].Schedule();
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("Spline Curve Mesher: Build spline mesh");
            {
                JobHandle.CompleteAll(jobHandles);
                jobHandles.Dispose();
            }
            Profiler.EndSample();

            Profiler.BeginSample("Spline Curve Mesher: Create output meshes");
            {
                container.PrepareSegments(segmentCount);

                string objName = this.name;
                for (int i = 0; i < segmentCount; i++)
                {
                    SplineMeshSegment segment = container.GetSegment(i);
                    
                    segment.gameObject.layer = this.gameObject.layer;

                    segment.SetMeshCollider(createCollider);
                    
                    if (createCollider)
                    {
                        Mesh collisionMesh = segment.collisionMesh;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        collisionMesh.name = objName + CurveToMesh.kColliderSuffix + i;
#endif
                        curveToMeshJobs[i].CreateCollider(ref collisionMesh);
                        
                        //Debug.Log($"Created collider for segment {i} with {collisionMesh.vertexCount} vertices.");
                        
                        segment.collisionMesh = collisionMesh;
                    }

                    if (createMesh)
                    {
                        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        if (curveToMeshJobs[i].HasInvalidBounds())
                        {
                            Debug.LogError($"Spline Curve Mesher: Segment {i} has invalid bounds.", this);
                            return;
                        }
                        #endif

                        Mesh segmentMesh = segment.mesh;

                        //Assign a name. This has CG allocations due to string concatenation but remains important to identify meshes in a build report or the memory profiler
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        segmentMesh.name = objName + CurveToMesh.kMeshName + i;
#endif
                        segment.mesh = curveToMeshJobs[i].CreateMesh(ref segmentMesh, i, settings.output.keepReadable, drawWireFrame);
                        
                        segment.EnsureUniqueMesh();
                        segment.SetMaterials(settings.renderer.materials);
                        segment.SetRendererState(true);
                        segment.SetRendererParameters(settings.renderer.shadowCastingMode, settings.renderer.lightProbeUsage, settings.renderer.reflectionProbeUsage, settings.renderer.renderingLayerMask, settings.output.forceMeshLod, settings.output.lodSelectionBias);
                    }
                    else
                    {
                        segment.mesh = null;
                        segment.SetRendererState(false);
                    }
                }
            }
            Profiler.EndSample();

            for (int i = 0; i < curveToMeshJobs.Length; i++)
            {
                curveToMeshJobs[i].Dispose();
            }
            
            //InputMeshData data is disposed later in RebuildSpline()
        }

        /// <summary>
        /// Applies Renderer and Output settings to the created Mesh Renderers without rebuilding the mesh
        /// </summary>
        public void SetRendererParameters()
        {
            foreach (var container in containers)
            {
                foreach (var segment in container.Segments)
                {
                    segment.SetRendererParameters(settings.renderer.shadowCastingMode, settings.renderer.lightProbeUsage, settings.renderer.reflectionProbeUsage, settings.renderer.renderingLayerMask, settings.output.forceMeshLod,  settings.output.lodSelectionBias);
                }
            }
        }

        private NativeArray<RaycastHit> GetRaycasts(NativeArray<Structs.SplinePoint> splinePoints, SplineMeshContainer container)
        {
            if (settings.conforming.enable)
            {
                Profiler.BeginSample("Spline Curve Mesher: Raycasting");

                foreach (var segment in container.Segments)
                {
                    //Avoid conforming to the collider itself
                    segment.SetColliderEnabled(false);
                }
                
                raycaster = new ConformRaycaster();
                raycaster.Raycast(splinePoints, settings.conforming.layerMask, settings.conforming.seekDistance, 
                    settings.conforming.direction == CurveMeshSettings.Conforming.Direction.SplineNormal);

                hits = raycaster.Hits;
                
                //Debug.Log($"{raycaster.Hits.Length} hits");
                
                Profiler.EndSample();
            }
            else
            {
                //Array must still be allocated, even if there are no hits
                hits = new NativeArray<RaycastHit>(0, Allocator.TempJob);
            }

            return hits;
        }
        
        private void Reset()
        {
            splineContainer = GetComponentInParent<SplineContainer>();
            root = this.transform;
            settings.input.SetDefaults();
            settings.renderer.SetDefaults();
        }

        private void OnEnable()
        {
            Instances.Add(this);
            SubscribeCallbacks();
        }

        private void OnDisable()
        {
            Instances.Remove(this);
            UnsubscribeCallbacks();
            
            Dispose();
        }

        public override void DrawComponentGizmos()
        {
            //Debug segments
            if (drawSegments)
            {
                foreach (var container in containers)
                {
                    int segmentCount = container.SegmentCount;
                    
                    for (int i = 0; i < segmentCount; i++)
                    {
                        float t = math.frac(i / (float)16f);
                        Color color = Color.HSVToRGB(Mathf.Sin(t), 0.95f, 1f);
                        color.a = 1f;
                        Gizmos.color = color;
                        
                        container.Segments[i].DrawMeshGizmo();
                    }

                }
            }
        }

        /// <summary>
        /// Call when mesh generation is no longer needed and allocated resources can be disposed of.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            inputMeshProcessJob.Dispose();
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

        public int CountSegments()
        {
            int count = 0;

            foreach (var container in containers)
            {
                foreach (var segment in container.Segments)
                {
                    count++;
                }
            }
            
            return count;
        }
    }
}