Shader "Unlit/Accumulator"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
            sampler2D _MainTexOld;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            int Frame;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 oldRender = tex2D(_MainTexOld, i.uv);
                float4 newRender = tex2D(_MainTex, i.uv);

                float weight = 1.0 / (Frame + 1);
                float4 accumulatedAverage = saturate(oldRender * (1-weight) + newRender * weight);

                return accumulatedAverage;
            }

            ENDCG
        }
    }
}
