// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using sc.splinemesher.pro.editor;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

using Unity.Mathematics;
using UnityEditor.Overlays;

namespace sc.splinemesher.pro.editor
{
    [EditorTool("Spline Mesh Roll", typeof(SplineCurveMesher))]
    sealed class RollTool : EditorTool
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;
        
        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = UI.Icons.Roll,
                text = "Roll",
                tooltip = "Adjust the mesh's roll along the spline"
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
                    if (i < mesher.rollData.Count)
                    {
                        NativeSpline nativeSpline = mesher.GetNativeSpline(i);

                        Undo.RecordObject(mesher, "Modifying Mesh Roll");

                        SplineData<float> splineData = mesher.rollData[i];
                        
                        int changedIndex = DrawIndexPointHandles(nativeSpline, splineData);
                        if (changedIndex >= 0)
                        {
                            mesher.Rebuild();
                        }

                        changedIndex = DrawDataPointHandles(nativeSpline, splineData);
                        if (changedIndex >= 0)
                        {
                            mesher.Rebuild();
                        }

                        if (GUI.changed)
                        {
                            mesher.Rebuild();
                        }
                    }
                }
            }
            
            UI.DrawToolInstructions(SceneView.lastActiveSceneView);
        }

        int DrawIndexPointHandles(NativeSpline spline, SplineData<float> splineData)
        {
            int anchorId = GUIUtility.GetControlID(FocusType.Passive);
            spline.DataPointHandles(splineData);
            int nearestIndex = ControlIdToIndex(anchorId, HandleUtility.nearestControl, splineData.Count);
            var hotIndex = ControlIdToIndex(anchorId, GUIUtility.hotControl, splineData.Count);
            var tooltipIndex = hotIndex >= 0 ? hotIndex : nearestIndex;
            if (tooltipIndex >= 0)
                DrawTooltip(spline, splineData, tooltipIndex);

            // Return the index that's being changed, or -1
            return hotIndex;

            // Local function
            static int ControlIdToIndex(int anchorId, int controlId, int targetCount)
            {
                int index = controlId - anchorId - 2;
                return index >= 0 && index < targetCount ? index : -1;
            }
        }

        // inverse pre-calculation optimization
        readonly Quaternion m_DefaultHandleOrientation = Quaternion.Euler(270, 0, 0);
        readonly Quaternion m_DefaultHandleOrientationInverse = Quaternion.Euler(90, 0, 0);

        int DrawDataPointHandles(NativeSpline spline, SplineData<float> splineData)
        {
            int changed = -1;
            int tooltipIndex = -1;
            for (var i = 0; i < splineData.Count; ++i)
            {
                var dataPoint = splineData[i];
                var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
                spline.Evaluate(t, out var position, out var tangent, out var up);

                var id = GUIUtility.GetControlID(FocusType.Passive);
                if (DrawDataPoint(id, position, tangent, up, dataPoint.Value, out var result))
                {
                    dataPoint.Value = result;
                    splineData.SetDataPoint(i, dataPoint);
                    changed = i;
                }

                //Actively altering the roll
                if (GUIUtility.hotControl == id)
                {
                    UI.SceneView.DrawLabel(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin, $"X: {System.Math.Round(dataPoint.Value, 4)}", -0.1f);
                }
                else
                {
                    if (tooltipIndex < 0 && id == HandleUtility.nearestControl || id == GUIUtility.hotControl)
                        tooltipIndex = i;
                }
            }
            if (tooltipIndex >= 0)
                DrawTooltip(spline, splineData, tooltipIndex);
            return changed;

            // local function
            bool DrawDataPoint(int controlID, Vector3 position, Vector3 tangent, Vector3 up, float rollData, out float result)
            {
                result = 0;
                if (tangent == Vector3.zero)
                    return false;

                var drawMatrix = Handles.matrix * Matrix4x4.TRS(position, Quaternion.LookRotation(tangent, up), Vector3.one);
                using (new Handles.DrawingScope(drawMatrix)) // use draw matrix, so we work in local space
                {
                    var localRot = Quaternion.Euler(0, rollData, 0);
                    var globalRot = m_DefaultHandleOrientation * localRot;

                    var handleSize = HandleUtility.GetHandleSize(Vector3.zero) / 2f;
                    if (Event.current.type == EventType.Repaint)
                        Handles.ArrowHandleCap(-1, Vector3.zero, globalRot, handleSize, EventType.Repaint);

                    var newGlobalRot = Handles.Disc(controlID, globalRot, Vector3.zero, Vector3.forward, handleSize, false, 0);
                    if (GUIUtility.hotControl == controlID)
                    {
                        // Handles.Disc returns roll values in the [0, 360] range. Therefore, it works only in fixed ranges
                        // For example, within any of these ..., [-720, -360], [-360, 0], [0, 360], [360, 720], ...
                        // But we want to be able to rotate through these ranges, and not get stuck. We can detect when to
                        // move between ranges: when the roll delta is big. e.g. 359 -> 1 (358), instead of 1 -> 2 (1)
                        var newLocalRot = m_DefaultHandleOrientationInverse * newGlobalRot;
                        var deltaRoll = newLocalRot.eulerAngles.y - localRot.eulerAngles.y;
                        if (deltaRoll > 180)
                            deltaRoll -= 360; // Roll down one range
                        else if (deltaRoll < -180)
                            deltaRoll += 360; // Roll up one range

                        rollData += deltaRoll;
                        
                        result = rollData;
                        return true;
                    }
                }
                return false;
            }
        }
        
        void DrawTooltip(ISpline spline, SplineData<float> splineData, int index)
        {
            var dataPoint = splineData[index];
            var text = $"Index: {dataPoint.Index}\nRoll: {dataPoint.Value}\u00b0";

            var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);

            UI.SceneView.DrawLabel(spline.EvaluatePosition(t), text);
        }
    }
}