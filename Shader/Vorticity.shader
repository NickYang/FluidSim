Shader "FluidSim/Vorticity"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
		_SecondTex("_SecondTex", 2D) = "black" {}
		curl("curl", Float) = 30
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

		ENDCG
		Pass //0
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform float curl;

			fixed4 frag(v2f i) : SV_Target
			{
				float4 vLR = i.vLR;
				float4 vTB = i.vTB;
				float2 vUv = i.vUv;

				float L = tex2D(_MainTex, vLR.xy).x;
				float R = tex2D(_MainTex, vLR.zw).x;
				float T = tex2D(_MainTex, vTB.xy).x;
				float B = tex2D(_MainTex, vTB.zw).x;
				float C = tex2D(_MainTex, vUv).x;

				// 像素的两个方向的旋度差作为一个外力
				float2 force = 0.5 * float2(abs(T) -abs(B), abs(R)- abs(L));
				force /= length(force) + 0.0001;
				force *= curl * C;
				force.xy *=float2( 1.0,-1.0);

				float2 vel = tex2D(_SecondTex, vUv).xy;
				float4 col = float4(vel + force * unity_DeltaTime.x, 0.0, 1.0);
				return col;
			}
			ENDCG

		}
	}
}
