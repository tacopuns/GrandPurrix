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

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Alignment = sc.splinemesher.pro.runtime.Structs.Alignment;

namespace sc.splinemesher.pro.runtime
{
    public static class ProceduralMesh
    {
        public static float3 CalculateOffset(Alignment alignment, float3 min, float3 max)
        {
            if(alignment == Alignment.PivotPoint) return float3.zero;
            
            float3 center = (min + max) * 0.5f;
            float x;
            float y;

            float xMin = min.x;
            float xMax = max.x;
            float xCenter = center.x;

            float yMin = min.y;
            float yMax = max.y;
            float yCenter = center.y;

            switch (alignment)
            {
                case Alignment.TopLeft:      x = xMin;    y = yMax;    break;
                case Alignment.TopCenter:    x = xCenter; y = yMax;    break;
                case Alignment.TopRight:     x = xMax;    y = yMax;    break;

                case Alignment.MiddleLeft:   x = xMin;    y = yCenter; break;
                case Alignment.MiddleCenter: x = xCenter; y = yCenter; break;
                case Alignment.MiddleRight:  x = xMax;    y = yCenter; break;

                case Alignment.BottomLeft:   x = xMin;    y = yMin;    break;
                case Alignment.BottomCenter: x = xCenter; y = yMin;    break;
                case Alignment.BottomRight:  x = xMax;    y = yMin;    break;

                default: x = xCenter; y = yCenter; break;
            }

            //Alignment is 2D (X/Y). Keep Z centered.
            return -new float3(x, y, center.z);
        }
        
        public static class Cube
        {
            private struct Job : IJob
            {
                private bool caps;
                private float width, height, length;
                private float3 offset;
                int subdivisions;
                float uvScaleX, uvScaleY, uvScaleZ;

                private int numFaces;
                public NativeArray<Vertex> vertices;
                public NativeArray<ushort> indices;

                public void Setup(float width, float height, float length, float edgeLoopDistance, bool caps, float2 uvTiling, float3 pivot, Alignment alignment = Alignment.MiddleCenter)
                {
                    this.caps = caps;
                    this.width = width;
                    this.height = height;
                    this.length = length;
                    this.offset = pivot;
                    this.subdivisions = Mathf.CeilToInt(Mathf.Max(1, length / edgeLoopDistance));
                
                    uvScaleX = width * uvTiling.x;
                    uvScaleY = height;
                    uvScaleZ = length * uvTiling.y;
                
                    //With caps: 6 faces, without caps: 4 faces (no front/back)
                    //Each face now has subdivisions along Z
                    numFaces = caps ? 6 : 4;
                    int verticesPerFace = 4 * (this.subdivisions + 1);
                    int numVertices = numFaces * verticesPerFace;
                    int numTriangles = numFaces * this.subdivisions * 6;

                    vertices = new NativeArray<Vertex>(numVertices, Allocator.TempJob);
                    indices = new NativeArray<ushort>(numTriangles, Allocator.TempJob);
                }
        
                public void Execute()
                {
                    int vertexIndex = 0;
                    int indexIndex = 0;

                    if (caps)
                    {
                        //Front face (Z+)
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, -0.5f * height, 0.5f * length) + offset, new float3(0, 0, 1), new float4(1, 0, 0, 1), new float4(0 * uvScaleX, 0 * uvScaleY, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, -0.5f * height, 0.5f * length) + offset, new float3(0, 0, 1), new float4(1, 0, 0, 1), new float4(1 * uvScaleX, 0 * uvScaleY, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, 0.5f * height, 0.5f * length) + offset, new float3(0, 0, 1), new float4(1, 0, 0, 1), new float4(1 * uvScaleX, 1 * uvScaleY, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, 0.5f * height, 0.5f * length) + offset, new float3(0, 0, 1), new float4(1, 0, 0, 1), new float4(0 * uvScaleX, 1 * uvScaleY, 0, 0));
                    
                        //Back face (Z-)
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, -0.5f * height, -0.5f * length) + offset, new float3(0, 0, -1), new float4(-1, 0, 0, 1), new float4(0 * uvScaleX, 0 * uvScaleY, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, -0.5f * height, -0.5f * length) + offset, new float3(0, 0, -1), new float4(-1, 0, 0, 1), new float4(1 * uvScaleX, 0 * uvScaleY, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, 0.5f * height, -0.5f * length) + offset, new float3(0, 0, -1), new float4(-1, 0, 0, 1), new float4(1 * uvScaleX, 1 * uvScaleY, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, 0.5f * height, -0.5f * length) + offset, new float3(0, 0, -1), new float4(-1, 0, 0, 1), new float4(0 * uvScaleX, 1 * uvScaleY, 0, 0));

                        //2 triangles for front cap
                        int baseVertex = 0;
                        indices[indexIndex++] = (ushort)(baseVertex + 0);
                        indices[indexIndex++] = (ushort)(baseVertex + 1);
                        indices[indexIndex++] = (ushort)(baseVertex + 2);
                        indices[indexIndex++] = (ushort)(baseVertex + 0);
                        indices[indexIndex++] = (ushort)(baseVertex + 2);
                        indices[indexIndex++] = (ushort)(baseVertex + 3);
                    
                        //2 triangles for back cap
                        baseVertex = 4;
                        indices[indexIndex++] = (ushort)(baseVertex + 0);
                        indices[indexIndex++] = (ushort)(baseVertex + 1);
                        indices[indexIndex++] = (ushort)(baseVertex + 2);
                        indices[indexIndex++] = (ushort)(baseVertex + 0);
                        indices[indexIndex++] = (ushort)(baseVertex + 2);
                        indices[indexIndex++] = (ushort)(baseVertex + 3);
                    }

                    //Generate subdivided faces (Top, Bottom, Right, Left)
                    //Top face (Y+)
                    for (int z = 0; z <= subdivisions; z++)
                    {
                        float zPos = 0.5f * length - (z / (float)subdivisions) * length;
                        float uvZ = z / (float)subdivisions;
                    
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, 0.5f * height, zPos) + offset, new float3(0, 1, 0), new float4(1, 0, 0, 1), new float4(0 * uvScaleX, -uvZ * uvScaleZ, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, 0.5f * height, zPos) + offset, new float3(0, 1, 0), new float4(1, 0, 0, 1), new float4(1 * uvScaleX, -uvZ * uvScaleZ, 0, 0));
                    }

                    //Bottom face (Y-)
                    for (int z = 0; z <= subdivisions; z++)
                    {
                        float zPos = -0.5f * length + (z / (float)subdivisions) * length;
                        float uvZ = z / (float)subdivisions;
                    
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, -0.5f * height, zPos) + offset, new float3(0, -1, 0), new float4(1, 0, 0, 1), new float4(0 * uvScaleX, uvZ * uvScaleZ, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, -0.5f * height, zPos) + offset, new float3(0, -1, 0), new float4(1, 0, 0, 1), new float4(1 * uvScaleX, uvZ * uvScaleZ, 0, 0));
                    }

                    //Right face (X+)
                    for (int z = 0; z <= subdivisions; z++)
                    {
                        float zPos = 0.5f * length - (z / (float)subdivisions) * length;
                        float uvZ = z / (float)subdivisions;
                    
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, -0.5f * height, zPos) + offset, new float3(1, 0, 0), new float4(0, 0, -1, 1), new float4(0 * uvScaleY, -uvZ * uvScaleZ, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(0.5f * width, 0.5f * height, zPos) + offset, new float3(1, 0, 0), new float4(0, 0, -1, 1), new float4(1 * uvScaleY, -uvZ * uvScaleZ, 0, 0));
                    }

                    //Left face (X-)
                    for (int z = 0; z <= subdivisions; z++)
                    {
                        float zPos = 0.5f * length - (z / (float)subdivisions) * length;
                        float uvZ = z / (float)subdivisions;
                    
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, 0.5f * height, zPos) + offset, new float3(-1, 0, 0), new float4(0, 0, 1, 1), new float4(1 * uvScaleY, -uvZ * uvScaleZ, 0, 0));
                        vertices[vertexIndex++] = new Vertex(new float3(-0.5f * width, -0.5f * height, zPos) + offset, new float3(-1, 0, 0), new float4(0, 0, 1, 1), new float4(0 * uvScaleY, -uvZ * uvScaleZ, 0, 0));
                    }

                    //Generate indices for subdivided faces
                    int faceStartVertex = caps ? 8 : 0; //Skip cap vertices if they exist
                
                    for (int face = 0; face < (caps ? 4 : 4); face++)
                    {
                        int baseFaceVertex = faceStartVertex + face * ((subdivisions + 1) * 2);
                    
                        for (int seg = 0; seg < subdivisions; seg++)
                        {
                            int baseVertex = baseFaceVertex + seg * 2;
                        
                            // Top and Bottom faces need reversed winding
                            if (face == 0 || face == 1) // Top (face 0) and Bottom (face 1)
                            {
                                //First triangle
                                indices[indexIndex++] = (ushort)(baseVertex + 0);
                                indices[indexIndex++] = (ushort)(baseVertex + 1);
                                indices[indexIndex++] = (ushort)(baseVertex + 2);
                        
                                //Second triangle
                                indices[indexIndex++] = (ushort)(baseVertex + 1);
                                indices[indexIndex++] = (ushort)(baseVertex + 3);
                                indices[indexIndex++] = (ushort)(baseVertex + 2);
                            }
                            else // Right (face 2) and Left (face 3) faces
                            {
                                //First triangle
                                indices[indexIndex++] = (ushort)(baseVertex + 0);
                                indices[indexIndex++] = (ushort)(baseVertex + 2);
                                indices[indexIndex++] = (ushort)(baseVertex + 1);
                        
                                //Second triangle
                                indices[indexIndex++] = (ushort)(baseVertex + 1);
                                indices[indexIndex++] = (ushort)(baseVertex + 2);
                                indices[indexIndex++] = (ushort)(baseVertex + 3);
                            }
                        }
                    }
                }

                public void Dispose()
                {
                    if (vertices.IsCreated) vertices.Dispose();
                    if (indices.IsCreated) indices.Dispose();
                }
            }
            
            public static Mesh Create(float width, float height, float length, float edgeLoopDistance, bool caps, float2 uvTiling, float3 pivot, Alignment alignment)
            {
                Job job = new Job();
                job.Setup(width, height, length, edgeLoopDistance, caps, uvTiling, pivot, alignment);

                JobHandle jobHandle =  job.Schedule();
                jobHandle.Complete();

                Mesh mesh = CreateMesh(job.vertices, job.indices);
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                mesh.name = "Procedural Cube";
                #endif
                
                job.Dispose();
                
                return mesh;
            }
        }
        
        public static class Cylinder
        {
            private struct Job : IJob
            {
                private float outerRadius;
                private float innerRadius;
                private uint radialSegments;

                private bool hollow, caps;
                
                private float edgeLoopDistance;
                private float2 uvTiling;
                private float3 offset;
                private bool rotateUV, flipUV;
                
                uint widthSegments;
                float length;
                int lengthSegments;
                uint hCount;
                int zCount;
                
                public NativeArray<Vertex> vertices;
                public NativeArray<ushort> indices;
                
                public void Setup(float radius, bool hollow, float innerRadius, bool caps, int radialSegments, float length, float edgeLoopDistance, float2 uvTiling, float3 pivot, Alignment alignment)
                {
                    this.outerRadius = radius;
                    this.innerRadius = Mathf.Max(0.1f, Mathf.Min(radius - 0.01f, innerRadius));
                    this.radialSegments = (uint)Mathf.Max(3, radialSegments);
                    this.edgeLoopDistance = edgeLoopDistance;
                    this.length = Mathf.Max(1f, length);

                    this.offset = pivot;
                    
                    this.hollow = hollow;
                    this.caps = hollow && caps;
                    
                    this.uvTiling = uvTiling;
                    //rotateUV = settings.rotateUV;
                    //flipUV = settings.flipUV;
                    
                    widthSegments = this.radialSegments;
                    lengthSegments = Mathf.CeilToInt(this.length / this.edgeLoopDistance);
                    
                    hCount = widthSegments + 1;
                    zCount = lengthSegments + 1;
                    
                    //Outer + inner cylinder surfaces
                    int cylinderVertices = (int)hCount * zCount;
                    if(hollow) cylinderVertices *= 2;
                    
                    //Front and back caps (each has radialSegments * 2 vertices for the ring)
                    int capVertices = caps ? (int)this.radialSegments * 2 * 2 : 0;
                    int numVertices = cylinderVertices + capVertices;
                    
                    //Outer + inner cylinder triangles
                    int cylinderTriangles = (int)widthSegments * lengthSegments * 6 * 2;
                    //Front and back cap triangles
                    int capTriangles = caps ? (int)this.radialSegments * 6 * 2 : 0;
                    int numTriangles = cylinderTriangles + capTriangles;
                    
                    vertices = new NativeArray<Vertex>(numVertices, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    indices = new NativeArray<ushort>(numTriangles, Allocator.TempJob);
                }
                
                public void Execute()
                {
                    float outerCircumference = 2f * math.PI * outerRadius;
                    float2 outerUVScale = new float2(uvTiling.x * (outerCircumference / this.radialSegments), uvTiling.y / lengthSegments);
                    
                    float innerCircumference = 2f * math.PI * innerRadius;
                    float2 innerUVScale = new float2(uvTiling.x * (innerCircumference / this.radialSegments), uvTiling.y / lengthSegments);
                    
                    float scaleY = length / lengthSegments;

                    int index = 0;
                    float startAngle = -(180f / this.radialSegments);
                    
                    //Combined loop for outer and inner cylinder surfaces
                    int surfaceCount = hollow ? 2 : 1;
        
                    for (int surface = 0; surface < surfaceCount; surface++)
                    {
                        bool isInner = surface == 1;
                        float radius = isInner ? innerRadius : outerRadius;
                        float2 uvScale = isInner ? innerUVScale : outerUVScale;
                        float normalSign = isInner ? -1f : 1f;
                        float tangentX = isInner ? 1f : -1f;
            
                        for (int z = 0; z < zCount; z++)
                        {
                            var angleStep = (2 * math.PI) / radialSegments;
                            var angle = math.radians(startAngle) + angleStep;

                            for (int x = 0; x < hCount; x++)
                            {
                                float cos = math.cos(angle);
                                float sin = math.sin(angle);
                                
                                float3 position = new float3(cos * radius, sin * radius, z * scaleY - (length * 0.5f));
                                position += offset;

                                float3 normal = new float3(cos * normalSign, sin * normalSign, 0f);
                                float4 tangent = new float4(tangentX, 0f, 0f, -1f);
                                float4 uv0 = new float4(x * uvScale.x, z * uvScale.y, 0f, 0f);

                                if (rotateUV) (uv0.y, uv0.x) = (uv0.x, uv0.y);
                                if (flipUV)
                                {
                                    uv0.y = 1 - uv0.y;
                                    tangent.w *= -1;
                                }

                                vertices[index] = new Vertex(position, normal, tangent, uv0);

                                angle += angleStep;
                                index++;
                            }
                        }
                    }
                    
                    int outerVertexCount = (int)hCount * zCount;

                    int capVertexStart = index;
                    if (caps)
                    {
                        float capUVScale = outerRadius * 2f;
                        var angleStepCap = (2 * math.PI) / radialSegments;
                        
                        //Combined loop for FRONT and BACK CAP VERTICES
                        for (int cap = 0; cap < 2; cap++)
                        {
                            bool isFront = cap == 0;
                            float z = (isFront ? -1f : 1f) * (length * 0.5f) + offset.z;
                            float3 normal = new float3(0f, 0f, isFront ? -1f : 1f);
                            float sign = isFront ? 1f : -1f;
                            
                            for (int i = 0; i < radialSegments; i++)
                            {
                                var angle = math.radians(startAngle) + angleStepCap * (i + 1);
                                float cos = math.cos(angle);
                                float sin = math.sin(angle);

                                //Outer vertex - polar UV coordinates
                                float outerU = 0.5f + (cos * 0.5f);
                                float outerV = 0.5f + (sin * 0.5f);

                                //Outer vertex
                                vertices[index] = new Vertex(
                                    new float3(cos * outerRadius, sin * outerRadius, z) + offset,
                                    normal,
                                    new float4(sign, 0f, 0f, -1),
                                    new float4(outerU * sign, outerV, 0f, 0f) * capUVScale
                                );
                                index++;

                                //Inner vertex - polar UV coordinates scaled by radius ratio
                                float radiusRatio = innerRadius / outerRadius;
                                float innerU = 0.5f + (cos * radiusRatio * 0.5f);
                                float innerV = 0.5f + (sin * radiusRatio * 0.5f);

                                //Inner vertex
                                vertices[index] = new Vertex(
                                    new float3(cos * innerRadius, sin * innerRadius, z) + offset,
                                    normal,
                                    new float4(sign, 0f, 0f, -1),
                                    new float4(innerU * sign, innerV, 0f, 0f) * capUVScale
                                );
                                index++;
                            }
                        }
                    }

                    //BUILD INDICES
                    index = 0;
                    
                    //OUTER CYLINDER TRIANGLES
                    for (uint y = 0; y < lengthSegments; y++)
                    {
                        for (uint x = 0; x < widthSegments; x++)
                        {
                            indices[index] = (ushort)((y * hCount) + x);
                            indices[index + 1] = (ushort)((y * hCount) + x + 1);
                            indices[index + 2] = (ushort)(((y + 1) * hCount) + x);

                            indices[index + 3] = (ushort)(((y + 1) * hCount) + x);
                            indices[index + 4] = (ushort)((y * hCount) + x + 1);
                            indices[index + 5] = (ushort)(((y + 1) * hCount) + x + 1);
                            index += 6;
                        }
                    }

                    if (hollow)
                    {
                        //INNER CYLINDER TRIANGLES (reversed winding order)
                        for (uint y = 0; y < lengthSegments; y++)
                        {
                            for (uint x = 0; x < widthSegments; x++)
                            {
                                uint innerOffset = (uint)outerVertexCount;

                                indices[index] = (ushort)(innerOffset + (y * hCount) + x);
                                indices[index + 1] = (ushort)(innerOffset + ((y + 1) * hCount) + x);
                                indices[index + 2] = (ushort)(innerOffset + (y * hCount) + x + 1);

                                indices[index + 3] = (ushort)(innerOffset + ((y + 1) * hCount) + x);
                                indices[index + 4] = (ushort)(innerOffset + ((y + 1) * hCount) + x + 1);
                                indices[index + 5] = (ushort)(innerOffset + (y * hCount) + x + 1);
                                index += 6;
                            }
                        }
                    }

                    if (caps)
                    {
                        //FRONT CAP TRIANGLES
                        for (int i = 0; i < radialSegments; i++)
                        {
                            int nextI = (i + 1) % (int)radialSegments;
                            ushort outerCurrent = (ushort)(capVertexStart + i * 2);
                            ushort innerCurrent = (ushort)(capVertexStart + i * 2 + 1);
                            ushort outerNext = (ushort)(capVertexStart + nextI * 2);
                            ushort innerNext = (ushort)(capVertexStart + nextI * 2 + 1);

                            //Triangle 1
                            indices[index] = outerCurrent;
                            indices[index + 1] = innerCurrent;
                            indices[index + 2] = outerNext;

                            //Triangle 2
                            indices[index + 3] = outerNext;
                            indices[index + 4] = innerCurrent;
                            indices[index + 5] = innerNext;

                            index += 6;
                        }

                        //BACK CAP TRIANGLES
                        int backCapStart = capVertexStart + (int)radialSegments * 2;
                        for (int i = 0; i < radialSegments; i++)
                        {
                            int nextI = (i + 1) % (int)radialSegments;
                            ushort outerCurrent = (ushort)(backCapStart + i * 2);
                            ushort innerCurrent = (ushort)(backCapStart + i * 2 + 1);
                            ushort outerNext = (ushort)(backCapStart + nextI * 2);
                            ushort innerNext = (ushort)(backCapStart + nextI * 2 + 1);

                            //Triangle 1 (reversed winding for back face)
                            indices[index] = outerCurrent;
                            indices[index + 1] = outerNext;
                            indices[index + 2] = innerCurrent;

                            //Triangle 2
                            indices[index + 3] = outerNext;
                            indices[index + 4] = innerNext;
                            indices[index + 5] = innerCurrent;

                            index += 6;
                        }
                    }
                }
                
                public void Dispose()
                {
                    if (vertices.IsCreated) vertices.Dispose();
                    if (indices.IsCreated) indices.Dispose();
                }
            }
            
            public static Mesh Create(float radius, bool hollow, float innerRadius, bool caps, int radialSegments, float length, float edgeLoopDistance, float2 uvTiling, float3 pivot, Alignment alignment)
            {
                Job job = new Job();
                job.Setup(radius, hollow, innerRadius, caps, radialSegments, length, edgeLoopDistance, uvTiling, pivot, alignment);

                JobHandle jobHandle =  job.Schedule();
                jobHandle.Complete();

                Mesh mesh = CreateMesh(job.vertices, job.indices);
                mesh.name = "Procedural Cylinder";
                job.Dispose();
                
                return mesh;
            }
        }

        public static class Plane
        {
            private struct Job : IJob
            {
                private float width, length, widthEdgeDistance, lengthEdgeDistance;
                private float3 offset;
                private float2 uvTiling;
                private bool rotateUV, flipUV, stretchUV;
                
                [ReadOnly]
                private NativeArray<float> curvature;
                private const int CurvatureResolution = 16;
                
                public NativeArray<Vertex> vertices;
                public NativeArray<ushort> indices;

                private int widthSegments, lengthSegments;
                private uint xCount, zCount;
                private float scaleX, scaleY;

                public void Setup(CurveMeshSettings.InputMesh settings, float width, float widthEdgeDistance, float lengthEdgeDistance, bool stretchUV, AnimationCurve curve, float curveScale, float3 minSize, Alignment alignment)
                {
                    this.width = Mathf.Max(minSize.x, width);
                    this.length = Mathf.Max(Mathf.Max(1f, lengthEdgeDistance), minSize.z);
                    
                    curvature = new NativeArray<float>(CurvatureResolution, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    float maxCurvature = -1f;
                    
                    //Cache the animation curve values
                    for (int i = 0; i < CurvatureResolution; i++)
                    {
                        float t = (float)i / (CurvatureResolution-1);
                        float value = curve.Evaluate(t) * curveScale;
                        
                        curvature[i] = value;
                        
                        maxCurvature = Mathf.Max(maxCurvature, Mathf.Abs(value));
                    }
                    offset = 0f;
                    
                    widthEdgeDistance = Mathf.Max(0.02f, widthEdgeDistance);
                    lengthEdgeDistance = Mathf.Max(0.02f, lengthEdgeDistance);
                    uvTiling = settings.uvTiling;
                    rotateUV = settings.rotateUV;
                    flipUV = settings.flipUV;
                    this.stretchUV = stretchUV;

                    widthSegments = Mathf.CeilToInt(this.width / widthEdgeDistance);
                    lengthSegments = Mathf.CeilToInt(this.length / lengthEdgeDistance);
                    xCount = (uint)(widthSegments + 1u);
                    zCount = (uint)(lengthSegments + 1u);
                    
                    scaleX = (this.width / widthSegments);
                    scaleY = (this.length / lengthSegments);
                    
                    int numTriangles = (widthSegments * lengthSegments * 6);
                    int numVertices = (int)(xCount * zCount);

                    vertices = new NativeArray<Vertex>(numVertices, Allocator.TempJob);
                    indices = new NativeArray<ushort>(numTriangles, Allocator.TempJob);
                }
        
                public void Execute()
                {
                    int index = 0;
                    
                    float uvFactorX = uvTiling.x / widthSegments * (stretchUV ? widthSegments / width : widthSegments);
                    float uvFactorY = uvTiling.y / lengthSegments * length;
                    
                    for (uint y = 0; y < zCount; y++)
                    {
                        for (uint x = 0; x < xCount; x++)
                        {
                            float t = (float)x / (xCount - 1);
                            
                            float heightCurve = EvaluateCurvature(t);

                            float3 position = new float3((x * scaleX) - (width * 0.5f), heightCurve, (y * scaleY) - (length * 0.5f));
                            
                            float derivative = SampleCurvatureDerivative(t, 1f/(xCount - 1));
                            float3 normal = math.normalize(new float3(-derivative, 1f, 0f));
                            
                            float4 uv = new float4((position.x + (width * 0.5f)) * uvFactorX, y * uvFactorY, 0, 0);
                            
                            //Apply offset after UV
                            position += offset;

                            if (rotateUV) (uv.x, uv.y) = (uv.y, uv.x);
                            if (flipUV) uv.y = 1 - uv.y;
                            
                            float4 tangent = new float4(1f, 0f, 0f, -1f);
                            if (uvTiling.x < 0 || flipUV) tangent.w *= -1;

                            vertices[index] = new Vertex(position, normal, tangent, uv);
                            
                            index++;
                        }
                    }
                    
                    index = 0;
                    for (int y = 0; y < lengthSegments; y++)
                    {
                        for (int x = 0; x < widthSegments; x++)
                        {
                            indices[index] = (ushort)((y * xCount) + x);
                            indices[index + 1] = (ushort)(((y + 1) * xCount) + x);
                            indices[index + 2] = (ushort)((y * xCount) + x + 1);

                            indices[index + 3] = (ushort)(((y + 1) * xCount) + x);
                            indices[index + 4] = (ushort)(((y + 1) * xCount) + x + 1);
                            indices[index + 5] = (ushort)((y * xCount) + x + 1);
                            index += 6;
                        }
                    }
                }
                
                private float EvaluateCurvature(float t)
                {
                    float i = t * (float)(CurvatureResolution - 1);
                    int prev = (int)math.floor(i);
                    int next = (int)math.floor(i + 1);
                    
                    if (next >= CurvatureResolution)
                    {
                        return curvature[^1];
                    }
                    if (prev < 0)
                    {
                        return curvature[0];
                    }

                    return math.lerp(curvature[prev], curvature[next], i - prev);
                }

                private float SampleCurvatureDerivative(float t, float stride)
                {
                    //Calculate derivative using finite differences
                    float epsilon = stride;
                    float tPrev = math.max(0f, t - epsilon);
                    float tNext = math.min(1f, t + epsilon);

                    float heightPrev = EvaluateCurvature(tPrev);
                    float heightNext = EvaluateCurvature(tNext);

                    //Derivative with respect to normalized t (0 to 1)
                    float derivative = (heightNext - heightPrev) / (tNext - tPrev);

                    //Scale by width to get derivative in world space
                    return derivative / width;
                }

                public void Dispose()
                {
                    vertices.Dispose();
                    indices.Dispose();
                    curvature.Dispose();
                }
            }
            
            public static Mesh Create(CurveMeshSettings.InputMesh settings, float width, float widthEdgeDistance, float lengthEdgeDistance, bool stretchUV, AnimationCurve curve, float curveScale, float3 minSize, Alignment alignment)
            {
                Job job = new Job();
                job.Setup(settings, width, widthEdgeDistance, lengthEdgeDistance, stretchUV, curve, curveScale, minSize, alignment);

                JobHandle jobHandle =  job.Schedule();
                jobHandle.Complete();

                Mesh mesh = CreateMesh(job.vertices, job.indices);
                mesh.name = "Procedural Plane";
                job.Dispose();
                
                return mesh;
            }
        }
        
        //Output vertex layout
        public struct Vertex
        {
            //Note: The order is important and what Unity excepts
            public readonly half4 position;
            public readonly half4 normal;
            public readonly half4 tangent;
            public readonly half4 uv0;

            //Creates a new vertex in the packed format
            public Vertex(float3 inputPosition, float3 inputNormal, float4 inputTangent, float4 inputUv0)
            {
                position = new half4((half3)inputPosition.xyz, (half)0);
                
                inputNormal = math.normalize(inputNormal);
                //Appears to have some issues with most meshes!
                //normal = new Structs.sbyte4((sbyte)(inputNormal.x * 127), (sbyte)(inputNormal.y * 127), (sbyte)(inputNormal.z * 127), 0);
                normal = new half4((half3)inputNormal, (half)0);
                //tangent = new Structs.sbyte4((sbyte)(inputTangent.x * 127), (sbyte)(inputTangent.y * 127), (sbyte)(inputTangent.z * 127), (sbyte)(inputTangent.w * 127));
                tangent = new half4(inputTangent);
                uv0 = (half4)inputUv0;
            }
        }
        
        public static readonly VertexAttributeDescriptor[] Attributes = new VertexAttributeDescriptor[]
        {
            new (VertexAttribute.Position, VertexAttributeFormat.Float16, 4),
            new (VertexAttribute.Normal, VertexAttributeFormat.Float16, 4),
            new (VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4),
            new (VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4),
        };
        
        private const MeshUpdateFlags noValidation = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds;
        
        private static Mesh CreateMesh(NativeArray<Vertex> vertices, NativeArray<ushort> triangles)
        {
            Mesh mesh = new Mesh();

            int vertexCount = vertices.Length;
            int indexCount = triangles.Length;
            
            mesh.SetVertexBufferParams(vertexCount, Attributes);
            
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0, noValidation);
            
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData(triangles, 0, 0, indexCount, noValidation);
            
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount), noValidation);
            
            //Calculate bounds
            //Maybe do this in the job?
            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);
            for (int i = 0; i < vertexCount; i++)
            {
                float3 position = vertices[i].position.xyz;
                
                min = math.min(min, position);
                max = math.max(max, position);
            }

            float3 size = max - min;
            Bounds bounds = new Bounds(Vector3.zero, size);
            mesh.bounds = bounds;
            
            //mesh.RecalculateTangents();
            //mesh.RecalculateNormals();

            return mesh;
        }
    }
}