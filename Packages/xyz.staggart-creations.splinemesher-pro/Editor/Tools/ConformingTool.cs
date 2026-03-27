// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.UIElements;

using Unity.Mathematics;
using UnityEditor.Overlays;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEditor.Splines;

namespace sc.splinemesher.pro.editor
{
    [EditorTool("Spline Mesh Conforming", typeof(SplineCurveMesher))]
    sealed class ConformingTool : EditorTool
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;
        
        private bool m_DisableHandles = false;
        private const float SLIDER_WIDTH = 150f;
        
        static readonly Color headerBackgroundDark = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        static readonly Color headerBackgroundLight = new Color(1f, 1f, 1f, 0.9f);
        public static Color headerBackground => EditorGUIUtility.isProSkin ? headerBackgroundDark : headerBackgroundLight;

        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = UI.Icons.Conforming,
                text = "Conforming",
                tooltip = "Adjust the mesh's conforming strength along the spline"
            };
        }
        
        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var m_target in targets)
            {
                var mesher = m_target as SplineCurveMesher;
                if (mesher == null || mesher.SplineContainer == null)
                    return;

                base.OnToolGUI(window);

                Handles.color = Color.yellow;
                
                var splines = mesher.SplineContainer.Splines;
                for (var i = 0; i < splines.Count; i++)
                {
                    if (i < mesher.conformingData.Count)
                    {
                        NativeSpline nativeSpline = mesher.GetNativeSpline(i);

                        Undo.RecordObject(mesher, "Modifying Mesh Conforming");
                        
                        //User defined handles to manipulate width
                        DrawDataPoints(nativeSpline, mesher.conformingData[i]);

                        nativeSpline.DataPointHandles<ISpline, float>(mesher.conformingData[i], true, i);
                    
                        if (GUI.changed)
                        {
                            mesher.Rebuild();
                        }
                    }
                }
            }
            
            UI.DrawToolInstructions(SceneView.lastActiveSceneView);
        }
        
        private bool DrawDataPoints(ISpline spline, SplineData<float> splineData)
        {
            SplineMesher modeler = target as SplineMesher;

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
        
        private bool DrawDataPoint(Vector3 position, Vector3 tangent, Vector3 up, float inValue, out float outValue)
        {
            int id = m_DisableHandles ? -1 : GUIUtility.GetControlID(FocusType.Passive);
            outValue = inValue;
            
            if (tangent == Vector3.zero) return false;

            if (Event.current.type == EventType.MouseUp && Event.current.button != 0 && (GUIUtility.hotControl == id))
            {
                Event.current.Use();
                return false;
            }

            var handleColor = Handles.color;
            if (GUIUtility.hotControl == id)
                handleColor = Handles.selectedColor;
            else if (GUIUtility.hotControl == 0 && (HandleUtility.nearestControl == id))
                handleColor = Handles.preselectionColor;

            var right = math.normalize(math.cross(tangent, up));

            EditorGUI.BeginChangeCheck();
            //if (GUIUtility.hotControl == id)
            {
                using (new Handles.DrawingScope(handleColor))
                {
                    Handles.BeginGUI();

                    Vector2 screenPos = HandleUtility.WorldToGUIPoint(position);
                    Rect bgRect = new Rect(screenPos.x - (SLIDER_WIDTH * 0.5f) - boxPadding, screenPos.y - 52f, SLIDER_WIDTH + boxPadding, 28);
                    EditorGUI.DrawRect(bgRect, headerBackground);
                    
                    Rect sliderRect = new Rect(screenPos.x - (SLIDER_WIDTH * 0.5f), screenPos.y - 50f, SLIDER_WIDTH - boxPadding, 22f);

                    outValue = EditorGUI.Slider(sliderRect, inValue, 0f, 1f);
                    outValue = math.clamp(outValue, 0f, 1f);
    
                    Handles.EndGUI();
                }
            }

            if (inValue != outValue)
            {
                //return true;
            }

            if (EditorGUI.EndChangeCheck()) return true;

            return false;
        }
    }
}