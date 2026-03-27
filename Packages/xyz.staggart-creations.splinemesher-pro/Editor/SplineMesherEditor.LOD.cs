using System.Collections.Generic;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace sc.splinemesher.pro.editor
{
    public static partial class SplineMesherEditor
    {
        /// <summary>
        /// Mesh LOD is supported in Unity 6.2 and newer
        /// </summary>
        public static bool meshLodSupport;

        private static void CheckMeshLodSupport()
        {
#if UNITY_6000_2_OR_NEWER
            meshLodSupport = true;
#else
            meshLodSupport = false;
#endif
        }
        
        /// <summary>
        /// Generate all MeshLOD's for any Spline Curve- and Spline Fill Mesher components that require it.
        /// </summary>
        public static void GenerateLODS()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                GenerateLODS(scene);
            }
        }
        
        private static List<SplineMesher> GetMeshersUsingLODs()
        {
            List<SplineMesher> meshers = new List<SplineMesher>();
            
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                
                List<SplineMesher> sceneMeshers = GetMeshersUsingLODs(scene);
                foreach (var mesher in sceneMeshers)
                {
                    meshers.Add(mesher);
                }

            }

            return meshers;
        }
        
        private static List<SplineMesher> GetMeshersUsingLODs(Scene scene)
        {
            List<SplineMesher> meshers = new List<SplineMesher>();
            
            GameObject[] sceneObjects = scene.GetRootGameObjects();
            foreach (GameObject go in sceneObjects)
            {
                SplineMesher mesher = go.GetComponent<SplineMesher>();
                if (mesher && mesher.GetLODCount() > 0) meshers.Add(mesher);
            }

            return meshers;
        }

        public static bool RequiresLODGeneration(SplineMesher mesher)
        {
            #if UNITY_6000_2_OR_NEWER
            int maxLodCount = Mathf.Max(1, mesher.GetLODCount());
            for (int j = 0; j < mesher.Containers.Count; j++)
            {
                SplineMeshContainer container = mesher.Containers[j];

                for (int s = 0; s < container.SegmentCount; s++)
                {
                    SplineMeshSegment segment = container.Segments[s];
                    
                    if (RequiresLODGeneration(segment, maxLodCount))
                    {
                        return true;
                    }
                }
            }
            #endif

            return false;
        }
        
        public static bool RequiresLODGeneration(SplineMeshSegment segment, int maxLodCount)
        {
#if UNITY_6000_2_OR_NEWER
            int currentLODCount = segment.mesh.lodCount;

            //Max LOD may be set to 7, but the mesh may only have 4 LODS if low-poly.
            if (maxLodCount > currentLODCount || EditorUtility.IsDirty(segment.mesh))
            {
                return true;
            }
#endif

            return false;
        }

        public static List<SplineMesher> GetMeshersRequiringLODGeneration()
        {
            List<SplineMesher> meshers = GetMeshersUsingLODs();
            
            meshers.RemoveAll(RequiresLODGeneration);
            
            return meshers;
        }
        
        public static List<SplineMesher> GetMeshersRequiringLODGeneration(Scene scene)
        {
            List<SplineMesher> meshers = GetMeshersUsingLODs(scene);
            
            meshers.RemoveAll(RequiresLODGeneration);
            
            return meshers;
        }
        
        public static void GenerateLODS(Scene scene)
        {        
            #if UNITY_6000_2_OR_NEWER
            List<SplineMesher> meshers = GetMeshersRequiringLODGeneration(scene);
            
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            
            //Debug.Log($"{meshers.Count} use LODs");
            
            GenerateLODS(meshers, out var count);
            
            timer.Stop();
            
            if (count > 0)
            {
                Debug.Log($"[Spline Mesher] MeshLOD generated for {count} spline meshes in {scene.name} (Duration: {timer.ElapsedMilliseconds}ms)");
            }
            #endif
        }

        public static void GenerateLODS(List<SplineMesher> meshers, out int count)
        {
            count = 0;
            #if UNITY_6000_2_OR_NEWER
            for (int i = 0; i < meshers.Count; i++)
            {
                SplineMesher mesher = meshers[i];
                if (mesher.GetLODCount() > 0)
                {
                    if (GenerateLODS(mesher))
                    {
                        count++;
                    }
                }
            }
            #endif
        }
        
        public static bool GenerateLODS(SplineMesher mesher)
        {
            bool requiredLODS = false;
            
            #if UNITY_6000_2_OR_NEWER
            int maxLodCount = Mathf.Max(1, mesher.GetLODCount());
            
            for (int j = 0; j < mesher.Containers.Count; j++)
            {
                SplineMeshContainer container = mesher.Containers[j];

                for (int s = 0; s < container.SegmentCount; s++)
                {
                    SplineMeshSegment segment = container.Segments[s];

                    //Debug.Log($"Has: {currentLODCount}. Needs:{lodCount}", segment);
                    
                    //Note: Desired lodCount may never be met, as it only sets a maximum.
                    //So lods may always appear to be needing to rebuild for some instances.
                    
                    if(RequiresLODGeneration(segment, maxLodCount))
                    {
                        //Debug.Log($"Mesh LODs generating for {segment.mesh}. Has:{currentLODCount} Needs:{lodCount}");
                        
                        requiredLODS = true;
                        
                        MeshLodUtility.GenerateMeshLods(segment.mesh, (MeshLodUtility.LodGenerationFlags)0, maxLodCount);
                    }
                }
            }
            #endif

            return requiredLODS;
        }
    }
}