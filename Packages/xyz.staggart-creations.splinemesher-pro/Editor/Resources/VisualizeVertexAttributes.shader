Shader "Hidden/VisualizeVertexAttributes"
{
	Properties
	{
		[KeywordEnum(Color,UV0,UV2,Normals)] _Display("Display",int) = 0
		
		[Space]
		
		[Enum(Red,0,Green,1,Blue,2,Alpha,3,All,4)] _ColorChannel ("Color Channel", Float) = 0
		[Enum(X,0,Y,1,Z,2,W,3)] _UVChannel ("UV Channel", Float) = 0
		[Toggle] _UVChecker ("UV Checkers", Float) = 0
		
		[Space]

		[Toggle] _Transparent ("Transparent", Float) = 0
		[Toggle] _Shaded ("Shaded", Float) = 0
		[MaterialEnum(Both,0,Front,1,Back,2)] _Cull("Render faces", Float) = 2
	}
	
	SubShader
	{
		Tags { "RenderType"="Transparent" "RenderQueue"="Transparent" }
		
		Blend SrcAlpha OneMinusSrcAlpha
		Cull [_Cull]
		ZWrite On
		//ZTest Always
		
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			
			#include "UnityCG.cginc"

			#pragma multi_compile_local_fragment _DISPLAY_COLOR _DISPLAY_UV0 _DISPLAY_UV2 _DISPLAY_NORMALS
			
			uint _ColorChannel;
			uint _UVChannel;
			bool _Transparent;
			bool _UVChecker;
			bool _Shaded;
			
			struct Attributes
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
				float4 uv1 : TEXCOORD0;
				float4 uv2 : TEXCOORD2;
			};

			struct Varyings
			{
				float4 vertex : SV_POSITION;
				float4 color : TEXCOORD0;
				float3 normal : TEXCOORD1;
				float4 uv0 : TEXCOORD2;
				float4 uv2 : TEXCOORD3;
				float3 worldPos : TEXCOORD4;
			};
			
			Varyings Vert (Attributes v)
			{
				Varyings o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.normal = normalize(UnityObjectToWorldNormal(v.normal));
				o.uv0 = v.uv1;
				o.uv2 = v.uv2;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				return o;
			}

			float4 RenderVertexColor(Varyings i)
			{
				float alpha = _Transparent ? i.color.a : 1.0;

				//RGB(A)
				if(_ColorChannel == 4) return float4(i.color.rgb, alpha);

				//Specific channel
				float value = i.color[_ColorChannel];
				
				return float4(value.xxx, alpha);
			}

			inline float mod(float a, float b) { return a - (b * floor(a / b)); }

            float Checker(float2 uv)
            {
                float fmodResult = mod(floor(uv.x) + floor(uv.y), 2.0);
                return max(sign(fmodResult), 0.0);
            }
			
			float4 RenderUV(float4 uv)
			{
				//Specific channel
				float value = uv[_UVChannel];
				
				float3 color = smoothstep(0, 1, value.xxx);
				
				if (_UVChecker)
				{
					float texelChecker = Checker(uv.xy * 2.0);
					color = lerp(1.0, 0.6, texelChecker);
				}
				
				return float4(color, 1.0);
			}
			
			float4 Frag(Varyings i) : SV_Target
			{
				float4 color = float4(0, 0, 0, 1.0);
				
				#if _DISPLAY_COLOR
				color = RenderVertexColor(i);
				#endif
				
				#if _DISPLAY_UV0
				color = RenderUV(i.uv0);
				#endif
				
				#if _DISPLAY_UV2
				color = RenderUV(i.uv2);
				#endif

				#if _DISPLAY_NORMALS
				color = float4(i.normal * 0.5 + 0.5, 1.0);
				#endif
				
				if (_Shaded)
				{
					float3 positionWS = i.worldPos;
					float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - positionWS);
					float VdotN = max(0.0, dot(i.normal, viewDir));
					
					float diffuse = saturate((VdotN * 0.5 + 0.5));
					color.rgb *= diffuse;
				}
				
				return color;
			}
			ENDCG
		}
	}
}