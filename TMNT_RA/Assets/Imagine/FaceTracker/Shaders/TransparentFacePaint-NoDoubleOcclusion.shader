Shader "Imagine/FacePaint" {
    Properties {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
    }
     
    Category {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
     
     
        SubShader {
     
        ColorMask 0
        Cull Back Lighting Off ZWrite On Fog { Color (0,0,0,0) }
        Pass
            {
         
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
     
                #include "UnityCG.cginc"
     
             
                struct appdata_t {
                    float4 vertex : POSITION;
                };
     
                struct v2f {
                    float4 vertex : POSITION;
                };
             
     
                v2f vert (appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    return o;
                }
             
                fixed4 frag (v2f i) : COLOR
                {
                    return 1;
                }
                ENDCG
            }
     
            Pass
            {
                Blend SrcAlpha OneMinusSrcAlpha
                ColorMask RGB
                Cull Back Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
     
                #include "UnityCG.cginc"
     
                sampler2D _MainTex;
                fixed4 _TintColor;
             
                struct appdata_t {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };
     
                struct v2f {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };
             
                float4 _MainTex_ST;
     
                v2f vert (appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.color = v.color;
                    o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
                    return o;
                }
     
                float _Alpha;
             
                fixed4 frag (v2f i) : COLOR
                {      
                    float4 col = i.color * tex2D(_MainTex, i.texcoord);
                    col.a *= _Alpha;
                    return col;
                }
                ENDCG
            }
        }  
    }
    }