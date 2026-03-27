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
using UnityEngine.Serialization;
using UnityEngine.Splines;
using Cap = sc.splinemesher.pro.runtime.SplineCurveMesher.Cap;

namespace sc.splinemesher.pro.runtime
{
    /// <summary>
    /// Settings container for various aspects
    /// </summary>
    [Serializable]
    public class CurveMeshSettings
    {
        public enum InterpolationType
        {
            Linear,
            EaseInEaseOut,
        }
        
        public enum Shape
        {
            Custom,
            Cube,
            Cylinder,
            Plane
        }
        
        [Serializable]
        public class InputMesh
        {
            public Shape shape = Shape.Custom;
            
            [Serializable]
            public class Cube
            {
                public Vector3 scale = Vector3.one;
                [Min(0.05f)]
                public float edgeLoopDistance = 1f;

                public bool caps;
            }
            public Cube cube = new Cube();
            
            [Serializable]
            public class Cylinder
            {
                [Min(0.02f)]
                public float outerRadius = 0.5f;
                
                [Space]
                
                [Range(3, 32)]
                public int radialSegments = 16;
                [Min(0.05f)]
                public float edgeLoopDistance = 0.25f;
                
                [Space]
                
                public bool hollow;
                [Min(0.02f)]
                public float innerRadius = 0.25f;
                public bool caps;
            }
            public Cylinder cylinder = new Cylinder();

            [Serializable]
            public class Plane
            {
                [Min(0.1f)]
                public float width = 1f;
                public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
                public float curveScale = 1f;
                
                [Space]
                
                [Min(0.1f)]
                public float widthEdgeDistance = 1f;
                [Min(0.1f)]
                public float lengthEdgeDistance = 0.5f;

                [Space]
                
                [Tooltip("Repeat the UV once over the entire width of the plane")]
                public bool stretchUV;
            }
            public Plane plane = new Plane();
            
            public Mesh mesh;

            public Vector3 rotation;
            [Tooltip("Post-op scale of the mesh. Will result in the UV stretching.")]
            public Vector3 scale = Vector3.one;
            public Structs.Alignment alignment;
            
            public Vector2 uvTiling = new Vector2(1f, 1f);
            public bool flipUV = false;
            public bool rotateUV = false;
            
            public void SetDefaults()
            {
                mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                alignment = Structs.Alignment.PivotPoint;
            }
        }
        public InputMesh input = new InputMesh();

        [Serializable]
        public class Renderer
        {
            public Material[] materials;
            public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
            public LightProbeUsage lightProbeUsage = LightProbeUsage.BlendProbes;
            public ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;

            [Tooltip("Mask used for SRP rendering features")]
            public uint renderingLayerMask = 0xFFFFFFFF;
            
            public void SetDefaults()
            {
                materials = new [] { SplineMesher.GetDefaultMaterial() };
            }
        }
        public Renderer renderer = new Renderer();
        
        [Serializable]
        public struct Distribution
        {
            [Min(1)]
            [Tooltip("The number of times the mesh is repeated along the spline curve")]
            public int tiles;
            [Tooltip("Automatically calculate the number of tiles based on the length of the spline")]
            public bool autoTileCount;
                        
            [Min(-5f)]
            [Tooltip("Space between each mesh segment")]
            public float spacing;
            
            [Tooltip("Stretch the tiles so that they fit exactly over the entire spline")]
            public bool scaleToFit;
            [Tooltip("Ensure the input mesh is repeated evenly, instead of cutting it off when it doesn't fit on the remainder of the spline.")]
            public bool evenOnly;
            
            [Tooltip("Attempt to snap an edge loop that's within this distance to the nearest Linear knot")]
            [Min(0)]
            public float knotSnapDistance;
            [Min(0f)]
            [Tooltip("Shift the mesh X number of units from the start of the spline")]
            public float trimStart;
            [Min(0f)]
            [Tooltip("Shift the mesh X number of units from the end of the spline")]
            public float trimEnd;
            
            [Tooltip("Note that offsetting can cause vertices to sort of bunch up." +
                     "\n\nFor the best results, create a separate spline parallel to the one you are trying to offset from.")]
            public float2 curveOffset;
            
            public static Distribution Default()
            {
                return new Distribution()
                {
                    tiles = 1,
                    autoTileCount = true,
                    scaleToFit = true
                };
            }
        }
        public Distribution distribution = Distribution.Default();

        [Serializable]
        public struct Scale
        {
            public float3 scale;

            public PathIndexUnit pathIndexUnit;
            [Tooltip("Defines how the data is interpolated from one data point, to the other")]
            public InterpolationType interpolation;
            
            public static Scale Default()
            {
                return new Scale()
                {
                    scale = new float3(1f),
                    pathIndexUnit = PathIndexUnit.Distance,
                    interpolation = InterpolationType.Linear,
                };
            }
        }
        public Scale scale = Scale.Default();
        //Sits outside of Scale struct, since AnimationCurve is a managed type
        public AnimationCurve scaleOverCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        
        [Serializable]
        public struct Rotation
        {
            [Tooltip("Adopt the spline curve's rotation on these axis.")]
            public bool3 align;
            
            public enum RollMode
            {
                PerVertex,
                PerTile
            }
            [Tooltip("Specify if the rotation roll is calculated for every vertex, or once and applied over the entire tile")]
            public RollMode rollMode;
            [Min(0f)]
            public float rollFrequency;
            [Range(-360f, 360f)]
            public float rollAngle;
            public PathIndexUnit pathIndexUnit;
            
            public static Rotation Default()
            {
                return new Rotation()
                {
                    align = true,
                    rollFrequency = 0.1f,
                    pathIndexUnit = PathIndexUnit.Distance
                };
            }
        }
        [FormerlySerializedAs("deformation")] public Rotation rotation = Rotation.Default();
        
        [Serializable]
        public struct UV
        {
            public float2 scale;
            public float2 offset;

            [Flags]
            public enum Axis
            {
                None,
                [InspectorName("U (X)")]
                U,
                [InspectorName("V (Y)")]
                V
            }
            [Tooltip("Overwrite the target UV value with that of the vertex position over the spline (normalized 0-1 value)." +
                     "\n\n" +
                     "Use this to create a continuous UV without seams (eg. rivers)")]
            public Axis stretch;
            public Axis flip;

            [Tooltip("Rotate the UV 90 degrees")]
            public bool rotate;
            
            public bool StretchX => (Axis.U & stretch) != 0;
            public bool StretchY => (Axis.V & stretch) != 0;
            
            public bool FlipX => (Axis.U & flip) != 0;
            public bool FlipY => (Axis.V & flip) != 0;
            
            public static UV Default()
            {
                return new UV()
                {
                    scale = Vector2.one,
                    offset = Vector2.zero,
                };
            }
        }
        public UV uv = UV.Default();

        [Serializable]
        public struct Color
        {
            [Tooltip("Use the vertex colors from the input mesh. If disabled, the mesh will be colored with a single color.")]
            public bool retainVertexColors;
            [Tooltip("Color value to flood the mesh with")]
            public UnityEngine.Color baseColor;
            public PathIndexUnit pathIndexUnit;

            [Tooltip("Store a gradient value over the start/end of the spline mesh.")]
            public bool tipGradients;

            public enum BlendMode
            {
                Min,
                Max,
                Add
            }

            public Structs.VertexColorChannel tipGradientChannel;
            public BlendMode tipBlendMode;
            [Min(0f)]
            public float startGradientOffset;
            [Min(0.1f)]
            public float startGradientFalloff;
            [Min(0f)]
            public float endGradientOffset;
            [Min(0.1f)]
            public float endGradientFalloff;

            public bool invertTipGradient;

            public bool widthGradients;
            public Structs.VertexColorChannel widthGradientChannel;
            public BlendMode widthBlendMode;
            
            [Min(0f)]
            public float widthGradientOffset;
            [Min(0f)]
            public float widthGradientFalloff;
            public bool invertWidthGradient;
            
            public float4 NativeBaseColor => new float4(baseColor.r, baseColor.g, baseColor.b, baseColor.a);
            
            public static Color Default()
            {
                return new Color()
                {
                    baseColor = UnityEngine.Color.white,
                    pathIndexUnit = PathIndexUnit.Distance,
                    tipGradientChannel = Structs.VertexColorChannel.Alpha,
                    startGradientFalloff = 1f,
                    endGradientFalloff = 1f,
                };
            }
        }
        public Color color = Color.Default();
        
        [Serializable]
        public struct Conforming
        {
            [Tooltip("Project the spline curve into the geometry underneath it. Relies on physics raycasts.")]
            public bool enable;
            
            public PathIndexUnit pathIndexUnit;
            
            public enum Direction
            {
                StraightDown,
                SplineNormal
            }
            public Direction direction;
            
            [Tooltip("A ray is shot this high above every vertex, and reach this much units below it." +
                     "\n\n" +
                     "If a spline is dug into the terrain too much, increase this value to still get valid raycast hits." +
                     "\n\n" +
                     "Internally, the minimum distance is always higher than the mesh's total height.")]
            public float seekDistance;
            [Min(0)]
            public int skipping;

            public float heightOffset;
            
            [Tooltip("Only accept raycast hits from colliders on these layers")]
            public LayerMask layerMask;

            [Tooltip("Rotate the geometry to match the orientation of the surface beneath it")]
            public bool3 align;
            
            [Tooltip("Reorient the geometry normals to match the surface hit, for correct lighting")]
            public bool blendNormal;
            
            [Tooltip("Cancel conforming up to this distance from the start of the spline")]
            [Min(0f)]
            public float startOffset;
            [Min(0f)]
            public float startFalloff;
            [Tooltip("Cancel conforming up to this distance from the end of the spline")]
            [Min(0f)]
            public float endOffset;
            [Min(0f)]
            public float endFalloff;
            
            public static Conforming Default()
            {
                return new Conforming()
                {
                    pathIndexUnit = PathIndexUnit.Distance,
                    seekDistance = 10f,
                    heightOffset = 0.02f,
                    align = true,
                    layerMask = -1,
                    blendNormal = true,
                };
            }
        }
        public Conforming conforming = Conforming.Default();
        
        [Serializable]
        public struct OutputMesh
        {
            [Tooltip("Specify how long the mesh is allowed to be, before it is split up into a separate mesh object. Set to 0 to create a segment of each individual tile." +
                     "\n\n" +
                     "Segmenting the final mesh is important for culling and rendering performance." +
                     "\n\n" +
                     "Note that the final mesh may be longer or shorter than this value, due to stretching and rounding.")]
            [Min(0f)]
            public float maxSegmentLength;
            
            [Tooltip("If enabled, Unity will keep a readable copy of the mesh around in memory. Allowing other scripts to access its data, and possible alter it.")]
            public bool keepReadable;

            [Tooltip("Save relative vertex positions in the (assumingly) unused UV components. If disabled, the source mesh's original values are retained." +
                     "\n" +
                     "\n[UV0.Z]: (0-1) distance over spline length" +
                     "\n[UV0.W]: (0-1) distance over height of mesh" +
                     "\n\n" +
                     "This data may be used in shaders for tailored effects, such as animations.")]
            public bool storeGradientsInUV;
            
            [Space]
            
            [Min(0.01f)]
            [Tooltip("Multiplier for the pack-margin value. A value of 1 equates to 1 texel")]
            public float lightmapUVMarginMultiplier;
            [Range(15f, 90f)]
            [Tooltip("This angle (in degrees) or greater between triangles will cause UV seam to be created.")]
            public float lightmapUVAngleThreshold;
            
            [Space]
            
            [Min(0)]
            [Tooltip("Specify a maximum number of LOD levels to generate.")]
            public int maxLodCount;
            [Tooltip("Force the mesh to use a specific LOD level. If 0, the LOD level is automatically chosen.")]
            [Min(0)]
            public int forceMeshLod;
            [Range(-5f, 5f)]
            public float lodSelectionBias;
            
            public static OutputMesh Default()
            {
                return new OutputMesh()
                {
                    maxSegmentLength = 50f,
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

            public InputMesh inputMesh;
            
            public int layer;
            [Tooltip("If enabled, a Rigidbody component will be added to the object and set to kinematic")]
            public bool isKinematic;
            [Tooltip("Use convex collision geometry (if possible). Yields better results if smaller segments are created (see Output section)")]
            public bool convex;
            public bool isTrigger;
            public bool provideContacts;
            
            public static Collision Default()
            {
                Collision c = new Collision()
                {
                    layer = 0,
                    inputMesh = new InputMesh(),
                };

                c.inputMesh.shape = Shape.Cube;

                return c;
            }
        }
        public Collision collision = Collision.Default();

        [Serializable]
        public class Caps
        {
            public Cap startCap = new Cap(Cap.Position.Start);
            public Cap endCap = new Cap(Cap.Position.End);
        }
        public Caps caps = new Caps();
    }
}