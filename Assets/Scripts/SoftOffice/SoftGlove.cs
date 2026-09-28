using UnityEngine;
namespace TheElevator.SoftOffice
{
 public sealed class SoftGlove : MonoBehaviour
 {
  readonly Transform[] joints=new Transform[4];readonly Transform[] tips=new Transform[4];Transform thumb;
  int side;
  public void Build(SoftArt a,int handedness)
  {
   side=handedness;
   a.Round(transform,"Rounded palm",Vector3.zero,new Vector3(.14f,.145f,.072f),a.Cream,.055f);
   a.Ball(transform,"Thumb cushion",new Vector3(-side*.048f,-.025f,.008f),new Vector3(.077f,.09f,.074f),a.Cream);
   float[] lengths={.105f,.119f,.108f,.083f};
   for(int i=0;i<4;i++)
   {
    float x=side*(-.050f+i*.034f);float length=lengths[i];
    joints[i]=a.Group(transform,"Finger "+i+" knuckle",new Vector3(x,.054f,0));
    a.Round(joints[i],"Plump proximal finger",new Vector3(0,length*.27f,0),new Vector3(.032f,length*.65f,.047f),a.Cream,.019f);
    tips[i]=a.Group(joints[i],"Finger curl",new Vector3(0,length*.52f,0));
    a.Round(tips[i],"Soft rounded fingertip",new Vector3(0,length*.22f,0),new Vector3(.029f,length*.57f,.042f),a.Cream,.019f);
   }
   thumb=a.Group(transform,"Opposed inward thumb",new Vector3(-side*.063f,-.026f,.008f));
   a.Round(thumb,"Thumb base",new Vector3(-side*.013f,.017f,0),new Vector3(.056f,.074f,.061f),a.Cream,.029f);
   a.Round(thumb,"Thumb tip",new Vector3(-side*.029f,.051f,.012f),new Vector3(.049f,.063f,.055f),a.Cream,.026f);
   a.Round(transform,"Cuff rim",new Vector3(0,-.100f,0),new Vector3(.145f,.054f,.11f),a.Coral,.026f);
   a.Round(transform,"Sleeve",new Vector3(0,-.255f,.01f),new Vector3(.13f,.27f,.12f),a.Teal,.059f);
   foreach(Renderer r in GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
  public void Pose(bool holding,float movement)
  {
   for(int i=0;i<4;i++){float flex=holding?47:11+i*3+Mathf.Sin(Time.time*1.7f+i*.4f)*2+movement*6;joints[i].localRotation=Quaternion.Euler(flex,0,side*(i-1.5f)*-3);tips[i].localRotation=Quaternion.Euler(holding?55:17+i*2,0,0);}
   thumb.localRotation=Quaternion.Euler(holding?20:8,holding?side*20:0,side*(holding?4:24));
  }
 }
}
