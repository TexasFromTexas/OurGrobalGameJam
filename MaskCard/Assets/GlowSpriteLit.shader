Shader "Custom/URP2DGlowSpriteLit"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Glow Settings)]
        [HDR] _GlowColor ("Glow Color", Color) = (1, 2, 5, 1) // 使用 HDR 颜色
        _GlowSize ("Glow Size", Range(0, 0.1)) = 0.02
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.0
        _GlowSoftness ("Glow Softness", Range(0, 1)) = 0.5

        _Dissolve("Dissolve", Range(-1,1)) = 0.0
        _BurnWidth("Burn Width", Range(0,0.5)) = 0
        _BurnIntensity("Burn Intensity", Range(0,5)) = 1.0
        _MaskTex("Burn Mask", 2D) = "white" {}
        _NoiseTex ("Noise Texture (R)", 2D) = "gray" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpriteLit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile _ DEBUG_DISPLAY
            
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"


            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                half2   lightingUV  : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GlowColor;
            float _GlowSize;
            float _GlowIntensity;
            half _GlowSoftness;

            sampler2D _NoiseTex;
            sampler2D _MaskTex;
            float _Dissolve;
            float _BurnWidth;
            float _BurnIntensity;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                output.lightingUV = half2(ComputeScreenPos(output.positionCS / output.positionCS.w).xy);
                return output;
            }

            // 计算外发光函数
            float GetGlowAlpha(float2 uv)
            {
                float alpha = 0;
                // 对周围 8 个方向进行采样（可根据需要增加采样点以获得更平滑的效果）
                float2 offsets[8] = {
                    float2(-1, -1), float2(0, -1), float2(1, -1),
                    float2(-1,  0),                float2(1,  0),
                    float2(-1,  1), float2(0,  1), float2(1,  1)
                };

                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + offsets[i] * _GlowSize;
                    alpha += tex2D(_MainTex, sampleUV).a;
                }
                return alpha / 8.0;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 获取原始纹理颜色
                float4 mainTex = tex2D(_MainTex, input.uv);
                float4 finalColor = mainTex * input.color;

                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(mainTex.rgb, mainTex.a, 1, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

                finalColor = CombinedShapeLightShared(surfaceData, inputData);
                // 计算外发光
                float glowAlpha = GetGlowAlpha(input.uv);
                
                // 只在原图透明度较低的地方显示发光（即“外”发光）
                // 使用 smoothstep 让边缘平滑一点
                float outerGlowMask = glowAlpha * step (mainTex.a + 0.01 , glowAlpha) * _GlowSoftness;
                float4 glow = _GlowColor * outerGlowMask * _GlowIntensity;

                // 叠加发光和原图颜色
                finalColor.rgb += glow.rgb;
                finalColor.a = max(mainTex.a, glow.a * _GlowIntensity);

                float noise = tex2D(_NoiseTex, input.uv).r - 0.0001f;

                // 溶解遮罩
             float dissolveMask = step(_Dissolve, noise);

                // 燃烧边缘（过渡带）
                 float edge = //smoothstep(max(_Dissolve - _BurnWidth, 0.0f), _Dissolve, noise) *
                              (1 - smoothstep(_Dissolve, min(_Dissolve + _BurnWidth, 1.0f), noise));
                float4 burnCol;
                burnCol.rgb = tex2D(_MaskTex,float2( 1 - edge, 0.5)).rgb;
                burnCol.rgb *= edge *  _BurnIntensity;
                finalColor.rgb += burnCol.rgb;
                finalColor.a *= dissolveMask ;
                // 注意：为了让 2D Light 影响此 Shader，
                // 在 URP 2D 中，管线会自动将 Light Color 乘到 input.color 上
                // 所以我们直接返回 finalColor 即可
                return finalColor;
            }
            ENDHLSL
        }
    }
}