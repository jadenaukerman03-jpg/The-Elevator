Shader "Elevator/OfficeGlass"
{
    Properties { _Color("Glass tint",Color)=(.55,.72,.7,.3) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            fixed4 _Color;
            v2f vert(app v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.normal=UnityObjectToWorldNormal(v.normal); return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float f=pow(1-abs(dot(normalize(_WorldSpaceCameraPos-i.world),normalize(i.normal))),3);
                float band=step(.8,frac(i.world.y*1.3));
                return fixed4(_Color.rgb+f*.25,_Color.a+f*.25+band*.28);
            }
            ENDCG
        }
    }
}
