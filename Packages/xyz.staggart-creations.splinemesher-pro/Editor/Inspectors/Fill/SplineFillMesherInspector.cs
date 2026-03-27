using System;
using System.Collections.Generic;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace sc.splinemesher.pro.editor
{
    [CustomEditor(typeof(SplineFillMesher))]
    [CanEditMultipleObjects]
    public class SplineFillMesherInspector : SplineMesherInspector
    {
        private UI.Section rendererSection;

        new void OnEnable()
        {
            base.OnEnable();

            sections.Add(UI.Section.Create<SplineFillSettingsEditor.Topology>(this, "topology",  new GUIContent("Topology", UI.Icons.Topology), settings.FindPropertyRelative("topology")));
            rendererSection = UI.Section.Create<SplineFillSettingsEditor.Renderer>(this, "renderer", new GUIContent("Renderer", UI.Icons.Renderer), settings.FindPropertyRelative("renderer"));
            sections.Add(rendererSection);
            sections.Add(UI.Section.Create<SplineFillSettingsEditor.Displacement>(this, "displacement",  new GUIContent("Displacement", UI.Icons.Displacement), settings.FindPropertyRelative("displacement")));
            sections.Add(UI.Section.Create<SplineFillSettingsEditor.UV>(this, "uv",  new GUIContent("UV", UI.Icons.UV), settings.FindPropertyRelative("uv")));
            sections.Add(UI.Section.Create<SplineFillSettingsEditor.Conforming>(this, "conforming",  new GUIContent("Conforming", UI.Icons.Conforming), settings.FindPropertyRelative("conforming")));
            sections.Add(UI.Section.Create<SplineFillSettingsEditor.Collision>(this, "collision",  new GUIContent("Collision", UI.Icons.Collision), settings.FindPropertyRelative("collision")));
            sections.Add(UI.Section.Create<SplineFillSettingsEditor.OutputMesh>(this, "output",  new GUIContent("Output", UI.Icons.GameObject), settings.FindPropertyRelative("output")));

            Verify();
            
            SplineFillMesher mesher = (SplineFillMesher)target;
            //mesher.CountMeshVertTris(out vertexCount, out triangleCount);
            
            base.Initialize();
        }
        
        private void Verify()
        {
            SplineFillSettingsEditor.Renderer rendererEditor = (SplineFillSettingsEditor.Renderer)sections[1].editor;
            missingMaterials = rendererEditor.HasMissingMaterials();
        }
        
        public override void OnInspectorGUI()
        {
            DrawHeader();
            
            SplineFillMesher component = (SplineFillMesher)target;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            requiresRebuild = false;

            DrawSplineContainerField(splineContainer);
            
            if (splineContainer.objectReferenceValue || isInspectingPrefab)
            {
                int rebuildTrigger = rebuildTriggers.intValue;
                        
                if ((rebuildTrigger & (int)SplineMesher.RebuildTriggers.OnSplineChanged) == (int)SplineMesher.RebuildTriggers.OnSplineChanged)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(splineChangeTrigger, new GUIContent("Change Trigger", splineChangeTrigger.tooltip), GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 140f));
                    EditorGUI.indentLevel--;
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(rebuildTriggers, new GUIContent("Rebuild triggers", rebuildTriggers.tooltip), GUILayout.Width(EditorGUIUtility.labelWidth + 140f));
                    if (GUILayout.Button(new GUIContent(" Rebuild", EditorGUIUtility.IconContent("d_Refresh").image)))
                    {
                        Rebuild();
                        return;
                    }
                    GUILayout.FlexibleSpace();
                }
                
                if ((rebuildTrigger & (int)SplineMesher.RebuildTriggers.OnTransformChange) == (int)SplineMesher.RebuildTriggers.OnTransformChange)
                {
                    //Check if Gizmos are disabled in the scene-view
                    if (SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.drawGizmos == false)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            foreach (var m_target in targets)
                            {
                                ((SplineMesher)m_target).ListenForTransformChanges();
                            }
                        };
                    } 
                }
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(root);
                    if (GUILayout.Button("This", EditorStyles.miniButton, GUILayout.Width(50f)))
                    {
                        root.objectReferenceValue = component.gameObject;
                        requiresRebuild = true;
                    }
                }
                
                if (missingMaterials)
                {
                    EditorGUILayout.HelpBox("Material is missing or has not been assigned", MessageType.Error);
                }
                
                if (rendererSection.Expanded == false)
                {
                    PerformMaterialDragAndDrop(ref requiresRebuild);
                }

                foreach (UI.Section section in sections)
                {
                    section.DrawHeader(() => SwitchSection(section));
                    EditorGUILayout.BeginFadeGroup(section.anim.faded);
                    {
                        if (section.Expanded)
                        {
                            EditorGUILayout.Space();
                            
                            section.DrawUI(ref requiresRebuild);
                            
                            EditorGUILayout.Space();
                        }
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                
                EditorGUILayout.Space();
                
                DrawStats();
                
                //base.OnInspectorGUI();
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                if (requiresRebuild)
                {
                    if(component.RebuildTriggersEnabled(SplineMesher.RebuildTriggers.OnUIChange))
                    {
                        Rebuild();
                    }
                }

                Verify();
            }
                        
            if (((SplineMesher)target).RebuildTriggersEnabled(SplineMesher.RebuildTriggers.OnUIChange) == false)
            {
                EditorGUILayout.HelpBox("Auto-rebuilding on UI change is disabled (see Rebuilder Triggers)", MessageType.None);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Rebuild now"))
                    {
                        Rebuild();
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            UI.DrawFooter();
        }
        
        private void Rebuild()
        {
            requiresRebuild = false;

            if (SplineMesherSettings.RebuildEveryFrame)
            {
                RebuildTargets();
            }
            else
            {
                EditorApplication.delayCall += RebuildTargets;
            }
            
            Recalculate();
        }
        
        private void RebuildTargets()
        {
            if (isAllowedToRebuild == false) return;
            
            foreach (var m_target in targets)
            {
                SplineFillMesher mesher = (SplineFillMesher)m_target;
                
                mesher.RebuildSplineCache();
                mesher.Rebuild();
                EditorUtility.SetDirty(mesher);
            }
        }
        
        public override SplineContainer CreateDefaultSpline()
        {
            GameObject gameObject = ((SplineFillMesher)target).gameObject;
            SplineContainer container = SplineMesherEditor.AddSplineContainer(gameObject);
            container.Spline = SplineMesherEditor.CreateDefaultFillSpline();

            return container;
        }

        private void SwitchSection(UI.Section targetSection)
        {
            if (SplineMesherSettings.SectionStyleMode == SplineMesherSettings.SectionStyle.Foldouts)
            {
                //Classic foldout behaviour
                targetSection.Expanded = !targetSection.Expanded;
            }
            else
            {
                //Accordion behaviour
                foreach (var section in sections)
                {
                    section.Expanded = (targetSection == section) && !section.Expanded;
                    //section.Expanded = true;
                }
            }
        }
        
        private void OnDisable()
        {
            foreach (var section in sections)
            {
                section.Disable();
            }
        }
    }
}