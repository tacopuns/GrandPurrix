1.0.2 (March 4th 2026)

Added:
- Support for usage with prefabs.
- Scale Tool: support for natively grid snapping scale values.

Changed:
- Splines shorter than 1m long are now allowed.
- SplineMeshContainer and SplineMeshSegment components now have an inspector UI showing relevant stats.
- The warning regarding prefabs and procedural meshes can now be dismissed.
- SplineMesher base component is now prevented from being manually added to a GameObject.

Fixed:
- Using the same Root transform for a Curve Mesher and Fill Mesher resulted in both trying to adopt eachother's output meshes.
- Collider some times not appearing to be generated, if the "Custom" was first selected with no mesh assigned.
- Input mesh alignment not correct when also applying a rotation.
- Script error when Bakery was installed (missing assembly reference).
- Leak error messages when calling Rebuild() from a script whilst adding new Splines.
- Error thrown when deleting the last point in the Scale editor.

1.0.1 (March 2nd 2026)

Fixed:
- Normals of Cube input mesh being flipped on the left face. Resulting in a gap in colliders.
- Several meshes in the demo scene inadvertently using materials from the Standard package.

1.0.0 (March 1st 2026)
This is a major rewrite, re-release and a new asset altogether. A migration tool is included to convert Spline Mesher Standard instances.

All functionality has been recreated from the ground up around a multithreaded design using Jobs, Burst and the low-level MeshData API. Performance has increased by at least 50x.

Added:
- Spline Fill Mesher component. Generates a mesh from a Spline's contour with a uniform topology
* Procedural displacement through bulging and/or noise
* Granular UV controls: Fitting, tiling, offset, rotation and flipping
* Conforming to underlying colliders
* Collision mesh generation
* Distance field baked into UV for shader effects (eg. gradients, water shorelines)

- New demo content
* Wind tube
* Aurora borealis
* Street curbs
* Stylized lake
* Rope bridge
* River
* Cliff faces
* Party lights
  * Tentacle
  * Conveyor belt
  * Tire tracks

- Inspector enhancements:
* Buttons to toggle Wireframe and UV visualization
* Ability to drag & drop a material
* Triangle/vertex count display + memory size

- Collider generation:
* Option to use an adjustable Cube/Cylinder/Plane mesh.
* Colliders can now have RigidBodies auto-attached and a separate layer
* Collision/Trigger events across the spline (C# or through the inspector)

- Vertex colors:
* A base color can now be specified (overwriting the input mesh's colors)
* A gradient can now be auto-generated along the start/end of the mesh (configurable length and falloff) and across the width.

- Other
- Input mesh can now be a configurable Cube, Tube or Plane mesh.
- Output mesh can now be split into segments of a maximum length.
- Support for MeshLOD in Unity 6.2. LODs can now be auto-generated when the scene saves (or manually through script)
- Conforming start/end falloff controls.
- Caps can now be aligned on specific axis.

Changed:
- Script namespace has changed to "sc.splinemesher.pro.runtime"
- Spline Mesher component is now called "Spline Curve Mesher"
- A separate mesh/collider objects is now created per spline.
- Render- and Collision mesh are now created in parallel for far better performance.
- Lightmap UV generation behaviour can now be configured in a “Project Settings” section, so can be commited to source control

Removed:
- Conforming, "Terrains Only" option. No longer possible with Job system raycasting.