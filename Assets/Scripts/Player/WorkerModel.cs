using UnityEngine;

namespace TheElevator
{
    public sealed class WorkerModel : MonoBehaviour
    {
        public static readonly Color[] SuitColors = {
            Workshop.Yellow, Workshop.Mint, new Color(0.73f, 0.53f, 0.91f), Workshop.Red
        };
        public static readonly Color Skin = BeanRig.SkinColors[6];
        static readonly Color HardHat = new Color(0.97f, 0.95f, 0.90f);
        BeanRig rig;
        int suit;
        TheElevator.Office.BusinessRobot officeRig;

        // First-person hands copy whatever the third-person body is wearing.
        public Color HandColor { get { return officeRig ? officeRig.SkinColor : BeanRig.Glove; } }
        public Color SleeveColor { get { return officeRig ? officeRig.JacketColor : SuitColors[suit]; } }

        public void UseOfficeRig(TheElevator.Office.OfficeArt art)
        {
            if(officeRig)return;
            foreach(Transform child in transform)child.gameObject.SetActive(false);
            GameObject root=new GameObject("Corporate disguise");root.transform.SetParent(transform,false);
            officeRig=root.AddComponent<TheElevator.Office.BusinessRobot>();officeRig.Build(art,4,false,Skin);
        }

        public void Build(Workshop w, int colorIndex)
        {
            suit = colorIndex;
            GameObject root = new GameObject("Crew body");
            root.transform.SetParent(transform, false);
            rig = root.AddComponent<BeanRig>();
            rig.Build(new BeanLook { Outfit = BeanOutfit.Crew, Skin = Skin, Primary = SuitColors[colorIndex], Accent = HardHat, Eyes = 0, Mouth = 0 }, 4);
        }

        public void SetColor(int index) { suit = index; rig.SetPrimary(SuitColors[index]); }

        public void Animate(float speed, bool carrying, float dt)
        {
            if(officeRig){officeRig.Speed=speed;officeRig.Animate(dt);return;}
            rig.Animate(dt, new BeanPose { Speed = speed, Carrying = carrying, ElbowBend = -12 });
        }
    }
}
