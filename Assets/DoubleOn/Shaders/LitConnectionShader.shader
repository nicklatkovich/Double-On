Shader "Custom/Lit Connection" {
	Properties {
		_Color_1 ("Color 1", Color) = (0.5, 0, 0, 1)
		_Color_2 ("Color 2", Color) = (0, 0.5, 0.5, 1)
		_MainTex ("Main Albedo (RGB)", 2D) = "white" {}
		_MainTexOffset ("Main Albedo Offset", Vector) = (0, 0, 0, 0)
		_AltTexOffset ("Alt Albedo Offset", Vector) = (0, 0, 0, 0)
		_Anim_Speed ("Animation Speed", Float) = 1.0
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex: POSITION;
				float2 uv: TEXCOORD0;
			};

			struct v2f {
				float2 uv: TEXCOORD0;
				float4 vertex: SV_POSITION;
			};

			fixed3 _Color_1;
			fixed3 _Color_2;

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed2 _MainTexOffset;
			fixed2 _AltTexOffset;
			fixed _Anim_Speed;
			
			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag(v2f i): SV_Target {
				fixed4 mainTex = tex2D(_MainTex, i.uv - fixed2(0.0, _Time.y * _Anim_Speed) + _MainTexOffset);
				fixed4 altTex = tex2D(_MainTex, i.uv - fixed2(0.0, _Time.y * _Anim_Speed) + _AltTexOffset);
				fixed4 c = (mainTex + altTex) / 2.0;

				if (c.r < 0.48) c = fixed4(_Color_1, 1.0);
				else if (c.r > 0.52) c = fixed4(_Color_2, 1.0);
				else c = fixed4(0.0, 0.0, 0.0, 1.0);

				c.rgb = c.rgb * 0.75 + 0.1;

				return c;
			}
			ENDCG
		}
	}
}
