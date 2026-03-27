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
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sc.splinemesher.pro.runtime
{
    public partial class SplineCurveMesher
    {
        [Serializable]
        public class Cap
        {
            public Cap(Position position)
            {
                this.position = position;
            }
            
            public enum Position
            {
                Start,
                End
            }
            public readonly Position position;
            
            [Tooltip("The source object to use. An instance of this will be spawned. It may be destroyed and recreated under certain conditions, so manual changes may be lost.")]
            public GameObject prefab;
            [SerializeField]
            //Solely used for reliable change tracking
            private GameObject previousPrefab;

            public bool HasPrefabChanged()
            {
                if (!prefab) return true;
                
                if (prefab != previousPrefab)
                {
                    //Debug.Log($"Prefab changed on {position} cap.");
                    previousPrefab = prefab;
                    
                    return true;
                }
                
                return false;
            }
            
            [Tooltip("Positional offset, relative to the curve's tangent")]
            public Vector3 offset;
            [Tooltip("Shifts the object along the spline curve by this many units")]
            [Min(0f)]
            public float shift = 0f;
            
            [Tooltip("Align the object's forward direction to the tangent and roll of the spline")]
            public bool3 align = true;
            [Tooltip("Rotation in degrees, added to the object's rotation")]
            public Vector3 rotation;
            
            [Tooltip("Factor in the scale configured under the Scale section, as well as scale data points created in the editor.")]
            public bool matchScale = true;
            public Vector3 scale = Vector3.one;
            
            //Save a reference to the instantiated objects, so they can be accessed again, deleted when necessary.
            //[HideInInspector]
            public GameObject[] instances = Array.Empty<GameObject>();
            public int InstanceCount => instances.Length;
            
            public bool RequiresRespawn()
            {
                return HasNoInstances() || HasPrefabChanged() || HasMissingInstances();
            }
            
            public bool HasNoInstances()
            {
                return InstanceCount == 0;
            }
            
            //User may delete the instances in the hierarchy
            public bool HasMissingInstances()
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    if (!instances[i]) return true;
                }
                return false;
            }

            public void DestroyInstances()
            {
                //Destroy any existing instances
                for (int i = 0; i < instances.Length; i++)
                {
                    if (instances[i]) DestroyInstance(instances[i]);
                }
            }
            
            private static void DestroyInstance(Object obj)
            {
                #if UNITY_EDITOR
                if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
                #else
                Destroy(obj);
                #endif
            }
            
            public void Respawn(int splineCount, Transform parent)
            {
                DestroyInstances();
            
                //Nothing to spawn, clear out
                if (!prefab)
                {
                    //Debug.Log("Cap has no prefab, clearing and bailing");
                    instances = Array.Empty<GameObject>();
                    return;
                }

                //One cap gets spawned per spline
                instances = new GameObject[splineCount];

                //Respawn the prefabs (once per spline)
                for (int i = 0; i < instances.Length; i++)
                {
                    GameObject instance = InstantiatePrefab(prefab);
                    instance.transform.SetParent(parent);
                
                    instances[i] = instance;
                    previousPrefab = prefab;
                }
            }
            
            private GameObject InstantiatePrefab(Object source)
            {
                bool isPrefab = false;
            
                #if UNITY_EDITOR
                if (UnityEditor.PrefabUtility.GetPrefabAssetType(source) == PrefabAssetType.Variant)
                {
                    //PrefabUtility.GetCorrespondingObjectFromSource still returns the base prefab. However, this does work.
                    isPrefab = source;
                }
                else
                {
                    Object original = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(source);

                    isPrefab = original;

                    //This is necessary if the source if a prefab already instantiate in the scene
                    if (isPrefab) source = original;
                }
                #endif
                
                GameObject instance = null;
            
                #if UNITY_EDITOR
                if (isPrefab)
                {
                    instance = UnityEditor.PrefabUtility.InstantiatePrefab(source) as GameObject;
                }
                #endif

                //Non-prefabs and builds
                if (!isPrefab)
                {
                    instance = GameObject.Instantiate(source) as GameObject;
                }

                if (instance == null)
                {
                    Debug.LogError($"Failed to spawn cap instance. Was the prefab source as scene object and deleted? Source is prefab: {isPrefab}");
                }
                
                return instance;
            }
            
            public void ApplyTransform(NativeSpline spline, int splineIndex, SplineCurveMeshData data, CurveMeshSettings settings)
            {
                Transform target = instances[splineIndex].transform;

                CurveMeshSettings.Conforming conformingSettings = settings.conforming;
                
                //Coincidentally corresponds to the start or end (0 & 1)
                float t = (float)position;
                float splineLength = spline.GetLength();
                
                float shiftLength = shift;

                if (position == Position.Start) shiftLength += settings.distribution.trimStart;
                else if (position == Position.End) shiftLength += settings.distribution.trimEnd;
                    
                //Shift along spline by X-units
                float tOffset = (shiftLength) / splineLength;
                if (position == Cap.Position.End) tOffset = -tOffset;

                t += tOffset;
                
                t = math.clamp(t, Utilities.MIN_T_VALUE, Utilities.MAX_T_VALUE); //Ensure a tangent can always be derived
                
                spline.Evaluate(t, out float3 splinePoint, out float3 tangent, out float3 up);
                
                float3 forward = math.normalize(tangent);
                float3 right = math.cross(forward, up);

                quaternion m_rotation = quaternion.identity;
                Vector3 eulerAngles = rotation;
                if (math.any(align))
                {
                    m_rotation = Utilities.LockRotationAngle(quaternion.identity, quaternion.LookRotation(forward, up), !align);

                    float rollFrequency = settings.rotation.rollFrequency > 0 ? settings.rotation.rollFrequency * (t * splineLength) : 1f;
                    float rollAngle = settings.rotation.rollAngle;

                    float rollValue = rollAngle * rollFrequency;
                    
                    if (data.Has(SplineCurveMeshData.DataType.Roll))
                    {
                        rollValue += data.roll.Evaluate(spline, t * splineLength);
                    }
                    
                    eulerAngles = math.mul(quaternion.AxisAngle(forward, -rollValue * Mathf.Deg2Rad), eulerAngles);
                }

                if (conformingSettings.enable)
                {
                    float conformStrength = 1f;
                    
                    if (data.Has(SplineCurveMeshData.DataType.Conforming))
                    {
                        conformStrength *= data.conforming.Evaluate(spline, t * splineLength, true);
                    }
                    
                    float conformingFalloff = 1f-Utilities.CalculateDistanceWeight(t * splineLength, splineLength, conformingSettings.startOffset, conformingSettings.startFalloff, 
                        conformingSettings.endOffset, conformingSettings.endFalloff, false);
                    conformStrength *= conformingFalloff;
                    
                    if (Physics.Raycast(splinePoint, conformingSettings.direction == CurveMeshSettings.Conforming.Direction.StraightDown ? -math.up() : -up, out RaycastHit hit, conformingSettings.seekDistance, conformingSettings.layerMask, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 hitPosition = hit.point;
                        Vector3 hitNormal = hit.normal;
                        
                        splinePoint.y = Mathf.Lerp(splinePoint.y, hitPosition.y, conformStrength);
                            
                        if (math.any(align))
                        {
                            //Rotate Y and Z
                            m_rotation = Quaternion.Lerp(m_rotation, quaternion.LookRotation(forward, hitNormal), conformStrength);
                                
                            /* This barely works, only along slopes in the negative direction
                            //Now rotate X to face along forward direction
                            Quaternion upRotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
                            float sign = Mathf.Sign(Vector3.Dot(Vector3.up, hitNormal));
                            float xRad = (-upRotation.eulerAngles.x * sign * Mathf.Deg2Rad);
                            m_rotation *= quaternion.AxisAngle(math.right(), xRad);
                            */
                        }
                    }
                }
                
                //Offset
                splinePoint += right * (offset.x - settings.distribution.curveOffset.x);
                splinePoint += up * (offset.y - settings.distribution.curveOffset.y);
                splinePoint += forward * offset.z;

                //End cap pretty much always gets rotated 180 degrees, so automatically factor this in
                if (position == Position.End) eulerAngles.y += 180f;
                
                //Apply custom added rotation last
                m_rotation *= Quaternion.Euler(eulerAngles);

                target.SetPositionAndRotation(splinePoint, m_rotation);
                
                Vector3 m_scale = scale;
                if (matchScale)
                {
                    m_scale.x *= settings.scale.scale.x;
                    m_scale.y *= settings.scale.scale.y;
                    m_scale.z *= settings.scale.scale.z;

                    if (data.Has(SplineCurveMeshData.DataType.Scale))
                    {
                        float3 splineScale = data.scale.Evaluate(spline, t * splineLength);
                        m_scale.x *= splineScale.x;
                        m_scale.y *= splineScale.y;
                    }
                }
                target.localScale = m_scale;
                
                //Gray out fields as any chances would be overwritten anyway
                target.hideFlags = HideFlags.NotEditable;
            }
        }

        //When using raycasts, the colliders on caps should be temporarily disabled
        private void SetCapColliderStates(bool startState, bool endState, out bool startDisabled, out bool endDisabled)
        {
            startDisabled = SetStateCollider(settings.caps.startCap, startState);
            endDisabled = SetStateCollider(settings.caps.endCap, endState);
        }
        
        private static bool SetStateCollider(Cap cap, bool state)
        {
            bool changed = false;
            if (cap.instances.Length > 0)
            {
                for (int i = 0; i < cap.instances.Length; i++)
                {
                    if (cap.instances[i])
                    {
                        Collider[] colliders = cap.instances[i].gameObject.GetComponentsInChildren<Collider>(false);

                        for (int j = 0; j < colliders.Length; j++)
                        {
                            if (colliders[j].enabled != state)
                            {
                                colliders[j].enabled = state;
                                changed = true;
                            }
                            
                        }
                    }
                }
            }

            return changed;
        }
        
        public void UpdateCaps(NativeSpline spline, int splineIndex, SplineCurveMeshData splineMeshData, CurveMeshSettings settings)
        {
            if (!splineContainer) return;

            if (spline.Knots.IsCreated == false)
            {
                throw new Exception("Update Caps called with null spline");
            }

            if (splineMeshData.IsCreated == false)
            {
                throw new Exception("Update Caps called with null splineMeshData");
            }
            
            var splineCountChanged = splineContainer.Splines.Count != splineCount;
            //if(splineCountChanged) Debug.Log($"Spline count changed from {splineCount} to {splineContainer.Splines.Count}");

            SplineMeshContainer container = containers[splineIndex];
            
            if (splineCountChanged || settings.caps.startCap.RequiresRespawn())
            {
                settings.caps.startCap.Respawn(splineCount, container.transform);
            }
            if(settings.caps.startCap.instances.Length > 0) settings.caps.startCap.ApplyTransform(spline, splineIndex, splineMeshData, settings);
            
            if (splineCountChanged || settings.caps.endCap.RequiresRespawn())
            {
                settings.caps.endCap.Respawn(splineCount, container.transform);
            }
            if(settings.caps.endCap.instances.Length > 0) settings.caps.endCap.ApplyTransform(spline, splineIndex, splineMeshData, settings);
        }
        
        public void DetachCaps()
        {
            void DetachCap(Cap cap)
            {
                int instanceCount = cap.instances.Length;
                
                if (instanceCount > 0)
                {
                    for (int i = 0; i < instanceCount; i++)
                    {
                        if (cap.instances[i])
                        {
                            cap.instances[i].transform.parent = this.transform.parent;
                        }
                    }

                    cap.instances = Array.Empty<GameObject>();

                    cap.prefab = null;
                }
            }
            
            DetachCap(settings.caps.startCap);
            DetachCap(settings.caps.endCap);
        }
    }
}