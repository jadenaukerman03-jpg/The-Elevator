using UnityEngine;
namespace TheElevator.Office
{
    public sealed class OfficeCoffeeStation : MonoBehaviour
    {
        public OfficeTaskPoint Station;
        public Transform Door;
        public Vector3 CupStorage { get { return transform.TransformPoint(new Vector3(-.31f,1.86f,1.04f)); } }
        public Vector3 PourPoint { get { return transform.TransformPoint(new Vector3(0,1.015f,.44f)); } }
        public void Build(OfficeArt art,OfficeTaskPoint point)
        {
            Station=point;point.Coffee=this;
            Transform cabinet=art.Group(transform,"Working mug cupboard",new Vector3(-.31f,1.99f,1.06f));
            art.Box(cabinet,"Cupboard backing",new Vector3(0,0,.12f),new Vector3(.328f,.56f,.025f),art.Plastic);
            for(int side=-1;side<=1;side+=2)art.Box(cabinet,"Cupboard side",new Vector3(side*.16f,0,0),new Vector3(.022f,.56f,.26f),art.Plastic);
            art.Box(cabinet,"Mug shelf",new Vector3(0,-.24f,0),new Vector3(.30f,.025f,.26f),art.Plastic);
            Door=art.Group(cabinet,"Cupboard hinge",new Vector3(-.16f,0,-.145f));
            art.Box(Door,"Cupboard door",new Vector3(.16f,0,0),new Vector3(.328f,.56f,.025f),art.Plastic);
            art.Box(Door,"Cupboard handle",new Vector3(.265f,-.14f,-.025f),new Vector3(.025f,.12f,.025f),art.Brass);
        }
        public void SetOpen(float amount){Door.localRotation=Quaternion.Euler(0,-105*Mathf.Clamp01(amount),0);}
    }
    public sealed class OfficeCoffeeCup : MonoBehaviour
    {
        public Transform Liquid;
        public float Fill { get; private set; }
        public void Build(OfficeArt art)
        {
            art.Round(transform,"Mug base",new Vector3(0,.006f,0),new Vector3(.115f,.006f,.115f),art.Plastic);
            for(int i=0;i<16;i++)
            {
                Transform wall=art.Group(transform,"Ceramic mug wall",Vector3.zero,i*22.5f);
                art.Box(wall,"Wall",new Vector3(0,.065f,.055f),new Vector3(.024f,.12f,.01f),art.Plastic);
            }
            art.Box(transform,"Cup handle",new Vector3(.077f,.067f,0),new Vector3(.05f,.075f,.02f),art.Plastic);
            Liquid=art.Round(transform,"Visible coffee surface",Vector3.up*.015f,new Vector3(.099f,.002f,.099f),art.Wood).transform;SetFill(0);
        }
        public void SetFill(float value){Fill=Mathf.Clamp01(value);Liquid.gameObject.SetActive(Fill>.005f);Liquid.localPosition=Vector3.up*Mathf.Lerp(.015f,.115f,Fill);}
    }
}

