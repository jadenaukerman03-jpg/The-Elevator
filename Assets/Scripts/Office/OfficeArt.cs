using System.Collections.Generic;
using UnityEngine;

namespace TheElevator.Office
{
    public sealed class OfficeArt
    {
        public readonly Workshop W;
        public Material Plaster, Carpet, Tile, Wood, Metal, Dark, Plastic, Paper, Upholstery, Screen, WarmLight, CoolLight, Glass, Brass, Red;
        public Material[] DepartmentAccents;
        public int Pieces;
        static Mesh rounded;
        public OfficeArt(Workshop workshop)
        {
            W=workshop;
            Plaster=Mat("Mineral wall finish",new Color(.67f,.69f,.63f),0,.18f,.14f);
            Carpet=Mat("Woven graphite carpet",new Color(.16f,.23f,.25f),0,.03f,.34f,1);
            Tile=Mat("Reception terrazzo",new Color(.55f,.58f,.52f),.08f,.62f,.34f,3);
            Wood=Mat("Walnut veneer",new Color(.32f,.16f,.085f),0,.38f,.2f,2);
            Metal=Mat("Brushed aluminum",new Color(.47f,.52f,.51f),.72f,.48f,.1f);
            Dark=Mat("Powdercoat graphite",new Color(.035f,.055f,.058f),.25f,.36f,.08f);
            Plastic=Mat("Warm polymer",new Color(.75f,.72f,.59f),0,.4f,.08f);
            Paper=Mat("Paper",new Color(.84f,.84f,.74f),0,.08f,.12f);
            Upholstery=Mat("Sage wool",new Color(.23f,.34f,.29f),0,.08f,.3f,1);
            Brass=Mat("Anodized champagne",new Color(.64f,.43f,.19f),.65f,.52f,.08f);
            Red=Mat("Corporate vermilion",new Color(.66f,.15f,.075f),.1f,.3f,.1f);
            Screen=Mat("Phosphor display",new Color(.045f,.13f,.13f),.15f,.65f,.02f); Screen.shader=Shader.Find("Elevator/OfficeDisplay");Screen.color=new Color(.025f,.06f,.09f);Screen.SetColor("_Emission",new Color(.02f,.055f,.085f));
            WarmLight=Mat("Warm opal diffuser",new Color(.95f,.85f,.61f),0,.4f,0); WarmLight.SetColor("_Emission",new Color(1,.72f,.37f)*1.1f);
            CoolLight=Mat("Neutral opal diffuser",new Color(.7f,.88f,.85f),0,.4f,0); CoolLight.SetColor("_Emission",new Color(.5f,.78f,.75f)*1.1f);
            Glass=W.Own(new Material(Shader.Find("Elevator/OfficeGlass"))); Glass.name="Etched privacy glass";
            Color[] colors={new Color(.55f,.36f,.16f),new Color(.39f,.22f,.20f),new Color(.18f,.38f,.40f),new Color(.38f,.36f,.22f),new Color(.35f,.27f,.43f),new Color(.43f,.36f,.26f)};
            DepartmentAccents=new Material[colors.Length];for(int i=0;i<colors.Length;i++)DepartmentAccents[i]=Mat("Department trim "+i,colors[i],.25f,.38f,.08f);
            if(!rounded) rounded=RoundedBox();
        }
        public Material Mat(string name,Color color,float metal,float polish,float grain,int pattern=0)
        {
            Material m=W.Own(new Material(Shader.Find("Elevator/OfficeSurface"))); m.name=name; m.color=color;
            m.SetFloat("_Metallic",metal); m.SetFloat("_Smoothness",polish); m.SetFloat("_Grain",grain*.55f); m.SetFloat("_Scale",170); m.SetFloat("_Pattern",pattern); m.enableInstancing=true; return m;
        }
        public Transform Group(Transform parent,string name,Vector3 position,float yaw=0)
        { Transform t=W.Group(name,parent,position); t.localRotation=Quaternion.Euler(0,yaw,0); return t; }
        public GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid=false,bool bevel=true)
        {
            Pieces++;
            GameObject go;
            if(bevel)
            {
                go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localScale=size;
                go.AddComponent<MeshFilter>().sharedMesh=rounded; go.AddComponent<MeshRenderer>().sharedMaterial=material;
                if(solid) go.AddComponent<BoxCollider>();
            }
            else { go=W.Shape(name,parent,p,size,Color.white,PrimitiveType.Cube,solid); go.GetComponent<Renderer>().sharedMaterial=material; }
            return go;
        }
        public GameObject Round(Transform parent,string name,Vector3 p,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cylinder)
        { Pieces++; GameObject go=W.Shape(name,parent,p,size,Color.white,type,false); go.GetComponent<Renderer>().sharedMaterial=material; return go; }
        public void Label(Transform parent,string text,Vector3 p,float size=.035f,Color? color=null,float yaw=0)
        { W.Label(text,parent,p,size*.62f,color??new Color(.78f,.85f,.76f),yaw); }
        public static Mesh RoundedBox()
        {
            List<Vector3> verts=new List<Vector3>(); List<Vector3> norms=new List<Vector3>(); List<int> tris=new List<int>();
            Vector3[] normals={Vector3.forward,Vector3.back,Vector3.right,Vector3.left,Vector3.up,Vector3.down};
            foreach(Vector3 n in normals)
            {
                Vector3 u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n); Vector3 v=Vector3.Cross(n,u);
                int start=verts.Count; const int steps=4;
                for(int y=0;y<=steps;y++) for(int x=0;x<=steps;x++)
                {
                    float[] divisions={-.5f,-.42f,0,.42f,.5f}; Vector3 p=n*.5f+u*divisions[x]+v*divisions[y];
                    Vector3 inner=new Vector3(Mathf.Clamp(p.x,-.42f,.42f),Mathf.Clamp(p.y,-.42f,.42f),Mathf.Clamp(p.z,-.42f,.42f));
                    Vector3 normal=(p-inner).normalized; verts.Add(inner+normal*.08f); norms.Add(normal);
                }
                for(int y=0;y<steps;y++) for(int x=0;x<steps;x++) { int a=start+y*5+x; tris.AddRange(new[]{a,a+1,a+6,a,a+6,a+5}); }
            }
            Mesh mesh=new Mesh { name="Shared beveled solid" }; mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTriangles(tris,0); mesh.RecalculateBounds(); return mesh;
        }
    }
}


