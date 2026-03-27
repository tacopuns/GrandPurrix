using System.Reflection;
using sc.splinemesher.pro.runtime;
using sc.splinemesher.pro.editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace sc.splinemesher.pro.editor
{
    public static class SplineCurveSettingsEditor
    {
        public class InputMesh : UI.Section.SectionEditor
        {
            SerializedProperty shape;
            SerializedProperty mesh;
            SerializedProperty cube;
            SerializedProperty cylinder;
            SerializedProperty plane;
            SerializedProperty rotation;
            SerializedProperty alignment;
            SerializedProperty scale;
            SerializedProperty uvTiling;

            private bool inputMeshReadable;
            
            private MeshPreview sourceMeshPreview;
            private PreviewRenderUtility meshPreviewUtility;
            private static bool PreviewMesh
            {
                get => SessionState.GetBool("SM_PREVIEW_MESH", false);
                set => SessionState.SetBool("SM_PREVIEW_MESH", value);
            }

            public override void OnEnable()
            {
                shape = settings.FindPropertyRelative("shape");
                mesh = settings.FindPropertyRelative("mesh");
                cube = settings.FindPropertyRelative("cube");
                cylinder = settings.FindPropertyRelative("cylinder");
                plane = settings.FindPropertyRelative("plane");
                
                rotation = settings.FindPropertyRelative("rotation");
                alignment = settings.FindPropertyRelative("alignment");
                scale = settings.FindPropertyRelative("scale");
                uvTiling = settings.FindPropertyRelative("uvTiling");
                
                CheckInputMeshReadability();
                
                sourceMeshPreview = new MeshPreview(new Mesh());
                
                //Override zoom level
                meshPreviewUtility = (PreviewRenderUtility)typeof(MeshPreview).GetField("m_PreviewUtility", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sourceMeshPreview);
                meshPreviewUtility.camera.fieldOfView = 17;
                meshPreviewUtility.camera.backgroundColor = UnityEngine.Color.white * 0.09f;

            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(shape);

                if (shape.intValue == (int)CurveMeshSettings.Shape.Custom)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(mesh);

                        if (mesh.objectReferenceValue)
                        {
                            PreviewMesh = GUILayout.Toggle(PreviewMesh,
                                new GUIContent(EditorGUIUtility.IconContent(UI.Icons.prefix + (PreviewMesh ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image,
                                    "Toggle mesh inspector"), "Button", GUILayout.MaxWidth(40f));
                        }
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    CheckInputMeshReadability();
                }
                else if (shape.intValue == (int)CurveMeshSettings.Shape.Cube)
                {
                    EditorGUILayout.PropertyField(cube, new GUIContent("Settings"));
                }
                else if (shape.intValue == (int)CurveMeshSettings.Shape.Cylinder)
                {
                    EditorGUILayout.PropertyField(cylinder, new GUIContent("Settings"));
                }
                else if (shape.intValue == (int)CurveMeshSettings.Shape.Plane)
                {
                    EditorGUILayout.PropertyField(plane, new GUIContent("Settings"));
                }
                
                if (inputMeshReadable == false && Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("This mesh is not marked as readable. In a build, it would not be usable.", MessageType.Warning);

                    if (GUILayout.Button("Enable Read/Write option on mesh"))
                    {
                        if(SplineMesherEditor.SetMeshReadWriteFlag(mesh.objectReferenceValue as Mesh))
                            inputMeshReadable = true;
                    }
                }

                if (shape.intValue == (int)CurveMeshSettings.Shape.Custom && mesh.objectReferenceValue && PreviewMesh)
                {
                    Mesh inputMesh = (Mesh)mesh.objectReferenceValue;

                    if (sourceMeshPreview.mesh != inputMesh) sourceMeshPreview.mesh = inputMesh;
                    Rect previewRect = EditorGUILayout.GetControlRect(false, 150f);
                    
                    var previewMouseOver = previewRect.Contains(Event.current.mousePosition);
                    var meshPreviewFocus = previewMouseOver && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag);

                    //EditorGUILayout.LabelField(meshPreviewFocus.ToString());

                    if (meshPreviewFocus)
                    {
                        sourceMeshPreview.OnPreviewGUI(previewRect, GUIStyle.none);
                    }
                    else
                    {
                        if (Event.current.type == EventType.Repaint)
                        {
                            GUI.DrawTexture(previewRect, sourceMeshPreview.RenderStaticPreview((int)previewRect.width, (int)previewRect.height));
                        }
                    }
                    previewRect.y += previewRect.height - 22f;
                    previewRect.x += 5f;
                    previewRect.height = 22f;

                    GUI.Label(previewRect, MeshPreview.GetInfoString(inputMesh), EditorStyles.miniLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        sourceMeshPreview.OnPreviewSettings();
                    }

                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(rotation);

                using (new EditorGUILayout.HorizontalScope())
                {

                    GUILayout.Space(EditorGUIUtility.labelWidth + 20f);

                    EditorGUI.BeginChangeCheck();
                    Vector3 eulerAngles = rotation.vector3Value;

                    if (GUILayout.Button(new GUIContent("-90"), EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(true)))
                    {
                        eulerAngles.x -= 90f;
                    }
                    if (GUILayout.Button(new GUIContent("+90"), EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true)))
                    {
                        eulerAngles.x += 90f;
                    }
                    
                    GUILayout.Space(15f);
                    
                    if (GUILayout.Button(new GUIContent("-90"), EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(true)))
                    {
                        eulerAngles.y -= 90f;
                    }
                    if (GUILayout.Button(new GUIContent("+90"), EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true)))
                    {
                        eulerAngles.y += 90f;
                    }
                    
                    GUILayout.Space(15f);
                    
                    if (GUILayout.Button(new GUIContent("-90"), EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(true)))
                    {
                        eulerAngles.z -= 90f;
                    }
                    if (GUILayout.Button(new GUIContent("+90"), EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true)))
                    {
                        eulerAngles.z += 90f;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        //Validate eulerAngles
                        if(eulerAngles.x >= 360) eulerAngles.x = eulerAngles.x - 360;
                        if(eulerAngles.x <= -360) eulerAngles.x = eulerAngles.x + 360;
                            
                        if(eulerAngles.y >= 360) eulerAngles.y = eulerAngles.y - 360;
                        if(eulerAngles.y <= -360) eulerAngles.y = eulerAngles.y + 360;
                            
                        if(eulerAngles.z >= 360) eulerAngles.z = eulerAngles.z - 360;
                        if(eulerAngles.z <= -360) eulerAngles.z = eulerAngles.z + 360;
                        
                        rotation.vector3Value = eulerAngles;
                    }
                }
                UI.DrawAlignmentSelector(alignment);
                EditorGUILayout.PropertyField(scale);
                EditorGUILayout.PropertyField(uvTiling);
                
                changed |= EditorGUI.EndChangeCheck();
            }
            
            private void CheckInputMeshReadability()
            {
                if ((CurveMeshSettings.Shape)shape.intValue == CurveMeshSettings.Shape.Custom)
                {
                    if (mesh.objectReferenceValue)
                    {
                        inputMeshReadable = SplineMesherEditor.CheckInputMeshReadability(mesh.objectReferenceValue as Mesh);
                        return;
                    }
                }

                //Default
                inputMeshReadable = true;
            }

            public override void OnDisable()
            {
                if (sourceMeshPreview != null)
                {
                    sourceMeshPreview.Dispose();
                    sourceMeshPreview = null;
                }
            }
        }
        
        public class Renderer : UI.Section.SectionEditor
        {
            SerializedProperty materials;
            SerializedProperty shadowCastingMode;
            SerializedProperty lightProbeUsage;
            SerializedProperty reflectionProbeUsage;
            SerializedProperty renderingLayerMask;
            
            public override void OnEnable()
            {
                materials = settings.FindPropertyRelative("materials");
                shadowCastingMode = settings.FindPropertyRelative("shadowCastingMode");
                lightProbeUsage = settings.FindPropertyRelative("lightProbeUsage");
                reflectionProbeUsage = settings.FindPropertyRelative("reflectionProbeUsage");
                renderingLayerMask = settings.FindPropertyRelative("renderingLayerMask");
                
                materials.isExpanded = true;
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                SplineMesherInspector.DrawMaterialQuickEditor(materials);
                
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
                bool missingMaterials = false;
                int materialCount = materials.arraySize;

                if (materialCount == 0) return true;
                
                for (int i = 0; i < materialCount; i++)
                {
                    Material material = materials.GetArrayElementAtIndex(i).objectReferenceValue as Material;

                    if (material == null) missingMaterials = true;
                }

                return missingMaterials;
            }

            public override void OnDisable()
            {
                SplineMesherInspector.CleanupMaterialEditors();
            }
        }
        
        public class Distribution : UI.Section.SectionEditor
        {
            SerializedProperty tiles;
            SerializedProperty autoTileCount;
            SerializedProperty spacing;
            SerializedProperty scaleToFit;
            SerializedProperty evenOnly;
            SerializedProperty knotSnapDistance;
            SerializedProperty trimStart;
            SerializedProperty trimEnd;
            SerializedProperty curveOffset;
            
            public override void OnEnable()
            {
                tiles = settings.FindPropertyRelative("tiles");
                autoTileCount = settings.FindPropertyRelative("autoTileCount");
                spacing = settings.FindPropertyRelative("spacing");
                scaleToFit = settings.FindPropertyRelative("scaleToFit");
                evenOnly = settings.FindPropertyRelative("evenOnly");
                knotSnapDistance = settings.FindPropertyRelative("knotSnapDistance");
                trimStart = settings.FindPropertyRelative("trimStart");
                trimEnd = settings.FindPropertyRelative("trimEnd");
                curveOffset = settings.FindPropertyRelative("curveOffset");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(autoTileCount.boolValue))
                    {
                        EditorGUILayout.PropertyField(tiles,GUILayout.Width(EditorGUIUtility.labelWidth + 60f));
                    }

                    autoTileCount.boolValue = GUILayout.Toggle(autoTileCount.boolValue, new GUIContent(" Auto", autoTileCount.tooltip), "Button", GUILayout.MaxWidth(60f), GUILayout.MaxHeight(19f));
                }
                EditorGUILayout.PropertyField(spacing);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(scaleToFit);
                EditorGUILayout.PropertyField(evenOnly);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField("Curve", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(knotSnapDistance);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(trimStart);
                EditorGUILayout.PropertyField(trimEnd);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(curveOffset);
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }

        public class Scale : UI.Section.SectionEditor
        {
            SerializedProperty scale;
            SerializedProperty scaleOverCurve;
            SerializedProperty pathIndexUnit;
            SerializedProperty interpolation;

            public override void OnEnable()
            {
                scale = settings.FindPropertyRelative("scale");

                SerializedProperty baseSettings = serializedObject.FindProperty("settings");
                scaleOverCurve = baseSettings.FindPropertyRelative("scaleOverCurve");
                
                pathIndexUnit = settings.FindPropertyRelative("pathIndexUnit");
                interpolation = settings.FindPropertyRelative("interpolation");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                SplineCurveMesher component = (SplineCurveMesher)target;
                
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(scale);
                EditorGUILayout.PropertyField(scaleOverCurve, new GUIContent("Over Spline", scaleOverCurve.tooltip));
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    UI.DrawToolToggle<ScaleTool>(UI.Icons.Scale);

                    if (GUILayout.Button(new GUIContent("▼"), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 5f)))
                    {
                        GenericMenu menu = new GenericMenu();

                    
                        menu.AddItem(new GUIContent("Clear Scale data"), false, () =>
                        {
                            component.ResetScaleData();
                            EditorUtility.SetDirty(component);
                        });
                            
                        menu.ShowAsContext();
                    }
                }
                EditorGUILayout.PropertyField(pathIndexUnit, GUILayout.Width(EditorGUIUtility.labelWidth + 180f));
                EditorGUILayout.PropertyField(interpolation, new GUIContent("Interpolation mode"), GUILayout.Width(EditorGUIUtility.labelWidth + 180f));
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }
        
        public class Rotation : UI.Section.SectionEditor
        {
            SerializedProperty align;
            SerializedProperty rollMode;
            SerializedProperty rollFrequency;
            SerializedProperty rollAngle;
            SerializedProperty pathIndexUnit;
            
            public override void OnEnable()
            {
                align = settings.FindPropertyRelative("align");
                rollMode = settings.FindPropertyRelative("rollMode");
                rollFrequency = settings.FindPropertyRelative("rollFrequency");
                rollAngle = settings.FindPropertyRelative("rollAngle");
                pathIndexUnit = settings.FindPropertyRelative("pathIndexUnit");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                SplineCurveMesher component = (SplineCurveMesher)target;
                
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(align, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100f));
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField("Roll", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    UI.DrawToolToggle<RollTool>(UI.Icons.Roll);

                    if (GUILayout.Button(new GUIContent("▼"), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 5f)))
                    {
                        GenericMenu menu = new GenericMenu();
                        
                        menu.AddItem(new GUIContent("Clear Roll data"), false, () =>
                        {
                            component.ResetScaleData();
                            EditorUtility.SetDirty(component);
                        });
                            
                        menu.ShowAsContext();
                    }
                }

                EditorGUILayout.PropertyField(pathIndexUnit, GUILayout.Width(EditorGUIUtility.labelWidth + 180f));
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(rollMode, GUILayout.Width(EditorGUIUtility.labelWidth + 180f));
                EditorGUILayout.PropertyField(rollFrequency, new GUIContent("Frequency", rollFrequency.tooltip));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(rollAngle, new GUIContent("Angle °", rollAngle.tooltip));
                    if (GUILayout.Button(new GUIContent("R", "Reset to 0"), EditorStyles.miniButton, GUILayout.MaxWidth(25f)))
                    {
                        rollAngle.floatValue = 0f;
                    }
                }
                if (rollAngle.floatValue != 0 && component.settings.conforming.enable)
                {
                    EditorGUILayout.HelpBox("The Conforming feature is enabled, which overrides this rotation completely", MessageType.Warning);
                }
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }
        
        public class UV : UI.Section.SectionEditor
        {
            SplineCurveMesher component;
            
            SerializedProperty scale;
            SerializedProperty offset;
            SerializedProperty stretch;
            SerializedProperty flip;
            SerializedProperty rotate;
            
            public override void OnEnable()
            {
                component = (SplineCurveMesher)target;
                
                scale = settings.FindPropertyRelative("scale");
                offset = settings.FindPropertyRelative("offset");
                stretch = settings.FindPropertyRelative("stretch");
                flip = settings.FindPropertyRelative("flip");
                rotate = settings.FindPropertyRelative("rotate");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                component.drawUV = GUILayout.Toggle(component.drawUV, new GUIContent(" Visualize", EditorGUIUtility.IconContent((component.drawUV ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image, 
                    "Toggle UV inspection"), "Button", GUILayout.Width(80f), GUILayout.Height(22f));
                
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(scale);
                EditorGUILayout.PropertyField(offset);
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(stretch,GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100f));
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(flip,GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100f));
                EditorGUILayout.PropertyField(rotate);
                
                changed |= EditorGUI.EndChangeCheck();
            }
        }

        public class Color : UI.Section.SectionEditor
        {
            SerializedProperty retainVertexColors;
            SerializedProperty baseColor;
            SerializedProperty pathIndexUnit;
            SerializedProperty tipGradients;
            
            SerializedProperty tipGradientChannel;
            SerializedProperty tipBlendMode;
            SerializedProperty startGradientOffset;
            SerializedProperty startGradientFalloff;
            SerializedProperty endGradientOffset;
            SerializedProperty endGradientFalloff;
            SerializedProperty invertTipGradient;

            SerializedProperty widthGradients;
            SerializedProperty widthGradientChannel;
            SerializedProperty widthBlendMode;
            
            SerializedProperty widthGradientOffset;
            SerializedProperty widthGradientFalloff;
            SerializedProperty invertWidthGradient;
            

            public override void OnEnable()
            {
                retainVertexColors = settings.FindPropertyRelative("retainVertexColors");
                baseColor = settings.FindPropertyRelative("baseColor");
                pathIndexUnit = settings.FindPropertyRelative("pathIndexUnit");
                
                tipGradients = settings.FindPropertyRelative("tipGradients");
                tipGradientChannel = settings.FindPropertyRelative("tipGradientChannel");
                tipBlendMode = settings.FindPropertyRelative("tipBlendMode");
                startGradientOffset = settings.FindPropertyRelative("startGradientOffset");
                startGradientFalloff = settings.FindPropertyRelative("startGradientFalloff");
                endGradientOffset = settings.FindPropertyRelative("endGradientOffset");
                endGradientFalloff = settings.FindPropertyRelative("endGradientFalloff");
                invertTipGradient = settings.FindPropertyRelative("invertTipGradient");
                
                widthGradients = settings.FindPropertyRelative("widthGradients");
                widthGradientChannel = settings.FindPropertyRelative("widthGradientChannel");
                widthBlendMode = settings.FindPropertyRelative("widthBlendMode");
                widthGradientOffset = settings.FindPropertyRelative("widthGradientOffset");
                widthGradientFalloff = settings.FindPropertyRelative("widthGradientFalloff");
                invertWidthGradient = settings.FindPropertyRelative("invertWidthGradient");
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(retainVertexColors);
                if (retainVertexColors.boolValue == false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(baseColor);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                EditorGUILayout.PropertyField(tipGradients);
                EditorGUILayout.Separator();
                if (tipGradients.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(tipGradientChannel, new GUIContent("Channel", tipGradientChannel.tooltip));
                    EditorGUILayout.PropertyField(tipBlendMode, new GUIContent("Blend", tipBlendMode.tooltip));
                    
                    GUILayout.Space(2f);
                    
                    EditorGUILayout.LabelField("Start", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(startGradientOffset, new GUIContent("Offset", startGradientOffset.tooltip));
                    EditorGUILayout.PropertyField(startGradientFalloff, new GUIContent("Falloff", startGradientFalloff.tooltip));
                    
                    GUILayout.Space(2f);
                    
                    EditorGUILayout.LabelField("End", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(endGradientOffset, new GUIContent("Offset", endGradientOffset.tooltip));
                    EditorGUILayout.PropertyField(endGradientFalloff, new GUIContent("Falloff", endGradientFalloff.tooltip));
                                        
                    EditorGUILayout.Separator();
                    
                    EditorGUILayout.PropertyField(invertTipGradient, new GUIContent("Invert", invertTipGradient.tooltip));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                EditorGUILayout.PropertyField(widthGradients);
                EditorGUILayout.Separator();
                if (widthGradients.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(widthGradientChannel, new GUIContent("Channel", widthGradientChannel.tooltip));
                    EditorGUILayout.PropertyField(widthBlendMode, new GUIContent("Blend", widthBlendMode.tooltip));
                    
                    EditorGUILayout.PropertyField(widthGradientOffset, new GUIContent("Offset", widthGradientOffset.tooltip));
                    EditorGUILayout.PropertyField(widthGradientFalloff, new GUIContent("Falloff", widthGradientFalloff.tooltip));
                    
                    GUILayout.Space(2f);
                    
                    EditorGUILayout.PropertyField(invertWidthGradient, new GUIContent("Invert", invertWidthGradient.tooltip));

                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    UI.DrawToolToggle<VertexColorTool>(UI.Icons.VertexColors);
                    
                    if (GUILayout.Button(new GUIContent("▼"), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 5f)))
                    {
                        GenericMenu menu = new GenericMenu();

                        SplineCurveMesher component = (SplineCurveMesher)target;
                    
                        menu.AddItem(new GUIContent("Clear Colors"), false, () =>
                        {
                            component.ResetVertexColorData();
                            EditorUtility.SetDirty(component);
                        });
                            
                        menu.ShowAsContext();
                    }
                }
                EditorGUILayout.PropertyField(pathIndexUnit);

                changed |= EditorGUI.EndChangeCheck();
                
            }
        }
        
        public class Conforming : UI.Section.SectionEditor
        {
            SerializedProperty enable;
            SerializedProperty direction;
            SerializedProperty pathIndexUnit;
            SerializedProperty seekDistance;
            SerializedProperty heightOffset;
            SerializedProperty skipping;
            SerializedProperty layerMask;
            SerializedProperty align;
            SerializedProperty blendNormal;
            
            SerializedProperty startOffset;
            SerializedProperty startFalloff;
            SerializedProperty endOffset;
            SerializedProperty endFalloff;
            
            public override void OnEnable()
            {
                enable = settings.FindPropertyRelative("enable");
                direction = settings.FindPropertyRelative("direction");
                pathIndexUnit = settings.FindPropertyRelative("pathIndexUnit");
                seekDistance = settings.FindPropertyRelative("seekDistance");
                heightOffset = settings.FindPropertyRelative("heightOffset");
                skipping = settings.FindPropertyRelative("skipping");
                layerMask = settings.FindPropertyRelative("layerMask");
                align = settings.FindPropertyRelative("align");
                blendNormal = settings.FindPropertyRelative("blendNormal");
                
                startOffset = settings.FindPropertyRelative("startOffset");
                startFalloff = settings.FindPropertyRelative("startFalloff");
                endOffset = settings.FindPropertyRelative("endOffset");
                endFalloff = settings.FindPropertyRelative("endFalloff");
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
                    EditorGUILayout.PropertyField(direction);
                    
                    EditorGUILayout.Separator();
                    
                    EditorGUILayout.PropertyField(layerMask);
                    EditorGUILayout.PropertyField(seekDistance);
                    EditorGUILayout.PropertyField(heightOffset);
                    
                    //Disabled until properly worked out
                    //EditorGUILayout.PropertyField(skipping);
                    
                    EditorGUILayout.Separator();
                    
                    EditorGUILayout.PropertyField(align, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100f));
                    EditorGUILayout.PropertyField(blendNormal);
                    
                    EditorGUILayout.Separator();
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        UI.DrawToolToggle<ConformingTool>(UI.Icons.Conforming);
                        
                        if (GUILayout.Button(new GUIContent("▼"), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + 5f)))
                        {
                            GenericMenu menu = new GenericMenu();

                            SplineCurveMesher component = (SplineCurveMesher)target;
                    
                            menu.AddItem(new GUIContent("Clear Conforming Strengths"), false, () =>
                            {
                                component.ResetConformingData();
                                EditorUtility.SetDirty(component);
                            });
                            
                            menu.ShowAsContext();
                        }
                    }
                    EditorGUILayout.PropertyField(pathIndexUnit);
                    
                    EditorGUILayout.Separator();
                    
                    EditorGUILayout.LabelField("Tapering", EditorStyles.boldLabel);
                    
                    EditorGUILayout.LabelField("Start", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(startOffset, new GUIContent("Offset", startOffset.tooltip));
                    EditorGUILayout.PropertyField(startFalloff, new GUIContent("Falloff", startFalloff.tooltip));
                    
                    EditorGUILayout.Separator();
                    
                    EditorGUILayout.LabelField("End", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(endOffset, new GUIContent("Offset", endOffset.tooltip));
                    EditorGUILayout.PropertyField(endFalloff, new GUIContent("Falloff", endFalloff.tooltip));
                }

                changed |= EditorGUI.EndChangeCheck();
            }
        }
        
        public class Collision : UI.Section.SectionEditor
        {
            SerializedProperty enable;
            SerializedProperty colliderOnly;
            SerializedProperty inputMesh;

            private SerializedProperty shape;
            private SerializedProperty mesh;
            private SerializedProperty cube;
            private SerializedProperty cylinder;
            private SerializedProperty plane;
            
            SerializedProperty layer;
            SerializedProperty isKinematic;
            SerializedProperty convex;
            SerializedProperty isTrigger;
            SerializedProperty provideContacts;
            
            private bool inputMeshReadable;
            
            public override void OnEnable()
            {
                enable = settings.FindPropertyRelative("enable");
                colliderOnly = settings.FindPropertyRelative("colliderOnly");
                inputMesh = settings.FindPropertyRelative("inputMesh");
                
                shape = inputMesh.FindPropertyRelative("shape");
                mesh = inputMesh.FindPropertyRelative("mesh");
                cube = inputMesh.FindPropertyRelative("cube");
                cylinder = inputMesh.FindPropertyRelative("cylinder");
                plane = inputMesh.FindPropertyRelative("plane");
                
                layer = settings.FindPropertyRelative("layer");
                isKinematic = settings.FindPropertyRelative("isKinematic");
                convex = settings.FindPropertyRelative("convex");
                isTrigger = settings.FindPropertyRelative("isTrigger");
                provideContacts = settings.FindPropertyRelative("provideContacts");

                CheckInputMeshReadability();
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(enable);
                if (enable.boolValue)
                {
                    EditorGUILayout.Separator();
                    
                    EditorGUILayout.PropertyField(colliderOnly);
                    
                    EditorGUILayout.Separator();
                    

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(shape);
                    if (shape.intValue == (int)CurveMeshSettings.Shape.Custom)
                    {
                        EditorGUILayout.PropertyField(mesh);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        CheckInputMeshReadability();
                    }
                    else if (shape.intValue == (int)CurveMeshSettings.Shape.Cube)
                    {
                        EditorGUILayout.PropertyField(cube, new GUIContent("Settings"));
                    }
                    else if (shape.intValue == (int)CurveMeshSettings.Shape.Cylinder)
                    {
                        EditorGUILayout.PropertyField(cylinder, new GUIContent("Settings"));
                    }
                    else if (shape.intValue == (int)CurveMeshSettings.Shape.Plane)
                    {
                        EditorGUILayout.PropertyField(plane, new GUIContent("Settings"));
                    }
                    
                    if (inputMeshReadable == false && Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("This mesh is not marked as readable. In a build, it would not be usable.", MessageType.Warning);

                        if (GUILayout.Button("Enable Read/Write option on mesh"))
                        {
                            if(SplineMesherEditor.SetMeshReadWriteFlag(mesh.objectReferenceValue as Mesh))
                                inputMeshReadable = true;
                        }
                    }
                    
                    EditorGUILayout.Separator();

                    EditorGUILayout.LabelField("Collider", EditorStyles.boldLabel);
                    UI.DrawLayerDropdown(layer);
                    EditorGUILayout.PropertyField(isKinematic);
                    EditorGUILayout.PropertyField(convex);
                    using (new EditorGUI.DisabledGroupScope(!convex.boolValue))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(isTrigger);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(provideContacts);
                }

                changed |= EditorGUI.EndChangeCheck();
            }
            
            private void CheckInputMeshReadability()
            {
                if ((CurveMeshSettings.Shape)shape.intValue == CurveMeshSettings.Shape.Custom)
                {
                    if (mesh.objectReferenceValue)
                    {
                        inputMeshReadable = SplineMesherEditor.CheckInputMeshReadability(mesh.objectReferenceValue as Mesh);
                        return;
                    }
                }

                //Default
                inputMeshReadable = true;
            }
        }
        
        public class OutputMesh : UI.Section.SectionEditor
        {
            private SplineCurveMesher component;
            
            private SerializedProperty keepReadable;
            private SerializedProperty storeGradientsInUV;
            private SerializedProperty lightmapUVMarginMultiplier;
            private SerializedProperty lightmapUVAngleThreshold;
            private SerializedProperty maxSegmentLength;
            private SerializedProperty maxLodCount;
            private SerializedProperty forceMeshLod;
            private SerializedProperty lodSelectionBias;
            
            public override void OnEnable()
            {
                keepReadable = settings.FindPropertyRelative("keepReadable");
                storeGradientsInUV = settings.FindPropertyRelative("storeGradientsInUV");
                lightmapUVMarginMultiplier = settings.FindPropertyRelative("lightmapUVMarginMultiplier");
                lightmapUVAngleThreshold = settings.FindPropertyRelative("lightmapUVAngleThreshold");
                maxSegmentLength = settings.FindPropertyRelative("maxSegmentLength");
                maxLodCount = settings.FindPropertyRelative("maxLodCount");
                forceMeshLod = settings.FindPropertyRelative("forceMeshLod");
                lodSelectionBias = settings.FindPropertyRelative("lodSelectionBias");
                
                component = (SplineCurveMesher)target;
            }

            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(keepReadable);
                EditorGUILayout.PropertyField(storeGradientsInUV);
                
                EditorGUILayout.Separator();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(maxSegmentLength, GUILayout.Width(EditorGUIUtility.labelWidth + 50f));

                    component.drawSegments = GUILayout.Toggle(component.drawSegments,
                        new GUIContent("", EditorGUIUtility.IconContent((component.drawSegments ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image, "Visualize with colors"), "Button", GUILayout.MaxWidth(30f),
                        GUILayout.MaxHeight(19f));
                }
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField("Lightmap UV", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(lightmapUVMarginMultiplier, new GUIContent("Margin multiplier", lightmapUVMarginMultiplier.tooltip));
                EditorGUILayout.PropertyField(lightmapUVAngleThreshold, new GUIContent("Angle threshold", lightmapUVAngleThreshold.tooltip));
                EditorGUILayout.HelpBox("Lightmap UV will be generated if the object is marked as static when light baking starts.", MessageType.None);
                
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Mesh LOD", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledGroupScope(SplineMesherEditor.meshLodSupport == false))
                {
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
        
        public class Caps : UI.Section.SectionEditor
        {
            SerializedProperty startCap;
            SerializedProperty endCap;

            private static bool LabelCaps
            {
                get => SessionState.GetBool("SplineCurveMesher.Caps.LabelCaps", true);
                set => SessionState.SetBool("SplineCurveMesher.Caps.LabelCaps", value);
            }
            
            public override void OnEnable()
            {
                startCap = settings.FindPropertyRelative("startCap");
                endCap = settings.FindPropertyRelative("endCap");
            }

            void DrawCap(SerializedProperty cap, SplineCurveMesher.Cap.Position position)
            {
                SerializedProperty prefab = cap.FindPropertyRelative("prefab");
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(prefab);
                    if (prefab.objectReferenceValue)
                    {
                        if (GUILayout.Button("X", GUILayout.MaxWidth(30f)))
                        {
                            prefab.objectReferenceValue = null;
                        }
                    }
                    else
                    {
                        //Use prefab from opposite cap
                        if (GUILayout.Button("Same", GUILayout.MaxWidth(50f)))
                        {
                            SplineCurveMesher component = (SplineCurveMesher)target;

                            if (position == SplineCurveMesher.Cap.Position.Start)
                            {
                                prefab.objectReferenceValue = component.settings.caps.endCap.prefab;
                            }
                            else if (position == SplineCurveMesher.Cap.Position.End)
                            {
                                prefab.objectReferenceValue = component.settings.caps.startCap.prefab;
                            }
                            
                            EditorUtility.SetDirty(component);
                        }
                    }
                }

                if (prefab.objectReferenceValue)
                {
                    EditorGUILayout.Separator();

                    EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(cap.FindPropertyRelative("offset"));
                    EditorGUILayout.PropertyField(cap.FindPropertyRelative("shift"));
                            
                    EditorGUILayout.Separator();

                    EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(cap.FindPropertyRelative("align"), GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 100f));
                    EditorGUILayout.PropertyField(cap.FindPropertyRelative("rotation"));
                            
                    EditorGUILayout.Separator();

                    EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(cap.FindPropertyRelative("matchScale"));
                    EditorGUILayout.PropertyField(cap.FindPropertyRelative("scale"));
                            
                    SerializedProperty instances = cap.FindPropertyRelative("instances");
                    EditorGUILayout.LabelField($"Instances: {instances.arraySize}", EditorStyles.miniLabel);
                }
            }
            
            public override void OnInspectorGUI(ref bool changed)
            {
                EditorGUILayout.Space();
                    
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Label in scene view", EditorStyles.miniLabel, GUILayout.MaxWidth(100f));
                    LabelCaps = GUILayout.Toggle(LabelCaps,
                        new GUIContent(EditorGUIUtility.IconContent(UI.Icons.prefix + (LabelCaps ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image,
                            "Identify the caps in the scene view"), "Button", GUILayout.MaxWidth(40f));
                }
                
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("Start", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawCap(startCap, SplineCurveMesher.Cap.Position.Start);
                }
                    
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("End", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawCap(endCap, SplineCurveMesher.Cap.Position.End);
                }
                    
                EditorGUILayout.Separator();
                    
                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                }

            }

            public override void OnSceneGUI()
            {
                if (LabelCaps == false) return;
        
                Handles.BeginGUI();

                Handles.color = UnityEngine.Color.black;

                SplineCurveMesher splineMesher = (SplineCurveMesher)target;

                void DrawCap(SplineCurveMesher.Cap cap)
                {
                    if (cap.prefab)
                    {
                        for (int i = 0; i < cap.instances.Length; i++)
                        {
                            if(cap.instances[i] == null) continue;
                
                            Vector3 position = cap.instances[i].transform.position;

                            UI.SceneView.DrawBoxedLabel(position, cap.position == SplineCurveMesher.Cap.Position.Start ? "Start" : "End");
                        }
                    }
                }

                DrawCap(splineMesher.settings.caps.startCap);
                DrawCap(splineMesher.settings.caps.endCap);
                
                Handles.EndGUI();
            }
        }
    }
}