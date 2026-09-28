Shader "Elevator/FrostedDoor" {
 Properties { _Travel("Moving",Float)=0 }
 SubShader { Tags { "RenderType"="Opaque" } CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 float _Travel; struct Input { float3 worldPos; };
 void surf(Input i,inout SurfaceOutputStandard o){
 float band=pow(saturate(1-abs(frac(i.worldPos.y*.32-_Time.y*.8)-.5)*8),3)*_Travel;
 float grain=frac(sin(dot(floor(i.worldPos.xy*180),float2(12.98,78.23)))*43758.54);
 o.Albedo=float3(.23,.32,.33)+grain*.035;o.Metallic=.1;o.Smoothness=.32;
 o.Emission=float3(.7,.84,1)*band*.9;o.Alpha=1;
 } ENDCG } Fallback "Standard"
}
