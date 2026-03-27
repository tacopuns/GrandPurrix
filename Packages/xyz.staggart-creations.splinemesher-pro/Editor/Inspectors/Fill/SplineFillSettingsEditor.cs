using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEngine;

namespace sc.splinemesher.pro.editor
{
    public static class SplineFillSettingsEditor
    {
                
        public class Topology : UI.Section.SectionEditor
        {
            SerializedProperty triangleSize;
            SerializedProperty snapToKnot;
            SerializedProperty margin;
            SerializedProperty accuracy;
            
            public override void OnEnable()
            {
                triangleSize = settings.FindPropertyRelative("triangleSize");
                snapToKnot = settings.FindPropertyRelative("snapToKnot");
                margin = settings.FindPropertyRelative("margin");
                accuracy = settings.FindPropertyRelative("accuracy");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(triangleSize);
                EditorGUILayout.PropertyField(margin);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(snapToKnot);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(accuracy);
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }
        
        public class Renderer : UI.Section.SectionEditor
        {
            SerializedProperty material;
            SerializedProperty shadowCastingMode;
            SerializedProperty lightProbeUsage;
            SerializedProperty reflectionProbeUsage;
            SerializedProperty renderingLayerMask;
            
            public override void OnEnable()
            {
                material = settings.FindPropertyRelative("material");
                shadowCastingMode = settings.FindPropertyRelative("shadowCastingMode");
                lightProbeUsage = settings.FindPropertyRelative("lightProbeUsage");
                reflectionProbeUsage = settings.FindPropertyRelative("reflectionProbeUsage");
                renderingLayerMask = settings.FindPropertyRelative("renderingLayerMask");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                SplineMesherInspector.DrawMaterialQuickEditor(material);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.PropertyField(shadowCastingMode);
                EditorGUILayout.PropertyField(lightProbeUsage);
                EditorGUILayout.PropertyField(reflectionProbeUsage);
                
                EditorGUILayout.Space();

                UI.DrawRenderingLayerMask(renderingLayerMask);
                
                changed |= EditorGUI.EndChangeCheck();
            }

            public bool HasMissingMaterials()
            {
                return material.objectReferenceValue == null;
            }
        }

        public class Displacement : UI.Section.SectionEditor
        {
            private SerializedProperty offset;
            private SerializedProperty bulge;
            private SerializedProperty bulgeFalloff;
            private SerializedProperty flattening;
            private SerializedProperty flatteningFalloff;
            private SerializedProperty noise;
            private SerializedProperty noiseFrequency;
            private SerializedProperty noiseOffset;
            
            public override void OnEnable()
            {
                offset = settings.FindPropertyRelative("offset");
                bulge = settings.FindPropertyRelative("bulge");
                bulgeFalloff = settings.FindPropertyRelative("bulgeFalloff");
                flattening = settings.FindPropertyRelative("flattening");
                flatteningFalloff = settings.FindPropertyRelative("flatteningFalloff");
                noise = settings.FindPropertyRelative("noise");
                noiseFrequency = settings.FindPropertyRelative("noiseFrequency");
                noiseOffset = settings.FindPropertyRelative("noiseOffset");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(offset);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(bulge);
                EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(bulgeFalloff, new GUIContent("Falloff", bulgeFalloff.tooltip));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(flattening);
                EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(flatteningFalloff, new GUIContent("Falloff", flatteningFalloff.tooltip));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(noise);
                EditorGUILayout.PropertyField(noiseFrequency);
                EditorGUILayout.PropertyField(noiseOffset);
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }

        public class UV : UI.Section.SectionEditor
        {
            SplineFillMesher component;
            
            private SerializedProperty fitToMesh;
            private SerializedProperty tiling;
            private SerializedProperty offset;
            private SerializedProperty rotate;
            private SerializedProperty flip;
            
            public override void OnEnable()
            {
                component = (SplineFillMesher)target;
                
                fitToMesh = settings.FindPropertyRelative("fitToMesh");
                tiling = settings.FindPropertyRelative("tiling");
                offset = settings.FindPropertyRelative("offset");
                rotate = settings.FindPropertyRelative("rotate");
                flip = settings.FindPropertyRelative("flip");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                component.drawUV = GUILayout.Toggle(component.drawUV, new GUIContent(" Visualize", EditorGUIUtility.IconContent((component.drawUV ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image, 
                    "Toggle UV inspection"), "Button", GUILayout.Width(80f), GUILayout.Height(22f));

                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(fitToMesh);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(tiling);
                EditorGUILayout.PropertyField(offset);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(rotate);
                EditorGUILayout.PropertyField(flip,GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100f));
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }
        
        public class Conforming : UI.Section.SectionEditor
        {
            private SerializedProperty enable;
            private SerializedProperty layerMask;
            private SerializedProperty seekDistance;
            private SerializedProperty heightOffset;
            
            public override void OnEnable()
            {
                enable = settings.FindPropertyRelative("enable");
                layerMask = settings.FindPropertyRelative("layerMask");
                seekDistance = settings.FindPropertyRelative("seekDistance");
                heightOffset = settings.FindPropertyRelative("heightOffset");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
#if !UNITY_2022_3_OR_NEWER
                EditorGUILayout.HelpBox("This functionality requires Unity 2022.3+", MessageType.Error);
#endif
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(enable);
                
                if (enable.boolValue)
                {
                    EditorGUILayout.PropertyField(layerMask);
                    
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.PropertyField(seekDistance);
                    EditorGUILayout.PropertyField(heightOffset);
                }
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }
        
        public class OutputMesh : UI.Section.SectionEditor
        {
            private SplineFillMesher component;
            
            private SerializedProperty keepReadable;
            private SerializedProperty storeGradientsInUV;
            private SerializedProperty lightmapUVMarginMultiplier;
            private SerializedProperty lightmapUVAngleThreshold;
            private SerializedProperty maxLodCount;
            private SerializedProperty forceMeshLod;
            private SerializedProperty lodSelectionBias;
            
            public override void OnEnable()
            {
                keepReadable = settings.FindPropertyRelative("keepReadable");
                storeGradientsInUV = settings.FindPropertyRelative("storeGradientsInUV");
                lightmapUVMarginMultiplier = settings.FindPropertyRelative("lightmapUVMarginMultiplier");
                lightmapUVAngleThreshold = settings.FindPropertyRelative("lightmapUVAngleThreshold");
                maxLodCount = settings.FindPropertyRelative("maxLodCount");
                forceMeshLod = settings.FindPropertyRelative("forceMeshLod");
                lodSelectionBias = settings.FindPropertyRelative("lodSelectionBias");
                
                component = (SplineFillMesher)target;
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(keepReadable);
                EditorGUILayout.PropertyField(storeGradientsInUV);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField("Lightmap UV", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(lightmapUVMarginMultiplier, new GUIContent("Margin multiplier", lightmapUVMarginMultiplier.tooltip));
                EditorGUILayout.PropertyField(lightmapUVAngleThreshold, new GUIContent("Angle threshold", lightmapUVAngleThreshold.tooltip));
                EditorGUILayout.HelpBox("Lightmap UV will be generated if the object is marked as static when light baking starts.", MessageType.None);
                
                EditorGUILayout.Separator();

                using (new EditorGUI.DisabledGroupScope(SplineMesherEditor.meshLodSupport == false))
                {
                    EditorGUILayout.LabelField("Mesh LOD", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(maxLodCount);
                    
                    EditorGUILayout.Separator();
                    
                    using (new EditorGUI.DisabledGroupScope(maxLodCount.intValue == 0))
                    {
                        int forceLodValue = forceMeshLod.intValue;
                        var overrideLod = forceLodValue >= 0;

                        EditorGUI.BeginChangeCheck();
                        overrideLod = EditorGUILayout.Toggle("Override LOD", overrideLod);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (overrideLod)
                            {
                                forceMeshLod.intValue = 0;
                            }
                            else
                            {
                                forceMeshLod.intValue = -1;
                            }
                        }

                        if (forceMeshLod.intValue >= 0)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(forceMeshLod);
                            EditorGUI.indentLevel--;
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(lodSelectionBias);
                        }
                    }
                }
                
#pragma warning disable CS0162 // Unreachable code detected
                if(SplineMesherEditor.meshLodSupport == false) EditorGUILayout.HelpBox("LOD generation requires Unity 6.2 or newer", MessageType.Info);
                else if (SplineMesherEditor.meshLodSupport && maxLodCount.intValue > 0 &&
                         SplineMesherSettings.GenerateLODs == SplineMesherSettings.BakingMode.Automatic)
                {
                    EditorGUILayout.HelpBox("LODs are generated when the scene is saved", MessageType.Info);

                    if (SplineMesherEditor.RequiresLODGeneration(component))
                    {
                        GUILayout.Space(-32);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(
                                    new GUIContent("Generate LODs now",
                                        EditorGUIUtility.IconContent("d_tab_next").image), GUILayout.Width(160f)))
                            {
                                SplineMesherEditor.GenerateLODS(component);
                                changed = false;
                                GUI.changed = false;
                            }

                            GUILayout.Space(8);
                        }

                        GUILayout.Space(11);
                    }
                }
#pragma warning restore CS0162

                changed |= EditorGUI.EndChangeCheck();
            }
        }
        
        public class Collision : UI.Section.SectionEditor
        {
            private SerializedProperty enable;
            private SerializedProperty colliderOnly;
            private SerializedProperty layer;
            
            public override void OnEnable()
            {
                enable = settings.FindPropertyRelative("enable");
                colliderOnly = settings.FindPropertyRelative("colliderOnly");
                layer = settings.FindPropertyRelative("layer");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(enable);
                if (enable.boolValue)
                {
                    EditorGUILayout.PropertyField(colliderOnly);
                    UI.DrawLayerDropdown(layer);
                }

                changed |= EditorGUI.EndChangeCheck();
            }
        }
    }
}