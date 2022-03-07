Shader "Custom/Button" {
	Properties {
		_Color_1 ("Color 1", Color) = (0.5, 0, 0, 1)
		_Color_2 ("Color 2", Color) = (0, 0.5, 0.5, 1)
		_MainTex ("Main Albedo (RGB)", 2D) = "white" {}
		_MainTexOffset ("Main Albedo Offset", Vector) = (0, 0, 0, 0)
		_AltTexOffset ("Alt Albedo Offset", Vector) = (0, 0, 0, 0)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		struct Input {
			fixed2 uv_MainTex;
		};

		fixed3 _Color_1;
		fixed3 _Color_2;
		sampler2D _MainTex;
		fixed2 _MainTexOffset;
		fixed2 _AltTexOffset;
		half _Glossiness;
		half _Metallic;

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 mainTex = tex2D(_MainTex, IN.uv_MainTex + _MainTexOffset);
			fixed4 altTex = tex2D(_MainTex, IN.uv_MainTex + _AltTexOffset);
			fixed4 c = (mainTex + altTex) / 2.0;

			// c = fixed4(c.r < 0.5 ? _Color_1 : _Color_2, 1.0);

			if (c.r < 0.48) c = fixed4(_Color_1, 1.0);
			else if (c.r > 0.52) c = fixed4(_Color_2, 1.0);
			else c = fixed4(0.0, 0.0, 0.0, 1.0);

			c.rgb = c.rgb * 0.5 + 0.1;

			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
