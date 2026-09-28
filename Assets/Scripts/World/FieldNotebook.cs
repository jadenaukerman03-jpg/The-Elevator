using UnityEngine;
namespace TheElevator
{
    public sealed class FieldNotebook : MonoBehaviour
    {
        DescentGame game; TextMesh page; int index; Vector3 home;Quaternion homeRotation; bool reading; int openedFrame;
        readonly string[] pages={
            "FIELD GUIDE / 01\n\nMORROW SYSTEMS\n\nFind the bright access card\non the reception counter.\nUse it at the secured room.\nBring the contract asset\nfully inside the lift.",
            "FIELD GUIDE / 02\n\nWASD: move   SHIFT: run\nCTRL / C: crouch\nE or click: interact\nE: put down a held item\nHold Q: charge a throw\nRelease Q: throw\nL: flashlight",
            "FIELD GUIDE / 03\n\nLook directly at a number\non the right-hand panel.\nGreen means available.\nAmber means this floor.\nGrey means locked.\nComplete this contract to\nunlock the next floor.\nFloor 0 is reserved for the hub.",
            "FIELD GUIDE / 04\n\nEmployees notice theft.\nAn occupied computer draws\nextra attention. Keep quiet.\n\nF connects a held battery\ninside the lift.\n\nClick the page edges to turn.\nE closes this notebook."};
        public void Build(DescentGame owner,Workshop w){game=owner;w.Shape("Bound notebook cover",transform,Vector3.zero,new Vector3(.48f,.035f,.62f),Workshop.Ink);w.Shape("Paper pages",transform,new Vector3(0,.024f,0),new Vector3(.45f,.022f,.59f),Workshop.Cream);page=w.Label(pages[0],transform,new Vector3(0,.037f,0),.015f,Workshop.Ink);page.transform.localRotation=Quaternion.Euler(90,0,0);FitPage();BoxCollider c=gameObject.AddComponent<BoxCollider>();c.size=new Vector3(.5f,.10f,.64f);}
        void FitPage(){Bounds b=page.GetComponent<Renderer>().localBounds;page.transform.localScale=Vector3.one*Mathf.Min(.41f/Mathf.Max(.01f,b.size.x),.55f/Mathf.Max(.01f,b.size.y));}
        public void Open(){if(reading||game.Player.Held)return;home=transform.position;homeRotation=transform.rotation;reading=true;openedFrame=Time.frameCount;game.Player.ReadingNotebook=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void Close(){reading=false;transform.SetPositionAndRotation(home,homeRotation);game.Player.ReadingNotebook=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        void LateUpdate(){if(!reading)return;Transform view=game.Player.View.transform;transform.position=view.TransformPoint(new Vector3(0,-.05f,.65f));transform.rotation=view.rotation*Quaternion.Euler(-90,0,0);if(Time.frameCount>openedFrame&&(Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Escape)))Close();}
        void OnGUI(){if(!reading)return;if(GUI.Button(new Rect(Screen.width*.5f-190,Screen.height-90,140,42),"Previous page")){index=(index+pages.Length-1)%pages.Length;page.text=pages[index];FitPage();}if(GUI.Button(new Rect(Screen.width*.5f+50,Screen.height-90,140,42),"Next page")){index=(index+1)%pages.Length;page.text=pages[index];FitPage();}}
    }
}


