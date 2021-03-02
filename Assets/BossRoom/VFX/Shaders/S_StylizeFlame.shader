// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_StylizeFlame"
{
	Properties
	{
		_CenterAmount("Center Amount", Range( 0 , 1)) = 0.42
		_OutsideAmount("Outside  Amount", Range( 0 , 1)) = 0.212
		[HDR]_OutsideColor("Outside Color", Color) = (0.6795426,0.3371637,0,1)
		[HDR]_CenterColor("Center Color", Color) = (1,0.8796226,0.1559265,1)
		_T_Alpha("T_Alpha", 2D) = "white" {}
		_NoiseFlame("Noise Flame", 2D) = "white" {}
		_Texture1TileSpeed("Texture 1 Tile/Speed", Vector) = (1,0.5,0,-0.45)
		_Texture2TileSpeed("Texture 2 Tile/Speed", Vector) = (2,1,0,-0.35)
		_Flame("Flame", Range( 0 , 1)) = 1
		_Emissive("Emissive", Float) = 2
		_Distortion("Distortion", Float) = 2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#endif//ASE Sampling Macros

		#pragma surface surf Unlit alpha:fade keepalpha noshadow 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform float4 _CenterColor;
		uniform float _CenterAmount;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_NoiseFlame);
		uniform float4 _Texture1TileSpeed;
		SamplerState sampler_NoiseFlame;
		uniform float4 _Texture2TileSpeed;
		uniform float _Distortion;
		uniform float _Flame;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_T_Alpha);
		SamplerState sampler_T_Alpha;
		uniform float4 _T_Alpha_ST;
		uniform float _OutsideAmount;
		uniform float4 _OutsideColor;
		uniform float _Emissive;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 appendResult102 = (float2(_Texture1TileSpeed.z , _Texture1TileSpeed.w));
			float2 appendResult100 = (float2(_Texture1TileSpeed.x , _Texture1TileSpeed.y));
			float2 panner92 = ( 1.0 * _Time.y * appendResult102 + ( appendResult100 * i.uv_texcoord ));
			float2 appendResult105 = (float2(_Texture2TileSpeed.z , _Texture2TileSpeed.w));
			float2 appendResult104 = (float2(_Texture2TileSpeed.x , _Texture2TileSpeed.y));
			float2 panner93 = ( 1.0 * _Time.y * appendResult105 + ( appendResult104 * i.uv_texcoord ));
			float2 uv_T_Alpha = i.uv_texcoord * _T_Alpha_ST.xy + _T_Alpha_ST.zw;
			float4 tex2DNode109 = SAMPLE_TEXTURE2D( _T_Alpha, sampler_T_Alpha, uv_T_Alpha );
			float temp_output_108_0 = ( ( pow( ( SAMPLE_TEXTURE2D( _NoiseFlame, sampler_NoiseFlame, panner92 ).r + SAMPLE_TEXTURE2D( _NoiseFlame, sampler_NoiseFlame, panner93 ).r ) , _Distortion ) * _Flame ) * tex2DNode109.r );
			float temp_output_60_0 = step( _CenterAmount , temp_output_108_0 );
			float clampResult162 = clamp( ( step( _OutsideAmount , temp_output_108_0 ) - temp_output_60_0 ) , 0.0 , 1.0 );
			float smoothstepResult71 = smoothstep( 0.0 , 1.0 , 0.0);
			o.Emission = ( ( ( _CenterColor * temp_output_60_0 ) + ( clampResult162 * ( _OutsideColor * ( 1.0 - smoothstepResult71 ) ) ) ) * _Emissive ).rgb;
			float lerpResult179 = lerp( 0.0 , tex2DNode109.r , _Flame);
			o.Alpha = ( lerpResult179 * i.vertexColor.a );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
1920;6;1920;1133;-1828.564;516.0603;1.763518;True;False
Node;AmplifyShaderEditor.CommentaryNode;182;-550.3054,-425.4788;Inherit;False;2088.385;711.6667;Noise Flame;15;92;100;104;103;101;105;95;106;97;93;98;102;94;99;107;;1,0.7182769,0,1;0;0
Node;AmplifyShaderEditor.Vector4Node;106;-244.681,-8.47888;Inherit;False;Property;_Texture2TileSpeed;Texture 2 Tile/Speed;7;0;Create;True;0;0;False;0;False;2,1,0,-0.35;0.5,1,0,-1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;99;-217.681,-375.4788;Inherit;False;Property;_Texture1TileSpeed;Texture 1 Tile/Speed;6;0;Create;True;0;0;False;0;False;1,0.5,0,-0.45;1,0.5,0,-0.5;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;104;108.3193,5.187818;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;100;91.31924,-357.4788;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;94;-500.3054,16.51176;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;283.3195,55.18783;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;102;93.31924,-211.4788;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;101;266.3193,-307.4788;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;105;110.3193,151.1879;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;93;459.7834,115.4917;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;98;694.934,-108.1028;Inherit;True;Property;_NoiseFlame;Noise Flame;5;0;Create;True;0;0;False;0;False;None;09ff3236ed6851c44b22b13107c206f5;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.PannerNode;92;442.8615,-228.3573;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;97;961.0631,28.58685;Inherit;True;Property;_Texture2;Texture 2;7;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;95;966.7271,-264.279;Inherit;True;Property;_Texture1;Texture 1;7;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;176;1627.941,-16.59843;Inherit;False;Property;_Distortion;Distortion;10;0;Create;True;0;0;False;0;False;2;0.6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;107;1303.08,-95.57346;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;175;1834.501,-97.44958;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;1748.023,147.1686;Inherit;False;Property;_Flame;Flame;8;0;Create;True;0;0;False;0;False;1;0.44;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;115;2115.373,-98.21167;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;109;1833.849,242.479;Inherit;True;Property;_T_Alpha;T_Alpha;4;0;Create;True;0;0;False;0;False;109;27964659a7d4fef44ba88a7b6967fa31;27964659a7d4fef44ba88a7b6967fa31;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;63;2461.605,124.2576;Inherit;False;Property;_OutsideAmount;Outside  Amount;1;0;Create;True;0;0;False;0;False;0.212;0.15;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;59;2455.578,-307.7041;Inherit;False;Property;_CenterAmount;Center Amount;0;0;Create;True;0;0;False;0;False;0.42;0.307;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;129;2590.839,340.5382;Inherit;False;1150.022;839.5004;Blend Top Color;4;74;66;71;73;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;108;2487.925,-157.488;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;60;3023.218,-232.2726;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;62;3023.219,-78.16787;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;71;2960.573,651.4669;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;73;3192.103,653.769;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;66;2677.373,390.5382;Inherit;False;Property;_OutsideColor;Outside Color;2;1;[HDR];Create;True;0;0;False;0;False;0.6795426,0.3371637,0,1;0.06603777,0.8434222,2,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;64;3223.226,-79.07581;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;162;3377.06,-75.01045;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;67;3247.549,-348.3041;Inherit;False;Property;_CenterColor;Center Color;3;1;[HDR];Create;True;0;0;False;0;False;1,0.8796226,0.1559265,1;0.5647059,7.341177,11.98431,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;3354.861,553.8661;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;3568.916,-78.56142;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;1,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;3560.933,-249.67;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;179;3935.394,136.2059;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;185;3971.239,360.4087;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;69;3825.73,-113.5277;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;132;3821.046,-8.621033;Inherit;False;Property;_Emissive;Emissive;9;0;Create;True;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;4042.624,-113.7688;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;187;4246.348,325.138;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;183;4391.073,-147.403;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;S_StylizeFlame;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;104;0;106;1
WireConnection;104;1;106;2
WireConnection;100;0;99;1
WireConnection;100;1;99;2
WireConnection;103;0;104;0
WireConnection;103;1;94;0
WireConnection;102;0;99;3
WireConnection;102;1;99;4
WireConnection;101;0;100;0
WireConnection;101;1;94;0
WireConnection;105;0;106;3
WireConnection;105;1;106;4
WireConnection;93;0;103;0
WireConnection;93;2;105;0
WireConnection;92;0;101;0
WireConnection;92;2;102;0
WireConnection;97;0;98;0
WireConnection;97;1;93;0
WireConnection;95;0;98;0
WireConnection;95;1;92;0
WireConnection;107;0;95;1
WireConnection;107;1;97;1
WireConnection;175;0;107;0
WireConnection;175;1;176;0
WireConnection;115;0;175;0
WireConnection;115;1;116;0
WireConnection;108;0;115;0
WireConnection;108;1;109;1
WireConnection;60;0;59;0
WireConnection;60;1;108;0
WireConnection;62;0;63;0
WireConnection;62;1;108;0
WireConnection;73;0;71;0
WireConnection;64;0;62;0
WireConnection;64;1;60;0
WireConnection;162;0;64;0
WireConnection;74;0;66;0
WireConnection;74;1;73;0
WireConnection;65;0;162;0
WireConnection;65;1;74;0
WireConnection;68;0;67;0
WireConnection;68;1;60;0
WireConnection;179;1;109;1
WireConnection;179;2;116;0
WireConnection;69;0;68;0
WireConnection;69;1;65;0
WireConnection;125;0;69;0
WireConnection;125;1;132;0
WireConnection;187;0;179;0
WireConnection;187;1;185;4
WireConnection;183;2;125;0
WireConnection;183;9;187;0
ASEEND*/
//CHKSM=2C871F7DACAA1F894928AD50959E0402B1DC3A68