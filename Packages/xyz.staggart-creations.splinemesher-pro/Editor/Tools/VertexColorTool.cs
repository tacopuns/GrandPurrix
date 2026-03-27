// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using System;
using System.Collections.Generic;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

using Unity.Mathematics;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor.Splines;

namespace sc.splinemesher.pro.editor
{
    [EditorTool("Spline Mesh Vertex Color", typeof(SplineCurveMesher))]
    public class VertexColorTool : EditorTool
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;

        protected bool m_DisableHandles = false;
        protected const float SLIDER_WIDTH = 150f;
        
        static readonly Color headerBackgroundDark = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        static readonly Color headerBackgroundLight = new Color(1f, 1f, 1f, 0.9f);
        public static Color headerBackground => EditorGUIUtility.isProSkin ? headerBackgroundDark : headerBackgroundLight;
        
        private static Structs.VertexColorChannel targetChannel
        {
            get => (Structs.VertexColorChannel)SessionState.GetInt(PlayerSettings.productName + "_SM_targetChannel", (int)Structs.VertexColorChannel.Red);
            set => SessionState.SetInt(PlayerSettings.productName + "_SM_targetChannel", (int)value);
        }
        
        public enum VisualizationChannel
        {
            None,
            Current,
            All
        }
        private static VisualizationChannel visualizationChannel
        {
            get => (VisualizationChannel)SessionState.GetInt(PlayerSettings.productName + "_SM_visualizationChannel", (int)VisualizationChannel.Current);
            set => SessionState.SetInt(PlayerSettings.productName + "_SM_visualizationChannel", (int)value);
        }
        
        private VertexColorToolUI ui;
        private Material vertexColorMaterial;
        
        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = UI.Icons.VertexColors,
                text = "Vertex Colors",
                tooltip = "Adjust the mesh's vertex color along the spline"
            };
            
            string shaderPath = $"{SplineMesher.kPackageRoot}/Editor/Resources/VisualizeVertexAttributes.shader";
            Shader vertexColorShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);

            if (!vertexColorShader)
            {
                throw new Exception($"[Spline Mesher] Could not locate the vertex color shader at path \"{shaderPath}\", was it deleted or not imported?");
            }
            vertexColorMaterial = new Material(vertexColorShader);
        }

        public override void OnActivated()
        {
            #if UNITY_2022_1_OR_NEWER
            SceneView.AddOverlayToActiveView(ui = new VertexColorToolUI());
            #endif

            VertexColorToolUI.Show = true;
        }

        private Color GetColor()
        {
            switch (targetChannel)
            {
                case Structs.VertexColorChannel.Red: return Color.red;
                case Structs.VertexColorChannel.Green: return Color.green;
                case Structs.VertexColorChannel.Blue: return Color.blue;
                case Structs.VertexColorChannel.Alpha: return Color.white;
                default: return Color.white;
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var m_target in targets)
            {
                SplineCurveMesher splineMesher = (SplineCurveMesher)m_target;
                
                if (splineMesher == null || splineMesher.SplineContainer == null)
                    continue;
                
                if (visualizationChannel != VisualizationChannel.None && vertexColorMaterial)
                {
                    int channel = -1;
                    bool transparent = false;

                    if (visualizationChannel == VisualizationChannel.All)
                    {
                        channel = 4;
                        transparent = true;
                    }
                    else channel = (int)targetChannel;
                    
                    vertexColorMaterial.EnableKeyword("_DISPLAY_COLOR");
                    vertexColorMaterial.SetFloat("_ColorChannel", channel);
                    vertexColorMaterial.SetFloat("_Transparent", transparent ? 1 : 0);
                    vertexColorMaterial.SetPass(0);

                    foreach (var container in splineMesher.Containers)
                    {
                        foreach (var segment in container.Segments)
                        {
                            Graphics.DrawMeshNow(segment.mesh, segment.transform.localToWorldMatrix);
                        }
                    }
                }
                
                var splines = splineMesher.SplineContainer.Splines;

                List<SplineData<SplineCurveMesher.VertexColorChannel>> data = null;

                switch (targetChannel)
                {
                    case Structs.VertexColorChannel.Red: data = splineMesher.vertexColorRedData;
                        break;
                    case Structs.VertexColorChannel.Green: data = splineMesher.vertexColorGreenData;
                        break;
                    case Structs.VertexColorChannel.Blue: data = splineMesher.vertexColorBlueData;
                        break;
                    case Structs.VertexColorChannel.Alpha: data = splineMesher.vertexColorAlphaData;
                        break;
                }

                Handles.color = GetColor();
                
                for (var i = 0; i < splines.Count; i++)
                {
                    if (i < data.Count)
                    {
                        var nativeSpline = splineMesher.GetNativeSpline(i);

                        Undo.RecordObject(splineMesher, "Modifying Spline Mesh vertex color");

                        //User defined handles to manipulate width
                        DrawDataPoints(nativeSpline, data[i]);

                        nativeSpline.DataPointHandles<ISpline, SplineCurveMesher.VertexColorChannel>(data[i], true, i);
                    
                        if (GUI.changed)
                        {
                            splineMesher.Rebuild();
                        }
                    }
                }
            }
            
            UI.DrawToolInstructions(SceneView.lastActiveSceneView);
        }
        
        protected bool DrawDataPoints(ISpline spline, SplineData<SplineCurveMesher.VertexColorChannel> splineData)
        {
            SplineCurveMesher modeler = target as SplineCurveMesher;

            var inUse = false;
            for (int dataFrameIndex = 0; dataFrameIndex < splineData.Count; dataFrameIndex++)
            {
                var dataPoint = splineData[dataFrameIndex];

                var normalizedT = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
                spline.Evaluate(normalizedT, out var position, out var tangent, out var up);

                if (DrawDataPoint(position, tangent, up, dataPoint.Value, out var result))
                {
                    dataPoint.Value = result;
                    splineData[dataFrameIndex] = dataPoint;
                    inUse = true;
                    
                    modeler.Rebuild();
                }
            }
            return inUse;
        }

        private const float boxPadding = 5f;
        
        protected bool DrawDataPoint(Vector3 position, Vector3 tangent, Vector3 up, SplineCurveMesher.VertexColorChannel inValue, out SplineCurveMesher.VertexColorChannel outValue)
        {
            int id = m_DisableHandles ? -1 : GUIUtility.GetControlID(FocusType.Passive);

            outValue = inValue;
            
            if (tangent == Vector3.zero) return false;

            // Properly handle mouse events - consume ALL mouse events when we're the hot control
            if (GUIUtility.hotControl == id && Event.current.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                Event.current.Use();
                return false;
            }

            var handleColor = Handles.color;
            if (GUIUtility.hotControl == id)
                handleColor = Handles.selectedColor;
            else if (GUIUtility.hotControl == 0 && (HandleUtility.nearestControl == id))
                handleColor = Handles.preselectionColor;
            
            EditorGUI.BeginChangeCheck();
            //if (GUIUtility.hotControl == id || (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id))
            {
                using (new Handles.DrawingScope(handleColor))
                {
                    Handles.BeginGUI();

                    Vector2 screenPos = HandleUtility.WorldToGUIPoint(position);
                    Rect bgRect = new Rect(screenPos.x - (SLIDER_WIDTH * 0.5f) - boxPadding, screenPos.y - 80f, SLIDER_WIDTH + boxPadding, 55);
                    EditorGUI.DrawRect(bgRect, headerBackground);
                    
                    Rect sliderRect = new Rect(screenPos.x - (SLIDER_WIDTH * 0.5f), screenPos.y - 50f, SLIDER_WIDTH - boxPadding, 22f);

                    outValue.value = EditorGUI.Slider(sliderRect, inValue.value, -1f, 1f);
                    outValue.value = math.clamp(outValue.value, -1f, 1f);
                    
                    sliderRect.y -= 27f;
                    outValue.blend = EditorGUI.ToggleLeft(sliderRect, new GUIContent("Blend", "Blend the value with the original vertex color value"), inValue.blend);
                    
                    Handles.EndGUI();
                }
            }

            if (EditorGUI.EndChangeCheck()) return true;

            return false;
        }

        public override void OnWillBeDeactivated()
        {
            #if UNITY_2022_1_OR_NEWER
            SceneView.RemoveOverlayFromActiveView(ui);
            #endif
            
            VertexColorToolUI.Show = false;
        }
        
        #if UNITY_2022_1_OR_NEWER
        [Overlay(defaultDisplay = true)]
        #else
        [Overlay(typeof(SceneView), "Spline Mesh Vertex Tool")]
        #endif
        public class VertexColorToolUI : Overlay, ITransientOverlay
        {
            public static bool Show;
            public bool visible => Show;

            private Button redButton, greenButton, blueButton, alphaButton;

            public override void OnCreated()
            {
                redButton = CreateChannelButton(Structs.VertexColorChannel.Red, new Color(1f, 0.2f, 0.2f, 0.3f));
                greenButton = CreateChannelButton(Structs.VertexColorChannel.Green, new Color(0.2f, 1f, 0.2f, 0.3f));
                blueButton = CreateChannelButton(Structs.VertexColorChannel.Blue, new Color(0.2f, 0.2f, 1f, 0.3f));
                alphaButton = CreateChannelButton(Structs.VertexColorChannel.Alpha, default);
            }
            
            // Helper method to create channel button
            Button CreateChannelButton(Structs.VertexColorChannel channel, Color color)
            {
                var button = new Button()
                {
                    text = channel.ToString(),
                    style =
                    {
                        flexGrow = 1,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        backgroundColor = color,
                        marginLeft = 0
                    }
                };
                
                button.clicked += () => 
                { 
                    targetChannel = channel;
                    UpdateButtonStates();
                };
                return button;
            }
            
            //Update button visual states based on selection
            void UpdateButtonStates()
            {
                redButton.style.borderBottomWidth = targetChannel == Structs.VertexColorChannel.Red ? 3 : 0;
                greenButton.style.borderBottomWidth = targetChannel == Structs.VertexColorChannel.Green ? 3 : 0;
                blueButton.style.borderBottomWidth = targetChannel == Structs.VertexColorChannel.Blue ? 3 : 0;
                alphaButton.style.borderBottomWidth = targetChannel == Structs.VertexColorChannel.Alpha ? 3 : 0;
                
                Color selectionColor = new Color(0,0.58f,1,1f);
                redButton.style.borderBottomColor = selectionColor;
                greenButton.style.borderBottomColor = selectionColor;
                blueButton.style.borderBottomColor = selectionColor;
                alphaButton.style.borderBottomColor = selectionColor;
            }


            public override VisualElement CreatePanelContent()
            {
                this.displayName = "Vertex Colors";
                
                var root = new VisualElement();
                
                //Create horizontal toolbar
                var toolbar = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginBottom = 5
                    }
                };

                toolbar.Add(redButton);
                toolbar.Add(greenButton);
                toolbar.Add(blueButton);
                toolbar.Add(alphaButton);
                
                root.Add(toolbar);
                
                UpdateButtonStates();

                EnumField vizChannel = new EnumField("Visualize")
                {
                    value = visualizationChannel,
                    tooltip = "Draw the spline mesh with its vertex color value visualized"
                };
                vizChannel.Init(vizChannel.value);
                vizChannel.RegisterValueChangedCallback(evt => { visualizationChannel = (VisualizationChannel)evt.newValue; } );

                root.Add(vizChannel);

                return root;
            }
        }
    }
}