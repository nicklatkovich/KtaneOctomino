Shader "Octomino/FaceShader" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags {
			"Queue" = "Transparent-200"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		LOD 200
		ZTest On
		ZWrite Off
		Lighting Off
		Fog { Mode Off }
		AlphaToMask On

		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float4 _Color;

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				float2 pos = i.uv - 0.5;
				float a = max(abs(pos.x), abs(pos.y)) > 0.4 ? 1.0 : 0.25;
				// if (max(abs(pos.x), abs(pos.y)) <= 0.4) discard;
				return float4(_Color.xyz, a);
			}
			ENDCG
		}

		Pass {
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float4 _Color;

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				float2 pos = i.uv - 0.5;
				float a = max(abs(pos.x), abs(pos.y)) > 0.4 ? 1.0 : 0.75;
				return float4(_Color.xyz, a);
			}
			ENDCG
		}
	}
}
