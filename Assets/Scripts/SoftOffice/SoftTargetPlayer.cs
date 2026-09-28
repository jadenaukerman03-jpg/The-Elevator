using UnityEngine;
namespace TheElevator.SoftOffice
{
 public sealed class SoftTargetPlayer : MonoBehaviour
 {
  public Camera View {get;private set;} public SoftPickup Held {get;private set;}
  public Transform LeftHand,RightHand;
  public SoftRobot Body {get;private set;}
  CharacterController motor;SoftPickup target;float yaw,pitch=8,vertical,charge;Vector3 velocity;bool locked=true;
  public void Build(SoftArt a)
  {
   gameObject.layer=2;motor=gameObject.AddComponent<CharacterController>();motor.height=1.65f;motor.center=Vector3.up*.825f;motor.radius=.32f;
   View=a.Group(transform,"Relaxed first person view",new Vector3(0,1.5f,0)).gameObject.AddComponent<Camera>();View.tag="MainCamera";View.fieldOfView=74;View.nearClipPlane=.06f;View.farClipPlane=60;View.backgroundColor=new Color(.70f,.81f,.83f);View.clearFlags=CameraClearFlags.SolidColor;View.gameObject.AddComponent<AudioListener>();
   var avatar=a.Group(transform,"Complete player body",new Vector3(0,0,-.28f));avatar.localScale=new Vector3(.9f,1,.8f);avatar.localRotation=Quaternion.Euler(0,180,0);Body=avatar.gameObject.AddComponent<SoftRobot>();Body.Build(a,0);Body.ConfigureAsPlayer();
   LeftHand=Hand(a,-1);RightHand=Hand(a,1);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  }
  Transform Hand(SoftArt a,int side){var t=a.Group(View.transform,side<0?"Left bubbly glove":"Right bubbly glove",Vector3.zero);t.gameObject.AddComponent<SoftGlove>().Build(a,side);return t;}
  void Update()
  {
   if(Input.GetKeyDown(KeyCode.Escape)){locked=!locked;Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;}
   if(!locked)return;
   yaw+=Input.GetAxisRaw("Mouse X")*1.8f;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.8f,-70,75);
   transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
   Vector3 input=Vector3.ClampMagnitude(new Vector3((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),0,(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0)),1);bool sprint=Input.GetKey(KeyCode.LeftShift);Vector3 wish=transform.TransformDirection(input)*(sprint?5.7f:3.5f);velocity=Vector3.MoveTowards(velocity,wish,Time.deltaTime*(input.sqrMagnitude>.1f?13:18));
   if(motor.isGrounded)vertical=-2;vertical-=15*Time.deltaTime;motor.Move((velocity+Vector3.up*vertical)*Time.deltaTime);
   View.fieldOfView=Mathf.Lerp(View.fieldOfView,sprint&&velocity.magnitude>1?78:74,Time.deltaTime*4);
   target=null;if(Physics.Raycast(View.transform.position,View.transform.forward,out RaycastHit hit,2.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))target=hit.collider.GetComponentInParent<SoftPickup>();
   if(Input.GetKeyDown(KeyCode.E)||Input.GetMouseButtonDown(0)){if(Held)Release(0);else if(target)Grab(target);}
   if(Held&&Input.GetKey(KeyCode.Q))charge=Mathf.Clamp01(charge+Time.deltaTime/1.2f);if(Held&&Input.GetKeyUp(KeyCode.Q))Release(charge);
   if(transform.position.y < -3){motor.enabled=false;transform.position=new Vector3(0,.15f,-5.7f);motor.enabled=true;}
  }
  public void Grab(SoftPickup item){if(!item||Held)return;Held=item;charge=0;foreach(Collider c in item.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(motor,c,true);}
  public void Release(float force){if(!Held)return;var item=Held;Held=null;foreach(Collider c in item.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(motor,c,false);item.Body.linearVelocity=Vector3.ClampMagnitude(item.Body.linearVelocity,2)+View.transform.forward*Mathf.Clamp01(force)*7;item.Body.angularVelocity=View.transform.right*force*2;charge=0;}
  void FixedUpdate(){if(!Held)return;Rigidbody b=Held.Body;Vector3 goal=View.transform.TransformPoint(new Vector3(0,-.24f,1.05f));Vector3 acceleration=(goal-b.worldCenterOfMass)*55-b.linearVelocity*11;b.AddForce(Vector3.ClampMagnitude(acceleration,45),ForceMode.Acceleration);b.angularVelocity*=.9f;}
  void LateUpdate(){if(!View)return;PoseHand(LeftHand,-1);PoseHand(RightHand,1);if(Body)Body.PlayerPose(velocity.magnitude);LeftHand.GetComponent<SoftGlove>().Pose(Held,velocity.magnitude/6);RightHand.GetComponent<SoftGlove>().Pose(Held,velocity.magnitude/6);}
  void PoseHand(Transform hand,int side)
  {
   float moving=Mathf.Clamp01(velocity.magnitude/5),t=Time.time*(velocity.magnitude>4?9:6)+side*1.5f;
   Vector3 p=View.transform.TransformPoint(new Vector3(side*.27f,-.31f+Mathf.Sin(Time.time*1.6f)*.006f+Mathf.Sin(t)*moving*.026f,.56f+Mathf.Cos(t)*moving*.025f));
   Quaternion q=View.transform.rotation*Quaternion.Euler(35,-side*12,-side*12);
   if(Held){Bounds bounds=Held.GetComponent<Collider>().bounds;p=bounds.center+View.transform.right*side*(bounds.extents.x+.05f)-View.transform.up*.04f-View.transform.forward*.08f;q=View.transform.rotation*Quaternion.Euler(0,-side*70,0);}
   hand.SetPositionAndRotation(Vector3.Lerp(hand.position,p,1-Mathf.Exp(-Time.deltaTime*18)),Quaternion.Slerp(hand.rotation,q,1-Mathf.Exp(-Time.deltaTime*18)));
  }
  void OnGUI(){if(!View)return;float x=Screen.width*.5f,y=Screen.height*.5f;GUI.color=new Color(.14f,.24f,.3f);GUI.DrawTexture(new Rect(x-2,y-2,4,4),Texture2D.whiteTexture);if(target&&!Held){GUI.DrawTexture(new Rect(x-2,y,5,17),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x-2,y+9,17,12),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x-8,y+6,8,8),Texture2D.whiteTexture);}GUI.color=Color.white;GUIStyle label=new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleCenter};if(Held||target)GUI.Label(new Rect(x-220,y+44,440,28),Held?Held.Title:target.Title,label);if(charge>0){GUI.color=new Color(.95f,.65f,.21f);GUI.DrawTexture(new Rect(x-70,y+80,140*charge,6),Texture2D.whiteTexture);}GUI.color=Color.white;if(!locked)GUI.Label(new Rect(x-180,y+100,360,30),"Escape to resume the visual target",label);}
  void OnDestroy(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 }
}
