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
using Interpolators = UnityEngine.Splines.Interpolators;
using VertexColorChannel = sc.splinemesher.pro.runtime.SplineCurveMesher.VertexColorChannel;

namespace sc.splinemesher.pro.runtime
{
    /// <summary>
    /// Holds all the spline data in a Job-friendly format
    /// </summary>
    public struct SplineCurveMeshData
    {
        private bool isCreated;
        public bool IsCreated => isCreated;
        
        public NativeSplineData<float3> scale;
        public NativeSplineData<float> roll;
        public NativeSplineData<VertexColorChannel> red;
        public NativeSplineData<VertexColorChannel> green;
        public NativeSplineData<VertexColorChannel> blue;
        public NativeSplineData<VertexColorChannel> alpha;
        public NativeSplineData<float> conforming;

        [Flags]
        public enum DataType
        {
            None = 0,
            Scale = 1,
            Roll = 2,
            VertexColorRed = 4,
            VertexColorGreen = 8,
            VertexColorBlue = 16,
            VertexColorAlpha = 32,
            Conforming = 64
        }
        public DataType dataTypes;
        
        public void Setup(NativeSpline spline, SplineData<float3> scaleData, 
            SplineData<float> rollData,
            SplineData<VertexColorChannel> redData,
            SplineData<VertexColorChannel> greenData,
            SplineData<VertexColorChannel> blueData,
            SplineData<VertexColorChannel> alphaData,
            SplineData<float> conformingData)
        {
            //Dispose existing data before recreating
            if(isCreated) Dispose();
            
            scale = new NativeSplineData<float3>();
            scale.Create(spline, scaleData, new Interpolators.LerpFloat3());
            
            roll = new NativeSplineData<float>();
            roll.Create(spline, rollData, new Interpolators.LerpFloat());
            
            red = new NativeSplineData<VertexColorChannel>();
            red.Create(spline, redData, new VertexColorChannel.LerpVertexColor());
            green = new NativeSplineData<VertexColorChannel>();
            green.Create(spline, greenData, new VertexColorChannel.LerpVertexColor());
            blue = new NativeSplineData<VertexColorChannel>();
            blue.Create(spline, blueData, new VertexColorChannel.LerpVertexColor());
            alpha = new NativeSplineData<VertexColorChannel>();
            alpha.Create(spline, alphaData, new VertexColorChannel.LerpVertexColor());
            
            conforming = new NativeSplineData<float>();
            conforming.Create(spline, conformingData, new Interpolators.LerpFloat());
            
            //Reset
            dataTypes = 0;
            
            if (scale.HasData) dataTypes |= DataType.Scale;
            if (roll.HasData) dataTypes |= DataType.Roll;
            
            if (red.HasData) dataTypes |= DataType.VertexColorRed;
            if (green.HasData) dataTypes |= DataType.VertexColorGreen;
            if (blue.HasData) dataTypes |= DataType.VertexColorBlue;
            if (alpha.HasData) dataTypes |= DataType.VertexColorAlpha;

            if (conforming.HasData) dataTypes |= DataType.Conforming;
            
            //Debug.Log($"SplineMeshData created with {dataTypes} data");

            isCreated = true;
        }

        public bool Has(DataType type)
        {
            return (type & dataTypes) != 0;
        }

        public void Dispose()
        {
            scale.Dispose();
            roll.Dispose();
            conforming.Dispose();
            
            red.Dispose();
            green.Dispose();
            blue.Dispose();
            alpha.Dispose();

            isCreated = false;
        }
    }
}