using System;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace sc.splinemesher.pro.editor
{
    public static class MenuActions
    {
        #region Curve Mesher menu additions
        [MenuItem("CONTEXT/MeshFilter/Convert to Spline", true)]
        private static bool AddMesherToMeshFilterValidation(MenuCommand cmd)
        {
            MeshFilter meshFilter = (MeshFilter)cmd.context;
            
            return !meshFilter.GetComponent<SplineCurveMesher>();
        }
        
        [MenuItem("CONTEXT/MeshFilter/Convert to Spline")]
        private static void AddMesherToMeshFilter(MenuCommand cmd)
        {
            MeshFilter meshFilter = (MeshFilter)cmd.context;

            if (meshFilter.sharedMesh == null)
            {
                throw new Exception("Mesh Filter component requires a mesh to convert");
            }
            
            if (EditorUtility.IsPersistent(meshFilter.sharedMesh) == false)
            {
                if (EditorUtility.DisplayDialog("Spline Mesher", "Mesh Filter uses a procedural mesh, it could have already been created from a spline.", "Continue", "Cancel") == false)
                {
                    return;
                }
            }

            SplineCurveMesher component = meshFilter.gameObject.AddComponent<SplineCurveMesher>();
            Undo.RegisterCreatedObjectUndo(component, "Created Spline Mesher");
            
            component.root = meshFilter.transform;
            component.settings.input.mesh = meshFilter.sharedMesh;
            
            MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
            if(renderer) component.AssignMaterials(renderer.sharedMaterials);
            
            if (EditorUtility.DisplayDialog("Convert Mesh to Spline", "Create with a new spline?", "Yes", "No"))
            {
                SplineContainer splineContainer = meshFilter.gameObject.AddComponent<SplineContainer>();
                Undo.RegisterCreatedObjectUndo(splineContainer, "Created Spline Mesher");

                //Spline container will be instantiated with a default spline, so overwrite it
                splineContainer.Spline = SplineMesherEditor.CreateDefaultCurveSpline();
                
                component.SetSplineContainer(splineContainer);

                //Activate the spline editor
                EditorApplication.delayCall += ToolManager.SetActiveContext<SplineToolContext>;
            }
            
            Object.DestroyImmediate(meshFilter);
            if(renderer) Object.DestroyImmediate(renderer);
            
            component.Rebuild();

            if (Application.isPlaying == false) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        
        [MenuItem("GameObject/Spline/Mesh/Curve", false, 0)]
        private static GameObject CreateSplineCurveMesh()
        {
            GameObject parent = Selection.activeGameObject ? Selection.activeGameObject : null;
            GameObject gameObject = new GameObject(GameObjectUtility.GetUniqueNameForSibling(parent ? parent.transform : null, "Spline Curve Mesh"));
            Undo.RegisterCreatedObjectUndo(gameObject, "Created Spline Mesh Object");
            
#if UNITY_2022_3_OR_NEWER
            ObjectFactory.PlaceGameObject(gameObject, parent);
#endif
            
            SplineCurveMesher component = gameObject.AddComponent<SplineCurveMesher>();
            
            component.root = gameObject.transform;

            //SetupSplineRenderer(gameObject);

            if (parent) gameObject.transform.parent = parent.transform;
            
            Selection.activeGameObject = gameObject;

            component.SetSplineContainer(gameObject.GetComponentInParent<SplineContainer>());
            //component.Rebuild();

            return gameObject;
        }
        
        [MenuItem("CONTEXT/SplineContainer/Add Curver Mesher", true, 3000)]
        private static bool AddMesherToSplineValidation(MenuCommand cmd)
        {
            SplineContainer splineContainer = (SplineContainer)cmd.context;
            
            return !splineContainer.GetComponent<SplineCurveMesher>();
        }

        [MenuItem("CONTEXT/SplineContainer/Add Curve Mesher", false, 3000)]
        private static void AddCurveMesherToSpline(MenuCommand cmd)
        {
            SplineContainer splineContainer = (SplineContainer)cmd.context;

            SplineCurveMesher component = splineContainer.GetComponent<SplineCurveMesher>();

            if (component) return;
            
            component = splineContainer.gameObject.AddComponent<SplineCurveMesher>();
            Undo.RegisterCreatedObjectUndo(component, $"Add Spline Mesher to {splineContainer.name}");
            
            component.SetSplineContainer(splineContainer);
            component.root = component.transform;
            component.Rebuild();
            
            EditorUtility.SetDirty(splineContainer.gameObject);
        }
        #endregion
        
        #region Fill mesher menus
        [MenuItem("CONTEXT/SplineContainer/Add Fill Mesher", false, 3000)]
        private static void AddFillMesher(MenuCommand cmd)
        {
            SplineContainer splineContainer = (SplineContainer)cmd.context;
            
            SplineFillMesher component = splineContainer.GetComponent<SplineFillMesher>();
            if (component) return;
            
            component = splineContainer.gameObject.AddComponent<SplineFillMesher>();
            Undo.RegisterCreatedObjectUndo(component, $"Add Spline Mesher to {splineContainer.name}");
            
            component.SetSplineContainer(splineContainer);
            component.root = component.transform;
            component.Rebuild();
            
            EditorUtility.SetDirty(splineContainer.gameObject);
        }
        
        [MenuItem("GameObject/Spline/Mesh/Fill", false, 0)]
        public static GameObject CreateSplineFillMesh()
        {
            GameObject parent = Selection.activeGameObject ? Selection.activeGameObject : null;
            GameObject gameObject = new GameObject(GameObjectUtility.GetUniqueNameForSibling(parent ? parent.transform : null, "Spline Fill Mesh"));
            Undo.RegisterCreatedObjectUndo(gameObject, "Created Spline Mesh Object");
            
#if UNITY_2022_3_OR_NEWER
            ObjectFactory.PlaceGameObject(gameObject, parent);
#endif
            
            SplineFillMesher component = gameObject.AddComponent<SplineFillMesher>();

            SplineContainer splineContainer = gameObject.GetComponentInParent<SplineContainer>();
            
            component.root = gameObject.transform;
            component.SetSplineContainer(splineContainer);
            component.settings.topology.triangleSize = 2;
            
            if (parent) gameObject.transform.parent = parent.transform;
            
            component.SetSplineContainer(gameObject.GetComponentInParent<SplineContainer>());
            
            Selection.activeGameObject = gameObject;
            
            //component.Rebuild();

            return gameObject;
        }
        #endregion
    }
}