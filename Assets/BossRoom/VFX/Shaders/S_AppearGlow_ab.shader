// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom/AS/Dissolve (Alpha Blended)"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		[HDR]_Tint("Tint", Color) = (1,1,1,1)
		_Dissolve("Dissolve", Range( 0 , 1)) = 0
		[KeywordEnum(Custom,VertexAlpha,Slider)] _DissolveType("DissolveType", Float) = 2
		_Panner("Panner", Vector) = (0,0.01,0,0)
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 0
		[KeywordEnum(Red,UV,Alpha,DissolveTexRed,DissolveTexAlpha)] _DisType("DisType", Float) = 0
		[Toggle(_INVERTDISSOLVE_ON)] _InvertDissolve("InvertDissolve", Float) = 0
		_DissolveTex("DissolveTex", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.CompareFunction)]_ZTestMode("Z Test Mode", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		AlphaToMask Off
		Cull [_Cull]
		ColorMask RGBA
		ZWrite Off
		ZTest [_ZTestMode]
		
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _DISTYPE_RED _DISTYPE_UV _DISTYPE_ALPHA _DISTYPE_DISSOLVETEXRED _DISTYPE_DISSOLVETEXALPHA
			#pragma shader_feature_local _DISSOLVETYPE_CUSTOM _DISSOLVETYPE_VERTEXALPHA _DISSOLVETYPE_SLIDER
			#pragma shader_feature_local _INVERTDISSOLVE_ON
			#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
			#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
			#else//ASE Sampling Macros
			#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
			#endif//ASE Sampling Macros
			


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform half _Cull;
			uniform half _ZTestMode;
			UNITY_DECLARE_TEX2D_NOSAMPLER(_MainTex);
			uniform half2 _Panner;
			SamplerState sampler_MainTex;
			uniform half4 _Tint;
			UNITY_DECLARE_TEX2D_NOSAMPLER(_DissolveTex);
			SamplerState sampler_DissolveTex;
			uniform half4 _DissolveTex_ST;
			uniform half _Dissolve;

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.ase_texcoord1 = v.ase_texcoord;
				o.ase_color = v.color;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				half2 texCoord28 = i.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				half2 panner27 = ( 1.0 * _Time.y * _Panner + texCoord28);
				half4 tex2DNode1 = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, panner27 );
				half4 texCoord22 = i.ase_texcoord1;
				texCoord22.xy = i.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_DissolveTex = i.ase_texcoord1.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
				half4 tex2DNode38 = SAMPLE_TEXTURE2D( _DissolveTex, sampler_DissolveTex, uv_DissolveTex );
				#if defined(_DISTYPE_RED)
				half staticSwitch32 = tex2DNode1.r;
				#elif defined(_DISTYPE_UV)
				half staticSwitch32 = ( texCoord22.x * texCoord22.y );
				#elif defined(_DISTYPE_ALPHA)
				half staticSwitch32 = tex2DNode1.a;
				#elif defined(_DISTYPE_DISSOLVETEXRED)
				half staticSwitch32 = tex2DNode38.r;
				#elif defined(_DISTYPE_DISSOLVETEXALPHA)
				half staticSwitch32 = tex2DNode38.a;
				#else
				half staticSwitch32 = tex2DNode1.r;
				#endif
				#if defined(_DISSOLVETYPE_CUSTOM)
				half staticSwitch24 = texCoord22.z;
				#elif defined(_DISSOLVETYPE_VERTEXALPHA)
				half staticSwitch24 = i.ase_color.a;
				#elif defined(_DISSOLVETYPE_SLIDER)
				half staticSwitch24 = _Dissolve;
				#else
				half staticSwitch24 = _Dissolve;
				#endif
				half temp_output_15_0 = step( staticSwitch32 , staticSwitch24 );
				#ifdef _INVERTDISSOLVE_ON
				half staticSwitch37 = ( 1.0 - temp_output_15_0 );
				#else
				half staticSwitch37 = temp_output_15_0;
				#endif
				half4 appendResult35 = (half4((( ( tex2DNode1 * i.ase_color * _Tint ) + ( temp_output_15_0 * tex2DNode1.r ) )).rgb , ( staticSwitch37 * ( tex2DNode1.a * i.ase_color.a * _Tint.a ) )));
				
				
				finalColor = appendResult35;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18400
2560;0;1920;1019;1270.922;511.2987;1.8611;True;True
Node;AmplifyShaderEditor.Vector2Node;30;-1562.52,271.4536;Inherit;False;Property;_Panner;Panner;4;0;Create;True;0;0;False;0;False;0,0.01;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;28;-1728.029,82.42163;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-1024.553,-245.1849;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;27;-1432.429,84.82163;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-0.1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-682.6218,-397.2775;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;38;-1028.554,291.5708;Inherit;True;Property;_DissolveTex;DissolveTex;8;0;Create;True;0;0;False;0;False;-1;None;9aae952339c781441a8f6b8155ee221a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;2;-558.5,269.5;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-1033.312,44.25732;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;False;-1;None;78f5b23dbc61d1042b153173e9609b8e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;16;-150.2654,-58.05077;Inherit;False;Property;_Dissolve;Dissolve;2;0;Create;True;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;32;-196.3052,-426.4481;Inherit;False;Property;_DisType;DisType;6;0;Create;True;0;0;False;0;False;0;0;3;True;;KeywordEnum;5;Red;UV;Alpha;DissolveTexRed;DissolveTexAlpha;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;24;-128.0322,-181.225;Inherit;False;Property;_DissolveType;DissolveType;3;0;Create;True;0;0;False;0;False;0;2;0;True;;KeywordEnum;3;Custom;VertexAlpha;Slider;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;6;-597.4861,468.1086;Inherit;False;Property;_Tint;Tint;1;1;[HDR];Create;True;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;15;175.3861,-324.8806;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-259.3,219.8;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;571.0199,-12.14673;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;36;280.0128,541.9551;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;661.3809,188.9222;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;37;438.6324,455.8954;Inherit;True;Property;_InvertDissolve;InvertDissolve;7;0;Create;True;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-258.3,362.8;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;34;852.0558,202.7788;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;703.5673,330.9464;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;35;1105.172,253.402;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;31;1280.877,367.3398;Half;False;Property;_Cull;Cull;5;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;39;1269.434,451.1546;Inherit;False;Property;_ZTestMode;Z Test Mode;9;1;[Enum];Create;True;0;1;UnityEngine.Rendering.CompareFunction;True;0;False;0;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;33;1245.753,218.3959;Half;False;True;-1;2;ASEMaterialInspector;100;1;Custom/AS/Dissolve (Alpha Blended);0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;False;False;False;False;False;False;True;0;False;-1;True;0;True;31;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;0;True;39;True;False;0;False;-1;1;False;-1;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;True;0
WireConnection;27;0;28;0
WireConnection;27;2;30;0
WireConnection;26;0;22;1
WireConnection;26;1;22;2
WireConnection;1;1;27;0
WireConnection;32;1;1;1
WireConnection;32;0;26;0
WireConnection;32;2;1;4
WireConnection;32;3;38;1
WireConnection;32;4;38;4
WireConnection;24;1;22;3
WireConnection;24;0;2;4
WireConnection;24;2;16;0
WireConnection;15;0;32;0
WireConnection;15;1;24;0
WireConnection;3;0;1;0
WireConnection;3;1;2;0
WireConnection;3;2;6;0
WireConnection;23;0;15;0
WireConnection;23;1;1;1
WireConnection;36;0;15;0
WireConnection;14;0;3;0
WireConnection;14;1;23;0
WireConnection;37;1;15;0
WireConnection;37;0;36;0
WireConnection;4;0;1;4
WireConnection;4;1;2;4
WireConnection;4;2;6;4
WireConnection;34;0;14;0
WireConnection;21;0;37;0
WireConnection;21;1;4;0
WireConnection;35;0;34;0
WireConnection;35;3;21;0
WireConnection;33;0;35;0
ASEEND*/
//CHKSM=17386F2D746E389B8DFE53C8E18B342307155D0D