using UnityEngine;
using UnityEngine.Rendering;
using TheElevator.SoftOffice;
namespace TheElevator
{
    public sealed class WorkerModel : MonoBehaviour
    {
        public static readonly Color[] SuitColors={Workshop.Yellow,Workshop.Mint,new Color(.43f,.34f,.56f),Workshop.Red};
        SoftRobot rig;Workshop workshop;Renderer jacket;
        public void Build(Workshop w,int colorIndex)
        {
            workshop=w;var root=w.Soft.Group(transform,"Complete employee body",Vector3.zero);
            root.localRotation=Quaternion.Euler(0,180,0);rig=root.gameObject.AddComponent<SoftRobot>();rig.Build(w.Soft,0);rig.ConfigureAsPlayer();
            jacket=root.Find("Jacket and shoulders/Padded jacket").GetComponent<Renderer>();
        }
        public void UseOfficeRig(TheElevator.Office.OfficeArt art) { }
        public void SetColor(int index){if(jacket)jacket.sharedMaterial=workshop.Mat(SuitColors[index]);}
        public void SetView(bool firstPerson,bool crouched)
        {
            gameObject.SetActive(true);transform.localPosition=firstPerson?new Vector3(0,0,-.28f):Vector3.zero;
            transform.localScale=new Vector3(.9f,crouched?.59f:1,.8f);
            rig.transform.Find("Jacket and shoulders/Floating collar and face").localScale=new Vector3(1/.9f,1/transform.localScale.y,1/.8f);
            foreach(Renderer renderer in rig.GetComponentsInChildren<Renderer>())
            {
                bool hidden=renderer.transform.IsChildOf(rig.transform.Find("Jacket and shoulders/Floating collar and face"));
                for(Transform p=renderer.transform;p&&p!=rig.transform;p=p.parent)if(p.name=="Relaxed arm")hidden=true;
                renderer.shadowCastingMode=firstPerson&&hidden?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
            }
        }
        public void Animate(float speed,bool carrying,float dt){if(rig)rig.PlayerPose(speed);}
    }
}
