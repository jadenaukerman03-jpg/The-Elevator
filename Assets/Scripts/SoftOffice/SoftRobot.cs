using UnityEngine;
namespace TheElevator.SoftOffice
{
 public sealed class SoftRobot : MonoBehaviour
 {
  Transform body,head,left,right,legL,legR;Vector3 origin;float offset;
  public bool IsPlayer {get;private set;}
  public void Build(SoftArt a,int variant)
  {
   origin=transform.localPosition;offset=variant*2.1f;Material suit=variant%2==0?a.Teal:a.Coral;
   body=a.Group(transform,"Jacket and shoulders",new Vector3(0,.82f,0));
   a.Round(body,"Padded jacket",new Vector3(0,.05f,0),new Vector3(.76f,.79f,.48f),suit,.23f);
   a.Round(body,"Shirt bib",new Vector3(0,.19f,-.247f),new Vector3(.25f,.32f,.035f),a.Cream,.03f);
   a.Round(body,"Big soft tie",new Vector3(.025f,.13f,-.28f),new Vector3(.085f,.26f,.055f),a.Gold,.038f).transform.localRotation=Quaternion.Euler(0,0,-9);
   a.Round(body,"Clearance badge",new Vector3(-.22f,.1f,-.28f),new Vector3(.18f,.23f,.055f),a.Cream,.04f).transform.localRotation=Quaternion.Euler(0,0,8);
   a.Ball(body,"Badge dot",new Vector3(-.22f,.14f,-.315f),new Vector3(.08f,.08f,.012f),a.Coral);
   head=a.Group(body,"Floating collar and face",new Vector3(0,.69f,0));
   a.Ball(head,"Spherical porcelain head",Vector3.zero,Vector3.one*.68f,a.Cream);
   a.Round(head,"Rounded faceplate",new Vector3(0,-.015f,-.34f),new Vector3(.49f,.29f,.06f),a.Ink,.10f);
   a.Round(head,"Left optic",new Vector3(-.105f,.01f,-.375f),new Vector3(.073f,.11f,.03f),a.Gold,.033f);
   a.Round(head,"Right optic",new Vector3(.105f,.035f,-.375f),new Vector3(.073f,.11f,.03f),a.Gold,.033f);
   a.Round(head,"Service tab",new Vector3(.33f,.01f,0),new Vector3(.08f,.15f,.19f),suit,.04f);
   left=Limb(a,body,-1,suit);right=Limb(a,body,1,suit);
   legL=Leg(a,-1,suit);legR=Leg(a,1,suit);
  }
  Transform Limb(SoftArt a,Transform root,int side,Material suit){var t=a.Group(root,"Relaxed arm",new Vector3(side*.41f,.25f,0));a.Round(t,"Puffy sleeve",new Vector3(side*.04f,-.20f,0),new Vector3(.25f,.52f,.28f),suit,.12f);a.Round(t,"Rubber mitten",new Vector3(side*.05f,-.48f,-.035f),new Vector3(.24f,.25f,.25f),a.Cream,.10f);return t;}
  Transform Leg(SoftArt a,int side,Material suit){var t=a.Group(transform,"Short trouser leg",new Vector3(side*.22f,.51f,0));a.Round(t,"Trouser",new Vector3(0,-.14f,0),new Vector3(.29f,.45f,.31f),suit,.12f);a.Round(t,"Soft shoe",new Vector3(0,-.40f,-.10f),new Vector3(.35f,.21f,.51f),a.Ink,.1f);return t;}
  public void ConfigureAsPlayer()
  {
   IsPlayer=true;
   foreach(Transform part in new[]{head,left,right})foreach(Renderer r in part.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
  }
  public void PlayerPose(float speed)
  {
   float gait=Mathf.Clamp01(speed/4),t=Time.time*(speed>4?10:7);
   body.localPosition=new Vector3(0,.82f+Mathf.Sin(t*2)*.012f*gait,0);body.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t)*2*gait);
   legL.localRotation=Quaternion.Euler(Mathf.Sin(t)*22*gait,0,0);legR.localRotation=Quaternion.Euler(-Mathf.Sin(t)*22*gait,0,0);
   left.localRotation=Quaternion.Euler(-Mathf.Sin(t)*22*gait,0,8);right.localRotation=Quaternion.Euler(Mathf.Sin(t)*22*gait,0,-8);
   head.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time)*2,0);
  }
  void Update(){if(IsPlayer)return;float t=Time.time+offset;float walking=Mathf.SmoothStep(0,1,Mathf.Sin(t*.45f)*.5f+.5f);transform.localPosition=origin+new Vector3(Mathf.Sin(t*.45f)*.48f,0,Mathf.Sin(t*.9f)*.13f);body.localPosition=new Vector3(0,.82f+Mathf.Sin(t*5)*.025f*walking,0);body.localRotation=Quaternion.Euler(0,Mathf.Sin(t*.7f)*8,Mathf.Sin(t*2.5f)*3);head.localRotation=Quaternion.Euler(Mathf.Sin(t*1.2f)*4,Mathf.Sin(t*.65f)*15,-Mathf.Sin(t*2.5f)*2);left.localRotation=Quaternion.Euler(Mathf.Sin(t*5)*18*walking,0,9);right.localRotation=Quaternion.Euler(-Mathf.Sin(t*5)*18*walking,0,-9);legL.localRotation=Quaternion.Euler(-Mathf.Sin(t*5)*10*walking,0,0);legR.localRotation=Quaternion.Euler(Mathf.Sin(t*5)*10*walking,0,0);}
 }
 public sealed class SoftPickup : MonoBehaviour
 {
  public string Title;public Rigidbody Body;
  void OnCollisionEnter(Collision c){if(c.contactCount>0&&c.relativeVelocity.magnitude>2)Body.AddForce(c.GetContact(0).normal*Mathf.Min(1.2f,c.relativeVelocity.magnitude*.15f),ForceMode.VelocityChange);}
  public void Setup(string title,float mass){Title=title;Body=gameObject.AddComponent<Rigidbody>();Body.mass=mass;Body.linearDamping=.35f;Body.angularDamping=2;Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;}
 }
 public sealed class SoftMachine : MonoBehaviour
 {
  public Transform Paper;Vector3 origin;
  void Start(){origin=transform.localPosition;}
  void Update(){float pulse=Mathf.Max(0,Mathf.Sin(Time.time*1.2f));transform.localPosition=origin+Vector3.right*Mathf.Sin(Time.time*24)*pulse*.012f;if(Paper)Paper.localPosition=new Vector3(0,.48f,-.36f-pulse*.19f);}
 }
}
