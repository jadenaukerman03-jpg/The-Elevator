using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheElevator.Office;
namespace TheElevator.Editor
{
    // Renders the actual game models: the four crew teammates and every office job look.
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
                for(int i=0;i<4;i++)
                {
                    var slot=new GameObject("Crew slot "+i).transform;slot.SetParent(root.transform);Place(slot,i,4);
                    var crew=new GameObject("Crew "+i).AddComponent<WorkerModel>();crew.transform.SetParent(slot,false);
                    crew.Build(workshop,i);crew.SetView(false,false);crew.Animate(0,false,1);
                }
                Capture(root,"TestResults/Character/crew.png");
                foreach(Transform child in root.transform)if(child.GetComponentInChildren<WorkerModel>())child.gameObject.SetActive(false);
                BeanOutfit[] jobs={BeanOutfit.Office,BeanOutfit.Reception,BeanOutfit.Technician,BeanOutfit.Clerk,BeanOutfit.Security,BeanOutfit.Office};
                var art=new OfficeArt(workshop);
                for(int i=0;i<jobs.Length;i++)
                {
                    var staff=new GameObject(jobs[i]+" "+i).AddComponent<BusinessRobot>();staff.transform.SetParent(root.transform);Place(staff.transform,i,jobs.Length);
                    staff.Build(art,i*3+1,i==jobs.Length-1,jobs[i]);staff.Activity=OfficeTask.Reading;staff.Animate(1);
                }
                Capture(root,"TestResults/Character/staff.png");
                Debug.Log("CHARACTER PORTRAIT PASS: 4 crew looks and "+jobs.Length+" office staff rendered.");
            }
            finally{Object.DestroyImmediate(root);workshop.Dispose();}
        }
        static void Place(Transform t,int index,int count){t.position=new Vector3((index-(count-1)*.5f)*1.05f,0,0);t.rotation=Quaternion.Euler(0,180+(index-(count-1)*.5f)*7,0);}
        static void Capture(GameObject root,string path)
        {
            var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.position=new Vector3(0,1.35f,-5.2f);camera.transform.LookAt(new Vector3(0,.95f,0));camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.31f,.35f);
            var rt=RenderTexture.GetTemporary(1600,900,24);var old=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(camera.gameObject);}
        }
    }
}
