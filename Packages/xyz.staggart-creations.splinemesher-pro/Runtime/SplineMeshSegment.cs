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
using UnityEngine;
using UnityEngine.Rendering;

namespace sc.splinemesher.pro.runtime
{
    public class SplineMeshSegment : MonoBehaviour
    {
        [HideInInspector] [SerializeField]
        internal SplineMeshContainer container;
        /// <summary>
        /// The container this segment belongs to, which in turn belongs to a Spline Mesher <see cref="SplineMeshContainer.Owner"/>
        /// </summary>
        public SplineMeshContainer Container => container;
        
        [SerializeField] [HideInInspector]
        private MeshFilter meshFilter;
        [SerializeField] [HideInInspector]
        private MeshRenderer meshRenderer;
        [SerializeField] [HideInInspector]
        private MeshCollider meshCollider;
        
        [SerializeField] [HideInInspector]
        private Mesh m_CollisionMesh;

        /// <summary>
        /// The generated mesh
        /// </summary>
        public Mesh mesh
        {
            get
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!meshFilter)
                {
                    Debug.LogError("Mesh filter not found on " + gameObject.name, this);
                }
                #endif
                if(!meshFilter.sharedMesh) 
                {
                    meshFilter.sharedMesh = new Mesh();
                    meshID = meshFilter.GetInstanceID();
                }
                return meshFilter.sharedMesh;
            }
            set => meshFilter.sharedMesh = value;
        }
        
        /// <summary>
        /// Materials assigned to the mesh renderer
        /// </summary>
        public Material[] materials => meshRenderer.sharedMaterials;

        //Save the mesh's hashcode to verify against
        //Avoids having to recreate a new mesh every rebuild operation
        //Whilst ensuring that does happen when duplicating a mesh object
        [SerializeField] [HideInInspector] private int meshID;
        
        internal void EnsureUniqueMesh()
        {
            if (!meshFilter.sharedMesh) return;
            
            int hash = meshFilter.GetInstanceID();
            if (meshID != hash)
            {
                //Debug.Log("Duplicate mesh ID: " + meshID, this);
                
                //Duplicate
                var newMesh = Instantiate(mesh);
                meshFilter.mesh = newMesh;
                
                meshID = meshFilter.GetInstanceID();
            }
        }

        /// <summary>
        /// Adds or removes a Mesh Collider to the segment.
        /// </summary>
        /// <param name="state"></param>
        public void SetMeshCollider(bool state)
        {
            if (state && !meshCollider)
            {
                meshCollider = this.gameObject.AddComponent<MeshCollider>();
                // Ensure we have a dedicated collision mesh
                if (!m_CollisionMesh) m_CollisionMesh = new Mesh();
                meshCollider.sharedMesh = m_CollisionMesh;
            }
            if (!state && meshCollider)
            {
                if (Application.isPlaying)
                    Destroy(meshCollider);
                else
                    DestroyImmediate(meshCollider);
            }
        }
    
        /// <summary>
        /// The collision mesh assigned to the mesh collider
        /// </summary>
        public Mesh collisionMesh
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!meshCollider)
                {
                    Debug.LogError("[Accessing collision mesh failed] Mesh Collider not found on mesh segment belonging to " + Container.Owner.name, this);
                    SetMeshCollider(true);
                }
#endif
            
                //Always use the dedicated collision mesh
                if (!m_CollisionMesh)
                {
                    m_CollisionMesh = new Mesh();
                }
            
                return m_CollisionMesh;
            }
            set
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!meshCollider)
                {
                    Debug.LogError("[Setting collision mesh failed] Mesh Collider not found on mesh segment belonging to " + Container.Owner.name, this);
                    SetMeshCollider(true);
                }
#endif
            
                m_CollisionMesh = value;
                if (meshCollider) meshCollider.sharedMesh = value;
            }
        }

        internal static SplineMeshSegment Create(SplineMeshContainer container, int segmentIndex)
        {
            GameObject go = new GameObject($"Spline mesh #{segmentIndex}");
            go.transform.SetParent(container.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            
            SplineMeshSegment segment = go.AddComponent<SplineMeshSegment>();
            segment.container = container;
            segment.meshFilter = go.AddComponent<MeshFilter>();
            segment.meshFilter.mesh = new Mesh();
            segment.meshRenderer = go.AddComponent<MeshRenderer>();

            return segment;
        }
        
        public void SetMaterials(Material[] materials)
        {
            meshRenderer.sharedMaterials = materials;
        }
        
        public void SetMaterial(Material material)
        {
            meshRenderer.sharedMaterial = material;
        }

        //TODO: Temporarily switch the object's layer to "Ignore Raycast". As enabling a MeshCollider triggers its cooking process
        public void SetColliderEnabled(bool state)
        {
            if (meshCollider) meshCollider.enabled = state;
        }

        public void SetColliderSettings(int layer, bool isKinematic, bool convex, bool isTrigger, bool provideContacts)
        {
            if (!meshCollider) return;
            
            meshCollider.gameObject.layer = layer;
#if UNITY_2022_3_OR_NEWER
            meshCollider.includeLayers = -1;
#endif
            
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            if (isKinematic == false && rb)
            {
                if (Application.isPlaying)
                    Destroy(rb);
                else
                    DestroyImmediate(rb);
            }
            if(isKinematic && !rb) rb = gameObject.AddComponent<Rigidbody>();
            if (rb)
            {
#if UNITY_2022_3_OR_NEWER
                rb.includeLayers = -1;
#endif
                rb.isKinematic = isKinematic;
            }
            
#if UNITY_2022_3_OR_NEWER
            meshCollider.providesContacts = provideContacts;
#endif
            meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.UseFastMidphase;
            
            //Set convex first, as isTrigger depends on it
            if(meshCollider.convex != convex) meshCollider.convex = convex;
    
            //Only allow isTrigger if the mesh is convex
            bool shouldBeTrigger = convex && isTrigger;
            if(meshCollider.isTrigger != shouldBeTrigger) meshCollider.isTrigger = shouldBeTrigger;
        }

        public void SetRendererParameters(ShadowCastingMode shadowCastingMode, LightProbeUsage lightProbeUsage, ReflectionProbeUsage reflectionProbeUsage, uint renderingLayerMask, int forceMeshLod, float lodSelectionBias)
        {
            meshRenderer.shadowCastingMode = shadowCastingMode;
            meshRenderer.lightProbeUsage = lightProbeUsage;
            meshRenderer.reflectionProbeUsage = reflectionProbeUsage;
            meshRenderer.renderingLayerMask = renderingLayerMask;
            #if UNITY_6000_2_OR_NEWER
            meshRenderer.forceMeshLod = (short)forceMeshLod;
            meshRenderer.meshLodSelectionBias = lodSelectionBias;
            #endif
        }

        public void SetRendererState(bool state)
        {
            meshRenderer.enabled = state;
        }

        internal void DrawWireframeGizmo()
        {
            if (!meshFilter.sharedMesh) return;
            
            Gizmos.matrix = this.transform.localToWorldMatrix;
            if(meshFilter.sharedMesh.vertexCount > 0) Gizmos.DrawWireMesh(meshFilter.sharedMesh);
        }
        
        internal void DrawMeshGizmo()
        {
            if (!meshFilter.sharedMesh) return;
            
            Gizmos.matrix = this.transform.localToWorldMatrix;
            Gizmos.DrawMesh(meshFilter.sharedMesh);
        }

        internal void DrawMeshNow()
        {
            if (!meshFilter.sharedMesh) return;
            
            Graphics.DrawMeshNow(meshFilter.sharedMesh, this.transform.localToWorldMatrix);
        }
        
        public void Destroy()
        {
            if (Application.isPlaying)
                Destroy(this.gameObject);
            else
                DestroyImmediate(this.gameObject, true);
        }

        [ContextMenu("Recalculate Normals")]
        //Purely for testing, to verify if normals are recalculated correctly
        private void RecalculateNormals()
        {
            meshFilter.sharedMesh.RecalculateNormals();
        }
        
        [ContextMenu("Recalculate Tangents")]
        //Purely for testing, to verify if tangents are recalculated correctly
        private void RecalculateTangents()
        {
            meshFilter.sharedMesh.RecalculateTangents();
        }
        
        public (int, int) GetMeshStats()
        {
            if (mesh)
            {
                int triCount = 0;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    triCount += (int)mesh.GetIndexCount(i) / 3;
                }
                return (mesh.vertexCount, triCount);
            }

            return (0, 0);
        }
        
        //In megabytes
        public float GetMemorySize()
        {
            return mesh ? Utilities.GetMemorySize(mesh) : 0;
        }
        
        #pragma warning disable CS0067 //Event is never used
        public delegate void CollisionAction(SplineMeshSegment splineMeshSegment, Collision other);
        /// <summary>
        /// Collision callbacks.
        /// </summary>
        public static event CollisionAction onCollisionEnter, onCollisionExit;
        public delegate void TriggerAction(SplineMeshSegment splineMeshSegment, Collider other);
        /// <summary>
        /// Trigger callbacks.
        /// </summary>
        public static event TriggerAction onTriggerEnter, onTriggerStay, onTriggerExit;
        #pragma warning restore CS0067
        
        #region Physics
        public void OnCollisionEnter(Collision other)
        {
            onCollisionEnter?.Invoke(this, other);
            Container.Owner.onCollisionEnter?.Invoke(other);
        }
        
        public void OnCollisionExit(Collision other)
        {
            onCollisionExit?.Invoke(this, other);
            Container.Owner.onCollisionExit?.Invoke(other);
        }

        public void OnTriggerEnter(Collider other)
        {
            onTriggerEnter?.Invoke(this, other);
            Container.Owner.onTriggerEnter?.Invoke(other);
        }

        public void OnTriggerStay(Collider other)
        {
            onTriggerStay?.Invoke(this, other);
            Container.Owner.onTriggerStay?.Invoke(other);
        }

        public void OnTriggerExit(Collider other)
        {
            onTriggerExit?.Invoke(this, other);
            Container.Owner.onTriggerExit?.Invoke(other);
        }
        #endregion
    }
}