Shader "Octomino/DisplayBackWithTimer" {
	Properties {
		_Timer ("Timer", float) = 0
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float _Timer;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float2 pos2 = (i.uv - 0.5) * 2;
				float rad = sqrt(pos2.x * pos2.x + pos2.y * pos2.y);
				if (rad > 1) discard;
				if (rad < 0.95) return float4(0, 0, 0, 1);
				float angle = (1 - (atan2(pos2.y, pos2.x) / 3.1415926535 / 2 + 0.5) + 0.75) % 1;
				float value = angle < _Timer ? 0.7 : 0;
				return float4(value, value, value, 1);
			}
			ENDCG
		}
	}
}
