Shader "Fluid/GradientSubtract"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
		_SecondTex("_SecondTex", 2D) = "black" {}

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
			float2 uv: TEXCOORD0;
			float4 lr: TEXCOORD1;
			float4 tb: TEXCOORD2;
			float4 vertex : SV_POSITION;
		};

		uniform sampler2D  _MainTex;
		float4 _MainTex_TexelSize;
		uniform sampler2D  _SecondTex;

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


		ENDCG
		Pass//gradient 减去当前压力的梯度，得到无散度的速度场
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			fixed4 frag(v2f i) : SV_Target
			{
				float4 vLR = i.lr;
				float4 vTB = i.tb;
				float2 vUv = i.uv;
				float L = tex2D(_MainTex, vLR.xy).x;
				float R = tex2D(_MainTex, vLR.zw).x;
				float T = tex2D(_MainTex, vTB.xy).x;
				float B = tex2D(_MainTex, vTB.zw).x;
				float2 velocity = tex2D(_SecondTex, vUv).xy;
				// R-L, T-B为求压强两个轴向的梯度，再用速度减去梯度
				velocity.xy -= float2(R - L, T - B);
				float4 col = float4(velocity, 0.0, 1.0);
				return col;
			}
			ENDCG
		}
	}
}
