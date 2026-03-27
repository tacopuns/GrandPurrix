# Spline Mesher - Pro
*by Staggart Creations*

## Project Requirements
Compatible with Unity 2022.3+

**Requires the following packages**
- Splines
- Mathematics
- Collections
- Burst
- Jobs

## New editor options
- Spline Mesher component
- Context menu on Mesh Filter component to convert it into a spline
- Context menu on Spline Container component to add a Curve- or Fill Mesher
- New menu options: 
	GameObject/Spline/Mesh/Curve
	GameObject/Spline/Mesh/Fill

## Runtime usage

There is no editor-only code in place, so runtime usage is possible. That's not to say that a complete UI and control system is in place.
You can simply edit the Spline or call the Rebuild() function on a Spliner Mesher component from any external script.

Note that the input mesh needs to have Read/Write enabled for this to work!

## Complete documentation
Go to *Help/Spline Mesher* Pro to open the asset window. Links to resources are available there.