Shader "GPUSKinning/GPUSkinnedObject"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_AnimMapArray("AnimationMap", 2DArray) = "white" {}
		_AnimLength("AnimationLength", Float) = 0
		_AnimTime("AnimationTime", Float) = 0
		_Index("AnimationIndex", Float) = 0
		_Alpha("Alpha", Float) = 1
	}

	SubShader
	{
		Tags{ "Queue" = "Geometry" }
		Blend Off
		LOD 100
		Cull Back

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			struct appdata
			{
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			UNITY_DECLARE_TEX2DARRAY(_AnimMapArray);
			float4 _AnimMapArray_TexelSize;

			UNITY_INSTANCING_BUFFER_START(MyProperties)

			UNITY_DEFINE_INSTANCED_PROP(float, _AnimLength)
			#define _AnimLength_arr MyProperties
			UNITY_DEFINE_INSTANCED_PROP(float, _AnimTime)
			#define _AnimTime_arr MyProperties
			UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
			#define _Alpha_arr MyProperties
			UNITY_DEFINE_INSTANCED_PROP(float, _Index)
			#define _Index_arr MyProperties

			UNITY_INSTANCING_BUFFER_END(MyProperties)

			v2f vert(appdata v, uint vid : SV_VertexID)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				fixed f = UNITY_ACCESS_INSTANCED_PROP(_AnimTime_arr, _AnimTime) / UNITY_ACCESS_INSTANCED_PROP(_AnimLength_arr, _AnimLength);

				fmod(f, 1.0);

				fixed x = (vid + 0.5) * _AnimMapArray_TexelSize.x;
				fixed y = f;

				fixed4 pos = UNITY_SAMPLE_TEX2DARRAY_LOD(_AnimMapArray, float3(x, y, UNITY_ACCESS_INSTANCED_PROP(_Index_arr, _Index)), 100);

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.vertex = UnityObjectToClipPos(pos);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				fixed4 col = tex2D(_MainTex, i.uv);
				col.a = UNITY_ACCESS_INSTANCED_PROP(_Alpha_arr, _Alpha);

				return col;
			}

			ENDCG
		}
	}
}