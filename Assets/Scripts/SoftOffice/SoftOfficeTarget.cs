using UnityEngine;
namespace TheElevator.SoftOffice
{
 public sealed class SoftOfficeTarget : MonoBehaviour
 {
  public SoftArt Art {get;private set;}
  public SoftTargetPlayer Player {get;private set;}
  public void Build(bool playable)
  {
   if(Art!=null)return;Art=new SoftArt();var a=Art;
   RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.40f,.47f,.53f);RenderSettings.ambientEquatorColor=new Color(.32f,.36f,.33f);RenderSettings.ambientGroundColor=new Color(.20f,.24f,.26f);
   QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=35;QualitySettings.antiAliasing=4;
   var sun=a.Group(transform,"Broad warm skylight",Vector3.zero).gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.9f,.73f);sun.intensity=.72f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.35f;sun.transform.rotation=Quaternion.Euler(48,-28,0);
   a.Round(transform,"Office foundation",new Vector3(0,-.25f,0),new Vector3(15,.5f,13),a.Cream,.24f,true);
   a.Round(transform,"Inset soft floor",new Vector3(0,.01f,0),new Vector3(14.3f,.05f,12.3f),a.Floor,.8f);
   a.Round(transform,"Reception carpet",new Vector3(-3,.055f,.1f),new Vector3(5.6f,.07f,5.5f),a.Teal,.7f);
   a.Round(transform,"Back wall west",new Vector3(-4.3f,2,6),new Vector3(6.4f,4,.5f),a.Cream,.24f,true);
   a.Round(transform,"Back wall east",new Vector3(4.3f,2,6),new Vector3(6.4f,4,.5f),a.Cream,.24f,true);
   a.Round(transform,"Door header",new Vector3(0,3.65f,6),new Vector3(2.2f,.7f,.5f),a.Cream,.24f,true);
   a.Round(transform,"Left wall",new Vector3(-7.2f,2,0),new Vector3(.5f,4,12),a.Cream,.24f,true);
   for(int i=0;i<4;i++)a.Round(transform,"Broad acoustic cushion",new Vector3(-5.85f+i*1.3f,1.8f,5.65f),new Vector3(1.05f,2.5f,.18f),a.Teal,.085f);
   for(int side=-1;side<=1;side+=2)a.Round(transform,"Thick door jamb",new Vector3(side*1.12f,1.65f,5.68f),new Vector3(.35f,3.3f,.6f),a.Coral,.17f,true);
   a.Round(transform,"Rounded door lintel",new Vector3(0,3.15f,5.68f),new Vector3(2.5f,.4f,.6f),a.Coral,.19f);
   a.Round(transform,"Restricted door",new Vector3(0,1.5f,5.88f),new Vector3(1.85f,2.9f,.18f),a.Teal,.088f,true);
   a.Round(transform,"Door porthole",new Vector3(0,2,5.76f),new Vector3(.8f,.63f,.09f),a.Screen,.044f);
   a.Round(transform,"Padded door pull",new Vector3(.6f,1.23f,5.65f),new Vector3(.14f,.5f,.18f),a.Gold,.067f);
   a.Round(transform,"Clearance sign",new Vector3(0,3.18f,5.345f),new Vector3(1.55f,.27f,.06f),a.Cream,.029f);a.Print(transform,"CLEARANCE 02",new Vector3(0,3.18f,5.307f),.024f,a.Ink);
   a.Round(transform,"Scanner",new Vector3(1.52f,1.35f,5.63f),new Vector3(.44f,.68f,.3f),a.Ink,.14f);a.Round(transform,"Clearance lamp",new Vector3(1.52f,1.53f,5.465f),new Vector3(.25f,.16f,.07f),a.Gold,.034f);
   // A few substantial ceiling forms instead of a grid of tiny fluorescent panels.
   for(int side=-1;side<=1;side+=2){a.Round(transform,"Suspended pill light",new Vector3(side*3.7f,3.7f,1.7f),new Vector3(2.8f,.22f,.65f),a.Cream,.3f);for(int z=-1;z<=1;z+=2)a.Round(transform,"Lamp suspension",new Vector3(side*3.7f+z*.9f,3.95f,1.7f),new Vector3(.06f,.45f,.06f),a.Ink,.025f);}
   a.Round(transform,"Ceiling crossbeam",new Vector3(0,4.18f,1.7f),new Vector3(14.4f,.22f,.42f),a.Cream,.105f);
   a.Round(transform,"Open-side structural column",new Vector3(7.0f,2.0f,1.7f),new Vector3(.40f,4.15f,.4f),a.Teal,.18f,true);
   Desk(a);Chair(a);Plant(a,new Vector3(-5.95f,0,3.55f));Plant(a,new Vector3(6.05f,0,4.1f));Vending(a);Printer(a);
   var robot=a.Group(transform,"Associate / coral",new Vector3(-3.8f,0,1.5f));robot.gameObject.AddComponent<SoftRobot>().Build(a,1);
   var robot2=a.Group(transform,"Associate / lagoon",new Vector3(2.1f,0,2.3f));robot2.localRotation=Quaternion.Euler(0,-20,0);robot2.gameObject.AddComponent<SoftRobot>().Build(a,0);
   var cart=a.Group(transform,"Rounded rolling cargo cart",new Vector3(3.8f,0,-1.4f));a.Round(cart,"Cart tray",new Vector3(0,.42f,0),new Vector3(1.45f,.2f,1.1f),a.Teal,.09f,true);
   foreach(float x in new[]{-.53f,.53f})foreach(float z in new[]{-.35f,.35f})a.Ball(cart,"Oversized castor",new Vector3(x,.2f,z),new Vector3(.31f,.36f,.31f),a.Ink);
   var cargo=a.Round(transform,"Mandatory cargo sample",new Vector3(3.8f,.94f,-1.4f),new Vector3(1.05f,.82f,.8f),a.Coral,.19f,true);a.Round(cargo.transform,"Big latch",new Vector3(0,.03f,-.43f),new Vector3(.3f,.25f,.1f),a.Gold,.049f);cargo.AddComponent<SoftPickup>().Setup("Prototype printer cartridge",2.8f);
   var guide=a.Round(transform,"Physical testing card",new Vector3(0,1,-4.1f),new Vector3(1.65f,1.0f,.1f),a.Cream,.049f);a.Round(transform,"Guide stand",new Vector3(0,.48f,-4.03f),new Vector3(.12f,.95f,.16f),a.Teal,.059f,true);a.Round(transform,"Guide foot",new Vector3(0,.07f,-4.03f),new Vector3(.7f,.14f,.5f),a.Teal,.065f);
   a.Print(guide.transform,"SOFT OFFICE\n\nWASD MOVE / SHIFT RUN\nCLICK OR E: PICK UP / DROP\nHOLD Q: TOSS / ESC: RELEASE MOUSE",new Vector3(0,0,-.056f),.019f,a.Ink);
   SoftOfficeIdentity.Build(transform,a);
   if(playable){var player=a.Group(transform,"Soft target player",new Vector3(0,.12f,-5.7f));Player=player.gameObject.AddComponent<SoftTargetPlayer>();Player.Build(a);}
  }
  void Desk(SoftArt a)
  {
   var t=a.Group(transform,"Curved reception desk",new Vector3(-3,0,-.7f));
   a.Round(t,"Curved plinth",new Vector3(0,.51f,.1f),new Vector3(3,.95f,1.0f),a.Coral,.44f,true);
   a.Round(t,"Pill counter",new Vector3(0,1.06f,0),new Vector3(3.7f,.22f,1.6f),a.Cream,.65f,true);
   var monitor=a.Group(t,"Chunky computer",new Vector3(-.45f,1.18f,.12f));a.Round(monitor,"Monitor foot",new Vector3(0,.04f,.05f),new Vector3(.55f,.08f,.37f),a.Teal,.039f);a.Round(monitor,"Monitor stem",new Vector3(0,.2f,.10f),new Vector3(.16f,.36f,.17f),a.Teal,.078f);
   a.Round(monitor,"Monitor shell",new Vector3(0,.56f,0),new Vector3(1.05f,.73f,.25f),a.Teal,.12f);a.Round(monitor,"Simple display",new Vector3(0,.56f,-.132f),new Vector3(.86f,.53f,.035f),a.Screen,.12f);
   for(int i=0;i<3;i++)a.Round(monitor,"Screen graphic",new Vector3(-.2f+i*.2f,.51f,-.158f),new Vector3(.10f,.12f+i*.075f,.02f),i==2?a.Coral:a.Gold,.009f);
   a.Round(t,"Wide keyboard",new Vector3(-.45f,1.2f,-.48f),new Vector3(.95f,.09f,.29f),a.Teal,.044f);
   for(int i=0;i<7;i++)a.Round(t,"Oversized key",new Vector3(-.81f+i*.12f,1.26f,-.48f),new Vector3(.092f,.06f,.18f),a.Cream,.029f);
   var card=a.Round(transform,"Oversized clearance card",new Vector3(-1.72f,1.22f,-1.0f),new Vector3(.4f,.08f,.55f),a.Gold,.039f,true);a.Ball(card.transform,"Credential stamp",new Vector3(0,.049f,-.11f),new Vector3(.16f,.014f,.16f),a.Ink);card.AddComponent<SoftPickup>().Setup("Clearance card / 02",.3f);
   var folder=a.Round(transform,"Oversized folder",new Vector3(-4.35f,1.22f,-.73f),new Vector3(.6f,.11f,.48f),a.Teal,.053f,true);a.Round(folder.transform,"Folder tab",new Vector3(-.15f,.015f,.235f),new Vector3(.22f,.08f,.14f),a.Teal,.035f);folder.AddComponent<SoftPickup>().Setup("Questionable paperwork",.6f);
  }
  void Chair(SoftArt a){var t=a.Group(transform,"Squishy office chair",new Vector3(-1.9f,0,1.25f));a.Round(t,"Seat cushion",new Vector3(0,.6f,0),new Vector3(.95f,.25f,.82f),a.Gold,.12f);a.Round(t,"Chair back cushion",new Vector3(0,1.05f,.35f),new Vector3(.9f,.91f,.27f),a.Gold,.13f);a.Round(t,"Chair pedestal",new Vector3(0,.3f,0),new Vector3(.20f,.6f,.2f),a.Ink,.09f);for(int i=0;i<4;i++){float angle=i*Mathf.PI/2;a.Ball(t,"Chair wheel",new Vector3(Mathf.Cos(angle)*.43f,.12f,Mathf.Sin(angle)*.43f),Vector3.one*.23f,a.Ink);a.Round(t,"Chair foot",new Vector3(Mathf.Cos(angle)*.2f,.16f,Mathf.Sin(angle)*.2f),new Vector3(.5f,.1f,.18f),a.Ink,.048f).transform.localRotation=Quaternion.Euler(0,-i*90,0);}}
  void Plant(SoftArt a,Vector3 p){var t=a.Group(transform,"Soft artificial plant",p);a.Round(t,"Heavy rounded pot",new Vector3(0,.33f,0),new Vector3(.85f,.66f,.85f),a.Coral,.3f);a.Round(t,"Thick plant stem",new Vector3(0,1.0f,0),new Vector3(.13f,1.4f,.13f),a.Teal,.06f);for(int i=0;i<7;i++){float q=i*2.4f;var leaf=a.Ball(t,"Broad rubber leaf",new Vector3(Mathf.Cos(q)*.29f,1.1f+i*.13f,Mathf.Sin(q)*.29f),new Vector3(.75f,.28f,.5f),i%2==0?a.Leaf:a.Teal);leaf.transform.localRotation=Quaternion.Euler(i*8,q*Mathf.Rad2Deg,25);}}
  void Vending(SoftArt a){var t=a.Group(transform,"Rounded vending machine",new Vector3(4.65f,0,3.8f));a.Round(t,"Vending body",new Vector3(0,1.22f,0),new Vector3(1.8f,2.44f,1.02f),a.Teal,.28f,true);a.Round(t,"Display inset",new Vector3(-.15f,1.36f,-.53f),new Vector3(1.15f,1.4f,.07f),a.Ink,.20f);for(int row=0;row<3;row++)for(int col=0;col<3;col++)a.Round(t,"Exaggerated product",new Vector3(-.5f+col*.35f,.89f+row*.42f,-.59f),new Vector3(.21f,.30f,.14f),col%2==0?a.Coral:a.Gold,.069f);a.Round(t,"Large selector",new Vector3(.63f,1.37f,-.6f),new Vector3(.20f,.45f,.16f),a.Gold,.079f);a.Round(t,"Collection mouth",new Vector3(0,.36f,-.55f),new Vector3(1,.24f,.15f),a.Ink,.074f);a.Round(t,"Brand pill",new Vector3(0,2.11f,-.54f),new Vector3(1.2f,.3f,.06f),a.Cream,.029f);a.Print(t,"NOURISH",new Vector3(0,2.11f,-.578f),.04f,a.Ink);}
  void Printer(SoftArt a){var t=a.Group(transform,"Printer station",new Vector3(-5.6f,0,1.0f));a.Round(t,"Printer cabinet",new Vector3(0,.5f,0),new Vector3(1.15f,1,.95f),a.Teal,.16f,true);var machine=a.Group(t,"Bumbling printer",new Vector3(0,1.04f,0));a.Round(machine,"Printer shell",new Vector3(0,.25f,0),new Vector3(1.28f,.5f,1.05f),a.Cream,.18f);a.Round(machine,"Printer lid",new Vector3(0,.52f,.08f),new Vector3(1.03f,.15f,.70f),a.Coral,.07f);var paper=a.Round(machine,"Large printed sheet",new Vector3(0,.48f,-.4f),new Vector3(.64f,.04f,.65f),a.Cream,.019f).transform;machine.gameObject.AddComponent<SoftMachine>().Paper=paper;}
  void Awake(){Build(true);}
  void OnDestroy(){if(Art!=null)Art.Dispose();}
 }
}
