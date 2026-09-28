Shader "Elevator/ViewmodelSkin" {
 Properties { _Color("Skin",Color)=(.70,.51,.36,1) }
 SubShader { Tags { "RenderType"="Opaque" } Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f { float4 position:SV_POSITION; float3 normal:TEXCOORD0; };
 fixed4 _Color;
 v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);return o;}
 fixed4 frag(v2f i):SV_Target {float light=.52+.48*saturate(dot(normalize(i.normal),normalize(float3(-.3,.8,-.5))));return fixed4(_Color.rgb*light,1);}
 ENDCG
 } } }
