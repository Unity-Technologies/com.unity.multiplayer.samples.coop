// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom/S_Slash"
{
	Properties
	{
		_MainTexture("Main Texture", 2D) = "white" {}
		_Scale("Scale", Range( 0 , 1)) = 1
		[HDR]_Color("Color", Color) = (1,1,1,0)
		_Noise("Noise", 2D) = "white" {}
		_FlowMain("Flow Main", Range( 0 , 1)) = 0.1263689
		_NoiseTileSpeed("Noise Tile/Speed", Vector) = (1,1,1,0)
		_Mask("Mask", Float) = 1
		_MaskPower("Mask Power", Float) = 1
		_MainTexturePower("Main Texture Power", Float) = 1
		[Toggle(_HASVERTEXALPHA_ON)] _HasVertexAlpha("Has Vertex Alpha", Float) = 0
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
		#pragma shader_feature_local _HASVERTEXALPHA_ON
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

		uniform float4 _Color;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_MainTexture);
		SamplerState sampler_MainTexture;
		uniform float _Scale;
		uniform float _FlowMain;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Noise);
		SamplerState sampler_Noise;
		uniform float4 _NoiseTileSpeed;
		uniform float _MainTexturePower;
		uniform float _Mask;
		uniform float _MaskPower;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 temp_cast_0 = (_Scale).xx;
			float2 temp_output_4_0_g1 = temp_cast_0;
			float2 appendResult19 = (float2(_NoiseTileSpeed.z , _NoiseTileSpeed.w));
			float2 appendResult17 = (float2(_NoiseTileSpeed.x , _NoiseTileSpeed.y));
			float2 panner9 = ( 1.0 * _Time.y * appendResult19 + ( i.uv_texcoord * appendResult17 ));
			float2 appendResult20 = (float2(( _FlowMain * SAMPLE_TEXTURE2D( _Noise, sampler_Noise, panner9 ).r ) , 0.0));
			float temp_output_37_0 = pow( SAMPLE_TEXTURE2D( _MainTexture, sampler_MainTexture, ( ( ( ( i.uv_texcoord / temp_output_4_0_g1 ) + 0.5 ) - ( 0.5 / temp_output_4_0_g1 ) ) + appendResult20 ) ).r , _MainTexturePower );
			o.Emission = ( _Color * temp_output_37_0 * i.vertexColor ).rgb;
			#ifdef _HASVERTEXALPHA_ON
				float staticSwitch39 = i.vertexColor.a;
			#else
				float staticSwitch39 = 1.0;
			#endif
			float clampResult27 = clamp( ( ( temp_output_37_0 * staticSwitch39 * pow( ( i.uv_texcoord.x * ( 1.0 - i.uv_texcoord.x ) * _Mask ) , _MaskPower ) ) - ( 1.0 - i.vertexColor.a ) ) , 0.0 , 1.0 );
			o.Alpha = clampResult27;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
1920;0;1920;1139;1862.791;89.01544;1;True;False
Node;AmplifyShaderEditor.Vector4Node;16;-2979.972,438.9508;Inherit;False;Property;_NoiseTileSpeed;Noise Tile/Speed;5;0;Create;True;0;0;False;0;False;1,1,1,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;14;-2971.549,164.8094;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;17;-2726.171,451.3719;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-2537.425,427.7973;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;19;-2731.421,548.6429;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;9;-2357.258,429.4046;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-2136.62,309.5079;Inherit;False;Property;_FlowMain;Flow Main;4;0;Create;True;0;0;False;0;False;0.1263689;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;10;-2143.597,401.5482;Inherit;True;Property;_Noise;Noise;3;0;Create;True;0;0;False;0;False;-1;a225a619c91968049ba2bdbc44c812fb;a225a619c91968049ba2bdbc44c812fb;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-1847.004,407.9918;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-2441.249,220.5333;Inherit;False;Property;_Scale;Scale;1;0;Create;True;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;20;-1843.885,300.6908;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;4;-2116.917,164.2326;Inherit;False;F_UVscaleCenter;-1;;1;eb779ef5bfeefcd438ac9bfbf3fda0e4;0;2;2;FLOAT2;0,0;False;4;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;35;-2753.694,783.7306;Inherit;False;922.96;383.5835;Mask;5;26;22;21;25;24;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;15;-1731.844,166.1418;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-2694.771,932.3458;Inherit;False;Property;_Mask;Mask;6;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;26;-2703.694,858.8692;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-1160.791,390.9846;Inherit;False;Constant;_Float2;Float 2;10;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-1530.783,136.8824;Inherit;True;Property;_MainTexture;Main Texture;0;0;Create;True;0;0;False;0;False;-1;e17ce5c9d3ab8e746a3e866767bcbbf6;e17ce5c9d3ab8e746a3e866767bcbbf6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;38;-1224.842,276.2576;Inherit;False;Property;_MainTexturePower;Main Texture Power;8;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-2368.432,833.7306;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-2337.949,1051.314;Inherit;False;Property;_MaskPower;Mask Power;7;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;2;-1458.885,334.0539;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;24;-2090.734,834.6378;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;39;-1025.791,396.9846;Inherit;False;Property;_HasVertexAlpha;Has Vertex Alpha;9;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;37;-1134.89,166.6585;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-603.6159,440.7625;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;30;-613.1768,569.438;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;6;-1482.497,-58.42908;Inherit;False;Property;_Color;Color;2;1;[HDR];Create;True;0;0;False;0;False;1,1,1,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;34;-444.8181,443.7504;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-609.5698,168.0276;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;27;-260.9947,442.4204;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-40.22657,122.5033;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Custom/S_Slash;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;17;0;16;1
WireConnection;17;1;16;2
WireConnection;18;0;14;0
WireConnection;18;1;17;0
WireConnection;19;0;16;3
WireConnection;19;1;16;4
WireConnection;9;0;18;0
WireConnection;9;2;19;0
WireConnection;10;1;9;0
WireConnection;12;0;13;0
WireConnection;12;1;10;1
WireConnection;20;0;12;0
WireConnection;4;2;14;0
WireConnection;4;4;5;0
WireConnection;15;0;4;0
WireConnection;15;1;20;0
WireConnection;26;0;14;1
WireConnection;3;1;15;0
WireConnection;21;0;14;1
WireConnection;21;1;26;0
WireConnection;21;2;22;0
WireConnection;24;0;21;0
WireConnection;24;1;25;0
WireConnection;39;1;40;0
WireConnection;39;0;2;4
WireConnection;37;0;3;1
WireConnection;37;1;38;0
WireConnection;8;0;37;0
WireConnection;8;1;39;0
WireConnection;8;2;24;0
WireConnection;30;0;2;4
WireConnection;34;0;8;0
WireConnection;34;1;30;0
WireConnection;7;0;6;0
WireConnection;7;1;37;0
WireConnection;7;2;2;0
WireConnection;27;0;34;0
WireConnection;0;2;7;0
WireConnection;0;9;27;0
ASEEND*/
//CHKSM=6EEB75D7000F334AB91897A06F019E32EC73CE79