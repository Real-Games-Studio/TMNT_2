Shader "Unlit/Teeth-Whitener"
{
    Properties
    {
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _BrightnessFactor ("Brightness Factor", Range(1, 3)) = 1.5
        _Threshold ("Brightness Threshold", Range(0, 1)) = 0.5
        _PurpleTint ("Purple Tint", Color) = (0.86, 0.86, 1, 1) // Default purple color
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        GrabPass
        {
            "_GrabTexture" // Capture the screen behind the object
        }

        Pass
        {
            // Enable blending for transparency
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 grabUV : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MaskTex;
            sampler2D _GrabTexture; // Texture that holds the grabbed screen content
            float4 _MaskTex_ST;
            float _BrightnessFactor;
            float _Threshold;
            float4 _PurpleTint;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MaskTex);
                o.grabUV = ComputeGrabScreenPos(o.vertex); // Compute UV for GrabPass texture
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the grabbed texture (the background)
                fixed4 grabbedColor = tex2D(_GrabTexture, i.grabUV.xy / i.grabUV.w);

                // Sample the mask texture
                fixed4 maskColor = tex2D(_MaskTex, i.uv);

                // Brighten areas of the grabbed texture that are above the threshold
                fixed3 brightenedColor = grabbedColor.rgb;
                float brightness = dot(grabbedColor.rgb, fixed3(0.299, 0.587, 0.114));
                // if (brightness > _Threshold)
                // {
                //     brightenedColor *= _BrightnessFactor;
                // }
                // Smooth transition for brightness effect
                float smooth = 1;
                float blendFactor = smoothstep(_Threshold - smooth, _Threshold + smooth, brightness);
                brightenedColor = lerp(grabbedColor.rgb, grabbedColor.rgb * _BrightnessFactor, blendFactor);


                // Apply purple tint to neutralize yellow
                fixed4 tintedColor = fixed4(brightenedColor * _PurpleTint.rgb, grabbedColor.a);

                // Apply the mask to determine the final color
                fixed4 finalColor = lerp(grabbedColor, tintedColor, maskColor.a);

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                // Return the masked, tinted, and brightened color
                return finalColor;
            }
            ENDCG
        }
    }
}
