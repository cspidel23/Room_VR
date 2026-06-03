// When using this shader, URP->Shadows->CascadeCount must be 1
Shader "Custom/ToonURP"
{
    Properties
    {
        _Color("Color", Color) = (0.5, 0.65, 1, 1)
        _MainTex("Main Texture", 2D) = "white" {}
        [HDR] _AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
        [HDR] _SpecularColor("Specular Color", Color) = (0.9, 0.9, 0.9, 1)
        [HDR] _ShadowColor("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)
        _Glossiness("Glossiness", Float) = 32

        [Space(20)]
        [Range(0,2)] _TriplanarMode("Triplanar Mode (0=None, 1=Full Blend, 2=Hybrid XZ/Y", Int) = 0
        _TriplanarScale("Triplanar Texture Scale", Float) = .1
        _TriplanarScaleXZ("Triplanar Texture ScaleXZ", Float) = .1

        [Range(0,1)] _TriplanarYThreshold("Y Threshold for Triplanar Hybrid", Float) = 0.5
        _ColorY("Y Area Color", Color) = (0.3568627, 0.6784314, 0.6, 1)
        _ColorXZ("XZ Area Color", Color) = (0.5, 0.65, 1, 1)

        _TexY("Y-Axis Texture", 2D) = "white" {}
        _TexXZ("X & Z Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS


            // URP Lighting
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


            // Enable shadows from main light
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;

                float3 worldPos : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _Color;
            float4 _AmbientColor;
            float4 _SpecularColor;
            float4 _ShadowColor;
            float _Glossiness;

            int _TriplanarMode;
            float _TriplanarScale;
            float _TriplanarScaleXZ;
            float _TriplanarYThreshold;
            float4 _ColorY;
            float4 _ColorXZ;
            TEXTURE2D(_TexY); SAMPLER(sampler_TexY);
            TEXTURE2D(_TexXZ); SAMPLER(sampler_TexXZ);

            // ---------------------------------------------------------
            // Vertex Shader
            // ---------------------------------------------------------
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(posWS);


                OUT.normalWS = normalWS;
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                OUT.viewDirWS = GetWorldSpaceViewDir(posWS);

                OUT.shadowCoord = TransformWorldToShadowCoord(posWS);

                OUT.worldPos = posWS;

                return OUT;
            }

            // -------------------------------
            // Triplanar Projection Function
            // -------------------------------
            float4 SampleTriplanar(
                TEXTURE2D(myTex), 
                SAMPLER(mySampler),
                float3 worldPos,
                float3 normalWS,
                float scale
            ){
                float3 n = abs(normalize(normalWS));
                n /= (n.x + n.y + n.z);

                float3 p = worldPos * scale;

                float4 xProj = SAMPLE_TEXTURE2D(myTex, mySampler, p.yz);
                float4 yProj = SAMPLE_TEXTURE2D(myTex, mySampler, p.xz);
                float4 zProj = SAMPLE_TEXTURE2D(myTex, mySampler, p.xy);

                return xProj * n.x +
                       yProj * n.y +
                       zProj * n.z;
            }

            // -------------------------------
            // Triplanar Projection Function 3
            // -------------------------------
            float4 SampleTriplanarHybrid(float3 worldPos, float3 normalWS, float scale, float scaleXZ)
            {
                float3 n = abs(normalize(normalWS));
                float3 p = worldPos * scale;
                float3 pXZ = worldPos * scaleXZ;

                // Sample XZ texture
                float4 xTex = SAMPLE_TEXTURE2D(_TexXZ, sampler_TexXZ, pXZ.zy) * _ColorXZ;
                float4 zTex = SAMPLE_TEXTURE2D(_TexXZ, sampler_TexXZ, pXZ.xy) * _ColorXZ;

                // Blend X and Z proportionally
                float xzWeightSum = n.x + n.z;
                float4 xzBlend = xzWeightSum > 0 ? (n.x*xTex + n.z*zTex) / xzWeightSum : xTex;

                // Threshold for Y dominance
                if (normalWS.y > 0 && n.y > _TriplanarYThreshold) // adjust 0.5 if you want the circle larger/smaller
                {
                    // Hard switch to Y texture
                    float2 uvY = p.xz;
                    return SAMPLE_TEXTURE2D(_TexY, sampler_TexY, uvY) * _ColorY;
                }
                

                // Otherwise use XZ blend
                return xzBlend;
            }


            // ---------------------------------------------------------
            // Fragment Shader
            // ---------------------------------------------------------
            half4 frag (Varyings IN) : SV_Target
            {
                // Normalize inputs
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // Sample main texture
                int mode = _TriplanarMode;
                float4 albedo;
                if(mode == 0)
                {
                    albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                }
                else if (mode == 1)
                {
                    albedo = SampleTriplanar(_MainTex, sampler_MainTex, IN.worldPos, normalWS, _TriplanarScale);
                }
                else if (mode == 2)
                {
                    albedo = SampleTriplanarHybrid(IN.worldPos, normalWS, _TriplanarScale, _TriplanarScaleXZ);
                }

                // Get main directional light
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir = mainLight.direction; // NOTE: negate URP light direction for correct dot
                float3 lightColor = mainLight.color;

                // ---------------------------------------------------------
                // Diffuse (Lambert) with toon step
                // ---------------------------------------------------------
                float NdotL = dot(normalWS, lightDir);

                float rawShadow = MainLightRealtimeShadow(IN.shadowCoord);
                float shadow = rawShadow > 0.5 ? 1.0 : 0.0;




                // Hard toon step (0 or 1)
                float diffuseStep = (NdotL > 0 && shadow > 0) ? 1.0 : 0.0;
                // Optional smooth step to reduce jaggies:
                // float diffuseStep = smoothstep(0.0, 0.01, NdotL * shadow);

                // Corrected diffuse
                float3 diffuseLit = diffuseStep * lightColor * 0.5; // Adjust 0.5 to match original brightness
                float3 diffuseShadow = _ShadowColor.rgb;
                float3 diffuse = lerp(diffuseShadow, diffuseLit, diffuseStep);

                // ---------------------------------------------
                // Additional Lights
                // ---------------------------------------------


                // ---------------------------------------------------------
                // Blinn-Phong Specular with toon step
                // ---------------------------------------------------------
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotH = max(dot(normalWS, halfVector), 0.0);

                float specularIntensity = pow(NdotH * diffuseStep, _Glossiness * _Glossiness);
                float specularStep = specularIntensity > 0.005 ? 1 : 0;
                // Optional smooth:
                // float specularStep = smoothstep(0.005, 0.01, specularIntensity);

                float3 specular = specularStep * _SpecularColor.rgb;

                // ---------------------------------------------------------
                // Combine color
                // ---------------------------------------------------------
                float3 finalColor;
                if(mode == 0)
                {
                    finalColor = _Color.rgb * albedo.rgb * (_AmbientColor.rgb + diffuse + specular);
                }
                else if (mode == 1)
                {
                    finalColor = _Color.rgb * albedo.rgb * (_AmbientColor.rgb + diffuse + specular);                }
                else if (mode == 2)
                {
                    finalColor = albedo.rgb * (_AmbientColor.rgb + diffuse + specular);
                }

                return float4(finalColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }

    }
}
