Shader "Octomino/StageDisplay" {
	Properties {
		_Progress ("Progress", float) = 0
		_ResetProgress ("ResetProgress", float) = 0
		_OuterColor ("OuterColor", Color) = (0, 0, 0, 1)
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			static const float PI = 3.1415926536;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float _Progress;
			float _ResetProgress;
			float4 _OuterColor;

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float2 pos = i.uv * 2 - 1;
				float radius = sqrt(pos.x * pos.x + pos.y * pos.y);
				if (radius > 1) discard;
				if (radius > 0.85) return 0;
                float angle = 1 * PI / 2 - atan2(pos.y, pos.x);
				float progress = min(frac(angle / (2.0 * PI)), 0.99); // fix thin around 0
				if (radius > 0.7) return _ResetProgress < progress ? _OuterColor : float4(1, 1, 1, 1);
				if (radius > 0.55 || radius < 0.2) return 0;
				return _Progress < progress ? 0 : float4(0, 1, 0, 1);
			}
			ENDCG
		}
	}
}
