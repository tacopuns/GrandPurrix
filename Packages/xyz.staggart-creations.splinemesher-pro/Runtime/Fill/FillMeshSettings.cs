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
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace sc.splinemesher.pro.runtime
{
    [Serializable]
    public class FillMeshSettings
    {
        [Serializable]
        public class Renderer
        {
            public Material material;
            public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
            public LightProbeUsage lightProbeUsage = LightProbeUsage.BlendProbes;
            public ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;

            public uint renderingLayerMask = 0xFFFFFFFF;
            
            public void SetDefaults()
            {
                material = SplineMesher.GetDefaultMaterial();
            }
        }
        public Renderer renderer = new Renderer();
        
        [Serializable]
        public struct Topology
        {
            [Tooltip("The size of the triangles in the mesh. Use the largest value possible for the best performance. Smaller triangles yield a better curve fit.")]
            [Min(0.5f)]
            public float triangleSize;

            [Tooltip("Snaps a vertex to the nearest linear knot, needed for precise shapes")]
            public bool snapToKnot;
            [Tooltip("Artificially push the spline curve inwards. Can result in vertices overlapping, especially in short turns.")]
            public float margin;
            
            [Tooltip("The accuracy of spline curve sampling. Higher values result in vertices neatly snapping to the curve. Use lower precision for large meshes, or when using a Margin to push the edge into geometry to hide it.")]
            public Structs.Accuracy accuracy;
            
            public static Topology Default()
            {
                return new Topology()
                {
                    triangleSize = 6,
                    accuracy = Structs.Accuracy.HighestPrecision,
                };
            }
        }
        public Topology topology = Topology.Default();

        [Serializable]
        public struct UV
        {
            [Tooltip("Span the UV once over the entire mesh on the XZ axis.")]
            public bool fitToMesh;
            
            public float2 tiling;
            public float2 offset;

            public CurveMeshSettings.UV.Axis flip;
            public bool rotate;
            
            public bool FlipX => (CurveMeshSettings.UV.Axis.U & flip) != 0;
            public bool FlipY => (CurveMeshSettings.UV.Axis.V & flip) != 0;
            
            public static UV Default()
            {
                return new UV()
                {
                    tiling = new float2(1f,1f),
                    offset = 0f,
                };
            }
        }
        public UV uv = UV.Default();
        
        [Serializable]
        public struct Displacement
        {
            [Tooltip("Vertically offset the mesh.")]
            public float offset;
            
            [Tooltip("Push vertices up in the center of the mesh, similar to an inflation effect.")]
            public float bulge;
            [Min(0.01f)]
            public float bulgeFalloff;
        
            [Tooltip("Flatten the mesh. If set to 0 the mesh tries to maintain the vertical spline curvature")]
            [Range(0f, 1f)]
            public float flattening;
            [Tooltip("The falloff of the flattening effect, moving inwards. May be used to smooth the transition from the edge towards the center.")]
            [Min(0.01f)]
            public float flatteningFalloff;

            public float3 noise;
            public float3 noiseFrequency;
            public float3 noiseOffset;
            
            public static Displacement Default()
            {
                return new Displacement()
                {
                    bulgeFalloff = 0.1f,
                    flattening = 0f,
                    flatteningFalloff = 1f,
                    noiseFrequency = 1,
                };
            }
        }
        public Displacement displacement = Displacement.Default();

        [Serializable]
        public struct Conforming
        {
            [Tooltip("Project the spline curve into the geometry underneath it. Relies on physics raycasts.")]
            public bool enable;
            [Tooltip("Only accept raycast hits from colliders on these layers")]
            public LayerMask layerMask;

            [Tooltip("A ray is shot this high above every vertex, and reach this much units below it." +
                     "\n\n" +
                     "If a spline is dug into the terrain too much, increase this value to still get valid raycast hits." +
                     "\n\n" +
                     "Internally, the minimum distance is always higher than the mesh's total height.")]
            public float seekDistance;
            public float heightOffset;
            
            public static Conforming Default()
            {
                return new Conforming()
                {
                    seekDistance = 15f,
                    heightOffset = 0.02f,
                    layerMask = -1
                };
            }
        }
        public Conforming conforming = Conforming.Default();
        
        [Serializable]
        public struct OutputMesh
        {
            [Tooltip("If enabled, Unity will keep a readable copy of the mesh around in memory. Allowing other scripts to access its data, and possible alter it.")]
            public bool keepReadable;

            [Tooltip("Save relative vertex positions in the (assumingly) unused UV components. If disabled, the source mesh's original values are retained." +
                     "\n" +
                     "\n[UV0.Z]: Distance to nearest spline point" +
                     "\n[UV0.W]: (0-1) Normalized distance to nearest spline point" +
                     "\n\n" +
                     "This data may be used in shaders for tailored effects, such as animations.")]
            public bool storeGradientsInUV;

            [Min(0.01f)]
            [Tooltip("Multiplier for the pack-margin value. A value of 1 equates to 1 texel")]
            public float lightmapUVMarginMultiplier;
            [Range(15f, 90f)]
            [Tooltip("This angle (in degrees) or greater between triangles will cause UV seam to be created.")]
            public float lightmapUVAngleThreshold;

            [Min(0)]
            public int maxLodCount;
            [Min(0)]
            public int forceMeshLod;
            [Range(-5f, 5f)]
            public float lodSelectionBias;
            
            public static OutputMesh Default()
            {
                return new OutputMesh()
                {
                    storeGradientsInUV = true,
                    lightmapUVMarginMultiplier = 1f,
                    lightmapUVAngleThreshold = 88f,
                    forceMeshLod = -1,
                };
            }
        }
        public OutputMesh output = OutputMesh.Default();
        
        [Serializable]
        public struct Collision
        {
            [Tooltip("Add a Mesh Collider component and also generate a collision mesh for it")]
            public bool enable;
            
            [Tooltip("Do not create a visible mesh, but only create the collision mesh")]
            public bool colliderOnly;

            public int layer;
            
            public static Collision Default()
            {
                return new Collision()
                {
                    layer = 0
                };
            }
        }
        public Collision collision = Collision.Default();
    }
}