Shader "Custom/ChromaKeyPicker"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "white" {}
        _KeyColor ("Color To Remove", Color) = (0,1,0,1)
        _Threshold ("How Much to Remove", Range(0, 0.3)) = 0.1
        _Softness ("Edge Softness", Range(0, 0.2)) = 0.05
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _KeyColor;
            float _Threshold;
            float _Softness;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Get video pixel color
                fixed4 videoColor = tex2D(_MainTex, i.uv);
                
                // Calculate how different from chosen color
                float colorDifference = length(videoColor.rgb - _KeyColor.rgb);
                
                // Make it transparent if similar to chosen color
                float alpha = 1.0;
                if (colorDifference < _Threshold) {
                    alpha = 0.0;
                } else if (colorDifference < _Threshold + _Softness) {
                    alpha = (colorDifference - _Threshold) / _Softness;
                }
                
                return fixed4(videoColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
