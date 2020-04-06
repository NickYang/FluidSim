Shader "FluidSim/Advection"
{
	Properties
	{
		_VelocityTex("Velocity Tex", 2D) = "black" {}
		_PhysicTex("Physic Tex", 2D) = "black" {}
	}

	SubShader
	{
		Pass//Advection   back trace的方式进行稳定平流
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

			// 速度RT
			uniform sampler2D  _VelocityTex;

			// 物理量RT, 可以是速度，颜色，温度等
			uniform sampler2D  _PhysicTex;

			// 纹素大小
			float4 _VelocityTex_TexelSize;

			v2f vert(appdata v)
			{
				v2f o;
				o.uv = v.uv;
				o.vertex = UnityObjectToClipPos(v.vertex.xyz);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				//平流公式：q(x, t+dt) = q(x - u(x,t)*dt, t)
				float2 coord = uv - unity_DeltaTime.z * tex2D(_VelocityTex, uv).xy * _VelocityTex_TexelSize;
				float4 result = tex2D(_PhysicTex, coord);

				return result;
			}
			ENDCG
		}
	}
}
