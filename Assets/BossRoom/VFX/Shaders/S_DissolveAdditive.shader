// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom/DissolveAdditive"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[HDR]_Tint("Tint", Color) = (1,1,1,1)
		_Dissolve("Dissolve", Range( 0 , 1)) = 0
		[KeywordEnum(Custom,VertexAlpha,Slider)] _DissolveType("DissolveType", Float) = 2
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 0
		[KeywordEnum(Red,UV,Alpha,DissolveTexRed,DissolveTexAlpha)] _DisType("DisType", Float) = 0
		[Toggle(_INVERTDISSOLVE_ON)] _InvertDissolve("InvertDissolve", Float) = 0
		_DissolveTex("DissolveTex", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.CompareFunction)]_ZTestMode("Z Test Mode", Float) = 0
		_DissolveTileSpeed("Dissolve Tile/Speed", Vector) = (1,1,0,0)
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		ZTest Always
		Blend OneMinusDstColor One
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _DISTYPE_RED _DISTYPE_UV _DISTYPE_ALPHA _DISTYPE_DISSOLVETEXRED _DISTYPE_DISSOLVETEXALPHA
		#pragma shader_feature_local _DISSOLVETYPE_CUSTOM _DISSOLVETYPE_VERTEXALPHA _DISSOLVETYPE_SLIDER
		#pragma shader_feature_local _INVERTDISSOLVE_ON
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#endif//ASE Sampling Macros

		#pragma surface surf Unlit keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float4 uv_tex4coord;
		};

		uniform half _Cull;
		uniform float _ZTestMode;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_MainTex);
		uniform float4 _MainTex_ST;
		SamplerState sampler_MainTex;
		uniform float4 _Tint;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_DissolveTex);
		SamplerState sampler_DissolveTex;
		uniform float4 _DissolveTileSpeed;
		uniform float _Dissolve;
		uniform float _Cutoff = 0.5;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode1 = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv_MainTex );
			float2 appendResult46 = (float2(_DissolveTileSpeed.z , _DissolveTileSpeed.w));
			float2 appendResult45 = (float2(_DissolveTileSpeed.x , _DissolveTileSpeed.y));
			float2 panner27 = ( 1.0 * _Time.y * appendResult46 + ( i.uv_texcoord * appendResult45 ));
			float4 tex2DNode38 = SAMPLE_TEXTURE2D( _DissolveTex, sampler_DissolveTex, panner27 );
			#if defined(_DISTYPE_RED)
				float staticSwitch32 = tex2DNode1.r;
			#elif defined(_DISTYPE_UV)
				float staticSwitch32 = ( i.uv_tex4coord.x * i.uv_tex4coord.y );
			#elif defined(_DISTYPE_ALPHA)
				float staticSwitch32 = tex2DNode1.r;
			#elif defined(_DISTYPE_DISSOLVETEXRED)
				float staticSwitch32 = tex2DNode38.r;
			#elif defined(_DISTYPE_DISSOLVETEXALPHA)
				float staticSwitch32 = tex2DNode38.a;
			#else
				float staticSwitch32 = tex2DNode1.r;
			#endif
			#if defined(_DISSOLVETYPE_CUSTOM)
				float staticSwitch24 = i.uv_tex4coord.z;
			#elif defined(_DISSOLVETYPE_VERTEXALPHA)
				float staticSwitch24 = i.vertexColor.a;
			#elif defined(_DISSOLVETYPE_SLIDER)
				float staticSwitch24 = _Dissolve;
			#else
				float staticSwitch24 = _Dissolve;
			#endif
			float temp_output_15_0 = step( staticSwitch32 , staticSwitch24 );
			o.Emission = ( ( tex2DNode1 * i.vertexColor * _Tint ) + ( temp_output_15_0 * tex2DNode1.r ) ).rgb;
			o.Alpha = 1;
			#ifdef _INVERTDISSOLVE_ON
				float staticSwitch37 = ( 1.0 - temp_output_15_0 );
			#else
				float staticSwitch37 = temp_output_15_0;
			#endif
			clip( ( staticSwitch37 * ( tex2DNode1.r * i.vertexColor.a * _Tint.a ) ) - _Cutoff );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
1920;6;1920;1133;2330.646;527.6992;1.377047;True;False
Node;AmplifyShaderEditor.Vector4Node;44;-1851.499,341.2175;Inherit;False;Property;_DissolveTileSpeed;Dissolve Tile/Speed;10;0;Create;True;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;28;-1828.554,148.5199;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;45;-1567.828,332.9553;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;46;-1567.827,423.8404;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-1386.057,313.6765;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-1024.553,-245.1849;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;27;-1218.984,318.9194;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-0.1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-682.6218,-397.2775;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-1033.312,61.0278;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;False;-1;None;78f5b23dbc61d1042b153173e9609b8e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;38;-1028.554,291.5708;Inherit;True;Property;_DissolveTex;DissolveTex;8;0;Create;True;0;0;False;0;False;-1;None;8a5b3af15db0b3d4e9b5fd91fe2e7c30;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;2;-558.5,269.5;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;16;-150.2654,-58.05077;Inherit;False;Property;_Dissolve;Dissolve;3;0;Create;True;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;32;-196.3052,-426.4481;Inherit;False;Property;_DisType;DisType;6;0;Create;True;0;0;False;0;False;0;0;3;True;;KeywordEnum;5;Red;UV;Alpha;DissolveTexRed;DissolveTexAlpha;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;24;-128.0322,-181.225;Inherit;False;Property;_DissolveType;DissolveType;4;0;Create;True;0;0;False;0;False;0;2;0;True;;KeywordEnum;3;Custom;VertexAlpha;Slider;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;15;175.3861,-324.8806;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;6;-597.4861,468.1086;Inherit;False;Property;_Tint;Tint;2;1;[HDR];Create;True;0;0;False;0;False;1,1,1,1;0.7264151,0.7264151,0.7264151,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;36;280.0128,541.9551;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-258.3,362.8;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;571.0199,-12.14673;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-259.3,219.8;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;37;438.6324,455.8954;Inherit;True;Property;_InvertDissolve;InvertDissolve;7;0;Create;True;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;1280.877,367.3398;Half;False;Property;_Cull;Cull;5;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;703.5673,330.9464;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;39;1269.434,451.1546;Inherit;False;Property;_ZTestMode;Z Test Mode;9;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CompareFunction;True;0;False;0;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;661.3809,188.9222;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;40;1245.753,218.3959;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Custom/DissolveAdditive;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;7;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;True;Transparent;;AlphaTest;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;5;4;False;-1;1;False;-1;0;1;False;-1;1;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;45;0;44;1
WireConnection;45;1;44;2
WireConnection;46;0;44;3
WireConnection;46;1;44;4
WireConnection;47;0;28;0
WireConnection;47;1;45;0
WireConnection;27;0;47;0
WireConnection;27;2;46;0
WireConnection;26;0;22;1
WireConnection;26;1;22;2
WireConnection;38;1;27;0
WireConnection;32;1;1;1
WireConnection;32;0;26;0
WireConnection;32;2;1;1
WireConnection;32;3;38;1
WireConnection;32;4;38;4
WireConnection;24;1;22;3
WireConnection;24;0;2;4
WireConnection;24;2;16;0
WireConnection;15;0;32;0
WireConnection;15;1;24;0
WireConnection;36;0;15;0
WireConnection;4;0;1;1
WireConnection;4;1;2;4
WireConnection;4;2;6;4
WireConnection;23;0;15;0
WireConnection;23;1;1;1
WireConnection;3;0;1;0
WireConnection;3;1;2;0
WireConnection;3;2;6;0
WireConnection;37;1;15;0
WireConnection;37;0;36;0
WireConnection;21;0;37;0
WireConnection;21;1;4;0
WireConnection;14;0;3;0
WireConnection;14;1;23;0
WireConnection;40;2;14;0
WireConnection;40;10;21;0
ASEEND*/
//CHKSM=140F15BD91CD35FEB7D0EC8D438C7854D6E14E3E