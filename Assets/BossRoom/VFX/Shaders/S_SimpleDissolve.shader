// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom/S_SimpleDissolve"
{
	Properties
	{
		_MainTex("Main Tex", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (1,1,1,0)
		[Toggle(_VERTEXAOPACITY_ON)] _VertexAOpacity("Vertex[A]Opacity", Float) = 0
		[Toggle(_USEALPHACHANNEL_ON)] _UseAlphaChannel("UseAlphaChannel", Float) = 0
		[Toggle(_HASTEXTUREDISSOLVE_ON)] _HasTextureDissolve("Has Texture Dissolve", Float) = 0
		[Toggle(_STEPSUBTRACT_ON)] _StepSubtract("Step/Subtract", Float) = 0
		_DissolveTexture("Dissolve Texture", 2D) = "white" {}
		_TileSpeedDissolveTexture("Tile/Speed Dissolve Texture", Vector) = (1,1,0,0)
		_TileSpeedMainTexture("Tile/Speed Main Texture", Vector) = (1,1,0,0)
		[Toggle(_HASDYNAMICDISSOLVE_ON)] _HasDynamicDissolve("Has Dynamic Dissolve", Float) = 0
		_Opacity("Opacity", Range( 0 , 1)) = 1
		[Toggle(_HASFLOWEFFECT_ON)] _HasFlowEffect("Has Flow Effect", Float) = 0
		_Flowintencity("Flow intencity", Range( 0 , 1)) = 0.15
		[Toggle(_HASMASK_ON)] _HasMask("Has Mask", Float) = 0
		_MaskIntenicty("Mask Intenicty", Float) = 1
		_MaskPower("Mask Power", Float) = 1
		_MaskTexture("Mask Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("CullMode", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord2( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull [_CullMode]
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 2.5
		#pragma shader_feature_local _USEALPHACHANNEL_ON
		#pragma shader_feature_local _HASFLOWEFFECT_ON
		#pragma shader_feature_local _HASMASK_ON
		#pragma shader_feature_local _VERTEXAOPACITY_ON
		#pragma shader_feature_local _HASTEXTUREDISSOLVE_ON
		#pragma shader_feature_local _STEPSUBTRACT_ON
		#pragma shader_feature_local _HASDYNAMICDISSOLVE_ON
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#endif//ASE Sampling Macros

		#pragma surface surf Unlit alpha:fade keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			float4 uv2_tex4coord2;
		};

		uniform float _CullMode;
		uniform float4 _Color;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_MainTex);
		SamplerState sampler_MainTex;
		uniform float4 _TileSpeedMainTexture;
		uniform float _Flowintencity;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_DissolveTexture);
		SamplerState sampler_DissolveTexture;
		uniform float4 _TileSpeedDissolveTexture;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_MaskTexture);
		SamplerState sampler_MaskTexture;
		uniform float4 _MaskTexture_ST;
		uniform float _MaskIntenicty;
		uniform float _MaskPower;
		uniform float _Opacity;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float temp_output_16_0 = ( 1.0 - i.vertexColor.a );
			float2 appendResult69 = (float2(_TileSpeedMainTexture.z , _TileSpeedMainTexture.w));
			float2 appendResult67 = (float2(_TileSpeedMainTexture.x , _TileSpeedMainTexture.y));
			float2 appendResult54 = (float2(_TileSpeedDissolveTexture.z , _TileSpeedDissolveTexture.w));
			float2 appendResult50 = (float2(_TileSpeedDissolveTexture.x , _TileSpeedDissolveTexture.y));
			float2 panner51 = ( 1.0 * _Time.y * appendResult54 + ( i.uv_texcoord * appendResult50 ));
			float4 tex2DNode48 = SAMPLE_TEXTURE2D( _DissolveTexture, sampler_DissolveTexture, panner51 );
			#ifdef _HASFLOWEFFECT_ON
				float2 staticSwitch62 = ( i.uv_texcoord + ( _Flowintencity * tex2DNode48.r ) );
			#else
				float2 staticSwitch62 = i.uv_texcoord;
			#endif
			float2 panner70 = ( 1.0 * _Time.y * appendResult69 + ( appendResult67 * staticSwitch62 ));
			float4 tex2DNode6 = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, panner70 );
			#ifdef _USEALPHACHANNEL_ON
				float staticSwitch15 = tex2DNode6.a;
			#else
				float staticSwitch15 = tex2DNode6.r;
			#endif
			float smoothstepResult7 = smoothstep( temp_output_16_0 , 1.0 , staticSwitch15);
			o.Emission = ( _Color * i.vertexColor * smoothstepResult7 ).rgb;
			#ifdef _VERTEXAOPACITY_ON
				float staticSwitch13 = i.vertexColor.a;
			#else
				float staticSwitch13 = 0.0;
			#endif
			#ifdef _HASDYNAMICDISSOLVE_ON
				float staticSwitch61 = i.uv2_tex4coord2.w;
			#else
				float staticSwitch61 = temp_output_16_0;
			#endif
			float temp_output_55_0 = ( ( tex2DNode48.r + 1.0 ) * staticSwitch61 );
			#ifdef _STEPSUBTRACT_ON
				float staticSwitch45 = step( temp_output_55_0 , staticSwitch15 );
			#else
				float staticSwitch45 = ( staticSwitch15 - temp_output_55_0 );
			#endif
			#ifdef _HASTEXTUREDISSOLVE_ON
				float staticSwitch44 = staticSwitch45;
			#else
				float staticSwitch44 = smoothstepResult7;
			#endif
			float temp_output_41_0 = ( ( _Color.a * staticSwitch13 ) * staticSwitch44 );
			float2 uv_MaskTexture = i.uv_texcoord * _MaskTexture_ST.xy + _MaskTexture_ST.zw;
			#ifdef _HASMASK_ON
				float staticSwitch72 = ( temp_output_41_0 * saturate( pow( ( SAMPLE_TEXTURE2D( _MaskTexture, sampler_MaskTexture, uv_MaskTexture ).r * _MaskIntenicty ) , _MaskPower ) ) );
			#else
				float staticSwitch72 = temp_output_41_0;
			#endif
			float clampResult23 = clamp( ( staticSwitch72 * _Opacity ) , 0.0 , 1.0 );
			o.Alpha = clampResult23;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
0;6;1920;1133;2024.286;-66.07246;1.403583;True;False
Node;AmplifyShaderEditor.Vector4Node;49;-3197.381,886.3096;Inherit;False;Property;_TileSpeedDissolveTexture;Tile/Speed Dissolve Texture;7;0;Create;True;0;0;False;0;False;1,1,0,0;2,0.5,0,1.5;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;53;-3463.573,493.6802;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;50;-2910.242,908.8526;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;54;-2911.011,989.8579;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;-2749.221,754.8551;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;51;-2481.221,812.8551;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;48;-2279.447,783.3586;Inherit;True;Property;_DissolveTexture;Dissolve Texture;6;0;Create;True;0;0;False;0;False;-1;09ff3236ed6851c44b22b13107c206f5;8a5b3af15db0b3d4e9b5fd91fe2e7c30;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;66;-3489.567,411.1879;Inherit;False;Property;_Flowintencity;Flow intencity;12;0;Create;True;0;0;False;0;False;0.15;0.172;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;-2925.644,439.758;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;71;-2655.372,59.43367;Inherit;False;Property;_TileSpeedMainTexture;Tile/Speed Main Texture;8;0;Create;True;0;0;False;0;False;1,1,0,0;1,1,0,2.5;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;63;-2798.906,338.8696;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;62;-2657.905,224.2697;Inherit;False;Property;_HasFlowEffect;Has Flow Effect;11;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;67;-2360.595,98.02464;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;69;-2338.221,319.4569;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-2203.87,211.6517;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;9;-1739.224,-70.92181;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;60;-2216.325,1015.935;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;70;-2080.192,222.5399;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;16;-1248.935,133.905;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;61;-1938.723,1084.035;Inherit;False;Property;_HasDynamicDissolve;Has Dynamic Dissolve;9;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;6;-1879.672,195.6047;Inherit;True;Property;_MainTex;Main Tex;0;0;Create;True;0;0;False;0;False;-1;1ee97322756ea2c489f9311bd5c9f72d;037eea83bb383c34ebc2ee292c903057;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;57;-1956.009,812.2713;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;15;-1519.082,231.1438;Inherit;False;Property;_UseAlphaChannel;UseAlphaChannel;3;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;-1642.548,812.7305;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;90;-1101.42,794.5466;Inherit;True;Property;_MaskTexture;Mask Texture;16;0;Create;True;0;0;False;0;False;-1;60db06eefbb5c364e9d80885854c080e;9a69c5a954f525a44a117b8d5d15e056;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;75;-1088.142,1016.214;Inherit;False;Property;_MaskIntenicty;Mask Intenicty;14;0;Create;True;0;0;False;0;False;1;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-1414.711,-8.864521;Inherit;False;Constant;_Float1;Float 1;3;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;47;-639.0303,462.9658;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-934.4677,239.1417;Inherit;False;Constant;_Float0;Float 0;1;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;46;-644.105,280.2146;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;87;-776.1385,873.2217;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;7;-937.261,130.8385;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;45;-380.6433,406.6898;Inherit;False;Property;_StepSubtract;Step/Subtract;5;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;13;-1243.347,18.5;Inherit;False;Property;_VertexAOpacity;Vertex[A]Opacity;2;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;10;-1287.108,-292.6477;Inherit;False;Property;_Color;Color;1;1;[HDR];Create;True;0;0;False;0;False;1,1,1,0;5.201142,5.201142,5.201142,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;77;-800.2184,1029.835;Inherit;False;Property;_MaskPower;Mask Power;15;0;Create;True;0;0;False;0;False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-688.976,-64.5;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;88;-579.5784,875.0416;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;44;-388.2643,126.8138;Inherit;False;Property;_HasTextureDissolve;Has Texture Dissolve;4;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;92;-306.3001,875.9405;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-114.311,107.0327;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;38.79973,199.1395;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;72;174.8944,104.5198;Inherit;False;Property;_HasMask;Has Mask;13;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;59;214.767,325.2502;Inherit;False;Property;_Opacity;Opacity;10;0;Create;True;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;415.6019,107.6684;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;23;546.8559,106.0625;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;85;803.8948,359.1748;Inherit;False;Property;_CullMode;CullMode;17;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-751,-216.5;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;43;781.6785,-87.72757;Float;False;True;-1;1;ASEMaterialInspector;0;0;Unlit;Custom/S_SimpleDissolve;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;1;False;-1;1;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;True;85;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;50;0;49;1
WireConnection;50;1;49;2
WireConnection;54;0;49;3
WireConnection;54;1;49;4
WireConnection;52;0;53;0
WireConnection;52;1;50;0
WireConnection;51;0;52;0
WireConnection;51;2;54;0
WireConnection;48;1;51;0
WireConnection;65;0;66;0
WireConnection;65;1;48;1
WireConnection;63;0;53;0
WireConnection;63;1;65;0
WireConnection;62;1;53;0
WireConnection;62;0;63;0
WireConnection;67;0;71;1
WireConnection;67;1;71;2
WireConnection;69;0;71;3
WireConnection;69;1;71;4
WireConnection;68;0;67;0
WireConnection;68;1;62;0
WireConnection;70;0;68;0
WireConnection;70;2;69;0
WireConnection;16;0;9;4
WireConnection;61;1;16;0
WireConnection;61;0;60;4
WireConnection;6;1;70;0
WireConnection;57;0;48;1
WireConnection;15;1;6;1
WireConnection;15;0;6;4
WireConnection;55;0;57;0
WireConnection;55;1;61;0
WireConnection;47;0;15;0
WireConnection;47;1;55;0
WireConnection;46;0;55;0
WireConnection;46;1;15;0
WireConnection;87;0;90;1
WireConnection;87;1;75;0
WireConnection;7;0;15;0
WireConnection;7;1;16;0
WireConnection;7;2;8;0
WireConnection;45;1;47;0
WireConnection;45;0;46;0
WireConnection;13;1;14;0
WireConnection;13;0;9;4
WireConnection;12;0;10;4
WireConnection;12;1;13;0
WireConnection;88;0;87;0
WireConnection;88;1;77;0
WireConnection;44;1;7;0
WireConnection;44;0;45;0
WireConnection;92;0;88;0
WireConnection;41;0;12;0
WireConnection;41;1;44;0
WireConnection;79;0;41;0
WireConnection;79;1;92;0
WireConnection;72;1;41;0
WireConnection;72;0;79;0
WireConnection;80;0;72;0
WireConnection;80;1;59;0
WireConnection;23;0;80;0
WireConnection;11;0;10;0
WireConnection;11;1;9;0
WireConnection;11;2;7;0
WireConnection;43;2;11;0
WireConnection;43;9;23;0
ASEEND*/
//CHKSM=527EF33D572C1DAF00732B4157F5E738913EB7E1