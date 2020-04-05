Shader "FluidSim/Diffusion"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
		_Viscosity("_Viscosity", Float) = 1
	}

	SubShader
	{
		CGINCLUDE
		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 lr : TEXCOORD1;
			float4 tb:  TEXCOORD2;
			float4 vertex : SV_POSITION;
		};

		uniform sampler2D  _MainTex;
		float4 _MainTex_TexelSize;
		float _Viscosity;
		v2f vert(appdata v)
		{
			v2f o;

			o.uv = v.uv;
			o.lr.xy = o.uv - float2(_MainTex_TexelSize.x, 0.0);
			o.lr.zw = o.uv + float2(_MainTex_TexelSize.x, 0.0);
			o.tb.xy = o.uv + float2(0.0, _MainTex_TexelSize.y);
			o.tb.zw = o.uv - float2(0.0, _MainTex_TexelSize.y);
			o.vertex = UnityObjectToClipPos(v.vertex.xyz);

			return o;
		}

		float4 jacobi(float4 LRBT, float4 bC, float alpha, float rbeta)
		{

			float4 ret = (LRBT + bC * alpha) * rbeta;
			return ret;

		}
		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			fixed4 frag(v2f i) : SV_Target
			{
				float4 vLR = i.lr;
				float4 vTB = i.tb;
				float2 vUv = i.uv;

				float4 L = tex2D(_MainTex, vLR.xy);
				float4 R = tex2D(_MainTex, vLR.zw);
				float4 T = tex2D(_MainTex, vTB.xy);
				float4 B = tex2D(_MainTex, vTB.zw);

				float4 bC = tex2D(_MainTex, vUv);

				float alpha =  1.0f / (_Viscosity*0.125);
				float rbeta = 1.0f / (4.0f + alpha);
				float4 diffusion = jacobi(L + R + B + T, bC, alpha, rbeta);

				float4 col = float4(diffusion.xy, 0.0, 1.0);
				return col;
			}
			ENDCG
		}

	}
}
