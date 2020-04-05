Shader "FluidSim/Divergence"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}//速度tex

	}
	SubShader
	{
		Pass//divergence  求取当前速度的散度
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

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

			fixed4 frag(v2f i) : SV_Target
			{
				float4 vLR = i.lr;
				float4 vTB = i.tb;
				float2 vUv = i.uv;

				float L = tex2D(_MainTex, vLR.xy).x;
				float R = tex2D(_MainTex, vLR.zw).x;
				float T = tex2D(_MainTex, vTB.xy).y;
				float B = tex2D(_MainTex, vTB.zw).y;
				float2 C = tex2D(_MainTex, vUv).xy;

				// 边界处理
				if (vLR.x < 0.0) { L = -C.x; }
				if (vLR.z > 1.0) { R = -C.x; }
				if (vTB.y > 1.0) { T = -C.y; }
				if (vTB.w < 0.0) { B = -C.y; }
				//根据散度的概念，散度等于x方向的相邻像素值差值和y方向相邻像素值差值之和，再除2dx
				float div = 0.5 * (R - L + T - B);
				float4 col = float4(div, 0.0, 0.0, 1.0);
				return col;
			}
			ENDCG
		}

					
	}
}
