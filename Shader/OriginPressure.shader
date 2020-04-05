Shader "FluidSim/OriginPressure"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
		value("value", Float) = 0.8
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		CGINCLUDE

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		uniform sampler2D  _MainTex;

		v2f vert(appdata v)
		{
			v2f o;

			o.uv = v.uv;
			o.vertex = UnityObjectToClipPos(v.vertex.xyz);

			return o;
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			uniform float value;

			fixed4 frag(v2f i) : SV_Target
			{
				return value;
			}
			ENDCG
		}
	}
}
