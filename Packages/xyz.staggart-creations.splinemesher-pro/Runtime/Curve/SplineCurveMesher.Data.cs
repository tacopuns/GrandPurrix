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

namespace sc.splinemesher.pro.runtime
{
    public partial class SplineCurveMesher
    {
        #region Scale
        public List<SplineData<float3>> scaleData = new List<SplineData<float3>>();

        public SplineData<float3> GetScaleData(int splineIndex)
        {
            if (scaleData == null) return null;

            if (splineIndex >= scaleData.Count)
            {
                scaleData.Add(new SplineData<float3>());
                scaleData[splineIndex].PathIndexUnit = PathIndexUnit.Normalized;
                scaleData[splineIndex].DefaultValue = new float3(1f);
            }
            
            return scaleData[splineIndex];
        }
        
        public void ResetScaleData()
        {
            scaleData = new List<SplineData<float3>>(splineCount);
        }
        #endregion
        
        #region Roll
        public List<SplineData<float>> rollData = new List<SplineData<float>>();

        public SplineData<float> GetRollData(int splineIndex)
        {
            if (rollData == null) return null;

            if (splineIndex >= rollData.Count)
            {
                rollData.Add(new SplineData<float>());
                rollData[splineIndex].PathIndexUnit = PathIndexUnit.Normalized;
                rollData[splineIndex].DefaultValue = 0;
            }
            
            return rollData[splineIndex];
        }

        public void ResetRollData()
        {
            rollData = new List<SplineData<float>>(splineCount);
        }
        #endregion
        
        #region Vertex Color
        [Serializable]
        public struct VertexColorChannel
        {
            public float value;
            public bool blend;

            ///<summary>Implicit conversion to float</summary>
            public static implicit operator float(VertexColorChannel value) => value.value;
            ///<summary>Implicit conversion from float</summary>
            public static implicit operator VertexColorChannel(float value) => new () { value = value };
            
            public struct LerpVertexColor : IInterpolator<VertexColorChannel>
            {
                public VertexColorChannel Interpolate(VertexColorChannel a, VertexColorChannel b, float t)
                {
                    //Only interpolate the value field, preserve the blend field from the appropriate data point
                    //Use 'a' blend if t < 0.5, otherwise use 'b' blend
                    VertexColorChannel result = new VertexColorChannel
                    {
                        value = math.lerp(a.value, b.value, t),
                        blend = t < 0.5f ? a.blend : b.blend
                    };
                    
                    return result;
                }
            }
        }
        
        public List<SplineData<VertexColorChannel>> vertexColorRedData = new List<SplineData<VertexColorChannel>>();
        public List<SplineData<VertexColorChannel>> vertexColorGreenData = new List<SplineData<VertexColorChannel>>();
        public List<SplineData<VertexColorChannel>> vertexColorBlueData = new List<SplineData<VertexColorChannel>>();
        public List<SplineData<VertexColorChannel>> vertexColorAlphaData = new List<SplineData<VertexColorChannel>>();
        
        public SplineData<VertexColorChannel> GetColorData(int splineIndex, int channel)
        {
            List<SplineData<VertexColorChannel>> data = null;
            switch (channel)
            {
                case 0: data = vertexColorRedData;
                    break;
                case 1: data = vertexColorGreenData;
                    break;
                case 2: data = vertexColorBlueData;
                    break;
                case 3: data = vertexColorAlphaData;
                    break;
            }
            
            if(data == null) return null;
            
            if (splineIndex >= data.Count)
            {
                data.Add(new SplineData<VertexColorChannel>());
                data[splineIndex].PathIndexUnit = PathIndexUnit.Normalized;
                VertexColorChannel defaultValue = new VertexColorChannel();
                
                //Alpha channel
                //if (channel == 3) defaultValue.value = 1f;

                data[splineIndex].DefaultValue = defaultValue;
            }
            
            return data[splineIndex];
        }
        
        
        public void ResetVertexColorData()
        {
            vertexColorRedData = new List<SplineData<VertexColorChannel>>(splineCount);
            vertexColorGreenData = new List<SplineData<VertexColorChannel>>(splineCount);
            vertexColorBlueData = new List<SplineData<VertexColorChannel>>(splineCount);
            vertexColorAlphaData = new List<SplineData<VertexColorChannel>>(splineCount);
        }
        #endregion
        
        #region Conforming
        public List<SplineData<float>> conformingData = new List<SplineData<float>>();

        public SplineData<float> GetConformingData(int splineIndex)
        {
            if (conformingData == null) return null;

            if (splineIndex >= conformingData.Count)
            {
                conformingData.Add(new SplineData<float>());
                conformingData[splineIndex].PathIndexUnit = PathIndexUnit.Normalized;
                conformingData[splineIndex].DefaultValue = 1;
            }
            
            return conformingData[splineIndex];
        }

        public void ResetConformingData()
        {
            conformingData = new List<SplineData<float>>(splineCount);
        }
        #endregion
        
        public override void ValidateData()
        {
            void ConvertIndexUnityIfNeeded<T>(ref List<SplineData<T>> data, PathIndexUnit targetUnit)
            {
                //One for every spline
                for (int j = 0; j < data.Count; j++)
                {
                    if (j > splineCount)
                    {
                        //Debug.LogWarning($"Spline index {j} is out of range for the number of splines in the mesher. Ensure the deletion of splines happens gracefully");
                        continue;
                    }
                    //Index unit has changed, convert the index value
                    if (data[j].PathIndexUnit != targetUnit)
                    {
                        Spline spline = splineContainer.Splines[j];
                        
                        ConvertIndexUnit(spline, ref data, j, targetUnit);
                    }
                }
            }
            
            //Ensure that the index unit configured in settings matches how its configured in the spline data
            ConvertIndexUnityIfNeeded<float3>(ref scaleData, settings.scale.pathIndexUnit);
            ConvertIndexUnityIfNeeded<float>(ref rollData, settings.rotation.pathIndexUnit);
            ConvertIndexUnityIfNeeded<VertexColorChannel>(ref vertexColorRedData, settings.color.pathIndexUnit);
            ConvertIndexUnityIfNeeded<VertexColorChannel>(ref vertexColorGreenData, settings.color.pathIndexUnit);
            ConvertIndexUnityIfNeeded<VertexColorChannel>(ref vertexColorBlueData, settings.color.pathIndexUnit);
            ConvertIndexUnityIfNeeded<VertexColorChannel>(ref vertexColorAlphaData, settings.color.pathIndexUnit);
            ConvertIndexUnityIfNeeded<float>(ref conformingData, settings.conforming.pathIndexUnit);
            
        }
    }
}