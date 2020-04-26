Shader "FluidSim/Display"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		CGINCLUDE

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 vUv : TEXCOORD0;
			float4 vLR : TEXCOORD1;
			float4 vTB:  TEXCOORD2;
			float4 vertex : SV_POSITION;
		};

		uniform sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		uniform sampler2D _SecondTex;

		v2f vert(appdata v)
		{
			v2f o;

			o.vUv = v.uv;
			o.vLR.xy = o.vUv - float2(_MainTex_TexelSize.x, 0.0);
			o.vLR.zw = o.vUv + float2(_MainTex_TexelSize.x, 0.0);
			o.vTB.xy = o.vUv + float2(0.0, _MainTex_TexelSize.y);
			o.vTB.zw = o.vUv - float2(0.0, _MainTex_TexelSize.y);
			o.vertex = UnityObjectToClipPos(v.vertex.xyz);

			return o;
		}
		float3 linearToGamma(float3 color) {
			color = max(color, float3(0,0,0));
			return max(1.055 * pow(color, 0.416666667) - 0.055, 0);
		}

		ENDCG
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 

			#pragma multi_compile SHADING

			#pragma multi_compile BLOOM

			#include "UnityCG.cginc"

			fixed4 frag(v2f i) : SV_Target
			{
				float3 c = tex2D(_MainTex, i.vUv).rgb;

				float3 lc = tex2D(_MainTex, i.vLR.xy).rgb;
				float3 rc = tex2D(_MainTex, i.vLR.zw).rgb;
				float3 tc = tex2D(_MainTex, i.vTB.xy).rgb;
				float3 bc = tex2D(_MainTex, i.vTB.zw).rgb;
				float dx = length(rc) - length(lc);
				float dy = length(tc) - length(bc);
				float3 n = normalize(float3(dx, dy, length(_MainTex_TexelSize.xy)));
				float3 l = float3(0.0, 0.0, 1.0);
				float diffuse = clamp(dot(n, l) + 0.7, 0.7, 1.0);
				c *= diffuse;

			
				float4 col = float4(cr.rgb, 1.0);
				return col;
			}
			ENDCG
		}
	}
}
