Shader "FluidSim/CirclePaint"
{	
	Properties
	{
		_MainTex("MainTex", 2D) = "black" {}
	}

    SubShader
    {
        Pass
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
			//pointAndRadius:xy for point pos, z for raidus
			uniform float4 pointAndRadius;
			uniform float4 color;

			fixed4 frag(v2f i) : SV_Target
			{
				float2 p = i.uv - pointAndRadius.xy;
				//绘制一个由中心向四周颜色渐变到黑色的圆
				float3 circle = exp(-dot(p, p) / pointAndRadius.z) * color.xyz;
				float3 base = tex2D(_MainTex, i.uv).xyz;
				//和source buffer叠加
				float4 col = float4(base + circle, 1.0);

				return col;
			}
            ENDCG
        }
    }
}
