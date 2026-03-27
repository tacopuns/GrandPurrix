using System;
using System.Collections.Generic;
using UnityEditor;
using sc.splinemesher.pro.runtime;
using UnityEngine;
using UnityEngine.Splines;

namespace sc.splinemesher.pro.editor
{
    [CustomEditor(typeof(SplineCurveMesher))]
    public class SplineCurveMesherInspector : SplineMesherInspector
    {
        private UI.Section rendererSection;
        
        new void OnEnable()
        {
            base.OnEnable();
            
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.InputMesh>(this, "InputMesh",  new GUIContent("Input Mesh", UI.Icons.Mesh), settings.FindPropertyRelative("input")));
            rendererSection = UI.Section.Create<SplineCurveSettingsEditor.Renderer>(this, "renderer", new GUIContent("Renderer", UI.Icons.Renderer), settings.FindPropertyRelative("renderer"));
            sections.Add(rendererSection);
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.Distribution>(this, "Distribution",  new GUIContent("Distribution", UI.Icons.Distribution), settings.FindPropertyRelative("distribution")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.Scale>(this, "Scale",  new GUIContent("Scale", UI.Icons.Scale), settings.FindPropertyRelative("scale")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.Rotation>(this, "Rotation",  new GUIContent("Rotation", UI.Icons.Roll), settings.FindPropertyRelative("rotation")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.UV>(this, "UV",  new GUIContent("UV", UI.Icons.UV), settings.FindPropertyRelative("uv")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.Color>(this, "Color",  new GUIContent("Vertex Colors", UI.Icons.VertexColors), settings.FindPropertyRelative("color")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.Conforming>(this, "Conforming",  new GUIContent("Conforming", UI.Icons.Conforming), settings.FindPropertyRelative("conforming")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.Collision>(this, "Collision",  new GUIContent("Collision", UI.Icons.Collision), settings.FindPropertyRelative("collision")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.OutputMesh>(this, "OutputMesh",  new GUIContent("Output", UI.Icons.GameObject), settings.FindPropertyRelative("output")));
            sections.Add(UI.Section.Create<SplineCurveSettingsEditor.Caps>(this, "Caps",  new GUIContent("Caps", UI.Icons.Cap), settings.FindPropertyRelative("caps")));

            Verify();
            
            base.Initialize();
        }

        private void Verify()
        {
            SplineCurveSettingsEditor.Renderer rendererEditor = (SplineCurveSettingsEditor.Renderer)sections[1].editor;
            missingMaterials = rendererEditor.HasMissingMaterials();
        }
        
        public override void OnInspectorGUI()
        {
            DrawHeader();
            
            SplineCurveMesher component = (SplineCurveMesher)target;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            requiresRebuild = false;
            //base.OnInspectorGUI();
            
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
                    if (GUILayout.Button("This", EditorStyles.miniButton, GUILayout.Width(60f)))
                    {
                        root.objectReferenceValue = component.gameObject;
                        requiresRebuild = true;
                    }
                }
                
                if (missingMaterials)
                {
                    EditorGUILayout.HelpBox("One or materials are missing or have not been assigned", MessageType.Error);
                    //SwitchSection(sections[1]);
                }

                if (rendererSection.Expanded == false)
                {
                    PerformMaterialDragAndDrop(ref requiresRebuild);
                }
                
                EditorGUILayout.Separator();
                
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
                    Rebuild();
                }

                Verify();
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
                SplineCurveMesher mesher = (SplineCurveMesher)m_target;
                
                mesher.RebuildSplineCache();
                mesher.Rebuild();
                EditorUtility.SetDirty(mesher);
            }
        }

        public override SplineContainer CreateDefaultSpline()
        {
            GameObject gameObject = ((SplineCurveMesher)target).gameObject;
            SplineContainer container = SplineMesherEditor.AddSplineContainer(gameObject);
            container.Spline = SplineMesherEditor.CreateDefaultCurveSpline();

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

        private void OnSceneGUI()
        {
            foreach (UI.Section section in sections)
            {
                if (section.Expanded)
                {
                    section.DrawSceneGUI();
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