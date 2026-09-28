using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.SoftOffice
{
 public sealed class SoftArt
 {
  public readonly List<Object> Owned=new List<Object>();
  readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
  public readonly Material Cream,Teal,Coral,Gold,Ink,Floor,Leaf,Screen;
  public SoftArt(){Cream=Mat("Porcelain",new Color(.94f,.88f,.73f));Teal=Mat("Lagoon",new Color(.16f,.53f,.53f));Coral=Mat("Persimmon",new Color(.9f,.37f,.27f));Gold=Mat("Clearance gold",new Color(.98f,.69f,.22f));Ink=Mat("Ink blue",new Color(.13f,.23f,.3f));Floor=Mat("Warm stone",new Color(.69f,.76f,.71f));Leaf=Mat("Rubber foliage",new Color(.31f,.61f,.42f));Screen=Mat("Quiet screen",new Color(.11f,.30f,.35f));}
  Material Mat(string name,Color color){var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.16f);Owned.Add(m);return m;}
  public Transform Group(Transform parent,string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
  public GameObject Round(Transform parent,string name,Vector3 p,Vector3 size,Material mat,float radius=.15f,bool solid=false)
  {
   var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;
   string key=size.ToString("F3")+radius.ToString("F3");if(!meshes.TryGetValue(key,out Mesh mesh)){mesh=Rounded(size,radius);meshes[key]=mesh;Owned.Add(mesh);}
   go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;if(solid)go.AddComponent<BoxCollider>().size=size;return go;
  }
  public GameObject Ball(Transform parent,string name,Vector3 p,Vector3 size,Material mat)
  {var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Collider>().enabled=false;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
  public TextMesh Print(Transform parent,string text,Vector3 p,float size,Material ink)
  {var t=Group(parent,"Printed / "+text,p);var tm=t.gameObject.AddComponent<TextMesh>();tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.text=text;tm.color=ink.color;var m=new Material(Shader.Find("Elevator/WorldLabel"));m.mainTexture=tm.font.material.mainTexture;Owned.Add(m);tm.GetComponent<Renderer>().sharedMaterial=m;return tm;}
  public static Mesh Rounded(Vector3 size,float radius)
  {
   Vector3 half=size*.5f;Vector3 bevel=new Vector3(Mathf.Min(radius,half.x*.98f),Mathf.Min(radius,half.y*.98f),Mathf.Min(radius,half.z*.98f));Vector3 core=half-bevel;
   var v=new List<Vector3>();var normals=new List<Vector3>();var tri=new List<int>();
   foreach(Vector3 normal in new[]{Vector3.forward,Vector3.back,Vector3.up,Vector3.down,Vector3.left,Vector3.right})
   {
    Vector3 u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,normal),w=Vector3.Cross(normal,u);
    float hu=Vector3.Dot(Abs(u),half),hv=Vector3.Dot(Abs(w),half),hn=Vector3.Dot(Abs(normal),half);int begin=v.Count;
    for(int y=0;y<10;y++)for(int x=0;x<10;x++)
    {Vector3 p=normal*hn+u*Axis(x,hu,Vector3.Dot(Abs(u),bevel))+w*Axis(y,hv,Vector3.Dot(Abs(w),bevel));Vector3 inner=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));Vector3 delta=p-inner;Vector3 n=new Vector3(delta.x/bevel.x,delta.y/bevel.y,delta.z/bevel.z).normalized;v.Add(inner+Vector3.Scale(n,bevel));normals.Add(new Vector3(n.x/bevel.x,n.y/bevel.y,n.z/bevel.z).normalized);}
    for(int y=0;y<9;y++)for(int x=0;x<9;x++){int a=begin+y*10+x;tri.AddRange(new[]{a,a+1,a+11,a,a+11,a+10});}
   }
   Mesh mesh=new Mesh{name="World-radius soft solid"};mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetTriangles(tri,0);mesh.RecalculateBounds();return mesh;
  }
  static Vector3 Abs(Vector3 v){return new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));}
  static float Axis(int i,float half,float r){return i<5?Mathf.Lerp(-half,-half+r,i/4f):Mathf.Lerp(half-r,half,(i-5)/4f);}
  public void Dispose(){foreach(Object o in Owned)if(o){if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}Owned.Clear();}
 }
}

