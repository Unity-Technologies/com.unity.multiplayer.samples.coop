// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_Ice"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Distortion("Distortion", Range( 0 , 1)) = 0.162
		_Color_1("Color _1", Color) = (1,1,1,0)
		[HDR]_Color_2("Color _2", Color) = (1,1,1,0)
		_FX_T_Ice("FX_T_Ice", 2D) = "white" {}
		_TileIceTexture("Tile Ice Texture", Float) = 2
		_TileNormalTexture("Tile Normal Texture", Float) = 2
		_PowerIceTexture("Power Ice Texture", Float) = 2
		[Normal]_T_NormalMap_2("T_NormalMap_2", 2D) = "bump" {}
		_Fresnel("Fresnel", Float) = 1
		_FresnelPower("Fresnel Power", Float) = 1
		_Ms_Ice_Material_BaseMap("Ms_Ice_Material_BaseMap", 2D) = "white" {}
		_DissolveTexture("Dissolve Texture", 2D) = "white" {}
		_TileSpeedDissolveTexture("Tile/Speed Dissolve Texture", Vector) = (1,1,0,-2)
		_Dissolve("Dissolve", Range( 1 , 3)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		GrabPass{ }
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
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

		struct Input
		{
			float4 screenPos;
			float3 worldPos;
			float2 uv_texcoord;
			float3 worldNormal;
		};

		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		UNITY_DECLARE_TEX2D_NOSAMPLER(_T_NormalMap_2);
		uniform float _TileNormalTexture;
		SamplerState sampler_T_NormalMap_2;
		uniform float _Distortion;
		uniform float _Dissolve;
		uniform float4 _Color_1;
		uniform float4 _Color_2;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_FX_T_Ice);
		SamplerState sampler_FX_T_Ice;
		uniform float _TileIceTexture;
		uniform float _PowerIceTexture;
		uniform float _Fresnel;
		uniform float _FresnelPower;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_Ms_Ice_Material_BaseMap);
		SamplerState sampler_Ms_Ice_Material_BaseMap;
		uniform float4 _Ms_Ice_Material_BaseMap_ST;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_DissolveTexture);
		SamplerState sampler_DissolveTexture;
		uniform float4 _TileSpeedDissolveTexture;
		uniform float _Cutoff = 0.5;


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
			float3 ase_worldPos = i.worldPos;
			float3 worldToObj49 = mul( unity_WorldToObject, float4( ase_worldPos, 1 ) ).xyz;
			float lerpResult94 = lerp( 0.0 , _Distortion , _Dissolve);
			float4 screenColor7 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( float3( (ase_grabScreenPosNorm).xy ,  0.0 ) + UnpackScaleNormal( SAMPLE_TEXTURE2D( _T_NormalMap_2, sampler_T_NormalMap_2, ( _TileNormalTexture * worldToObj49 ).xy ), lerpResult94 ) ).xy);
			float4 clampResult45 = clamp( screenColor7 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
			float temp_output_18_0 = pow( SAMPLE_TEXTURE2D( _FX_T_Ice, sampler_FX_T_Ice, ( i.uv_texcoord * _TileIceTexture ) ).r , _PowerIceTexture );
			float4 lerpResult15 = lerp( _Color_1 , _Color_2 , temp_output_18_0);
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV40 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode40 = ( 0.0 + _Fresnel * pow( 1.0 - fresnelNdotV40, _FresnelPower ) );
			float clampResult43 = clamp( fresnelNode40 , 0.0 , 1.0 );
			float4 lerpResult44 = lerp( clampResult45 , ( clampResult45 + lerpResult15 ) , clampResult43);
			float2 uv_Ms_Ice_Material_BaseMap = i.uv_texcoord * _Ms_Ice_Material_BaseMap_ST.xy + _Ms_Ice_Material_BaseMap_ST.zw;
			o.Emission = ( lerpResult44 + SAMPLE_TEXTURE2D( _Ms_Ice_Material_BaseMap, sampler_Ms_Ice_Material_BaseMap, uv_Ms_Ice_Material_BaseMap ).r ).rgb;
			o.Alpha = 1;
			float3 temp_cast_4 = (temp_output_18_0).xxx;
			float2 appendResult61 = (float2(_TileSpeedDissolveTexture.z , _TileSpeedDissolveTexture.w));
			float2 appendResult60 = (float2(_TileSpeedDissolveTexture.x , _TileSpeedDissolveTexture.y));
			float2 panner58 = ( 1.0 * _Time.y * appendResult61 + ( (ase_worldPos).xy * appendResult60 ));
			float3 temp_cast_5 = (( SAMPLE_TEXTURE2D( _DissolveTexture, sampler_DissolveTexture, panner58 ).r + 1.0 )).xxx;
			float3 clampResult88 = clamp( step( temp_cast_4 , ( ( worldToObj49 + _Dissolve ) - temp_cast_5 ) ) , float3( 0,0,0 ) , float3( 1,0,0 ) );
			clip( clampResult88.x - _Cutoff );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Unlit keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
				float3 worldNormal : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldNormal = worldNormal;
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.screenPos = ComputeScreenPos( o.pos );
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = IN.worldNormal;
				surfIN.screenPos = IN.screenPos;
				SurfaceOutput o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutput, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
1920;0;1920;1139;2769.459;417.1923;1.836819;True;False
Node;AmplifyShaderEditor.WorldPosInputsNode;48;-2164.651,259.165;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;28;-1525.571,-182.0277;Inherit;False;Property;_TileNormalTexture;Tile Normal Texture;6;0;Create;True;0;0;False;0;False;2;0.96;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TransformPositionNode;49;-1890.355,253.8652;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;79;-1712.669,528.4005;Inherit;False;Property;_Dissolve;Dissolve;14;0;Create;True;0;0;False;0;False;1;2.3;1;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-1646.217,-26.909;Inherit;False;Property;_Distortion;Distortion;1;0;Create;True;0;0;False;0;False;0.162;0.081;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;59;-1616.459,1138.435;Inherit;False;Property;_TileSpeedDissolveTexture;Tile/Speed Dissolve Texture;13;0;Create;True;0;0;False;0;False;1,1,0,-2;0.4,0.2,0,0.5;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GrabScreenPosition;6;-1152,-508.5;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;94;-1290.496,-40.97596;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-1255.544,-175.7322;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;97;-1203.943,123.2401;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;21;-1005.094,341.4954;Inherit;False;Property;_TileIceTexture;Tile Ice Texture;5;0;Create;True;0;0;False;0;False;2;1.16;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;60;-1339.459,1137.435;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;76;-1432.425,992.8563;Inherit;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;-1228.226,997.5682;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;24;-925.5439,-508.7322;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;61;-1342.459,1237.435;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-789.2675,226.6596;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;23;-1085.742,-206.1531;Inherit;True;Property;_T_NormalMap_2;T_NormalMap_2;8;1;[Normal];Create;True;0;0;False;0;False;-1;321db13ba4b71c046a8e127c711e7871;321db13ba4b71c046a8e127c711e7871;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;14;-599.6047,198.0731;Inherit;True;Property;_FX_T_Ice;FX_T_Ice;4;0;Create;True;0;0;False;0;False;-1;0b1de0bb73125e448ace918cf7bcd9c4;0b1de0bb73125e448ace918cf7bcd9c4;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;22;-488.6863,415.9061;Inherit;False;Property;_PowerIceTexture;Power Ice Texture;7;0;Create;True;0;0;False;0;False;2;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;58;-1062.887,998.0903;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;13;-679.616,-508.009;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;17;-601.28,-91.82106;Inherit;False;Property;_Color_2;Color _2;3;1;[HDR];Create;True;0;0;False;0;False;1,1,1,0;1.273585,1.687506,2,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;18;-252.9386,229.9059;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;52;-836.4849,964.6456;Inherit;True;Property;_DissolveTexture;Dissolve Texture;12;0;Create;True;0;0;False;0;False;-1;9ed3e7e3831313745a7354fa9e38753c;09ff3236ed6851c44b22b13107c206f5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;41;-536.5254,-603.1747;Inherit;False;Property;_Fresnel;Fresnel;9;0;Create;True;0;0;False;0;False;1;0.87;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;7;-421,-511.5;Inherit;False;Global;_GrabScreen0;Grab Screen 0;4;0;Create;True;0;0;False;0;False;Object;-1;False;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;42;-540.5255,-699.1747;Inherit;False;Property;_FresnelPower;Fresnel Power;10;0;Create;True;0;0;False;0;False;1;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;16;-592.5969,-333.9892;Inherit;False;Property;_Color_1;Color _1;2;0;Create;True;0;0;False;0;False;1,1,1,0;0,0.4192235,0.6132076,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;45;-101.5521,-505.1285;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-1306.542,506.3626;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;15;-281.2701,-103.9298;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;75;-466.4711,994.1073;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;40;-361.5254,-714.1747;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;89;-252.3497,504.1649;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;43;-103.5254,-713.1747;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;8;19.38403,-179.009;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;44;216.4785,-502.6745;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;46;39.3558,41.53414;Inherit;True;Property;_Ms_Ice_Material_BaseMap;Ms_Ice_Material_BaseMap;11;0;Create;True;0;0;False;0;False;-1;3f32cccbde31f004495ae8710935bd81;3f32cccbde31f004495ae8710935bd81;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;73;368.5094,387.3483;Inherit;True;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;88;572.4934,388.1971;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;1,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;47;423.8541,48.67094;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-1702.615,733.2145;Inherit;False;Property;_Hardness;Hardness;15;0;Create;True;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;916.0561,31.99199;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;S_Ice;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;49;0;48;0
WireConnection;94;1;10;0
WireConnection;94;2;79;0
WireConnection;27;0;28;0
WireConnection;27;1;49;0
WireConnection;60;0;59;1
WireConnection;60;1;59;2
WireConnection;76;0;48;0
WireConnection;53;0;76;0
WireConnection;53;1;60;0
WireConnection;24;0;6;0
WireConnection;61;0;59;3
WireConnection;61;1;59;4
WireConnection;20;0;97;0
WireConnection;20;1;21;0
WireConnection;23;1;27;0
WireConnection;23;5;94;0
WireConnection;14;1;20;0
WireConnection;58;0;53;0
WireConnection;58;2;61;0
WireConnection;13;0;24;0
WireConnection;13;1;23;0
WireConnection;18;0;14;1
WireConnection;18;1;22;0
WireConnection;52;1;58;0
WireConnection;7;0;13;0
WireConnection;45;0;7;0
WireConnection;96;0;49;0
WireConnection;96;1;79;0
WireConnection;15;0;16;0
WireConnection;15;1;17;0
WireConnection;15;2;18;0
WireConnection;75;0;52;1
WireConnection;40;2;41;0
WireConnection;40;3;42;0
WireConnection;89;0;96;0
WireConnection;89;1;75;0
WireConnection;43;0;40;0
WireConnection;8;0;45;0
WireConnection;8;1;15;0
WireConnection;44;0;45;0
WireConnection;44;1;8;0
WireConnection;44;2;43;0
WireConnection;73;0;18;0
WireConnection;73;1;89;0
WireConnection;88;0;73;0
WireConnection;47;0;44;0
WireConnection;47;1;46;1
WireConnection;0;2;47;0
WireConnection;0;10;88;0
ASEEND*/
//CHKSM=786C375307A4B12E149E0CE79C6DB34627AC1E40