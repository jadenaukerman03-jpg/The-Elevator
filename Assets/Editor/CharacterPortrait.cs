using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheElevator.Office;
namespace TheElevator.Editor
{
    // Renders the actual game models; no alternate gameplay scene or controller.
    public static class CharacterPortrait
    {
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var workshop=new Workshop();var root=new GameObject("Character portrait");
            try
            {
                var player=new GameObject("Player body").AddComponent<WorkerModel>();player.transform.SetParent(root.transform);player.transform.position=new Vector3(-.65f,0,0);player.transform.rotation=Quaternion.Euler(0,-18,0);player.Build(workshop,0);player.SetView(false,false);player.transform.position=new Vector3(-.65f,0,0);
                var npc=new GameObject("Office employee").AddComponent<BusinessRobot>();npc.transform.SetParent(root.transform);npc.transform.position=new Vector3(.65f,0,0);npc.transform.rotation=Quaternion.Euler(0,-28,0);npc.Build(new OfficeArt(workshop),1,false);
                var light=new GameObject("Portrait light").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=.85f;light.transform.rotation=Quaternion.Euler(35,-145,0);
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.53f,.55f);RenderSettings.fog=false;
                Directory.CreateDirectory("TestResults/Character");
                var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.position=new Vector3(0,1.6f,4.5f);camera.transform.LookAt(new Vector3(0,1.31f,0));camera.orthographic=true;camera.orthographicSize=1.02f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.31f,.35f);
                var rt=RenderTexture.GetTemporary(1400,900,24);var old=RenderTexture.active;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes("TestResults/Character/spherical-heads.png",image.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
                Debug.Log("CHARACTER PORTRAIT PASS: original player and office employee rendered.");
            }
            finally{Object.DestroyImmediate(root);workshop.Dispose();}
        }
    }
}
