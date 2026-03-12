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
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
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

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 viewDirWS:TEXCOORD2; float fogFactor:TEXCOORD3; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS  = IN.positionOS.xyz;
                float3 posWS0 = TransformObjectToWorld(posOS);
                float  t      = _Time.y * _WaveSpeed;
                float  h      = sin(posWS0.x*_WaveScale+t)*_WaveHeight
                              + cos(posWS0.z*_WaveScale*.7+t*1.3)*_WaveHeight*.6
                              + sin((posWS0.x+posWS0.z)*_WaveScale*.5+t*.8)*_WaveHeight*.4;
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
                float  t  = _Time.y * _WaveSpeed;
                float3 wp = IN.positionWS;
                float3 n  = normalize(float3(sin(wp.z*_WaveScale*1.5+t*1.1)*.35, 1.0, cos(wp.x*_WaveScale*1.2+t*.9)*.35));
                float3 vd = normalize(IN.viewDirWS);
                float  fr = pow(1.0 - saturate(dot(vd,n)), _FresnelPower);
                half4  col = lerp(_DeepColor, _ShallowColor, fr);
                Light  ml  = GetMainLight();
                float  NdL = saturate(dot(n, ml.direction));
                float3 lit = col.rgb*(ml.color*NdL*.8+.4);
                float3 hv  = normalize(vd+ml.direction);
                lit += ml.color * pow(saturate(dot(n,hv)), lerp(4,256,_Smoothness)) * _Smoothness;
                half4 r = half4(MixFog(lit, IN.fogFactor), col.a * _Transparency);
                return r;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
