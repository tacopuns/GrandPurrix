// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using System;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.UIElements;

using Unity.Mathematics;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor.Splines;

namespace sc.splinemesher.pro.editor
{
    [EditorTool("Spline Mesh Scale", typeof(SplineCurveMesher))]
    public class ScaleTool : EditorTool
    {
        private GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;
        private IDrawSelectedHandles drawSelectedHandlesImplementation;
        protected const float k_MinSliderSize = 1.35f;
        private const float k_HandleSize = 0.1f;
        private bool m_DisableHandles = false;

        private ScaleToolUI ui;
        
        public static bool UniformScaling
        {
            get => EditorPrefs.GetBool("SM_SCALE_UNIFORM", true);
            set => EditorPrefs.SetBool("SM_SCALE_UNIFORM", value);
        }
        public static bool GridSnapping
        {
            get => EditorPrefs.GetBool("SM_SCALE_SNAPPING", true);
            set => EditorPrefs.SetBool("SM_SCALE_SNAPPING", value);
        }
        
        void OnEnable()
        {
            name = "Spline Mesh Scale";
            m_IconContent = new GUIContent()
            {
                image = UI.Icons.Scale,
                text = "Scale",
                tooltip = "Adjust the scale of the created spline mesh."
            };
        }
        
        float snapIncrementY, snapIncrementX;

        public override void OnActivated()
        {
            #if UNITY_2022_1_OR_NEWER
            SceneView.AddOverlayToActiveView(ui = new ScaleToolUI());
            #endif
            
            ScaleToolUI.Show = true;
            
            foreach (var m_target in targets)
            {
                SplineCurveMesher modeler = m_target as SplineCurveMesher;

                if (modeler == null || modeler.SplineContainer == null)
                    return;

                modeler.ValidateData();

                for (int i = 0; i < modeler.scaleData.Count; i++)
                {
                    for (int j = 0; j < modeler.scaleData[i].Count; j++)
                    {
                        if (math.length(modeler.scaleData[i][j].Value) < 0.01f)
                        {
                            Debug.LogError($"{modeler.name} has a Scale data point for Spline #{i} with a value of (0,0,0). This creates invalid geometry. It has been reset to (1,1,1)");

                            DataPoint<float3> p = modeler.scaleData[i][j];
                            p.Value = new float3(1f);
                            modeler.scaleData[i][j] = p;

                            EditorUtility.SetDirty(modeler);
                        }
                    }
                }
            }
        }

        public override void OnWillBeDeactivated()
        {
            #if UNITY_2022_1_OR_NEWER
            SceneView.RemoveOverlayFromActiveView(ui);
            #endif
            
            ScaleToolUI.Show = false;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var m_target in targets)
            {
                var modeler = m_target as SplineCurveMesher;
                if (modeler == null || modeler.SplineContainer == null)
                    return;

                base.OnToolGUI(window);

                Handles.color = Color.yellow;
                m_DisableHandles = false;

                var splines = modeler.SplineContainer.Splines;
                for (var splineIndex = 0; splineIndex < splines.Count; splineIndex++)
                {
                    if (splineIndex < modeler.scaleData.Count)
                    {
                        NativeSpline spline = new NativeSpline(splines[splineIndex], modeler.SplineContainer.transform.localToWorldMatrix);

                        Undo.RecordObject(modeler, "Modifying Mesh Scale");

                        int pointCount = modeler.scaleData[splineIndex].Count;
                        for (int i = 0; i < pointCount; i++)
                        {
                            DataPoint<float3> dataPoint = modeler.scaleData[splineIndex][i];
                            var normalizedT = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, modeler.scaleData[splineIndex].PathIndexUnit);
                            spline.Evaluate(normalizedT, out var position, out var tangent, out var up);
                            
                            if (DrawDataPoint(position, tangent, up, dataPoint.Value, out var result))
                            {
                                dataPoint.Value = result;
                                modeler.scaleData[splineIndex][i] = dataPoint;
                    
                                modeler.Rebuild();
                            }
                        }

                        //Using the out-of the box behaviour to manipulate indexes
                        int anchorId = GUIUtility.GetControlID(FocusType.Passive);
                        spline.DataPointHandles(modeler.scaleData[splineIndex], true, splineIndex);

                        int nearestIndex = ControlIdToIndex(anchorId, HandleUtility.nearestControl, pointCount);
                        var hotIndex = ControlIdToIndex(anchorId, GUIUtility.hotControl, pointCount);
                        var tooltipIndex = hotIndex >= 0 ? hotIndex : nearestIndex;
                        if (tooltipIndex >= 0 && tooltipIndex < pointCount-1)
                            DrawTooltip(spline, modeler.scaleData[splineIndex], tooltipIndex);
                        
                        static int ControlIdToIndex(int anchorId, int controlId, int targetCount)
                        {
                            int index = controlId - anchorId - 2;
                            return index >= 0 && index < targetCount ? index : -1;
                        }
                        
                        if (GUI.changed)
                        {
                            modeler.Rebuild();
                        }
                    }
                }
            }
            
            UI.DrawToolInstructions(SceneView.lastActiveSceneView);
        }
        
        private bool DrawDataPoint(Vector3 position, Vector3 tangent, Vector3 up, float3 inValue, out float3 outValue)
        {
            int id = m_DisableHandles ? -1 : GUIUtility.GetControlID(FocusType.Passive);
            int id2 = m_DisableHandles ? -1 : GUIUtility.GetControlID(FocusType.Passive);

            outValue = inValue;
            if (tangent == Vector3.zero)
                return false;

            Event e = Event.current;
            if (e.type == EventType.MouseUp
                && e.button != 0
                && (GUIUtility.hotControl == id || GUIUtility.hotControl == id2))
            {
                e.Use();
                return false;
            }

            var handleColor = Handles.color;
            if ((GUIUtility.hotControl == id || GUIUtility.hotControl == id2))
                handleColor = Handles.selectedColor;
            else if (GUIUtility.hotControl == 0 && (HandleUtility.nearestControl == id || HandleUtility.nearestControl == id2))
                handleColor = Handles.preselectionColor;
            
            up = math.up();
            Vector3 right = math.normalize(math.cross(tangent, up));

            float handleScale = HandleUtility.GetHandleSize(position);

            /*
            Vector3 scale = inValue;
            quaternion rotation = quaternion.LookRotationSafe(tangent, up);

            using (new Handles.DrawingScope(handleColor))
            {
                scale = Handles.ScaleHandle(inValue, position, rotation, k_HandleSize * handleScale * 10);
            }
            
            if (GUIUtility.hotControl == id && math.abs(scale.magnitude - math.length(inValue)) > 0f)
            {
                outValue = scale / handleScale;
                return true;
            }
            */
            
            Vector3 x = position - (right * inValue.x * handleScale);
            Vector3 y = position + (up * inValue.y * handleScale);

            Vector3 width, height;

            snapIncrementX = 0.02f;
            snapIncrementY = 0.02f;

            bool snapping = GridSnapping && EditorSnapSettings.gridSnapEnabled;
            if (snapping)
            {
                snapIncrementX = EditorSnapSettings.gridSize.x;
                snapIncrementY = EditorSnapSettings.gridSize.y;
            }

            using (new Handles.DrawingScope(handleColor))
            {
                Handles.color = Color.red;
                if (Event.current.type == EventType.Repaint)
                {
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 3f, new []{position, x});
                }
                width = Handles.Slider(id, x, right, k_HandleSize * handleScale, CustomHandleCap, snapIncrementX);

                Handles.color = Color.green;
                if (Event.current.type == EventType.Repaint)
                {
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 3f, new []{position, y});
                }
                height = Handles.Slider(id2, y, up, k_HandleSize * handleScale, CustomHandleCap, snapIncrementY);

                if (GUIUtility.hotControl == id)
                {
                    UI.SceneView.DrawLabel(HandleUtility.GUIPointToWorldRay(e.mousePosition).origin, $"X: {Math.Round(inValue.x, 4)}");
                }
                if (GUIUtility.hotControl == id2)
                {
                    UI.SceneView.DrawLabel(HandleUtility.GUIPointToWorldRay(e.mousePosition).origin, $"Y: {Math.Round(inValue.y, 4)}");
                }
            }

            if (GUIUtility.hotControl == id && math.abs(width.x - x.x) > 0f)
            {
                outValue.x = math.distance(width, position) / handleScale;
                
                if (snapping && snapIncrementX > 0f)
                {
                    outValue.x = Mathf.Round(outValue.x / snapIncrementX) * snapIncrementX;
                }
                
                if (UniformScaling) outValue.y = outValue.x;
                
                return true;
            }

            if (GUIUtility.hotControl == id2 && math.abs(height.y - y.y) > 0f)
            {
                outValue.y = math.distance(height, position) / handleScale;
                
                if (snapping && snapIncrementY > 0f)
                {
                    outValue.y = Mathf.Round(outValue.y / snapIncrementY) * snapIncrementY;
                }
                
                if (UniformScaling) outValue.x = outValue.y;

                return true;
            }

            return false;
        }
        
        private void CustomHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (m_DisableHandles) // If disabled, do nothing unless it's a repaint event
            {
                if (Event.current.type == EventType.Repaint)
                    Handles.CubeHandleCap(controlID, position, rotation, size, eventType);
            }
            else
                Handles.CubeHandleCap(controlID, position, rotation, size, eventType);
        }
        
        void DrawTooltip(ISpline spline, SplineData<float3> splineData, int index)
        {
            var dataPoint = splineData[index];
            var text = $"Index: {dataPoint.Index}\n" +
                       $"Scale: {dataPoint.Value:F2}";

            var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);

            UI.SceneView.DrawLabel(spline.EvaluatePosition(t), text);
        }
    }
    
#if UNITY_2022_3_OR_NEWER
    [Overlay(defaultDisplay = true)]
#endif
    public class ScaleToolUI : Overlay, ITransientOverlay
    {
        public static bool Show;
        public bool visible => Show;
        
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            
            this.displayName = "Spline Scale Tool";
            
            Toggle uniformScaling = new Toggle("Uniform scaling")
            {
                value = ScaleTool.UniformScaling,
                tooltip = "Use any of the handles to uniformly scale the data point"
            };
            uniformScaling.RegisterValueChangedCallback(evt => { ScaleTool.UniformScaling = evt.newValue; });
            
            root.Add(uniformScaling);
            
            Toggle snapping = new Toggle("Grid Snapping")
            {
                value = ScaleTool.GridSnapping,
                tooltip = "Use any of the handles to uniformly scale the data point"
            };
            snapping.RegisterValueChangedCallback(evt => { ScaleTool.GridSnapping = evt.newValue; });
            
            root.Add(snapping);

            if (ScaleTool.GridSnapping && EditorSnapSettings.gridSnapEnabled == false)
            {
                Label gridSnapWarning = new Label("Grid snapping is disabled. Enable it in the Editor Settings to use this feature.");
                gridSnapWarning.style.fontSize = 10;
                gridSnapWarning.style.whiteSpace = WhiteSpace.Normal;
                gridSnapWarning.style.maxWidth = 250;
                root.Add(gridSnapWarning);
            }

            Vector2Field gridSizeField = new Vector2Field("Grid Size")
            {
                value = EditorSnapSettings.gridSize,
                tooltip = "Size of the grid snapping increments"
            };
            gridSizeField.RegisterValueChangedCallback(evt => { EditorSnapSettings.gridSize = evt.newValue; });
            gridSizeField.style.minWidth = 275;
            
            root.Add(gridSizeField);
            
            return root;
        }
    }
}