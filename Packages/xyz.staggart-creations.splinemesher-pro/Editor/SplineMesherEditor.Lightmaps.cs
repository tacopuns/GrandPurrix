using System;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace sc.splinemesher.pro.editor
{
    public static partial class SplineMesherEditor
    {
        private static void OnBakeryStart(object sender, EventArgs e)
        {
            if (SplineMesherSettings.GenerateLightmapUV == SplineMesherSettings.BakingMode.Automatic) GenerateLightmapUVs();
        }
        
        private static void OnLightBakeStart()
        {
            if (SplineMesherSettings.GenerateLightmapUV == SplineMesherSettings.BakingMode.Automatic)
            {
                #if SM_DEV
                Debug.Log("[Spline Mesher] Automatically generating lightmap UV's");
                #endif
                
                GenerateLightmapUVs();
            }
        }
        
        /// <summary>
        /// Generates lightmap UV's for any spline mesh that requires it
        /// </summary>
        public static void GenerateLightmapUVs()
        {
            SplineMesher[] splineMeshers = Object.FindObjectsByType<SplineMesher>(FindObjectsSortMode.None);
            
            int count = 0;
            System.Diagnostics.Stopwatch lightmapUVUnwrapTimer = new System.Diagnostics.Stopwatch();
            
            lightmapUVUnwrapTimer.Start();
            //Find the spline meshes that still require lightmap UV's
            foreach (SplineMesher splineMesher in splineMeshers)
            {
                if (GenerateLightmapUV(splineMesher))
                {
                    count++;
                }
            }
            lightmapUVUnwrapTimer.Stop();
            
            if (count > 0)
            {
                Debug.Log($"[Spline Mesher] Lightmap UV created for {count} spline meshes (Duration: {lightmapUVUnwrapTimer.ElapsedMilliseconds}ms)");
            }
        }

        public static bool GenerateLightmapUV(SplineMesher splineMesher)
        {
            bool generated = false;
            for (int i = 0; i < splineMesher.Containers.Count; i++)
            {
                SplineMeshContainer container = splineMesher.Containers[i];

                for (int j = 0; j < container.SegmentCount; j++)
                {
                    SplineMeshSegment segment = container.Segments[j];

                    if (RequiresLightmapUV(segment))
                    {
                        #if SM_DEV
                        Debug.Log($"{splineMesher.name} segment #{j} requires new lightmap UV's");
                        #endif

                        splineMesher.GetLightmapUVParameters(out float angleThreshold, out var packingMargin);
                            
                        GenerateLightmapUV(segment, angleThreshold, packingMargin);

                        generated = true;
                    }
                }
            }

            return generated;
        }

        public static bool RequiresLightmapUV(SplineMesher splineMesher)
        {
            for (int i = 0; i < splineMesher.Containers.Count; i++)
            {
                SplineMeshContainer container = splineMesher.Containers[i];

                for (int j = 0; j < container.SegmentCount; j++)
                {
                    SplineMeshSegment segment = container.Segments[j];

                    if (RequiresLightmapUV(segment))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private static bool RequiresLightmapUV(SplineMeshSegment segment)
        {
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(segment.gameObject);
        
            //Mesh renderer marked as static
            if (staticFlags.HasFlag(StaticEditorFlags.ContributeGI))
            {
                Mesh mesh = segment.mesh;

                if (mesh == null) return false;
        
                //Conditions that indicate the mesh has no lightmap UV's
                //Note that rebuilding the spline mesh clears the UV2 channel, automatically marking it as 'dirty' again.
                if (mesh.uv2 == null || mesh.uv2.Length == 0)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static void GenerateLightmapUV(SplineMeshSegment segment, float angleThreshold, float marginMultiplier)
        {
            MeshFilter mf = segment.GetComponent<MeshFilter>();

            if (!mf) return;
            
            Mesh mesh = mf.sharedMesh;
            if(mesh == null) return;
            
            UnwrapParam.SetDefaults(out var unwrapSettings);

            unwrapSettings.hardAngle = angleThreshold;
            unwrapSettings.packMargin *= Mathf.Max(0.01f, marginMultiplier);

            #if UNITY_2022_1_OR_NEWER
            if (Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings) == false)
            {
                throw new Exception($"Lightmap UV generation for mesh \"{mesh.name}\" failed.");
            }
            #else
            Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings);
            #endif
        }
    }
}