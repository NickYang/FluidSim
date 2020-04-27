Shader "FluidSim/Display"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
		_SecondTex("SecondTex", 2D) = "black" {}

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

		uniform sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		uniform sampler2D _SecondTex;

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
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 


			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;

				float3 c = tex2D(_MainTex, i.uv).rgb;

				float3 lc = tex2D(_MainTex, i.lr.xy).rgb;
				float3 rc = tex2D(_MainTex, i.lr.zw).rgb;
				float3 tc = tex2D(_MainTex, i.tb.xy).rgb;
				float3 bc = tex2D(_MainTex, i.tb.zw).rgb;
				float dx = length(rc) - length(lc);
				float dy = length(tc) - length(bc);
				float3 n = normalize(float3(dx, dy, length(_MainTex_TexelSize.xy)));
				float3 l = float3(0.0, 0.0, 1.0);
				float diffuse = clamp(dot(n, l) + 0.7, 0.7, 1.0);
			
				float3 rd = normalize(float3(uv, 1.));
				float spec =pow(max(dot(reflect(-n, l), -rd), 0.), 12.);

				c = (c.rgb *(diffuse*float3(1, .97, .92)*2. + 0.5) + float3(1., .6, .2)*spec*600);

				return float4(sqrt(clamp(c.rgb, 0., 1.)), 1.);

			}
			ENDCG
		}
	}
}
