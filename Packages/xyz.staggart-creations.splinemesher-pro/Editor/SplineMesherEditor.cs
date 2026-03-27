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
using System.Linq;
using sc.splinemesher.pro.runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace sc.splinemesher.pro.editor
{
    public static partial class SplineMesherEditor
    {
        private static bool STARTUP_PERFORMED
        {
            get => SessionState.GetBool("SPLINE_MESHER_EDITOR_STARTED", false);
            set => SessionState.SetBool("SPLINE_MESHER_EDITOR_STARTED", value);
        }
        
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            CheckMeshLodSupport();
            
            #if !SM_DEV
            if (STARTUP_PERFORMED == false)
            #endif
            {
                AssetInfo.VersionChecking.CheckForUpdate();
                STARTUP_PERFORMED = true;
            }

            Lightmapping.bakeStarted += OnLightBakeStart;
            EditorSceneManager.sceneSaving += WhenSceneSaves;

            #if BAKERY_INCLUDED
            ftRenderLightmap.OnPreFullRender += OnBakeryStart;
            #endif
        }

        private static void WhenSceneSaves(Scene scene, string path)
        {
            if (SplineMesherSettings.GenerateLODs == SplineMesherSettings.BakingMode.Automatic) GenerateLODS(scene);
        }

        private static Type prevToolType;
        public static void OpenTool<T>() where T : EditorTool
        {
            prevToolType = ToolManager.activeToolType;
            
            ToolManager.SetActiveTool<T>();
        }

        public static bool IsToolActive<T>() where T : EditorTool
        {
            return ToolManager.activeToolType == typeof(T);
        }
        
        public static void CloseTool<T>() where T : EditorTool
        {
            Type toolType = typeof(T);

            if (ToolManager.activeToolType == toolType)
            {
                
            }
            if(prevToolType != null) ToolManager.SetActiveTool(prevToolType);
            else ToolManager.SetActiveContext<GameObjectToolContext>();
        }

        public static void OpenPrefab(SplineMesher splineMesher)
        {
            bool isPrefab = PrefabUtility.GetPrefabAssetType(splineMesher) != PrefabAssetType.NotAPrefab;
            GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(splineMesher.gameObject);
            isPrefab |= prefabSource != null;

            if (!isPrefab) return;
            
            string sourcePath = AssetDatabase.GetAssetPath(splineMesher);
            string prefabSourcePath = string.IsNullOrEmpty(sourcePath) ? AssetDatabase.GetAssetPath(prefabSource) : sourcePath;
            PrefabStage prefabStage = PrefabStageUtility.OpenPrefab(prefabSourcePath, null, PrefabStage.Mode.InIsolation);
            
            //This instance would not have any meshes stored in it anymore, rebuild it now so the user sees them.
            EditorApplication.delayCall += () =>
            {
                splineMesher.Rebuild();
            };
        }

        private class ModelImportCallback : AssetPostprocessor
        {
            //Note, need to work with paths because object references won't be valid during the importing stage
            private static readonly List<string> importedMeshPaths = new();
            
            void OnPostprocessModel(GameObject gameObject)
            {
                MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
                if (meshFilters.Length == 0) return;
                
                importedMeshPaths.Clear();
                importedMeshPaths.Add(assetPath);

                //Defer handling until after import is fully done
                EditorApplication.delayCall += ProcessImportedMeshes;
            }

            private static void ProcessImportedMeshes()
            {
                if (importedMeshPaths.Count == 0)
                    return;
                
                //Debug.Log($"Processing {importedMeshes.Count} imported meshes");

                int instanceCount = SplineCurveMesher.Instances.Count;
                if (instanceCount == 0)
                {
                    //Debug.LogWarning("[SplineMesher.ModelImportCallback] Skipped, no mesher instances found");
                    importedMeshPaths.Clear();
                    return;
                }

                List<SplineCurveMesher> toRebuild = new();

                foreach (var importedMeshPath in importedMeshPaths)
                {
                    if (string.IsNullOrEmpty(importedMeshPath))
                    {
                        Debug.LogError($"[SplineMesher.ModelImportCallback] Attempting to process an empty mesh file path");
                    }
                    Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(importedMeshPath);

                    //Debug.Log($"[SplineMesher.ModelImportCallback] Processing {importedMeshPath}");
                    
                    for (int j = 0; j < instanceCount; j++)
                    {
                        var mesher = SplineCurveMesher.Instances[j];

                        if (!mesher.RebuildTriggersEnabled(SplineMesher.RebuildTriggers.OnMeshImported) || 
                            mesher.settings.input.shape != CurveMeshSettings.Shape.Custom) continue;

                        string mesherMeshPath = AssetDatabase.GetAssetPath(mesher.settings.input.mesh);

                        //Ignore procedural or null input meshes
                        if (string.IsNullOrEmpty(mesherMeshPath))
                        {
                            //Debug.LogWarning($"[SplineMesher.ModelImportCallback] {mesher.name} skipped because its input mesh ({mesh.name}) is missing", mesher);
                            continue;
                        }

                        if (mesherMeshPath == importedMeshPath)
                        {
                            //Debug.Log($"[SplineMesher.ModelImportCallback] Mesh file \"{importedMeshPath}\" has been changed and is used by {mesher.name}.");

                            toRebuild.Add(mesher);
                        }
                        
                    }
                }

                foreach (var mesher in toRebuild)
                {
                    //Debug.Log($"[SplineMesher.ModelImportCallback] {mesher.name} rebuilt because one of its input meshes was reimported.");
                    
                    mesher.Rebuild();
					EditorUtility.SetDirty(mesher);
                }
            }
        }

        public static bool CheckInputMeshReadability(Mesh mesh)
        {
            if (mesh == null) return false;

            //Saved on disk
            if (EditorUtility.IsPersistent(mesh))
            {
                string assetPath = AssetDatabase.GetAssetPath(mesh);
                
                //Default meshes
                if (assetPath.StartsWith("Library")) return true;
            }
            
            return mesh.isReadable;
        }
        
        public static bool HasImportedQuads(Mesh mesh)
        {
            if (mesh == null) return false;

            //Saved on disk
            if (EditorUtility.IsPersistent(mesh))
            {
                string assetPath = AssetDatabase.GetAssetPath(mesh);
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;

                return importer.keepQuads;
            }
            
            return false;
        }
        
        public static bool SetMeshReadWriteFlag(Mesh mesh)
        {
            if (mesh == null) return false;

            if (EditorUtility.IsPersistent(mesh) == false)
            {
                string msg = $"Can't enable the read/write option on \"{mesh.name}\". Because it is not an imported mesh." +
                             $"\n" +
                             $"\n" +
                             $"For script-based procedural geometry add the \"Mesh.UploadMeshData(false)\" function when creating the mesh to keep it readable.";
                EditorUtility.DisplayDialog("Spline Mesher", msg, "Ok");
                
                return false;
            }
            
            string assetPath = AssetDatabase.GetAssetPath(mesh);
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            
            importer.isReadable = true;
            importer.SaveAndReimport();
            
            return true;
        }
        
        public static SplineContainer AddSplineContainer(GameObject target)
        {
            if (target == null)
            {
                throw new Exception("Cannot add a Spline Container component to a null GameObject");
            }
            
            SplineContainer splineContainer = target.AddComponent<SplineContainer>();
            splineContainer.Spline = null;

            EditorUtility.SetDirty(target);
            
            return splineContainer;
        }

        public static void PositionObjectInScene(GameObject gameObject)
        {
            //Position in view
            if (SceneView.lastActiveSceneView)
            {
                var transform = SceneView.lastActiveSceneView.camera.transform;
                
                Selection.activeGameObject = gameObject;
                EditorApplication.ExecuteMenuItem("GameObject/Move To View");
                
                Vector3 spawnPosition = gameObject.transform.position;

                if (Physics.Raycast(transform.position, transform.forward, out var hit, 2000, -1, QueryTriggerInteraction.Ignore))
                {
                    float dist = Vector3.Distance(spawnPosition, hit.point);

                    if (dist < 50)
                    {
                        spawnPosition.y = hit.point.y + 0.02f;
                    }
                    else
                    {
                        spawnPosition = transform.position + (transform.forward * 25f);
                    }
                }
                else
                {
                    spawnPosition = transform.position + (transform.forward * 25f);
                }
                
                gameObject.transform.position = spawnPosition;
            }
        }
        
        //Wiggly line
        public static Spline CreateDefaultCurveSpline()
        {
            int knots = 5;
            float amplitude = 2f;
            float length = 15f;
            
            Spline spline = new Spline(knots, false);

            for (int i = 0; i < knots; i++)
            {
                float t = (float)i / (float)(knots-1);
                
                BezierKnot knot = new BezierKnot();
                knot.Position = new Vector3(Mathf.Sin(t * length) * amplitude, 0f, (t * length) - (length * 0.5f));
                spline.Add(knot, TangentMode.Linear);
            }

            //Automatically recalculate tangents
            spline.SetTangentMode(new SplineRange(0, spline.Count), TangentMode.AutoSmooth);

            return spline;
        }
        
        //Creates a circle
        public static Spline CreateDefaultFillSpline()
        {
            float radius = 15f;
            int knotCount = 8;
            
            float tangentLength = (4f * (MathF.Sqrt(2f) - 1f)) / 3f;
            tangentLength *= 4;
            tangentLength /= knotCount;
            
            float3 tangent = new float3(0f, 0f, tangentLength * radius);
            
            float curAngle = 0f;
            float angleStep = 360f / (float)knotCount;
            
            Spline spline = new Spline(knotCount, true)
            {
                Knots = new BezierKnot[knotCount]
            };

            for (int i = 0; i < knotCount; i++)
            {
                float3 position = new Vector3(
                    math.sin(Mathf.Deg2Rad * curAngle) * radius,
                    0,
                    math.cos(Mathf.Deg2Rad * curAngle) * radius);
                
                float yAngle = curAngle - 90f;
                BezierKnot knot = new BezierKnot(position, tangent, -tangent, 
                    quaternion.Euler(0f, math.radians(yAngle), 0f));
                
                spline.SetKnotNoNotify(i, knot);
                spline.SetTangentModeNoNotify(i, TangentMode.Mirrored);

                curAngle += angleStep;
            }
            
            return spline;
        }
    }
}