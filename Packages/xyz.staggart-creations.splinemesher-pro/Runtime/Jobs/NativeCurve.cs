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

using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace sc.splinemesher.pro.runtime
{
    [BurstCompile]
    public struct NativeCurve
    {
        [ReadOnly]
        private NativeArray<float> samples;
        private const int SampleCount = 16;
        
        public NativeCurve(AnimationCurve curve, Allocator allocator)
        {
            samples = new NativeArray<float>(SampleCount, allocator, NativeArrayOptions.UninitializedMemory);
            
            Update(curve);
        }

        public void Update(AnimationCurve curve)
        {
            for (int i = 0; i < SampleCount; i++)
            {
                float t = (float)i / (SampleCount - 1);
                samples[i] = curve.Evaluate(t);
            }
        }

        public float Sample(float t)
        {
            t = math.clamp(t, 0f, 1f);
            float scaled = t * (SampleCount - 1);
            int index = (int)math.floor(scaled);
            int nextIndex = math.min(index + 1, SampleCount - 1);
            float frac = scaled - index;

            return math.lerp(samples[index], samples[nextIndex], frac);
        }

        public void Dispose()
        {
            if (samples.IsCreated)
                samples.Dispose();
        }
    }
}