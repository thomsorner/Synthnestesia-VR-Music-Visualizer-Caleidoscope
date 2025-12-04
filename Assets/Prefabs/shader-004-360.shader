Shader "Unlit/AudioMandelKoch_URP_Final"
{
    Properties
    {
        _TimeScale   ("Time Scale", Float) = 1.0
        _Intensity   ("Global Intensity", Float) = 1.0
        _FFTtex      ("FFT 1D Texture", 2D) = "gray" {}
        _UseFFT      ("Use FFT (0/1)", Range(0,1)) = 0.0
        _UVScale     ("UV Scale", Float) = 1.0
        _UVRotate    ("UV Rotate (rad)", Float) = 0.0
        _VignetteAmt ("Vignette Amount", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
            "RenderType"="Opaque"
        }

        // Visible por ambas caras y sin escribir Z (ideal para “domo”)
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // --- Texturas ---
            TEXTURE2D(_FFTtex);
            SAMPLER(sampler_FFTtex);

            // --- Uniformes ---
            CBUFFER_START(UnityPerMaterial)
                float  _TimeScale;
                float  _Intensity;
                float  _UVScale;
                float  _UVRotate;
                float  _VignetteAmt;
                float  _UseFFT;
            CBUFFER_END

            // Reemplazo compatible
            #define PI 3.14159265359

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_Position;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            // utilidades
            float2 Rot2(float2 p, float a) {
                float c = cos(a), s = sin(a);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            float2 DirToLatLong(float3 dirWS)
            {
                dirWS = normalize(dirWS);
                float az = atan2(dirWS.x, dirWS.z); // [-pi..pi]
                float el = asin(clamp(dirWS.y, -1.0, 1.0));
                float u = az * (1.0 / (2.0 * PI)) + 0.5;
                float v = el * (1.0 / PI) + 0.5;
                return float2(u, v);
            }

            // FFT segura
            float readFFT(float u)
            {
                float2 tuv = float2(saturate(u), 0.5);
                return SAMPLE_TEXTURE2D(_FFTtex, sampler_FFTtex, tuv).r;
            }

            // paleta temporal
            float3 randomCol(float sc, float time)
            {
                float r = sin(sc * 1.0 * time)*0.5 + 0.5;
                float g = sin(sc * 2.0 * time)*0.5 + 0.5;
                float b = sin(sc * 4.0 * time)*0.5 + 0.5;
                return saturate(float3(r,g,b));
            }

            // Mandelbrot con smooth count (IQ) - iteraciones reducidas
            float mandelbrot(float2 c)
            {
                float c2 = dot(c,c);
                if (256.0*c2*c2 - 96.0*c2 + 32.0*c.x - 3.0 < 0.0) return 0.0;
                if (16.0*(c2 + 2.0*c.x + 1.0) - 1.0 < 0.0) return 0.0;

                const float B = 128.0;
                float l = 0.0;
                float2 z = float2(0.0, 0.0);

                [loop]
                for (int i=0; i<64; i++) // reducido a 64
                {
                    z = float2(z.x*z.x - z.y*z.y, 2.0*z.x*z.y) + c;
                    if (dot(z,z) > (B*B)) break;
                    l += 1.0;
                }

                if (l <= 0.0) return 0.0;

                float z2 = max(dot(z,z), 1e-6);
                float sl = l - log2(max(log2(z2), 1e-6)) + 4.0;
                return sl;
            }

            float3 mandelbrotImg(float2 p, float time, float freq3)
            {
                float mtime = time - freq3;
                float zoo   = 0.62 + 0.38*cos(0.1*mtime);
                float coa   = cos(0.015*(1.0 - zoo)*mtime);
                float sia   = sin(0.015*(1.0 - zoo)*mtime);
                zoo         = pow(saturate(zoo), 6.0);

                float2 xy = float2(p.x*coa - p.y*sia, p.x*sia + p.y*coa);
                float2 c  = float2(-0.745, 0.186) + xy*zoo;

                float l   = mandelbrot(c);

                float3 rc = max(randomCol(0.1, time), float3(0.001,0.001,0.001));
                float3 col1 = 0.5 + 0.5*cos(3.0 + l*0.15 + rc);
                float3 col2 = 0.5 + 0.5*cos(3.0 + l*0.15 / rc);
                float  t    = sin(mtime)*0.5 + 0.5;
                return lerp(col1, col2, t);
            }

            float2 Nvec(float a) { return float2(sin(a), cos(a)); }

            half4 frag (Varyings i) : SV_Target
            {
                // Tiempo base
                float time = _Time.y * _TimeScale;

                // FFT (fallback si _UseFFT=0)
                float f0 = 0.3 + 0.2*sin(time*0.7);
                float f1 = 0.3 + 0.2*sin(time*0.9 + 1.3);
                float f2 = 0.4 + 0.2*sin(time*0.5 + 2.1);
                float f3 = 0.3 + 0.2*sin(time*1.1 + 0.7);

                if (_UseFFT > 0.5)
                {
                    f0 = max(readFFT(0.01), 1e-4);
                    f1 = max(readFFT(0.07), 1e-4);
                    f2 = clamp(readFFT(0.15), 0.1, 0.9);
                    f3 = max(readFFT(0.30), 1e-4);
                }

                float avgFreq = (f0 + f1 + f2 + f3) * 0.25;

                // UV desde normal (lat-long)
                float2 pano = DirToLatLong(normalize(i.normalWS)); 
                float2 uv   = pano - 0.5;                          
                uv = Rot2(uv, _UVRotate);
                uv *= _UVScale;

                // dinámica espacial
                uv = Rot2(uv, (sin(time*0.1) / max(f0, 1e-3) * 0.1) * PI);
                uv *= (4.0 - (avgFreq * 1.5));
                uv.x = abs(uv.x);

                float3 col = float3(0.0, 0.0, 0.0);
                float  d;

                // Simetrías tipo “Koch”
                float2 n = Nvec((5.0/6.0)*PI);
                uv.y += tan((5.0/6.0)*PI)*0.5;
                d = dot(uv - float2(0.5, 0.0), n);
                uv -= max(0.0, d)*n*2.0;

                float scale = 1.0;
                n = Nvec(f0 * (2.0/3.0)*PI);
                uv.x += 0.5;

                [loop]
                for (int k=0; k<6; k++) // reducido a 6 iteraciones para móvil
                {
                    uv *= 3.0;
                    scale *= 3.0;
                    uv.x -= 1.5;

                    uv.x = abs(uv.x);
                    uv.x -= 0.5;
                    d = dot(uv, n);
                    uv -= min(0.0, d)*n*2.0;
                }

                d = length(uv / f2 - float2(clamp(uv.x, -1.0, 1.0), 0.0));
                col += smoothstep(10.0/600.0, 0.0, d/scale);

                uv /= scale;

                // Mandelbrot coloreado y mezcla
                float3 manCol = mandelbrotImg(uv, time, f3);
                col += manCol;

                // Vignette
                col *= 1.0 - (_VignetteAmt * 0.5) * length(uv * 0.5) * f1;

                // Intensidad global
                col *= _Intensity;

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
