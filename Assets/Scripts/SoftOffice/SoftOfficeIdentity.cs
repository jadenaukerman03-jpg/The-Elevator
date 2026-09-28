using UnityEngine;
namespace TheElevator.SoftOffice
{
 public static class SoftOfficeIdentity
 {
  public static void Build(Transform root,SoftArt a)
  {
   var mural=a.Group(root,"Department of Almost / wall identity",new Vector3(4.1f,3.28f,5.64f));
   a.Round(mural,"Inkberry signboard",Vector3.zero,new Vector3(3.15f,.94f,.13f),a.Plum,.23f);
   a.Print(mural,"DEPARTMENT\nOF ALMOST",new Vector3(.36f,0,-.075f),.047f,a.Cream);
   a.Ring(mural,"Company orbit mark",new Vector3(-1.06f,.02f,-.09f),.22f,.055f,a.Gold);
   a.Ball(mural,"Off-center orbit dot",new Vector3(-.90f,.16f,-.13f),new Vector3(.16f,.16f,.08f),a.Coral);
   var clock=a.Group(root,"Oversized overtime clock",new Vector3(-6.91f,2.15f,.10f));clock.localRotation=Quaternion.Euler(0,-90,0);
   a.Ball(clock,"Clock face",Vector3.zero,new Vector3(1.85f,1.85f,.18f),a.Cream);
   a.Ring(clock,"Clock rim",new Vector3(0,0,-.09f),.92f,.095f,a.Plum);
   for(int i=0;i<4;i++){float q=i*Mathf.PI*.5f;a.Round(clock,"Big hour mark",new Vector3(Mathf.Sin(q)*.7f,Mathf.Cos(q)*.7f,-.115f),new Vector3(.09f,.18f,.05f),a.Teal,.04f).transform.localRotation=Quaternion.Euler(0,0,-i*90);}
   a.Round(clock,"Minute hand",new Vector3(.19f,.18f,-.16f),new Vector3(.11f,.65f,.075f),a.Coral,.052f).transform.localRotation=Quaternion.Euler(0,0,-43);
   a.Round(clock,"Hour hand",new Vector3(-.17f,.015f,-.18f),new Vector3(.41f,.12f,.075f),a.Plum,.055f);
   a.Ball(clock,"Clock boss",new Vector3(0,0,-.23f),new Vector3(.22f,.22f,.11f),a.Gold);
   // Broad floor graphics form a route, not a screen of labels.
   for(int i=0;i<5;i++){float q=i*.5f;var arc=a.Round(root,"Curved wayfinding dash",new Vector3(1.05f+Mathf.Sin(q)*.5f,.075f,-2.2f+i*1.12f),new Vector3(.45f,.018f,.20f),i==4?a.Gold:a.Cream,.09f);arc.transform.localRotation=Quaternion.Euler(0,Mathf.Cos(q)*-20,0);}
   a.Round(root,"Counter inset stripe",new Vector3(-3,.52f,-1.225f),new Vector3(2.5f,.18f,.08f),a.Plum,.08f);
   a.Ring(root,"Counter seal",new Vector3(-3.9f,.64f,-1.285f),.17f,.035f,a.Cream);
   a.Round(root,"Asymmetric cabinet panel",new Vector3(4.8f,.67f,3.247f),new Vector3(1.5f,.25f,.09f),a.Plum,.095f);
   a.Round(root,"Vending top cap",new Vector3(4.65f,2.43f,3.8f),new Vector3(1.55f,.20f,.95f),a.Gold,.20f);
   // A chunky services loop gives the plain left wall an authored silhouette.
   Vector3[] path={new Vector3(-6.85f,.45f,3.7f),new Vector3(-6.85f,3.25f,3.7f),new Vector3(-4.9f,3.25f,5.57f),new Vector3(-4.9f,1.0f,5.57f)};
   for(int i=1;i<path.Length;i++){Vector3 d=path[i]-path[i-1];var pipe=a.Round(root,"Rounded service conduit",(path[i]+path[i-1])*.5f,new Vector3(.12f,d.magnitude,.12f),a.Gold,.059f);pipe.transform.rotation=Quaternion.FromToRotation(Vector3.up,d);a.Ball(root,"Conduit elbow",path[i],Vector3.one*.20f,a.Coral);}
  }
 }
}
