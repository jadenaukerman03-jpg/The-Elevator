Shader "Elevator/OfficeDisplay"
{
    Properties { _Color("Display tint",Color)=(.025,.06,.09,1) _Emission("Backlight",Color)=(.02,.055,.085,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 position:SV_POSITION; };
            fixed4 _Color,_Emission;
            v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);return o;}
            fixed4 frag(v2f i):SV_Target{return fixed4(_Color.rgb+_Emission.rgb,1);}
            ENDCG
        }
    }
}
