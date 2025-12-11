Shader "Imagine/FaceDeformation"
{
    Properties
    {
        _DeformationMap ("Deformation Map (Auto)", 2D) = "black" {}
        _DeformMinVals ("Min Vector", Vector) = (-1,-1,-1,0)
        _DeformMaxVals ("Max Vector", Vector) = (1,1,1,0)

        _EditModeTex ("EditMode Texture", 2D) = "white" {}
        [MaterialToggle] _EditMode ("Visualize in Edit Mode", Float) = 1

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
            // uniform float4 _DeformationOffsets[468]; 
            

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

            sampler2D _DeformationMap;
            float4 _DeformationMap_ST;
            float3 _DeformMinVals;
            float3 _DeformMaxVals;
            
            sampler2D _EditModeTex;
            float4 _EditModeTex_ST;
            sampler2D _GrabTexture; // Texture that holds the grabbed screen content
            float _EditMode;

            v2f vert (appdata v)
            {
                v2f o;

                // float3 offset = //_DeformationOffsets[v.vertexId].xyz;
                int row = v.vertexId / 26;  // Number of rows = 18
                int col = v.vertexId % 26;  // Number of columns = 26
                float2 uv = float2(col / 26.0f, row / 18.0f);
                float3 deformRGB = tex2Dlod(_DeformationMap, float4(uv, 0, 0)).rgb;
                float3 offset = lerp(_DeformMinVals, _DeformMaxVals, deformRGB);


                o.vertex = UnityObjectToClipPos(v.vertex + float4(offset, 0));
                o.uv = TRANSFORM_TEX(v.uv, _EditModeTex);
                // o.grabUV = ComputeGrabScreenPos(o.vertex); // Compute UV for GrabPass texture
                o.grabUV = ComputeGrabScreenPos(UnityObjectToClipPos(v.vertex));
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture at the given UV coordinates
                fixed4 col = tex2D(_EditModeTex, i.uv);
                fixed4 grabCol = tex2D(_GrabTexture, i.grabUV.xy/i.grabUV.w);

                // Apply fog effect
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * _EditMode + grabCol * (1 - _EditMode);
            }
            ENDCG
        }
    }
}