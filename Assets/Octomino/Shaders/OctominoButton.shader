Shader "Octomino/Button" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_BorderSize ("BorderSize", float) = 0.6
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
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			float _BorderSize;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float2 pos2 = (i.uv - 0.5) * 2;
				float pos = max(abs(pos2.x), abs(pos2.y));
				if (1.0 - pos >= _BorderSize) discard;
				return _Color;
			}
			ENDCG
		}
	}
}
