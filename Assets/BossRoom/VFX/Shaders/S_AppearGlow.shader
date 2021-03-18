// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom/AS/AppearGlow"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		[HDR]_Tint("Tint", Color) = (1,1,1,1)
		_FresnelColor("Fresnel Color", Color) = (0,0,0,0)
		_Fresnel("Fresnel", Vector) = (0,3,5,0)
		_Dissolve("Dissolve", Range( 0 , 1)) = 0
		[KeywordEnum(Custom,VertexAlpha,Slider)] _DissolveType("DissolveType", Float) = 2
		_Panner("Panner", Vector) = (0,0.01,0,0)
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 0
		[KeywordEnum(Red,UV)] _DisType("DisType", Float) = 0
		[Toggle(_GLOWWAVE_ON)] _GlowWave("GlowWave", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IsEmissive" = "true"  "PreviewType"="Plane" }
		Cull [_Cull]
		ZWrite Off
		Blend SrcAlpha One
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _DISTYPE_RED _DISTYPE_UV
		#pragma shader_feature_local _DISSOLVETYPE_CUSTOM _DISSOLVETYPE_VERTEXALPHA _DISSOLVETYPE_SLIDER
		#pragma shader_feature_local _GLOWWAVE_ON
		#pragma surface surf Unlit keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float3 worldPos;
			float3 viewDir;
			half3 worldNormal;
			INTERNAL_DATA
			float4 uv_tex4coord;
		};

		uniform half _Cull;
		uniform sampler2D _MainTex;
		uniform half2 _Panner;
		uniform half4 _Tint;
		uniform half3 _Fresnel;
		uniform half4 _FresnelColor;
		SamplerState sampler_MainTex;
		uniform half _Dissolve;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			half2 panner27 = ( 1.0 * _Time.y * _Panner + i.uv_texcoord);
			half4 tex2DNode1 = tex2D( _MainTex, panner27 );
			half3 ase_worldNormal = i.worldNormal;
			half fresnelNdotV7 = dot( ase_worldNormal, i.viewDir );
			half fresnelNode7 = ( _Fresnel.x + _Fresnel.y * pow( 1.0 - fresnelNdotV7, _Fresnel.z ) );
			#if defined(_DISTYPE_RED)
				half staticSwitch32 = tex2DNode1.r;
			#elif defined(_DISTYPE_UV)
				half staticSwitch32 = ( i.uv_tex4coord.x * i.uv_tex4coord.y );
			#else
				half staticSwitch32 = tex2DNode1.r;
			#endif
			#if defined(_DISSOLVETYPE_CUSTOM)
				half staticSwitch24 = i.uv_tex4coord.z;
			#elif defined(_DISSOLVETYPE_VERTEXALPHA)
				half staticSwitch24 = i.vertexColor.a;
			#elif defined(_DISSOLVETYPE_SLIDER)
				half staticSwitch24 = _Dissolve;
			#else
				half staticSwitch24 = _Dissolve;
			#endif
			half temp_output_18_0 = step( staticSwitch32 , ( staticSwitch24 + 0.05 ) );
			half saferPower50 = max( i.uv_texcoord.y , 0.0001 );
			half temp_output_50_0 = pow( saferPower50 , 2.0 );
			half temp_output_35_0 = frac( _Time.y );
			#ifdef _GLOWWAVE_ON
				half staticSwitch51 = ( step( temp_output_50_0 , ( temp_output_35_0 + 0.05 ) ) - step( temp_output_50_0 , temp_output_35_0 ) );
			#else
				half staticSwitch51 = 0.0;
			#endif
			o.Emission = ( ( ( tex2DNode1 * i.vertexColor * _Tint ) + ( fresnelNode7 * _FresnelColor ) + ( ( temp_output_18_0 - step( staticSwitch32 , staticSwitch24 ) ) * tex2DNode1.r ) ) + staticSwitch51 ).rgb;
			o.Alpha = ( temp_output_18_0 * ( tex2DNode1.a * i.vertexColor.a * _Tint.a ) );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
1388;162;1565;1019;545.8464;288.7044;2.035098;True;True
Node;AmplifyShaderEditor.TextureCoordinatesNode;28;-1277.082,49.14594;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;30;-1153.173,239.7779;Inherit;False;Property;_Panner;Panner;7;0;Create;True;0;0;False;0;False;0,0.01;0,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.VertexColorNode;2;-558.5,269.5;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;27;-1023.082,53.14594;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-0.1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-704.3682,-301.3614;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;16;-672.476,-84.6767;Inherit;False;Property;_Dissolve;Dissolve;5;0;Create;True;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;24;-271.4647,-128.9143;Inherit;False;Property;_DissolveType;DissolveType;6;0;Create;True;0;0;False;0;False;0;2;0;True;;KeywordEnum;3;Custom;VertexAlpha;Slider;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;34;-447.8481,1651.492;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-383.8658,-360.6896;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-699.5,16.23653;Inherit;True;Property;_MainTex;MainTex;1;0;Create;True;0;0;False;0;False;-1;None;a225a619c91968049ba2bdbc44c812fb;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;32;-100.537,-396.4481;Inherit;False;Property;_DisType;DisType;9;0;Create;True;0;0;False;0;False;0;0;1;True;;KeywordEnum;2;Red;UV;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;19;42.65839,58.25467;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;35;-204.5965,1668.985;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;45;-610.4689,1370.627;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldNormalVector;9;-516.8782,704.916;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;8;-505.4861,857.5088;Inherit;False;Property;_Fresnel;Fresnel;4;0;Create;True;0;0;False;0;False;0,3,5;0,3,3.32;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;10;-744.8782,803.916;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StepOpNode;15;50.51539,-196.6351;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;18;44.20993,-80.57181;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;41;122.9546,1767.253;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;50;-345.5039,1417.081;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;44;130.8115,1512.363;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;43;124.5061,1628.426;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;7;-206.586,761.0089;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;17;249.3089,-118.2348;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;6;-597.4861,468.1086;Inherit;False;Property;_Tint;Tint;2;1;[HDR];Create;True;0;0;False;0;False;1,1,1,1;1.150943,1.586963,4,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;13;-200.8781,949.916;Inherit;False;Property;_FresnelColor;Fresnel Color;3;0;Create;True;0;0;False;0;False;0,0,0,0;0.808876,0,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;59.12195,844.916;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-259.3,219.8;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;52;386.5256,1320.532;Inherit;False;Constant;_Float0;Float 0;11;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;473.1484,-91.45651;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;47;329.6052,1590.763;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;661.3809,188.9222;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-258.3,362.8;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;51;644.6334,1317.512;Inherit;False;Property;_GlowWave;GlowWave;10;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;1162.784,635.6478;Half;False;Property;_Cull;Cull;8;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;642.8195,347.8208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;48;883.0773,180.2477;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1153.112,148.1596;Half;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Custom/AS/AppearGlow;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;2;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;True;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;8;5;False;-1;1;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;1;PreviewType=Plane;False;0;0;True;31;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;27;0;28;0
WireConnection;27;2;30;0
WireConnection;24;1;22;3
WireConnection;24;0;2;4
WireConnection;24;2;16;0
WireConnection;26;0;22;1
WireConnection;26;1;22;2
WireConnection;1;1;27;0
WireConnection;32;1;1;1
WireConnection;32;0;26;0
WireConnection;19;0;24;0
WireConnection;35;0;34;2
WireConnection;15;0;32;0
WireConnection;15;1;24;0
WireConnection;18;0;32;0
WireConnection;18;1;19;0
WireConnection;41;0;35;0
WireConnection;50;0;45;2
WireConnection;44;0;50;0
WireConnection;44;1;35;0
WireConnection;43;0;50;0
WireConnection;43;1;41;0
WireConnection;7;0;9;0
WireConnection;7;4;10;0
WireConnection;7;1;8;1
WireConnection;7;2;8;2
WireConnection;7;3;8;3
WireConnection;17;0;18;0
WireConnection;17;1;15;0
WireConnection;12;0;7;0
WireConnection;12;1;13;0
WireConnection;3;0;1;0
WireConnection;3;1;2;0
WireConnection;3;2;6;0
WireConnection;23;0;17;0
WireConnection;23;1;1;1
WireConnection;47;0;43;0
WireConnection;47;1;44;0
WireConnection;14;0;3;0
WireConnection;14;1;12;0
WireConnection;14;2;23;0
WireConnection;4;0;1;4
WireConnection;4;1;2;4
WireConnection;4;2;6;4
WireConnection;51;1;52;0
WireConnection;51;0;47;0
WireConnection;21;0;18;0
WireConnection;21;1;4;0
WireConnection;48;0;14;0
WireConnection;48;1;51;0
WireConnection;0;2;48;0
WireConnection;0;9;21;0
ASEEND*/
//CHKSM=2C0F8205E9CFD6E8D2EFDF125DFAB2858B197AE2