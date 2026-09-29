Shader "Elevator/Foam" {
 // Extinguisher foam: soft, bright white that still reads as foam in dim rooms. Drawn instanced.
 Properties {
  _Color("Color",Color)=(1,1,1,1)
  _Glow("Self light",Range(0,1))=.5
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Foam fullforwardshadows
  #pragma multi_compile_instancing
  #pragma target 3.0
  fixed4 _Color;
  half _Glow;
  struct Input { float3 viewDir; };
  half4 LightingFoam(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten)
  {
   half wrap=dot(s.Normal,lightDir)*.5+.5;
   half4 c;c.rgb=s.Albedo*lerp(.7,1,wrap)*atten*_LightColor0.rgb;c.a=1;return c;
  }
  void surf(Input IN,inout SurfaceOutput o)
  {
   o.Albedo=_Color.rgb;
   o.Emission=_Color.rgb*_Glow;
   o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
