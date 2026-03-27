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
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Splines;
using static sc.splinemesher.pro.runtime.SplineCurveMesher;

namespace sc.splinemesher.pro.runtime
{
    //Cache SplineData into a NativeArray and offer Job-friendly evaluation functions
    public struct NativeSplineData<T> where T : unmanaged
    {
        private NativeArray<T> m_DataPoints;
        private NativeArray<float> m_DataIndices;
        private PathIndexUnit m_IndexUnit;
        private DataType dataType;
        
        private bool isAllocated;
        private bool hasData;
        /// <summary>
        /// Native arrays have been populated with valid data
        /// </summary>
        public bool HasData => hasData;

        private enum DataType
        {
            Float,
            Float2,
            Float3,
            Float4,
            VertexColor,
        }
        
        public void Create<TInterpolator>(NativeSpline spline, SplineData<T> data, TInterpolator interpolator) 
            where TInterpolator : IInterpolator<T>
        {
            if(isAllocated) Dispose();
            
            if (data == null || data.Count == 0)
            {
                //Note: if 0 data, the native array needs to at least be allocated
                m_DataPoints = new NativeArray<T>(0, Allocator.Persistent);
                m_DataIndices = new NativeArray<float>(0, Allocator.Persistent);
                isAllocated = true;
                
                //Has dummy data, so false
                hasData = false;
                
                return;
            }
            
            int dataCount = data.Count;
            m_DataPoints = new NativeArray<T>(dataCount, Allocator.Persistent);
            m_DataIndices = new NativeArray<float>(dataCount, Allocator.Persistent);
            
            m_IndexUnit = data.PathIndexUnit;
            
            int dataSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();
            switch (dataSize)
            {
                //float is 4 bytes
                case 4: dataType = DataType.Float; break;
                //float2 is 8 bytes (2 * 4)
                case 8: dataType = DataType.Float2; break;
                //float3 is 12 bytes (3 * 4)
                case 12: dataType = DataType.Float3; break;
                //float4 is 16 bytes (4 * 4)
                case 16: dataType = DataType.Float4; break;
            }
            //Also 16 bytes, since it's a float4
            if (typeof(T) == typeof(VertexColorChannel)) dataType = DataType.VertexColor;

            for (int i = 0; i < dataCount; i++)
            {
                float t = data[i].Index;

                //Store the indices, in whatever PathIndexUnit format they are
                //This is to remap the normalized `t` value to the actual position when sampling
                m_DataIndices[i] = t;

                m_DataPoints[i] = data.Evaluate(spline, t, m_IndexUnit, interpolator);
            }
            
            isAllocated = true;
            hasData = true;
        }

        [BurstCompile]
        //Note: t-value should be normalized 0-1 value! Cached data will already have been converted to normalized indices
        public T Evaluate(NativeSpline spline, float t, bool easeInOut = false)
        {
            if (!hasData) return default;
            
            //Convert t to match the stored PathIndexUnit if needed
            t = spline.ConvertIndexUnit(t, PathIndexUnit.Normalized, m_IndexUnit);
            
            var indices = GetIndices(t, spline.GetLength(), spline.Knots.Length, spline.Closed, easeInOut);

            int prev = indices.Item1;
            int next = indices.Item2;

            int count = m_DataPoints.Length;

            if (next >= count) return m_DataPoints[count - 1];
            if (prev < 0) return m_DataPoints[0];

            return Lerp(m_DataPoints[prev], m_DataPoints[next], indices.Item3);
        }
        
        [BurstCompile]
        private T Lerp(T a, T b, float t)
        {
            if (dataType == DataType.Float)
            {
                float aFloat = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, float>(ref a);
                float bFloat = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, float>(ref b);
                float result = math.lerp(aFloat, bFloat, t);
                return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<float, T>(ref result);
            }
            if (dataType == DataType.Float3)
            {
                float3 aFloat3 = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, float3>(ref a);
                float3 bFloat3 = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, float3>(ref b);
                float3 result = math.lerp(aFloat3, bFloat3, t);
                return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<float3, T>(ref result);
            }
            if (dataType == DataType.VertexColor)
            {
                VertexColorChannel aChannel = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, VertexColorChannel>(ref a);
                VertexColorChannel bChannel = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, VertexColorChannel>(ref b);

                VertexColorChannel result = new VertexColorChannel
                {
                    value = math.lerp(aChannel.value, bChannel.value, t),
                    blend = t < 0.5f ? aChannel.blend : bChannel.blend
                };

                return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<VertexColorChannel, T>(ref result);
            }
            if (dataType == DataType.Float4)
            {
                float4 aFloat3 = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, float4>(ref a);
                float4 bFloat3 = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<T, float4>(ref b);
                float4 result = math.lerp(aFloat3, bFloat3, t);
                return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<float4, T>(ref result);
            }

            throw new Exception($"Unsupported data type: {typeof(T)}");
        }
        
        [BurstCompile]
        //Converted to Job-friendly format from the `UnityEngine.Splines.SplineData` class
        private (int, int, float) GetIndices(float t, float splineLength, int knotCount, bool closed, bool easeInOut)
        {
            int Count = m_DataPoints.Length;
            if(Count < 1)
                return default;
            
            float splineLengthInIndexUnits = splineLength;
            if(m_IndexUnit == PathIndexUnit.Normalized)
                splineLengthInIndexUnits = 1f;
            else if(m_IndexUnit == PathIndexUnit.Knot)
                splineLengthInIndexUnits = closed ? knotCount : knotCount - 1;

            float maxDataPointTime = m_DataIndices[^1];
            float maxRevolutionLength = math.ceil(maxDataPointTime / splineLengthInIndexUnits) * splineLengthInIndexUnits;
            float maxTime = closed ? math.max(maxRevolutionLength, splineLengthInIndexUnits) : splineLengthInIndexUnits;
            
            if(closed)
            {
                if(t < 0f)
                    t = maxTime + t % maxTime;
                else
                    t = t % maxTime;
            }
            else
                t = math.clamp(t, 0f, maxTime);
            
            int index = BinarySearch(t, Count);
            int fromIndex = ResolveBinaryIndex(index, closed, Count);
            int toIndex = closed ? ( fromIndex + 1 ) % Count : math.clamp(fromIndex + 1, 0, Count - 1);

            float fromTime = m_DataIndices[fromIndex];
            float toTime = m_DataIndices[toIndex];

            if(fromIndex > toIndex)
                toTime += maxTime;

            if(t < fromTime && closed)
                t += maxTime;

            if (math.abs(fromTime - toTime) < 0.0001f)
                return ( fromIndex, toIndex, fromTime );

            float interpolator = math.abs(math.max(0f, t - fromTime) / (toTime - fromTime));

            if (easeInOut) interpolator = Utilities.EaseInOut(interpolator);

            return ( fromIndex, toIndex, interpolator );
        }
        
        static int Wrap(int value, int lowerBound, int upperBound)
        {
            int range_size = upperBound - lowerBound + 1;
            if(value < lowerBound)
                value += range_size * ( ( lowerBound - value ) / range_size + 1 );
            return lowerBound + ( value - lowerBound ) % range_size;
        }
        
        int ResolveBinaryIndex(int index, bool wrap, int count)
        {
            index = ( index < 0 ? ~index : index ) - 1;
            if(wrap)
                index = Wrap(index, 0, count - 1);
            return math.clamp(index, 0, count - 1);
        }
        
        [BurstCompile]
        private int BinarySearch(float searchValue, int count)
        {
            if (count == 0) return -1;

            int left = 0;
            int right = count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                float midValue = m_DataIndices[mid];

                if (math.abs(midValue - searchValue) < 0.0001f) //Exact match
                    return mid;

                if (midValue < searchValue)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            //Not found, return bitwise complement of the insertion point
            return ~left;
        }

        public void Dispose()
        {
            m_DataPoints.Dispose();
            m_DataIndices.Dispose();

            isAllocated = false;
            hasData = false;
        }
    }
}