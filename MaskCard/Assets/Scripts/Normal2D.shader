Shader "BD/Normal2D"
{
    Properties
    {
        [MainTexture] _BaseMap("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Sprite Color", Color) = (1,1,1,1)
        
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Toggle(_)] _AlphaClip("Use Alpha Clipping", Float) = 0.0
        [Toggle(_)] _FlipbookBlending("Use Flipbook Blending", Float) = 0.0
        
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AtlasST("Atlas ST", Vector) = (1,1,0,0)
        [PerRendererData] _IoTST("IoT ST", Vector) = (1,1,0,0)
        
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZWrite("Z Write", Float) = 1.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", Float) = 4.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0.0
        
        [Toggle(_)] _EnableVertexColor("Enable Vertex Color", Float) = 0.0
        [Toggle(_)] _EnableColorVariation("Enable Color Variation", Float) = 0.0
        [HDR] _ColorVariation("Color Variation", Color) = (0,0,0,0)
        
        [Toggle(_)] _EnableSoftClipping("Enable Soft Clipping", Float) = 0.0
        _SoftClippingDistance("Soft Clipping Distance", Range(0.0, 1.0)) = 0.01
        _SoftClippingRange("Soft Clipping Range", Range(0.0, 1.0)) = 0.01
        
        [Toggle(_)] _EnableCameraClipping("Enable Camera Clipping", Float) = 0.0
        _CameraClippingNearPlane("Camera Clipping Near Plane", Float) = -1.0
        _CameraClippingFarPlane("Camera Clipping Far Plane", Float) = -1.0
        
        [Toggle(_)] _EnableDistanceFading("Enable Distance Fading", Float) = 0.0
        _DistanceFadeNear("Distance Fade Near", Float) = 10.0
        _DistanceFadeFar("Distance Fade Far", Float) = 20.0
        
        [Toggle(_)] _EnableDistanceScaling("Enable Distance Scaling", Float) = 0.0
        _DistanceScaleNear("Distance Scale Near", Float) = 10.0
        _DistanceScaleFar("Distance Scale Far", Float) = 20.0
        _DistanceScaleMultiplier("Distance Scale Multiplier", Range(0.0, 5.0)) = 1.0
        
        [Toggle(_)] _EnableAngleFading("Enable Angle Fading", Float) = 0.0
        _AngleFadeMinDotProduct("Angle Fade Min Dot Product", Range(-1.0, 1.0)) = 0.0
        
        [Toggle(_)] _EnableAngleScaling("Enable Angle Scaling", Float) = 0.0
        _AngleScaleMaxDotProduct("Angle Scale Max Dot Product", Range(-1.0, 1.0)) = 0.0
        _AngleScaleMultiplier("Angle Scale Multiplier", Range(0.0, 5.0)) = 1.0
        
        [Toggle(_)] _EnableAngleClipping("Enable Angle Clipping", Float) = 0.0
        _AngleClipMinDotProduct("Angle Clip Min Dot Product", Range(-1.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalRenderPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }
        
        LOD 100
        
        Stencil 
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull [_Cull]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #pragma vertex CombinedShapeVertexMeta
            #pragma fragment CombinedShapeFragmentMeta
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _ _VERTEXCOLORRED_ON _VERTEXCOLORGREEN_ON _VERTEXCOLORBLUE_ON _VERTEXCOLORALPHA_ON
            #pragma shader_feature_local_fragment _ _COLORVARIATIONRED_ON _COLORVARIATIONGREEN_ON _COLORVARIATIONBLUE_ON _COLORVARIATIONALPHA_ON
            #pragma shader_feature_local_fragment _ _SOFTCLIPPING_ON
            #pragma shader_feature_local_fragment _ _CAMERACLIPPING_ON
            #pragma shader_feature_local_fragment _ _DISTANCEFADE_ON
            #pragma shader_feature_local_fragment _ _DIST-scales ON
            #pragma shader_feature_local_fragment _ _ANGLEFADE_ON
            #pragma shader_feature_local_fragment _ _ANGLESCALE_ON
            #pragma shader_feature_local_fragment _ _ANGLECLIP_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            // 避免PackHeightmap重定义冲突
            #ifdef PackHeightmap
            #undef PackHeightmap
            #endif
            
            float PackHeightmap(float height)
            {
                return height;
            }

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            uniform half4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            
            uniform half4 _RendererColor;
            uniform half2 _Flip;
            uniform half4 _AtlasST;
            uniform half4 _IoTST;
            
            uniform float _ZWrite;
            uniform float _ZTest;
            uniform float _Cull;
            
            uniform float _EnableVertexColor;
            uniform float _EnableColorVariation;
            uniform half4 _ColorVariation;
            
            uniform float _EnableSoftClipping;
            uniform float _SoftClippingDistance;
            uniform float _SoftClippingRange;
            
            uniform float _EnableCameraClipping;
            uniform float _CameraClippingNearPlane;
            uniform float _CameraClippingFarPlane;
            
            uniform float _EnableDistanceFading;
            uniform float _DistanceFadeNear;
            uniform float _DistanceFadeFar;
            
            uniform float _EnableDistanceScaling;
            uniform float _DistanceScaleNear;
            uniform float _DistanceScaleFar;
            uniform float _DistanceScaleMultiplier;
            
            uniform float _EnableAngleFading;
            uniform float _AngleFadeMinDotProduct;
            
            uniform float _EnableAngleScaling;
            uniform float _AngleScaleMaxDotProduct;
            uniform float _AngleScaleMultiplier;
            
            uniform float _EnableAngleClipping;
            uniform float _AngleClipMinDotProduct;
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float3 positionWS   : TEXCOORD1;
                float4 screenPos    : TEXCOORD2;
                float3 viewDir      : TEXCOORD3;
                float3 normalWS     : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings CombinedShapeVertexMeta(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 应用翻转
                input.positionOS.xy *= _Flip.xy;

                // 世界空间位置
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWs;
                output.positionCS = vertexInput.positionCS;
                
                // 视角方向
                output.viewDir = GetWorldSpaceNormalizeViewDir(vertexInput.positionWs);
                
                // 法线
                output.normalWS = TransformObjectToWorldNormal((float3)0);
                
                // UV坐标
                float4 uv = input.uv;
                uv.xy = TRANSFORM_TEX(uv, _BaseMap);
                output.uv = uv.xy * _AtlasST.xy + _AtlasST.zw;
                
                // 颜色
                output.color = input.color * _RendererColor * _BaseColor;
                
                // 屏幕位置
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);

                return output;
            }

            half4 CombinedShapeFragmentMeta(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // 应用颜色
                half4 color = texColor * input.color;
                
                // Alpha裁剪
            #ifdef _ALPHATEST_ON
                clip(color.a - _Cutoff);
            #endif

                // 顶点颜色混合
                if (_EnableVertexColor > 0.0)
                {
                    color.rgb *= input.color.rgb;
                }
                
                // 颜色变化
                if (_EnableColorVariation > 0.0)
                {
                    color += _ColorVariation;
                }
                
                // 软裁剪
            #if defined(_SOFTCLIPPING_ON)
                float distanceToEdge = min(min(input.screenPos.x, _ScreenParams.x - input.screenPos.x),
                                          min(input.screenPos.y, _ScreenParams.y - input.screenPos.y));
                if (distanceToEdge < _SoftClippingDistance)
                {
                    float alpha = saturate((distanceToEdge - _SoftClippingRange) / _SoftClippingDistance);
                    color.a *= alpha;
                }
            #endif
            
                // 距离淡出
            #if defined(_DISTANCEFADE_ON)
                float cameraToObjectDistance = length(_WorldSpaceCameraPos - input.positionWS);
                float fade = 1.0 - saturate((cameraToObjectDistance - _DistanceFadeNear) / 
                                           (_DistanceFadeFar - _DistanceFadeNear));
                color.a *= fade;
            #endif

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            half4 _BaseMap_ST;
            half _Cutoff;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
            #ifdef _ALPHATEST_ON
                clip(texColor.a - _Cutoff);
            #endif

                return 0;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}