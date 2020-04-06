Shader "FluidSim/Pressure"
{
	Properties
	{
		_MainTex("PressureTex", 2D) = "black" {}
		_SecondTex("DivergenceTex", 2D) = "black" {}
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
			float2 vUv : TEXCOORD0;
			float4 vLR : TEXCOORD1;
			float4 vTB:  TEXCOORD2;
			float4 vertex : SV_POSITION;
		};

		uniform sampler2D  _MainTex;
		float4 _MainTex_TexelSize;
		uniform sampler2D  _SecondTex;

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

		float jacobi(float LRBT, float bC, float alpha, float rbeta)
		{

			float ret = (LRBT + bC * alpha) * rbeta;
			return ret;

		}
		ENDCG



		Pass//pressure  通过散度求当前压强
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			fixed4 frag(v2f i) : SV_Target
			{
				float4 vLR = i.vLR;
				float4 vTB = i.vTB;
				float2 vUv = i.vUv;

				float L = tex2D(_MainTex, vLR.xy).x;
				float R = tex2D(_MainTex, vLR.zw).x;
				float T = tex2D(_MainTex, vTB.xy).x;
				float B = tex2D(_MainTex, vTB.zw).x;

				float divergence = tex2D(_SecondTex, vUv).x;
				float pressure = jacobi(L + R + B + T, divergence, -1, 0.25);

				float4 col = float4(pressure, 0.0, 0.0, 1.0);
				return col;
			}
			ENDCG
		}

	}
}
