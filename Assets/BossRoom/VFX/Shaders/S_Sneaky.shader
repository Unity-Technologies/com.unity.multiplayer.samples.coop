// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_Sneaky"
{
	Properties
	{
		_Fresnel("Fresnel", Float) = 1
		_FresnelPower("Fresnel Power", Float) = 1
		_Distortion("Distortion", Range( 0 , 1)) = 0.162
		[HDR]_FresnelColor("Fresnel Color ", Color) = (1,1,1,0)
		_Sneak("Sneak", Range( 0 , 1)) = 1
		[Normal]_T_NormalMap_2("T_NormalMap_2", 2D) = "bump" {}
		_NoiseFresnel("Noise Fresnel", 2D) = "white" {}
		_NoiseIntencity("Noise Intencity", Range( 0 , 1)) = 1
		_NotmalTileSpeed("Notmal Tile/Speed", Vector) = (0.1,0.1,0,0.1)
		_NoiseTileSpeed("Noise Tile/Speed", Vector) = (0.1,0.1,0,0.1)
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#endif//ASE Sampling Macros

		#pragma surface surf Unlit alpha:fade keepalpha noshadow 
		struct Input
		{
			float4 screenPos;
			float3 worldPos;
			float3 worldNormal;
		};

		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		UNITY_DECLARE_TEX2D_NOSAMPLER(_T_NormalMap_2);
		uniform float4 _NotmalTileSpeed;
		SamplerState sampler_T_NormalMap_2;
		uniform float _Distortion;
		uniform float _Sneak;
		uniform float4 _FresnelColor;
		uniform float _Fresnel;
		uniform float _FresnelPower;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_NoiseFresnel);
		SamplerState sampler_NoiseFresnel;
		uniform float4 _NoiseTileSpeed;
		uniform float _NoiseIntencity;


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float2 appendResult43 = (float2(_NotmalTileSpeed.z , _NotmalTileSpeed.w));
			float2 appendResult42 = (float2(_NotmalTileSpeed.x , _NotmalTileSpeed.y));
			float3 ase_worldPos = i.worldPos;
			float3 worldToObj24 = mul( unity_WorldToObject, float4( ase_worldPos, 1 ) ).xyz;
			float2 panner40 = ( 1.0 * _Time.y * appendResult43 + ( float3( appendResult42 ,  0.0 ) * worldToObj24 ).xy);
			float lerpResult18 = lerp( 0.0 , _Distortion , _Sneak);
			float4 screenColor15 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( float3( (ase_grabScreenPosNorm).xy ,  0.0 ) + UnpackScaleNormal( SAMPLE_TEXTURE2D( _T_NormalMap_2, sampler_T_NormalMap_2, panner40 ), lerpResult18 ) ).xy);
			float4 clampResult25 = clamp( screenColor15 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float lerpResult44 = lerp( 0.0 , _Fresnel , _Sneak);
			float fresnelNdotV1 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode1 = ( 0.0 + lerpResult44 * pow( 1.0 - fresnelNdotV1, _FresnelPower ) );
			float2 appendResult37 = (float2(_NoiseTileSpeed.z , _NoiseTileSpeed.w));
			float2 appendResult36 = (float2(_NoiseTileSpeed.x , _NoiseTileSpeed.y));
			float2 panner32 = ( 1.0 * _Time.y * appendResult37 + ( worldToObj24 * float3( appendResult36 ,  0.0 ) ).xy);
			float clampResult4 = clamp( ( fresnelNode1 - ( SAMPLE_TEXTURE2D( _NoiseFresnel, sampler_NoiseFresnel, panner32 ).r * _NoiseIntencity ) ) , 0.0 , 1.0 );
			float4 lerpResult7 = lerp( float4( 0,0,0,0 ) , ( _FresnelColor * clampResult4 ) , _Sneak);
			o.Emission = ( clampResult25 + lerpResult7 ).rgb;
			float clampResult38 = clamp( fresnelNode1 , 0.0 , 1.0 );
			o.Alpha = clampResult38;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
1920;6;1920;1133;3546.513;1123.683;2.290008;True;False
Node;AmplifyShaderEditor.WorldPosInputsNode;23;-2139.849,134.4991;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector4Node;35;-1857.153,407.0903;Inherit;False;Property;_NoiseTileSpeed;Noise Tile/Speed;9;0;Create;True;0;0;False;0;False;0.1,0.1,0,0.1;0.1,0.1,0,0.1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TransformPositionNode;24;-1845.45,126.999;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;36;-1575.153,435.0903;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;37;-1576.153,525.0903;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-1408.914,394.8086;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector4Node;41;-2061.686,-252.7476;Inherit;False;Property;_NotmalTileSpeed;Notmal Tile/Speed;9;0;Create;True;0;0;False;0;False;0.1,0.1,0,0.1;0.1,0.1,0,0.1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;2;-1250.128,185.4075;Inherit;False;Property;_Fresnel;Fresnel;0;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-1546.361,111.6848;Inherit;False;Property;_Sneak;Sneak;4;0;Create;True;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;32;-1236.424,394.1162;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;42;-1843.957,-252.8175;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-1610.543,-28.74619;Inherit;False;Property;_Distortion;Distortion;2;0;Create;True;0;0;False;0;False;0.162;0.081;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;27;-949.424,366.1162;Inherit;True;Property;_NoiseFresnel;Noise Fresnel;7;0;Create;True;0;0;False;0;False;-1;a225a619c91968049ba2bdbc44c812fb;a225a619c91968049ba2bdbc44c812fb;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;44;-1100.755,166.3206;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-1113,299.5;Inherit;False;Property;_FresnelPower;Fresnel Power;1;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-924.424,555.1162;Inherit;False;Property;_NoiseIntencity;Noise Intencity;8;0;Create;True;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;43;-1838.769,-137.6647;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-1529.737,-174.6533;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-603.5236,397.3165;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition;12;-1099.569,-406.6525;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;18;-1254.822,-42.81314;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;40;-1221.11,-163.0547;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FresnelNode;1;-944,147.5;Inherit;True;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;13;-864.0131,-407.2847;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;29;-555.6334,147.7112;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;11;-1035.569,-102.6527;Inherit;True;Property;_T_NormalMap_2;T_NormalMap_2;5;1;[Normal];Create;True;0;0;False;0;False;-1;321db13ba4b71c046a8e127c711e7871;321db13ba4b71c046a8e127c711e7871;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;14;-627.1851,-402.6616;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;5;-398,-110.5;Inherit;False;Property;_FresnelColor;Fresnel Color ;3;1;[HDR];Create;True;0;0;False;0;False;1,1,1,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;4;-378.0314,146.1809;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-164.9171,120.4115;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ScreenColorNode;15;-368.5691,-406.1525;Inherit;False;Global;_GrabScreen0;Grab Screen 0;4;0;Create;True;0;0;False;0;False;Object;-1;False;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;7;55.91714,95.32863;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;25;-188.7498,-398.801;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;245.9414,58.81925;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;38;-384.7213,273.7318;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;566,13;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;S_Sneaky;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;24;0;23;0
WireConnection;36;0;35;1
WireConnection;36;1;35;2
WireConnection;37;0;35;3
WireConnection;37;1;35;4
WireConnection;33;0;24;0
WireConnection;33;1;36;0
WireConnection;32;0;33;0
WireConnection;32;2;37;0
WireConnection;42;0;41;1
WireConnection;42;1;41;2
WireConnection;27;1;32;0
WireConnection;44;1;2;0
WireConnection;44;2;8;0
WireConnection;43;0;41;3
WireConnection;43;1;41;4
WireConnection;21;0;42;0
WireConnection;21;1;24;0
WireConnection;30;0;27;1
WireConnection;30;1;31;0
WireConnection;18;1;19;0
WireConnection;18;2;8;0
WireConnection;40;0;21;0
WireConnection;40;2;43;0
WireConnection;1;2;44;0
WireConnection;1;3;3;0
WireConnection;13;0;12;0
WireConnection;29;0;1;0
WireConnection;29;1;30;0
WireConnection;11;1;40;0
WireConnection;11;5;18;0
WireConnection;14;0;13;0
WireConnection;14;1;11;0
WireConnection;4;0;29;0
WireConnection;6;0;5;0
WireConnection;6;1;4;0
WireConnection;15;0;14;0
WireConnection;7;1;6;0
WireConnection;7;2;8;0
WireConnection;25;0;15;0
WireConnection;39;0;25;0
WireConnection;39;1;7;0
WireConnection;38;0;1;0
WireConnection;0;2;39;0
WireConnection;0;9;38;0
ASEEND*/
//CHKSM=3EC9D40A71EE5647DAEDF8777A9D0C7A529F6951