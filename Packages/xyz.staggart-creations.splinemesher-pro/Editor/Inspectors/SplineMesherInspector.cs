using System;
using System.Collections.Generic;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace sc.splinemesher.pro.editor
{
    public class SplineMesherInspector : Editor
    {
        private SplineMesher component;
        
        protected SerializedProperty splineContainer;
        protected SerializedProperty splineChangeTrigger;
        protected SerializedProperty root;
        protected SerializedProperty rebuildTriggers;
        
        protected SerializedProperty settings;
        
        internal int vertexCount;
        internal int triangleCount;
        internal string memorySize;
        
        internal bool isAllowedToRebuild;
                
        internal bool isPrefab;
        internal bool isInspectingPrefab;
        internal bool missingMaterials;

        internal bool requiresRebuild;
        
        internal readonly List<UI.Section> sections = new List<UI.Section>();
        internal UI.Section eventsSection;

        public void OnEnable()
        {
            splineContainer = serializedObject.FindProperty("splineContainer");
            splineChangeTrigger = serializedObject.FindProperty("splineChangeTrigger");
            root = serializedObject.FindProperty("root");
            rebuildTriggers = serializedObject.FindProperty("rebuildTriggers");
            
            settings = serializedObject.FindProperty("settings");
            
            component = (SplineMesher)target;
        }

        public void Initialize()
        {
            eventsSection = UI.Section.Create<Events>(this, "Events",  new GUIContent("Events", UI.Icons.Event), null);
            sections.Add(eventsSection);
            
            isPrefab = PrefabUtility.IsPartOfPrefabInstance(target)|| PrefabStageUtility.GetCurrentPrefabStage();
            isInspectingPrefab = PrefabUtility.IsPartOfPrefabAsset(this.target) && this.component.gameObject.scene.name != string.Empty;

            Recalculate();
        }
        
        internal new void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();

                UI.DrawHeader();

                SplineMesher component = (SplineMesher)target;
                component.drawWireFrame = GUILayout.Toggle(component.drawWireFrame, new GUIContent("", EditorGUIUtility.IconContent("d_MainStageView").image, "Toggle wire frame display"), "MiniButtonLeft", GUILayout.Width(30f), GUILayout.Height(22f));

                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(UI.Icons.prefix + "Settings").image, "Utility functions"), EditorStyles.miniButtonMid, GUILayout.Width(30f), GUILayout.Height(22f)))
                {
                    GenericMenu menu = new GenericMenu();
                    
                    menu.AddItem(new GUIContent("Open preferences"), false, SplineMesherSettings.OpenPreferences);
                    menu.AddItem(new GUIContent("Open project settings"), false, SplineMesherSettings.OpenProjectSettings);
                    
                    menu.AddSeparator(string.Empty);

                    if (component.GetType() == typeof(SplineCurveMesher))
                    {
                        SplineCurveMesher curveMesher = (SplineCurveMesher)component;
                        
                        menu.AddItem(new GUIContent("Clear Scale data"), false, () =>
                        {
                            curveMesher.ResetScaleData();
                            curveMesher.Rebuild();
                            EditorUtility.SetDirty(component);
                        });
                        menu.AddItem(new GUIContent("Clear Roll data"), false, () =>
                        {
                            curveMesher.ResetRollData();
                            curveMesher.Rebuild();
                            EditorUtility.SetDirty(component);
                        });
                        menu.AddItem(new GUIContent("Clear Vertex Color data"), false, () =>
                        {
                            curveMesher.ResetVertexColorData();
                            curveMesher.Rebuild();
                            EditorUtility.SetDirty(component);
                        });
                        menu.AddItem(new GUIContent("Clear Conforming data"), false, () =>
                        {
                            curveMesher.ResetConformingData();
                            curveMesher.Rebuild();
                            EditorUtility.SetDirty(component);
                        });

                        menu.AddSeparator(string.Empty);

                        menu.AddItem(new GUIContent("Detach caps", "Detach the created start/end cap objects"), false, () =>
                        {
                            curveMesher.DetachCaps();
                            EditorUtility.SetDirty(component);
                        });
                    }
                    menu.AddItem(new GUIContent("Force lightmap generation"), false, () => SplineMesherEditor.GenerateLightmapUV(component));
                    //if(SplineMesherEditor.meshLodSupport) 
                        menu.AddItem(new GUIContent("Generate MeshLOD"), false, () => SplineMesherEditor.GenerateLODS(component));
                    
                    menu.ShowAsContext();
                }
                
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(UI.Icons.prefix + "Help").image, "Help window"), EditorStyles.miniButtonMid, GUILayout.Width(30f)))
                {
                    HelpWindow.ShowWindow();
                }
            }
            
            EditorGUILayout.Space(AssetInfo.VersionChecking.UPDATE_AVAILABLE ? 12f : 0f);
            
            if (Unity.Burst.BurstCompiler.IsEnabled == false)
            {
                EditorGUILayout.HelpBox("Burst compilation is disabled, expect performance degradation", MessageType.Warning);
                EditorGUILayout.Separator();
            }
            
            if (isAllowedToRebuild == false)
            {
                EditorGUILayout.HelpBox("\nRebuilding disabled. This object is not a scene instance." +
                                        "\n\nOpen the prefab to edit it.\n", MessageType.Info);
                
                GUILayout.Space(-32);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(
                            new GUIContent("Open", EditorGUIUtility.IconContent("d_tab_next").image), GUILayout.Width(60f), GUILayout.Height(22f)))
                    {
                        SplineMesherEditor.OpenPrefab(component);
                    }

                    GUILayout.Space(8);
                }

                GUILayout.Space(11);
            }
            
            int rebuildTrigger = rebuildTriggers.intValue;
            
            if ((rebuildTrigger & (int)SplineMesher.RebuildTriggers.OnStart) != (int)SplineMesher.RebuildTriggers.OnStart && isPrefab && !SplineMesherSettings.HidePrefabWarning)
            {
                EditorGUILayout.HelpBox("\nProcedurally created geometry cannot be used with prefabs." +
                                        "\n\nMesh data will be lost when the prefab is used outside of the scene it was created in." +
                                        "\n\nExport the created meshes to an FBX file, and use those instead. Or enable the \"On Start()\" option under Rebuild Triggers.\n", MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Don't show again", EditorStyles.miniButton, GUILayout.Width(120f), GUILayout.Height(22f)))
                    {
                        SplineMesherSettings.HidePrefabWarning = true;
                        
                        if(EditorUtility.DisplayDialog("Spline Mesher", "The warning can be restored in your Preferences.", "Show me","OK"))
                        {
                            SplineMesherSettings.OpenPreferences();
                        }
                    }
                }            
            }
            
            EditorGUILayout.Space();
        }

        internal void DrawSplineContainerField(SerializedProperty splineContainer)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(splineContainer);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var m_target in targets)
                    {
                        ((SplineMesher)m_target).RebuildSplineCache();
                    }

                    requiresRebuild = true;
                }
                
                if (splineContainer.objectReferenceValue)
                {
                    var splineToolActive = ToolManager.activeContextType != null && ToolManager.activeContextType == typeof(SplineToolContext);
                    
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Toggle(splineToolActive, new GUIContent("Edit", "Toggle Spline Editor"), "Button", GUILayout.MaxWidth(60f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!splineToolActive)
                        {
                            Selection.activeGameObject = ((SplineMesher)target).SplineContainer.gameObject;
                            EditorApplication.delayCall += ToolManager.SetActiveContext<SplineToolContext>;
                        }
                        else ToolManager.SetActiveContext<GameObjectToolContext>();
                    }
                }
            }

            if (!splineContainer.objectReferenceValue)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox("Assign a Spline Container, or create one here", MessageType.Info);
                
                GUILayout.Space(-32);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(
                            new GUIContent("Create", EditorGUIUtility.IconContent("d_tab_next").image), GUILayout.Width(120f), GUILayout.Height(22f)))
                    {
                        splineContainer.objectReferenceValue = CreateDefaultSpline();
                        
                        foreach (var m_target in targets)
                        {
                            ((SplineMesher)m_target).RebuildSplineCache();
                        }
                        requiresRebuild = true;
                    }

                    GUILayout.Space(8);
                }

                GUILayout.Space(11);
            }
        }
        
        public virtual SplineContainer CreateDefaultSpline()
        {
            return null;
        }
        
        public void PerformMaterialDragAndDrop(ref bool changed)
        {
            Event currentEvent = Event.current;
            
            if(DragAndDrop.objectReferences.Length == 0) return;
            
            List<Material> materials = new List<Material>();
            foreach (Object draggedObject in DragAndDrop.objectReferences)
            {
                if (draggedObject is Material material)
                {
                    materials.Add(material);
                }
            }

            //Only accept dragging materials for this action, otherwise dragging other object types is hindered
            if (materials.Count == 0) return;
            
            Rect activeArea = GUILayoutUtility.GetRect(0, 50f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            GUIContent content = new GUIContent("+ Drag & drop material(s) to assign", EditorGUIUtility.IconContent("Material Icon").image);
            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!activeArea.Contains(currentEvent.mousePosition))
                    {
                        return;
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                    if (currentEvent.type == EventType.DragPerform)
                    {
                        if(DragAndDrop.objectReferences.Length > 0) Undo.RecordObjects(targets, "Drop & Drop material(s) into mesher");
                        
                        foreach (var m_target in targets)
                        {
                            SplineMesher mesher = (SplineMesher)m_target;
                            mesher.AssignMaterials(materials.ToArray());
                            
                            EditorUtility.SetDirty(mesher);
                        }
                        
                        changed = true;
                        
                        if(changed) DragAndDrop.AcceptDrag();
                        
                        DragAndDrop.activeControlID = 0;
                        Event.current.Use();
                    }

                    break;
            }
            
            GUI.Box(activeArea, GUIContent.none, EditorStyles.textArea);
            activeArea.y += 10f;
            activeArea.height = 30f;
            GUI.Label(activeArea, content, EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Separator();
        }

        private static bool _expandMaterialEditor;
        private static bool expandMaterialEditor
        {
            get => SessionState.GetBool("SplineMesherInspector_expandMaterialEditor", true);
            set => SessionState.SetBool("SplineMesherInspector_expandMaterialEditor", value);
        }
        
        // Cache material inspectors so we don't recreate them every repaint
        private static readonly Dictionary<int, MaterialEditor> s_MaterialEditors = new Dictionary<int, MaterialEditor>();
        
        public static void DrawMaterialQuickEditor(SerializedProperty materials)
        {
            var isArray = materials.isArray;

            if (isArray && materials.arraySize == 0)
            {
                EditorGUILayout.PropertyField(materials);
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(materials);
                
                expandMaterialEditor = GUILayout.Toggle(expandMaterialEditor, new GUIContent(expandMaterialEditor ? "▲" : "▼", "Toggle material editor"), "Button", GUILayout.MaxWidth(40f));
            }
            
            if (expandMaterialEditor)
            {
                if (isArray)
                {
                    //Draw a material editor for every material in the property's array
                    int count = materials.arraySize;
                    if (count == 0)
                    {
                        EditorGUILayout.HelpBox("Assign at least one material to edit it here.", MessageType.Info);
                        return;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        var element = materials.GetArrayElementAtIndex(i);
                        var mat = element.objectReferenceValue as Material;

                        if (!mat)
                        {
                            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                            {
                                EditorGUILayout.LabelField($"Element {i}: Missing Material", EditorStyles.miniLabel);
                            }
                            continue;
                        }

                        DrawInlineMaterialEditor(mat);
                        if (i < count - 1) EditorGUILayout.Space();
                    }
                }
                else
                {
                    //Draw a material editor for the single material reference
                    var mat = materials.objectReferenceValue as Material;
                    if (!mat)
                    {
                        EditorGUILayout.HelpBox("Assign a material to edit it here.", MessageType.Info);
                        return;
                    }

                    DrawInlineMaterialEditor(mat);
                }
            }
        }
        
        private static void DrawInlineMaterialEditor(Material mat)
        {
            if (!mat) return;

            void CleanupNullMaterialEditors()
            {
                if (s_MaterialEditors.Count == 0) return;

                // Collect first to avoid modifying dictionary while iterating
                List<int> toRemove = null;
                foreach (var kvp in s_MaterialEditors)
                {
                    if (kvp.Value == null)
                    {
                        toRemove ??= new List<int>(2);
                        toRemove.Add(kvp.Key);
                    }
                }

                if (toRemove == null) return;

                for (int i = 0; i < toRemove.Count; i++)
                {
                    s_MaterialEditors.Remove(toRemove[i]);
                }
            }
            CleanupNullMaterialEditors();

            int id = mat.GetInstanceID();
            s_MaterialEditors.TryGetValue(id, out var materialEditor);

            // Create (or refresh) the cached editor for this material
            Editor cached = materialEditor;
            MaterialEditor.CreateCachedEditor(mat, null, ref cached);
            materialEditor = cached as MaterialEditor;
            s_MaterialEditors[id] = materialEditor;

            if (materialEditor == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                materialEditor.DrawHeader();
                if (materialEditor.isVisible)
                {
                    if (mat.shader.isSupported)
                    {
                        EditorGUI.BeginChangeCheck();
                        materialEditor.PropertiesGUI();
                        if (EditorGUI.EndChangeCheck())
                        {
                            materialEditor.PropertiesChanged();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Material is missing a shader or uses an unsupported one.", MessageType.Error);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        public static void CleanupMaterialEditors()
        {
            foreach (var kvp in s_MaterialEditors)
            {
                if (kvp.Value == null)
                {
                    kvp.Value.OnDisable();
                    s_MaterialEditors.Remove(kvp.Key);
                }
            }
            s_MaterialEditors.Clear();
        }
        
        internal void Recalculate()
        {
            isAllowedToRebuild = component.IsAllowedToRebuild();
            //Debug.Log("Recalulate");
        }

        internal void DrawStats()
        {
            memorySize = Utilities.FormatMemorySize(component.CalculateMemorySize());
            component.CountMeshVertTris(out vertexCount, out triangleCount);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(new GUIContent($" Generation: {component.LastProcessingTime:F}ms", EditorGUIUtility.IconContent("Profiler.GPU").image), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(new GUIContent($"  Vertices: {vertexCount:N0}", EditorGUIUtility.IconContent("d_EditCollider").image), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(new GUIContent($" Triangles: {triangleCount:N0}", EditorGUIUtility.IconContent("d_ProfilerColumn.WarningCount").image), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(new GUIContent($" Size: {memorySize}", EditorGUIUtility.IconContent("Profiler.Memory").image), EditorStyles.miniLabel);
            }
        }
        
        public class Events : UI.Section.SectionEditor
        {
            private SerializedProperty rebuildWith;
            private SerializedProperty onPreRebuild, onPostRebuild;
            private SerializedProperty onCollisionEnter, onCollisionExit;
            private SerializedProperty onTriggerEnter, onTriggerStay, onTriggerExit;

            private bool isFillMesher;
            
            public override void OnEnable()
            {
                SplineMesher component = (SplineMesher)target;
                isFillMesher = component.GetType() == typeof(SplineFillMesher);
                
                rebuildWith = serializedObject.FindProperty("rebuildWith");
                onPreRebuild = serializedObject.FindProperty("onPreRebuild");
                onPostRebuild = serializedObject.FindProperty("onPostRebuild");
                
                onCollisionEnter = serializedObject.FindProperty("onCollisionEnter");
                onCollisionExit = serializedObject.FindProperty("onCollisionExit");
                
                onTriggerEnter = serializedObject.FindProperty("onTriggerEnter");
                onTriggerStay = serializedObject.FindProperty("onTriggerStay");
                onTriggerExit = serializedObject.FindProperty("onTriggerExit");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(rebuildWith);
                if (rebuildWith.isExpanded)
                {
                    EditorGUILayout.HelpBox("Rebuild this instance whenever any of the assigned other Spline Meshers rebuild", MessageType.Info);
                }
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField("Mesh rebuilding", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                {
                    EditorGUILayout.PropertyField(onPreRebuild);
                    EditorGUILayout.PropertyField(onPostRebuild);
                }

                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                {
                    EditorGUILayout.PropertyField(onCollisionEnter);
                    EditorGUILayout.PropertyField(onCollisionExit);
                }

                //Mesh colliders do not support triggers
                if (isFillMesher == false)
                {
                    EditorGUILayout.Separator();

                    EditorGUILayout.LabelField("Physics Triggers", EditorStyles.boldLabel);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                    {
                        EditorGUILayout.PropertyField(onTriggerEnter);
                        EditorGUILayout.PropertyField(onTriggerStay);
                        EditorGUILayout.PropertyField(onTriggerExit);
                    }
                }

                changed |= EditorGUI.EndChangeCheck();
            }
        }
    }
    
    [CustomEditor(typeof(SplineMesher), false)]
    public class SplineMesherBaseInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This is a base class for Spline Mesher components. It is not intended to be used directly.", MessageType.Error);
        }
    }
    
    [CustomEditor(typeof(SplineMeshContainer))]
    public class SplineMeshContainerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
                UI.DrawHeader();
            }
            
            EditorGUILayout.Space();
            
            SplineMeshContainer component = (SplineMeshContainer)target;

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.ObjectField("Owner", component.Owner, typeof(GameObject), true);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel($"Segments:");
                    EditorGUILayout.LabelField(component.SegmentCount.ToString());
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel($"Spline Index:");
                    EditorGUILayout.LabelField(component.SplineIndex.ToString());
                }
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("This component is created to container the various mesh segments for this spline", MessageType.Info);
            
            UI.DrawFooter();
        }
    }
    
    [CustomEditor(typeof(SplineMeshSegment))]
    public class SplineMeshSegmentInspector : Editor
    {
        private SplineMeshSegment component;
        
        private float memorySize;
        private int vertexCount, triangleCount;
        
        void OnEnable()
        {
            component = (SplineMeshSegment)target;

            if (component.mesh) memorySize = Utilities.GetMemorySize(component.mesh);

            (vertexCount, triangleCount) = component.GetMeshStats();
        }
        
        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
                UI.DrawHeader();
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.ObjectField("Container", component.Container, typeof(GameObject), true);
            }
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(new GUIContent($"  Vertices: {vertexCount:N0}", EditorGUIUtility.IconContent("d_EditCollider").image), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(new GUIContent($" Triangles: {triangleCount:N0}", EditorGUIUtility.IconContent("d_ProfilerColumn.WarningCount").image), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(new GUIContent($" Size: {Utilities.FormatMemorySize(memorySize)}", EditorGUIUtility.IconContent("Profiler.Memory").image), EditorStyles.miniLabel);
            }
            
            UI.DrawFooter();
        }
    }
}