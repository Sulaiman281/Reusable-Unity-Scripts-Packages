Shader "Custom/UnderwaterWaterURP"
{
    Properties
    {
        _ShallowColor   ("Shallow Color",   Color)  = (0.05, 0.55, 0.80, 0.55)
        _DeepColor      ("Deep Color",      Color)  = (0.01, 0.10, 0.40, 0.85)
        _FresnelPower   ("Fresnel Power",   Range(1,8)) = 3.0
        _Smoothness     ("Smoothness",      Range(0,1)) = 0.95
        _WaveSpeed      ("Wave Speed",      Range(0,3)) = 0.8
        _WaveScale      ("Wave Scale",      Range(0.1,5)) = 1.2
        _WaveHeight     ("Wave Height",     Range(0,0.5)) = 0.08
        _Transparency   ("Transparency",    Range(0,1)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType"  = "Transparent"
            "Queue"       = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _FresnelPower;
                float  _Smoothness;
                float  _WaveSpeed;
                float  _WaveScale;
                float  _WaveHeight;
                float  _Transparency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz;

                // Vertex wave displacement
                float3 posWS0 = TransformObjectToWorld(posOS);
                float  t      = _Time.y * _WaveSpeed;
                float  h      = sin(posWS0.x * _WaveScale + t)            * _WaveHeight
                              + cos(posWS0.z * _WaveScale * 0.7 + t*1.3)  * _WaveHeight * 0.6
                              + sin((posWS0.x+posWS0.z)*_WaveScale*0.5+t*0.8) * _WaveHeight * 0.4;
                posOS.y += h;

                VertexPositionInputs vpi = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = vpi.positionCS;
                OUT.positionWS  = vpi.positionWS;
                OUT.normalWS    = vni.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(vpi.positionWS);
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Animated surface normals
                float  t  = _Time.y * _WaveSpeed;
                float3 wp = IN.positionWS;
                float  nx = sin(wp.z * _WaveScale * 1.5 + t*1.1) * 0.35;
                float  nz = cos(wp.x * _WaveScale * 1.2 + t*0.9) * 0.35;
                float3 n  = normalize(float3(nx, 1.0, nz));

                // Fresnel
                float3 viewDir  = normalize(IN.viewDirWS);
                float  cosAngle = saturate(dot(viewDir, n));
                float  fresnel  = pow(1.0 - cosAngle, _FresnelPower);

                half4  col   = lerp(_DeepColor, _ShallowColor, fresnel);

                // Simple lighting
                Light  mainLight = GetMainLight();
                float  NdotL = saturate(dot(n, mainLight.direction));
                float3 lit   = col.rgb * (mainLight.color * NdotL * 0.8 + 0.4);

                // Specular highlight
                float3 halfDir = normalize(viewDir + mainLight.direction);
                float  spec    = pow(saturate(dot(n, halfDir)), lerp(4, 256, _Smoothness));
                lit += mainLight.color * spec * _Smoothness;

                half4 result = half4(lit, col.a * _Transparency);
                result.rgb   = MixFog(result.rgb, IN.fogFactor);
                return result;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
