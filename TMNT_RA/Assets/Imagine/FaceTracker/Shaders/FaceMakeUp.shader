Shader "Imagine/FaceMakeUp"
{
    Properties
    {
        _SmoothMaskTex ("Smoothness Mask Texture", 2D) = "black" {}
        _BlurAmount ("Face Smoothness", Range(1, 20)) = 5
        _BlushTex ("Blush Texture", 2D) = "black" {}
        _BlushColor ("Blush Color", Color) = (1,1,1,1)

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        GrabPass
        {
            "_GrabTexture" // Capture the screen behind the object
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            // Declare deformation offsets array
            uniform float4 _DeformationOffsets[468]; 

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexId : SV_VertexID; // Custom vertex ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 grabUV : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _SmoothMaskTex;
            float4 _SmoothMaskTex_ST;
            sampler2D _BlushTex;
            float4 _BlushColor;

            sampler2D _GrabTexture; // Texture that holds the grabbed screen content
            float4 _GrabTexture_TexelSize;
            float _BlurAmount;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _SmoothMaskTex);
                o.grabUV = ComputeGrabScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float2 grabUV = i.grabUV.xy/i.grabUV.w;
                fixed4 grabCol = tex2D(_GrabTexture, grabUV);
                fixed4 maskCol = tex2D(_SmoothMaskTex, i.uv).r;

                float2 texSize = _GrabTexture_TexelSize * _BlurAmount * maskCol;


                UNITY_APPLY_FOG(i.fogCoord, col);


                float3 blur = float3(0.0f, 0.0f, 0.0f);
                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-2.0, -2.0)) * 0.003f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-1.0, -2.0)) * 0.013f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 0.0, -2.0)) * 0.023f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 1.0, -2.0)) * 0.013f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 2.0, -2.0)) * 0.003f;

                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-2.0, -1.0)) * 0.013f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-1.0, -1.0)) * 0.059f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 0.0, -1.0)) * 0.097f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 1.0, -1.0)) * 0.059f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 2.0, -1.0)) * 0.013f;

                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-2.0, 0.0)) * 0.023f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-1.0, 0.0)) * 0.097f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 0.0, 0.0)) * 0.159f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 1.0, 0.0)) * 0.097f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 2.0, 0.0)) * 0.023f;

                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-2.0, 1.0)) * 0.013f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-1.0, 1.0)) * 0.059f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 0.0, 1.0)) * 0.097f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 1.0, 1.0)) * 0.059f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 2.0, 1.0)) * 0.013f;

                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-2.0, 2.0)) * 0.003f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2(-1.0, 2.0)) * 0.013f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 0.0, 2.0)) * 0.023f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 1.0, 2.0)) * 0.013f;
                blur += tex2D(_GrabTexture, grabUV + texSize * float2( 2.0, 2.0)) * 0.003f;

                float4 baseColor = float4(blur, 1.0);

                float blushMask = tex2D(_BlushTex, i.uv).r * _BlushColor.a;
                float4 finalColor = lerp(baseColor, baseColor * _BlushColor, blushMask * _BlushColor.a);
                return finalColor;
                // return maskCol;
            }
            ENDCG
        }
    }
}