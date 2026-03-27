// Spline Mesher Pro © Staggart Creations (http://staggart.xyz)
// COPYRIGHT PROTECTED UNDER THE UNITY ASSET STORE EULA (https://unity.com/legal/as-terms)
//
// ⚠️ WARNING: UNAUTHORIZED USE OR DISTRIBUTION IS STRICTLY PROHIBITED
// • Copying, referencing, or reverse-engineering this source code for the creation of new Asset Store or derivative products,
//   or any other publicly distributed content is strictly forbidden and will result in legal action.
// • Studying this file for the purpose of reproducing its functionality in your own assets or tools is not permitted.
// • If you are viewing this file as a reference, please close it immediately to avoid unintentional design influence or potential EULA violations.
// • Uploading this file or any derivative of it to a public GitHub or similar repository will trigger an automated DMCA takedown request.
// • Studying to understand for personal, educational or integration purposes is allowed, studying to reproduce is not.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Splines;

namespace sc.splinemesher.pro.runtime
{
    public partial class SplineMesher
    {
        [SerializeField]
        protected SplineContainer splineContainer;
        /// <summary>
        /// Geometry will be created from splines within this container. To change the container, use <see cref="SetSplineContainer()"/>
        /// </summary>
        public SplineContainer SplineContainer => splineContainer;
        [SerializeField] [HideInInspector]
        internal int splineCount = -1; //Change tracking
        
        //Creating a NativeSpline is costly, and isn't necessary if only spawning parameters are changed
        //Hence they are cached and rebuild when they change.
        internal readonly List<NativeSpline> nativeSplines = new List<NativeSpline>();
        
        public enum SplineChangeTrigger
        {
            [InspectorName("During Changes")]
            During,
            [InspectorName("After Changes")]
            WhenDone,
        }
        [Tooltip("Determines when a change to the spline should be detected. Using the After Changes option for complex set ups to improve performance.")]
        public SplineChangeTrigger splineChangeTrigger = SplineChangeTrigger.During;
        
        [Min(50)]
        [Tooltip("Time in milliseconds. If multiple changes to the spline are made within this time frame, no behaviour is triggered.")]
        public float delayTime = 100f; //In milliseconds

        private void SubscribeSplineCallbacks()
        {
            SplineContainer.SplineAdded += OnSplineAdded;
            SplineContainer.SplineRemoved += OnSplineRemoved;
            Spline.Changed += OnSplineChanged;
        }
        
        private void UnsubscribeSplineCallbacks()
        {
            SplineContainer.SplineAdded -= OnSplineAdded;
            SplineContainer.SplineRemoved -= OnSplineRemoved;
            Spline.Changed -= OnSplineChanged;
        }
        
         private float lastChangeTime = -1f;
        private bool isTrackingChanges = false;
        private Spline lastEditedSpline;
        private int lastEditedSplineIndex = -1;
        
        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (!splineContainer) return;
            
            if (RebuildTriggersEnabled(RebuildTriggers.OnSplineChanged) == false) return;
            
            //Spline belongs to the assigned container?
            var splineIndex = Array.IndexOf(splineContainer.Splines.ToArray(), spline);
            if (splineIndex < 0)
                return;

            splineCount = splineContainer.Splines.Count;
            
            lastEditedSpline = spline;
            lastEditedSplineIndex = splineIndex;

            if (splineChangeTrigger == SplineChangeTrigger.WhenDone)
            {
                lastChangeTime = Time.realtimeSinceStartup;

                if (Application.isPlaying)
                {
                    //Coroutines only work in play mode and builds
                    
                    //Cancel any existing debounce coroutine
                    if (debounceCoroutine != null) StopCoroutine(debounceCoroutine);
                
                    debounceCoroutine = StartCoroutine(DebounceCoroutine());
                }
                else
                {
                    if (!isTrackingChanges)
                    {
                        isTrackingChanges = true;
                        
                        #if UNITY_EDITOR
                        UnityEditor.EditorApplication.update += EditorUpdate;
                        #endif
                    }
                    
                }
            }
            else if (splineChangeTrigger == SplineChangeTrigger.During)
            {
                UpdateSpline(spline, splineIndex);
                Rebuild(splineIndex);
            }
        }
        
        private Coroutine debounceCoroutine;
        private IEnumerator DebounceCoroutine()
        {
            yield return new WaitForSeconds((delayTime * 0.001f));
            
            ExecuteAfterSplineChanges();
        }
        
        private void EditorUpdate()
        {
            if (isTrackingChanges && Time.realtimeSinceStartup - lastChangeTime >= (delayTime * 0.001f))
            {
                ExecuteAfterSplineChanges();
                
                isTrackingChanges = false;
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= EditorUpdate;
                #endif
            }
        }
        
        private void ExecuteAfterSplineChanges()
        {
            if(lastEditedSplineIndex < 0) return;
            
            UpdateSpline(lastEditedSpline, lastEditedSplineIndex);
            Rebuild();

            //UpdateCaps();
        }

        private void OnSplineAdded(SplineContainer container, int index)
        {
            if (RebuildTriggersEnabled(RebuildTriggers.OnSplineAdded) == false) return;
            
            if (container != splineContainer) return;

            splineCount = splineContainer.Splines.Count;

            containers.Add(SplineMeshContainer.Create(this, index));
            
            CacheSpline(container.Splines[index]);
            
            //Causes issues with SendMessage. Adding splines must never be done from an OnValidate function
            Rebuild();
        }
        
        private void OnSplineRemoved(SplineContainer container, int index)
        {
            if (RebuildTriggersEnabled(RebuildTriggers.OnSplineRemoved) == false) return;
            
            if (container != splineContainer) return;
            
            splineCount = splineContainer.Splines.Count;
            
            if (index <= nativeSplines.Count)
            {
                RemoveSpline(index);

                if (index < containers.Count && index >= 0)
                {
                    //Debug.Log($"Spline removed at index {index}. Mesh count: {meshSegments.Count}");
                    SplineMeshContainer meshContainer = containers[index];
                    meshContainer.Destroy();
                    
                    containers.RemoveAt(index);
                }
                else
                {
                    //throw new Exception($"Error when removing Spline #{index} from {targetContainer.name}. Index out of range ({containers.Count} containers).");
                }
            }
            
            Rebuild();
        }
        
        #region Caching
        /// <summary>
        /// Spline are converted to native arrays for fast parallel read access. The conversion process is fairly slow, use this function to perform it for all splines within the assigned container.
        /// Doing so before rebuilding makes the first rebuilding job faster
        /// </summary>
        public void WarmupSplineCache()
        {
            RebuildSplineCache();
        }
        
        /// <summary>
        /// Sets the source spline container and (optionally) forces the cache to be rebuilt.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="forceCacheRebuild">Force the cache for all the splines to be rebuilt</param>
        public void SetSplineContainer(SplineContainer container, bool forceCacheRebuild = true)
        {
            this.splineContainer = container;
            if(forceCacheRebuild) RebuildSplineCache();
        }
        
        /// <summary>
        /// Disposes and rebuilds the cached spline data
        /// </summary>
        [ContextMenu("Rebuild Spline Cache")]
        public void RebuildSplineCache()
        {
            if (!splineContainer)
            {
                splineCount = 0;
                DisposeSplineCache();
                return;
            }
            
            //When first adding the component, ensure count is updated
            splineCount = splineContainer.Splines.Count;
            
            #if ENABLE_PROFILER
            Profiler.BeginSample($"[Spline Mesher] Rebuild Spline Cache (x{splineCount})");
            #endif

            DisposeSplineCache();
            
            foreach (var spline in splineContainer.Splines)
            {
                CacheSpline(spline);
            }
            
            #if ENABLE_PROFILER
            Profiler.EndSample();
            #endif
        }
        
        private void DisposeSplineCache()
        {
            if (nativeSplines != null && nativeSplines.Count > 0)
            {
                foreach (var nativeSpline in nativeSplines)
                {
                    if(nativeSpline.Curves.IsCreated) nativeSpline.Dispose();
                }

                nativeSplines.Clear();
            }
        }

        private void RemoveSpline(int index)
        {
            if (index >= nativeSplines.Count) return;
            
            nativeSplines[index].Dispose();
            nativeSplines.RemoveAt(index);
        }

        private NativeSpline CreateNativeSpline(ISpline spline)
        {
            return new NativeSpline(spline, spline.Closed, splineContainer.transform.localToWorldMatrix, true, Allocator.Persistent);
        }
        
        private void UpdateSpline(Spline spline, int index)
        {
            if (index >= nativeSplines.Count)
            {
                CacheSpline(spline);
                return;
            }
            
            nativeSplines[index].Dispose();
            nativeSplines[index] = CreateNativeSpline(spline);
        }
        
        private void CacheSpline(Spline spline)
        {
            NativeSpline nativeSpline = CreateNativeSpline(spline);
            nativeSplines.Add(nativeSpline);
        }

        public NativeSpline GetNativeSpline(int index)
        {
            if (index < 0) return default;

            if (index >= nativeSplines.Count)
            {
                if (index <= splineCount)
                {
                    CacheSpline(splineContainer.Splines[index]);
                }
            }
            
            return nativeSplines[index];
        }
        #endregion
    }
}