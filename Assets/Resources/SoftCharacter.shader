Shader "Elevator/SoftCharacter" {
 // Flat-color character paint: wrapped two-tone light, tinted shadow side, faint rim and a small soft sheen.
 Properties {
  _Color("Color",Color)=(1,1,1,1)
  _ShadeColor("Shade tint",Color)=(.64,.60,.80,1)
  _Rim("Rim light",Range(0,1))=.16
  _Gloss("Soft sheen",Range(0,1))=.10
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Soft fullforwardshadows
  #pragma target 3.0
  fixed4 _Color,_ShadeColor;
  half _Rim,_Gloss;
  struct Input { float3 viewDir; };
  half4 LightingSoft(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten)
  {
   half wrap=dot(s.Normal,lightDir)*.5+.5;
   half band=smoothstep(.42,.62,wrap)*atten;
   #ifdef UNITY_PASS_FORWARDADD
   half3 tone=band;
   #else
   half3 tone=lerp(_ShadeColor.rgb,1,band);
   #endif
   half sheen=smoothstep(.93,.98,dot(s.Normal,normalize(lightDir+viewDir)))*_Gloss*atten;
   half4 c;c.rgb=(s.Albedo*tone+sheen)*_LightColor0.rgb;c.a=s.Alpha;return c;
  }
  void surf(Input IN,inout SurfaceOutput o)
  {
   o.Albedo=_Color.rgb;
   half fresnel=1-saturate(dot(normalize(IN.viewDir),o.Normal));
   o.Emission=_Color.rgb*pow(fresnel,3)*_Rim;
   o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
