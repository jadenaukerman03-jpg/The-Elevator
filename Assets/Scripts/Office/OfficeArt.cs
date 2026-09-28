using System.Collections.Generic;
using UnityEngine;

namespace TheElevator.Office
{
    public sealed class OfficeArt
    {
        public readonly Workshop W;
        public Material Plaster, Carpet, Tile, Wood, Metal, Dark, Plastic, Paper, Upholstery, Screen, WarmLight, CoolLight, Glass, Brass, Red;
        public Material[] DepartmentAccents;
        public Material Skin, HeadSkin, Lips, Eyes, Iris;
        public int Pieces;
        static Mesh rounded;
        public OfficeArt(Workshop workshop)
        {
            W=workshop;
            Skin=W.Soft.Cream;HeadSkin=Skin;Lips=W.Soft.Coral;Eyes=W.Soft.Cream;Iris=W.Soft.Teal;
            Plaster=W.Soft.Cream;Carpet=W.Soft.Floor;Tile=W.Soft.Floor;
            Wood=W.Soft.Coral;Metal=W.Soft.Teal;Dark=W.Soft.Ink;Plastic=W.Soft.Cream;
            Paper=W.Soft.Cream;Upholstery=W.Soft.Teal;Brass=W.Soft.Gold;Red=W.Soft.Coral;
            Screen=W.Soft.Screen;WarmLight=W.Soft.Cream;CoolLight=W.Soft.Cream;            Glass=W.Own(new Material(Shader.Find("Elevator/OfficeGlass"))); Glass.name="Etched privacy glass";
            DepartmentAccents=new[]{W.Soft.Gold,W.Soft.Coral,W.Soft.Teal,W.Soft.Leaf,W.Soft.Plum,W.Soft.Teal};
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
            if(bevel) return W.Soft.Round(parent,name,p,size,material,Mathf.Min(.18f,Mathf.Max(size.x,Mathf.Max(size.y,size.z))*.20f),solid);
            var go=W.Shape(name,parent,p,size,Color.white,PrimitiveType.Cube,solid);go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }        public GameObject Round(Transform parent,string name,Vector3 p,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cylinder)
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
