using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheElevator.Office;
namespace TheElevator.Editor
{
    // Renders the actual game models: avatar presets, office staff, and one sheet per wardrobe slot.
    public static class CharacterPortrait
    {
        [MenuItem("The Elevator/Render Character Cast")]
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var workshop=new Workshop();var root=new GameObject("Character portrait");
            try
            {
                var light=new GameObject("Portrait light").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=1.05f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(38,200,0);
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.47f,.49f,.56f);RenderSettings.fog=false;
                Directory.CreateDirectory("TestResults/Character");
                Lineup(root,workshop,AvatarWardrobe.Presets,"TestResults/Character/avatars.png",false);
                foreach(WardrobeSlot slot in Enum.GetValues(typeof(WardrobeSlot)))
                {
                    var looks=new AvatarLoadout[AvatarWardrobe.Count(slot)];
                    for(int i=0;i<looks.Length;i++){looks[i]=new AvatarLoadout();looks[i].Set(slot,i);}
                    bool face=slot==WardrobeSlot.Eyes||slot==WardrobeSlot.Mouth||slot==WardrobeSlot.Brows||slot==WardrobeSlot.Glasses||slot==WardrobeSlot.Head;
                    Lineup(root,workshop,looks,"TestResults/Character/options-"+slot.ToString().ToLower()+".png",face);
                }
                BeanOutfit[] jobs={BeanOutfit.Office,BeanOutfit.Reception,BeanOutfit.Technician,BeanOutfit.Clerk,BeanOutfit.Security,BeanOutfit.Office};
                var art=new OfficeArt(workshop);var holder=new GameObject("Staff").transform;holder.SetParent(root.transform);
                for(int i=0;i<jobs.Length;i++)
                {
                    var staff=new GameObject(jobs[i]+" "+i).AddComponent<BusinessRobot>();staff.transform.SetParent(holder);Place(staff.transform,i,jobs.Length,1.05f);
                    staff.Build(art,i*3+1,i==jobs.Length-1,jobs[i]);staff.Activity=OfficeTask.Reading;staff.Animate(1);
                }
                Capture(root,"TestResults/Character/staff.png",Distance(jobs.Length,1.05f),false);
                Debug.Log("CHARACTER PORTRAIT PASS: avatar presets, wardrobe sheets and "+jobs.Length+" office staff rendered.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);workshop.Dispose();}
        }
        static void Lineup(GameObject root,Workshop workshop,AvatarLoadout[] looks,string path,bool face)
        {
            var holder=new GameObject("Lineup").transform;holder.SetParent(root.transform);float spacing=face?.95f:1.05f;
            for(int i=0;i<looks.Length;i++)
            {
                var slot=new GameObject("Slot "+i).transform;slot.SetParent(holder);Place(slot,i,looks.Length,spacing);
                var avatar=new GameObject("Avatar "+i).AddComponent<WorkerModel>();avatar.transform.SetParent(slot,false);
                avatar.Apply(looks[i]);avatar.SetView(false,false);avatar.Animate(0,false,1);
            }
            Capture(root,path,face?looks.Length*spacing*.5f/.6f+.5f:Distance(looks.Length,spacing),face);
            UnityEngine.Object.DestroyImmediate(holder.gameObject);
        }
        static float Distance(int count,float spacing){return Mathf.Max(5.2f,count*spacing*.5f/.6f+1.6f);}
        static void Place(Transform t,int index,int count,float spacing){t.position=new Vector3((index-(count-1)*.5f)*spacing,0,0);t.rotation=Quaternion.Euler(0,180+(index-(count-1)*.5f)*5,0);}
        static void Capture(GameObject root,string path,float distance,bool face)
        {
            var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);
            Vector3 target=face?new Vector3(0,1.35f,0):new Vector3(0,.95f,0);
            camera.transform.position=target+new Vector3(0,face?.15f:.4f,-distance);camera.transform.LookAt(target);camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.31f,.35f);
            var rt=RenderTexture.GetTemporary(1600,900,24);var old=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
        }
    }
}
