Shader "Roystan/Shadow Receiver"
{
	Properties
	{
		_Alpha("Alpha", Range(0, 1)) = 1
	}
	SubShader
	{
		Pass
		{
			Tags
			{
				"Queue" = "Geometry+1"
			}

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				SHADOW_COORDS(0)
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_SHADOW(o)
				return o;
			}
			
			float _Alpha;
			
			float4 frag (v2f i) : SV_Target
			{
				float shadow = SHADOW_ATTENUATION(i);

				return float4(0, 0, 0, (1 - shadow) * _Alpha);
			}
			ENDCG
		}

		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
