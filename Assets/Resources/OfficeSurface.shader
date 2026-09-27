Shader "Elevator/OfficeSurface"
{
    Properties
    {
        _Color("Tint",Color)=(0.7,0.7,0.7,1)
        _Metallic("Metal",Range(0,1))=0
        _Smoothness("Polish",Range(0,1))=0.3
        _Grain("Grain strength",Range(0,1))=0.12
        _Scale("Surface scale",Float)=50
        _Pattern("0 plain 1 carpet 2 veneer 3 tile",Float)=0
        _Emission("Emission",Color)=(0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing
        fixed4 _Color,_Emission; half _Metallic,_Smoothness,_Grain,_Scale,_Pattern;
        struct Input { float3 worldPos; float3 worldNormal; };
        float noise(float3 p) { return frac(sin(dot(floor(p),float3(12.9898,78.233,37.719)))*43758.5453); }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 p=IN.worldPos; float n=noise(p*_Scale); float variation=1+(n-.5)*_Grain;
            if(_Pattern>0.5 && _Pattern<1.5)
            {
                float2 weave=abs(frac(p.xz*90)-.5);
                variation*=.88+.12*step(.18,min(weave.x,weave.y));
                float2 seam=abs(frac(p.xz*1.0)-.5); variation*=1-.15*step(.492,max(seam.x,seam.y));
            }
            if(_Pattern>1.5 && _Pattern<2.5) variation*=.87+.13*sin(p.x*44+sin(p.y*2+p.z*1.1)*3+n*.6);
            if(_Pattern>2.5) { float2 seam=abs(frac(p.xz*.8)-.5); variation*=1-.18*step(.486,max(seam.x,seam.y)); }
            o.Albedo=_Color.rgb*variation; o.Metallic=_Metallic; o.Smoothness=_Smoothness*(.92+n*.08);
            o.Occlusion=1; o.Emission=_Emission.rgb; o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
