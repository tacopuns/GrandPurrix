using System;
using System.Collections.Generic;
using sc.splinemesher.pro.runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using SplineMesher = sc.splinemesher.pro.runtime.SplineMesher;
#if SM1
using sc.modeling.splines.runtime;
using UnityEditor.SceneManagement;
using SplineMesher1 = sc.modeling.splines.runtime.SplineMesher;
#endif
using SplineMesher2 = sc.splinemesher.pro.runtime.SplineCurveMesher;

namespace sc.splinemesher.pro.editor
{
    public static class MigrationTool
    {
        private class MigrationWindow : EditorWindow
        {
            #if SM1
            private const float width = 450;
            private const float height = 600;
            private Vector2 scrollPos;
            
            [MenuItem("Spline Mesher Pro/Migration Tool")]
            private static void Open()
            {
                var window = GetWindow<MigrationWindow>(true, "Upgrade to Spline Mesher Pro", true);
                window.minSize = new Vector2(width, height);
                window.maxSize = new Vector2(width, height);
            }

            private SplineMesher1[] meshers = Array.Empty<SplineMesher1>();
            private SplineMesher2[] meshers2 = Array.Empty<SplineMesher2>();

            private List<SplineMesher1> mesherPrefabs = new();

            private List<string> errorMessages = new();
            private Vector2 logScrollPos;
            private GUIStyle wrappedStyle;
            
            private void OnGUI()
            {
                EditorGUILayout.Separator();
                
                EditorGUILayout.HelpBox("This tool will migrate the Spline Mesher Standard instances to Spline Mesher Pro." +
                                        "\n\nSettings will be copied and Undo is supported", MessageType.Info);
                

                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField($"Spline Mesher Standard prefabs in project ({mesherPrefabs.Count}):", EditorStyles.boldLabel);
                if (mesherPrefabs.Count > 0)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(5f+ Mathf.Min(250, mesherPrefabs.Count * EditorGUIUtility.singleLineHeight)));
                        foreach (var prefab in mesherPrefabs)
                        {
                            EditorGUILayout.ObjectField(prefab.name, prefab, typeof(SplineMesher1), false);
                        }

                        EditorGUILayout.EndScrollView();
                    }
                }
                
                EditorGUILayout.Space();
                
                if (mesherPrefabs.Count > 0) EditorGUILayout.HelpBox("Use of a version control system is advised!", MessageType.Warning);
                
                using (new EditorGUI.DisabledGroupScope(mesherPrefabs.Count == 0))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Migrate prefabs", GUILayout.Width(120f), GUILayout.Height(30f)))
                        {
                            Migrate(mesherPrefabs.ToArray());
                            return;
                        }

                        GUILayout.FlexibleSpace();
                    }
                }

                EditorGUILayout.Space();
                
                using (new EditorGUI.DisabledGroupScope(mesherPrefabs.Count > 0))
                {
                    EditorGUILayout.LabelField($"Spline Mesher Standard instances in scenes ({meshers.Length}):",
                        EditorStyles.boldLabel);
                    if (meshers.Length > 0)
                    {
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(5f+ Mathf.Min(250, meshers.Length * EditorGUIUtility.singleLineHeight)));
                            foreach (var mesher in meshers)
                            {
                                if (!mesher) return;
                                
                                EditorGUILayout.ObjectField(mesher.name, mesher, typeof(SplineMesher1), false);
                            }

                            EditorGUILayout.EndScrollView();
                        }
                    }

                    EditorGUILayout.Space();

                    using (new EditorGUI.DisabledGroupScope(meshers.Length == 0))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Migrate scene instances", GUILayout.Width(170f), GUILayout.Height(30f)))
                            {
                                Migrate(meshers);
                                return;
                            }

                            GUILayout.FlexibleSpace();
                        }
                    }
                }

                if (mesherPrefabs.Count > 0)
                {
                    EditorGUILayout.HelpBox("Migrate any prefabs first, instances of them may be in the scene", MessageType.Info);
                }

                EditorGUILayout.Space();

                if (errorMessages.Count > 0)
                {
                    EditorGUILayout.LabelField($"Messages ({errorMessages.Count}):", EditorStyles.boldLabel);

                    logScrollPos = EditorGUILayout.BeginScrollView(logScrollPos, GUILayout.Height(5f+ Mathf.Min(150, errorMessages.Count * EditorGUIUtility.singleLineHeight)));
                    using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                    {
                        foreach (var message in errorMessages)
                        {
                            EditorGUILayout.LabelField("• " + message, wrappedStyle);
                        }
                    }
                    EditorGUILayout.EndScrollView();
                    
                }
                if (meshers.Length == 0 && meshers2.Length > 0)
                {
                    EditorGUILayout.HelpBox("Migration complete. You may need to check other scenes.", MessageType.Info);
                }
            }

            PrefabStage prefabStage = null;
            GameObject prefabRoot = null;
            
            private void Migrate(SplineMesher1[] targets)
            {
                errorMessages.Clear();
                Undo.RecordObjects(targets, "Spline Mesher Standard to Pro Migration");
                
                foreach (var target in targets)
                {
                    SplineMesher1 mesher1 = target;
                    
                    bool isPrefab = PrefabUtility.GetPrefabAssetType(mesher1) != PrefabAssetType.NotAPrefab;
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(mesher1.gameObject);
                    isPrefab |= prefabSource != null;

                    GameObject mesherGameObject = mesher1.gameObject;
                    string sourcePath = AssetDatabase.GetAssetPath(mesherGameObject);
                    
                    //Debug.Log($"Migrating {mesher1.name} ({sourcePath}). Prefab:{isPrefab}");
   
                    //Open prefab in isolation mode to allow editing
                    if (isPrefab)
                    {
                        string prefabSourcePath = string.IsNullOrEmpty(sourcePath) ? AssetDatabase.GetAssetPath(prefabSource) : sourcePath;
                        prefabStage = PrefabStageUtility.OpenPrefab(prefabSourcePath, null, PrefabStage.Mode.InIsolation);
                        
                        if (prefabStage != null)
                        {
                            //Get the instance in the prefab stage, not the asset
                            prefabRoot = prefabStage.prefabContentsRoot;
                            mesher1 = prefabRoot.GetComponent<SplineMesher1>();
                            mesherGameObject = mesher1.gameObject;
                        }
                    }
                    
                    SplineMesher2 mesher2;
                    
                    if(isPrefab) mesher2 = mesherGameObject.AddComponent<SplineMesher2>();
                    else mesher2 = Undo.AddComponent(mesherGameObject, typeof(SplineMesher2)) as SplineMesher2;
                    
                    mesher2.SetSplineContainer(mesher1.splineContainer, false);
                    mesher2.root = mesher1.outputObject.transform;
                    mesher2.SetInputMesh(mesher1.sourceMesh);
                    mesher2.splineChangeTrigger = (SplineMesher.SplineChangeTrigger)mesher1.splineChangeMode;
                    mesher2.rebuildTriggers = (SplineMesher.RebuildTriggers)mesher1.rebuildTriggers;
                    
                    MeshRenderer meshRenderer = mesher1.GetComponent<MeshRenderer>();
                    if (meshRenderer)
                    {
                        mesher2.AssignMaterials(meshRenderer.sharedMaterials);
                    }
                    
                    //Distribution settings
                    mesher2.settings.distribution.autoTileCount = mesher1.settings.distribution.autoSegmentCount;
                    mesher2.settings.distribution.tiles = mesher1.settings.distribution.segments;
                    mesher2.settings.distribution.spacing = mesher1.settings.distribution.spacing;
                    mesher2.settings.distribution.evenOnly = mesher1.settings.distribution.evenOnly;
                    mesher2.settings.distribution.scaleToFit = mesher1.settings.distribution.stretchToFit;
                    mesher2.settings.distribution.trimStart = mesher1.settings.distribution.trimStart;
                    mesher2.settings.distribution.trimEnd = mesher1.settings.distribution.trimEnd;
                    mesher2.settings.distribution.curveOffset = mesher1.settings.deforming.curveOffset;

                    //Deformation settings
                    if (mesher1.settings.deforming.ignoreKnotRotation)
                    {
                        errorMessages.Add($"<b>{mesher1.name}</b> has a \"Ignore Knot Rotation\" enabled. Disabled Z-axis alignment, which may not be correct.");
                        
                        mesher2.settings.rotation.align = new bool3(true, true, false);
                    }
                    
                    if (mesher1.settings.deforming.pivotOffset.magnitude > 0)
                    {
                        errorMessages.Add($"<b>{mesher1.name}</b> has a pivot offset. This is has become obsolete in Spline Mesher Pro, instead change the Alignment of the input mesh.");
                    }
                    
                    mesher2.settings.scale.scale = mesher1.settings.deforming.scale;
                    mesher2.settings.scale.pathIndexUnit = mesher1.settings.deforming.scalePathIndexUnit;
                    mesher2.settings.scale.interpolation = (CurveMeshSettings.InterpolationType)mesher1.settings.deforming.scaleInterpolation;
                    
                    mesher2.settings.rotation.rollMode = (CurveMeshSettings.Rotation.RollMode)mesher1.settings.deforming.rollMode;
                    mesher2.settings.rotation.pathIndexUnit = mesher1.settings.deforming.rollPathIndexUnit;
                    mesher2.settings.rotation.rollAngle = mesher1.settings.deforming.rollAngle;
                    mesher2.settings.rotation.rollFrequency = mesher1.settings.deforming.rollFrequency;
                    
                    //Conforming
                    mesher2.settings.conforming.enable = mesher1.settings.conforming.enable;
                    mesher2.settings.conforming.align = mesher1.settings.conforming.align;
                    mesher2.settings.conforming.blendNormal = mesher1.settings.conforming.blendNormal;
                    mesher2.settings.conforming.layerMask = mesher1.settings.conforming.layerMask;
                    mesher2.settings.conforming.pathIndexUnit = mesher1.settings.conforming.pathIndexUnit;
                    

                    
                    //Copy data
                    bool validScaleData = false;
                    //Scale isn't valid if there is only one point
                    for (int i = 0; i < mesher1.scaleData.Count; i++)
                    {
                        validScaleData |= mesher1.scaleData[i].Count > 1;
                    }
                    if(validScaleData) mesher2.scaleData = mesher1.scaleData;
                    mesher2.rollData = mesher1.rollData;
                    mesher2.settings.color.pathIndexUnit = mesher1.settings.color.pathIndexUnit;
                    
                    var hasVertexColorBlending = false;
                    mesher2.vertexColorRedData = ConvertVertexColorData(mesher1.vertexColorRedData, ref hasVertexColorBlending);
                    mesher2.vertexColorGreenData = ConvertVertexColorData(mesher1.vertexColorGreenData, ref hasVertexColorBlending);
                    mesher2.vertexColorBlueData = ConvertVertexColorData(mesher1.vertexColorBlueData, ref hasVertexColorBlending);
                    mesher2.vertexColorAlphaData = ConvertVertexColorData(mesher1.vertexColorAlphaData, ref hasVertexColorBlending);
                    if (hasVertexColorBlending) mesher2.settings.color.retainVertexColors = true;
                    
                    mesher2.conformingData = mesher1.conformingStrength;
                    
                    //Collision
                    mesher2.settings.collision.enable = mesher1.settings.collision.enable;
                    if (mesher1.settings.collision.type == Settings.ColliderType.Box)
                    {
                        mesher2.settings.collision.inputMesh.shape = CurveMeshSettings.Shape.Cube;
                        //Make it fit to the input mesh
                        mesher2.settings.collision.inputMesh.cube.scale = new Vector3(0.1f, 0.1f, 1f);
                    }
                    else
                    {
                        mesher2.settings.collision.inputMesh.shape = CurveMeshSettings.Shape.Custom;
                    }
                    mesher2.settings.collision.inputMesh.mesh = mesher1.settings.collision.collisionMesh;
                    mesher2.settings.collision.colliderOnly = mesher1.settings.collision.colliderOnly;
                    
                    //Caps
                    void CopyCap(SplineMesher1.Cap source, ref SplineMesher2.Cap destination)
                    {
                        destination = new SplineCurveMesher.Cap((SplineCurveMesher.Cap.Position)source.position)
                        {
                            prefab = source.prefab,
                            offset = source.offset,
                            shift = source.shift,
                            align = source.align,
                            rotation = source.rotation,
                            matchScale = source.matchScale,
                            scale = source.scale
                        };
                    }
                    CopyCap(mesher1.startCap, ref mesher2.settings.caps.startCap);
                    CopyCap(mesher1.endCap, ref mesher2.settings.caps.endCap);

                    if (mesher2.settings.caps.endCap.prefab)
                    {
                        mesher2.settings.caps.endCap.rotation.y += 180f;
                    }

                    //Ensure the instances are destroyed
                    mesher1.startCap.prefab = null;
                    mesher1.endCap.prefab = null;
                    mesher1.UpdateCaps();
                    
                    if (mesher1.settings.conforming.terrainOnly)
                    {
                        errorMessages.Add($"<b>{mesher1.name}</b> has Conforming \"Terrain Only\" enabled. This is not supported in Spline Mesher Pro (not possible with DOTS)");
                    }
                    
                    MeshCollider meshCollider = mesher1.GetComponent<MeshCollider>();
                    
                    //Delete spline mesher 1
                    DestroyImmediate(mesher1, isPrefab);
                    DestroyImmediate(mesher1.meshFilter, isPrefab);
                    if (meshRenderer) DestroyImmediate(meshRenderer, isPrefab);
                    if (meshCollider) DestroyImmediate(meshCollider, isPrefab);

                    //Rebuild new spline mesh
                    mesher2.RebuildSplineCache();
                    mesher2.Rebuild();

                    //Save prefab changes
                    if (isPrefab && prefabStage != null)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabStage.assetPath);
                        StageUtility.GoToMainStage();
                    }

                    foreach (var container in mesher2.Containers)
                    {
                        if(!isPrefab) Undo.RegisterCreatedObjectUndo(container.gameObject, "SplineMesher Pro Containers Created");
                    }
                }

                AssetDatabase.SaveAssets();
                
                Refresh();
            }
            
            private void OnEnable()
            {
                Refresh();
                
                wrappedStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    richText = true
                };
            }

            private void OnFocus()
            {
                Refresh();
            }
            
            private List<SplineData<SplineMesher2.VertexColorChannel>> ConvertVertexColorData(
                List<SplineData<SplineMesher1.VertexColorChannel>> sourceData, ref bool hasBlending)
            {
                if (sourceData == null) return new List<SplineData<SplineMesher2.VertexColorChannel>>();
                
                var result = new List<SplineData<SplineMesher2.VertexColorChannel>>(sourceData.Count);
                
                foreach (var sourceSplineData in sourceData)
                {
                    var newSplineData = new SplineData<SplineMesher2.VertexColorChannel>();
                    newSplineData.PathIndexUnit = sourceSplineData.PathIndexUnit;
                    
                    //Convert default value
                    newSplineData.DefaultValue = new SplineMesher2.VertexColorChannel 
                    { 
                        value = sourceSplineData.DefaultValue.value,
                        blend = sourceSplineData.DefaultValue.blend
                    };
                    
                    //Convert all data points
                    foreach (var dataPoint in sourceSplineData)
                    {
                        hasBlending |= dataPoint.Value.blend;
                        
                        newSplineData.Add(dataPoint.Index, new SplineMesher2.VertexColorChannel
                        {
                            value = dataPoint.Value.value,
                            blend = dataPoint.Value.blend
                        });
                    }
                    
                    result.Add(newSplineData);
                }
                
                return result;
            }

            void Refresh()
            {
                meshers = FindObjectsOfType<SplineMesher1>();
                meshers2 = FindObjectsOfType<SplineMesher2>();
                
                //Also populate the `prefabs` list with prefabs in the project that have a SplineMesher1 component
                mesherPrefabs.Clear();
                string[] guids = AssetDatabase.FindAssets("t:GameObject");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    if(path.Contains("Packages")) continue;
                    
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    
                    if (prefab != null)
                    {
                        SplineMesher1[] prefabMeshers = prefab.GetComponentsInChildren<SplineMesher1>();
                        for (int i = 0; i < prefabMeshers.Length; i++)
                        {
                            mesherPrefabs.Add(prefabMeshers[i]);
                        }
                    }
                }
            }
#endif
        }
    }
}