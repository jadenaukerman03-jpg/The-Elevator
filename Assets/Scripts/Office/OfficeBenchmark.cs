using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TheElevator.Generation;
namespace TheElevator.Office
{
    // Opt-in standalone diagnostic. Inert during ordinary play.
    [DefaultExecutionOrder(10000)]
    public sealed class OfficeBenchmark : MonoBehaviour
    {
        readonly MapSize[] sizes={MapSize.Small,MapSize.Standard,MapSize.Extreme};
        readonly List<float> frameTimes=new List<float>();
        int index,stage;
        double start,deadline;
        string output;
        RenderTexture target;
        Texture2D pixels;
        float lastRenderMilliseconds;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--office-benchmark")<0)return;
            new GameObject("Office standalone benchmark").AddComponent<OfficeBenchmark>();
        }
        void Awake()
        {
            Application.runInBackground=true;output=Path.Combine(Application.dataPath,"../OfficeBenchmark");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"performance.csv"),"profile,rooms,npcs,modeled_pieces,generation_ms,mean_render_readback_ms,p95_render_readback_ms,allocated_mib\n");
            File.WriteAllText(Path.Combine(output,"machine.txt"),SystemInfo.processorType+"\n"+SystemInfo.graphicsDeviceName+"\nUnity "+Application.unityVersion+" / development build / 1280x720 / High / forced Camera.Render + synchronous GPU readback, NOT displayed-window FPS\n");
            target=new RenderTexture(1280,720,24);target.Create();pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            deadline=Time.realtimeSinceStartupAsDouble+150;
        }
        void Update()
        {
            if(Time.realtimeSinceStartupAsDouble>deadline){Debug.LogError("Office benchmark timed out.");Application.Quit(1);return;}
            DescentGame game=FindFirstObjectByType<DescentGame>();if(!game)return;
            if(game.Paused)game.SetPaused(false);
            if(game.Phase==DescentGame.RunPhase.Lost){Debug.LogError(game.Outcome);Application.Quit(1);return;}
            if(!game.CurrentOffice||!game.CurrentMap.Ready||game.Phase==DescentGame.RunPhase.Generating)return;
            if(stage==0)
            {
                if(game.Phase==DescentGame.RunPhase.Briefing)game.Begin();
                game.Player.Teleport(game.CurrentMap.Center(game.CurrentMap.Manifest.Rooms[2])+new Vector3(0,.08f,-2.8f));
                start=Time.realtimeSinceStartupAsDouble;frameTimes.Clear();stage=1;
            }
            else if(stage==1)
            {
                double elapsed=Time.realtimeSinceStartupAsDouble-start;
                if(elapsed>2&&lastRenderMilliseconds>0)frameTimes.Add(lastRenderMilliseconds);
                if(elapsed<8)return;
                frameTimes.Sort();float sum=0;foreach(float frame in frameTimes)sum+=frame;
                OfficeFloor office=game.CurrentOffice;
                string line=string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F0},{5:F2},{6:F2},{7}\n",sizes[index],game.CurrentMap.Manifest.Rooms.Count,office.Employees.Count,office.Kit.A.Pieces,office.GenerationMilliseconds,sum/frameTimes.Count,frameTimes[Mathf.Min(frameTimes.Count-1,Mathf.FloorToInt(frameTimes.Count*.95f))],UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576);
                File.AppendAllText(Path.Combine(output,"performance.csv"),line);Debug.Log("OFFICE PLAYER PROFILE "+line);
                File.WriteAllBytes(Path.Combine(output,sizes[index]+"-player.png"),pixels.EncodeToPNG());stage=2;start=Time.realtimeSinceStartupAsDouble;
            }
            else if(stage==2&&Time.realtimeSinceStartupAsDouble-start>1)
            {
                if(++index>=sizes.Length){Debug.Log("OFFICE PLAYER BENCHMARK PASSED");Application.Quit(0);return;}
                game.Player.Teleport(new Vector3(0,.08f,-6.5f));game.RegenerateMap(104729,sizes[index],false);stage=0;
            }
        }
        void LateUpdate()
        {
            DescentGame game=FindFirstObjectByType<DescentGame>();if(stage!=1||!game||!game.Player||!target)return;
            Camera camera=game.Player.View;RenderTexture old=RenderTexture.active,oldTarget=camera.targetTexture;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();}
            finally{camera.targetTexture=oldTarget;RenderTexture.active=old;}
            lastRenderMilliseconds=(float)timer.Elapsed.TotalMilliseconds;
        }
        void OnDestroy(){if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);}
    }
}
