// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Tiling_texture"
{
	Properties
	{
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (0,0,0,0)
		_Maintextilespeed("Main tex tile\speed", Vector) = (1,1,0,0)
		[Toggle(_HASMASK_ON)] _HasMask("Has Mask", Float) = 0
		_MaskIntenicty("Mask Intenicty", Float) = 1
		_MaskPower("Mask Power", Float) = 1
		[Toggle(_CHANGEMASKTOX_ON)] _ChangeMasktoX("Change Mask to X", Float) = 0
		[Toggle(_HASDOUBLEMASK_ON)] _HasDoubleMask("Has Double Mask", Float) = 0
		[Toggle(_INVERTMASK_ON)] _InvertMask("Invert Mask", Float) = 0
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
		#pragma shader_feature_local _HASMASK_ON
		#pragma shader_feature_local _HASDOUBLEMASK_ON
		#pragma shader_feature_local _INVERTMASK_ON
		#pragma shader_feature_local _CHANGEMASKTOX_ON
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

		UNITY_DECLARE_TEX2D_NOSAMPLER(_TextureSample0);
		SamplerState sampler_TextureSample0;
		uniform float4 _Maintextilespeed;
		uniform float4 _Color;
		uniform float _MaskIntenicty;
		uniform float _MaskPower;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 appendResult35 = (float2(_Maintextilespeed.z , _Maintextilespeed.w));
			float2 appendResult33 = (float2(_Maintextilespeed.x , _Maintextilespeed.y));
			float2 panner3 = ( 1.0 * _Time.y * appendResult35 + ( i.uv_texcoord * appendResult33 ));
			float4 tex2DNode1 = SAMPLE_TEXTURE2D( _TextureSample0, sampler_TextureSample0, panner3 );
			o.Emission = ( tex2DNode1.r * _Color * i.vertexColor ).rgb;
			float temp_output_38_0 = ( tex2DNode1.r * i.vertexColor.a );
			#ifdef _CHANGEMASKTOX_ON
				float staticSwitch39 = i.uv_texcoord.x;
			#else
				float staticSwitch39 = i.uv_texcoord.y;
			#endif
			#ifdef _INVERTMASK_ON
				float staticSwitch41 = ( 1.0 - staticSwitch39 );
			#else
				float staticSwitch41 = staticSwitch39;
			#endif
			#ifdef _HASDOUBLEMASK_ON
				float staticSwitch45 = ( staticSwitch41 * ( 1.0 - staticSwitch41 ) * _MaskIntenicty );
			#else
				float staticSwitch45 = staticSwitch41;
			#endif
			float clampResult48 = clamp( pow( staticSwitch45 , _MaskPower ) , 0.0 , 1.0 );
			#ifdef _HASMASK_ON
				float staticSwitch49 = ( temp_output_38_0 * clampResult48 );
			#else
				float staticSwitch49 = temp_output_38_0;
			#endif
			o.Alpha = staticSwitch49;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
1920;0;1920;1139;2212.815;495.8202;1.545529;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;7;-1504.465,-24.20159;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;39;-981.381,509.2463;Inherit;False;Property;_ChangeMasktoX;Change Mask to X;6;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;40;-557.9114,667.3445;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;41;-456.5141,512.6432;Inherit;False;Property;_InvertMask;Invert Mask;8;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;31;-407.7529,18.53017;Inherit;False;Property;_Maintextilespeed;Main tex tile\speed;2;0;Create;True;0;0;False;0;False;1,1,0,0;0.5,1,-2,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;42;-190.4002,722.7214;Inherit;False;Property;_MaskIntenicty;Mask Intenicty;4;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;33;-137.3525,48.43047;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;43;-180.4871,631.2975;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;43.34763,-99.76929;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;35;-142.5524,140.7308;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;63.41681,592.0074;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;45;331.2889,520.4434;Inherit;False;Property;_HasDoubleMask;Has Double Mask;7;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;46;535.2766,681.1525;Inherit;False;Property;_MaskPower;Mask Power;5;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;3;232.5476,-80.08929;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;1;479.5476,-157.0894;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;False;-1;cfc95521e1d3c2c4d846aa8a00ff8a0a;37fa9414a67aa3f408e36808a22d0f21;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;47;667.5756,525.9524;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;37;568.1164,268.3642;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;916.5621,150.4374;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;48;847.8354,524.4224;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;530.7894,68.20282;Inherit;False;Property;_Color;Color;1;1;[HDR];Create;True;0;0;False;0;False;0,0,0,0;2.670157,2.670157,2.670157,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;1022.957,269.2005;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;892.0261,-126.5567;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;49;1141.376,146.9778;Inherit;False;Property;_HasMask;Has Mask;3;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;36;1426.861,-48.56253;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Tiling_texture;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;39;1;7;2
WireConnection;39;0;7;1
WireConnection;40;0;39;0
WireConnection;41;1;39;0
WireConnection;41;0;40;0
WireConnection;33;0;31;1
WireConnection;33;1;31;2
WireConnection;43;0;41;0
WireConnection;34;0;7;0
WireConnection;34;1;33;0
WireConnection;35;0;31;3
WireConnection;35;1;31;4
WireConnection;44;0;41;0
WireConnection;44;1;43;0
WireConnection;44;2;42;0
WireConnection;45;1;41;0
WireConnection;45;0;44;0
WireConnection;3;0;34;0
WireConnection;3;2;35;0
WireConnection;1;1;3;0
WireConnection;47;0;45;0
WireConnection;47;1;46;0
WireConnection;38;0;1;1
WireConnection;38;1;37;4
WireConnection;48;0;47;0
WireConnection;50;0;38;0
WireConnection;50;1;48;0
WireConnection;11;0;1;1
WireConnection;11;1;16;0
WireConnection;11;2;37;0
WireConnection;49;1;38;0
WireConnection;49;0;50;0
WireConnection;36;2;11;0
WireConnection;36;9;49;0
ASEEND*/
//CHKSM=F22A907CDBA34DAEE2E8BD01AE817FA108D02ACE