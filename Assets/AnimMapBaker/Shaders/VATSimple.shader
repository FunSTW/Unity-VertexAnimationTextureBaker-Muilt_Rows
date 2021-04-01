/*
Created by jiadong chen
Modified by funs
*/


Shader "funs/VATSimple"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[NoScaleOffset]_AnimMap ("Anim Map", 2D) = "white" {}
		_AnimLen ("Anim Length (Speed)", Float) = 0
		[Space(10)][MaterialToggle(VATMultipleRows_ON)] _VATMultipleRows("Multi-Rows", Float) = 0
		_AnimOffsetYPixel("Multi-Rows Per-Colume Height Pixel", Int) = 16
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			Cull off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "VAT.cginc"
			#include "UnityCG.cginc"
			#pragma multi_compile __ VATMultipleRows_ON
			#pragma target 3.5
			struct appdata
			{
				float2 uv : TEXCOORD0;
				float4 pos : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;
			half4 _MainTex_ST;

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(half, _Diverse)
			UNITY_INSTANCING_BUFFER_END(Props)

			v2f vert (appdata v, uint vid : SV_VertexID)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				half diverse = UNITY_ACCESS_INSTANCED_PROP(Props, _Diverse);

				half4 pos = GetSampledVATVertPos(vid,_Time.y,diverse);
				v2f o;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.vertex = UnityObjectToClipPos(pos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
