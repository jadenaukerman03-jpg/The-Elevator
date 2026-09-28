using UnityEngine;
namespace TheElevator
{
    public sealed class ElevatorButton : MonoBehaviour
    {
        public DescentGame Game;
        public int Number;
        Renderer face;
        MaterialPropertyBlock tint;
        public bool Available { get { return Game && Game.CanSelectFloor(Number); } }
        public void Initialize(DescentGame game,int number){Game=game;Number=number;face=GetComponent<Renderer>();tint=new MaterialPropertyBlock();}
        void Update(){if(!face)return;tint.SetColor("_Color",Number==Game.FloorIndex+1?Workshop.Yellow:Number>0&&Number<=Game.HighestUnlocked?Workshop.Mint:new Color(.13f,.15f,.16f));face.SetPropertyBlock(tint);}
        public void Press(){if(Available)Game.SelectFloor(Number);}
    }
    public sealed class ElevatorCabin : MonoBehaviour
    {
        DescentGame game;
        Material glass;
        public void Build(DescentGame owner,Workshop w,Transform left,Transform right)
        {
            game=owner;
            Color metal=new Color(.43f,.46f,.47f);
            for(int side=-1;side<=1;side+=2)
            {
                for(int panel=0;panel<6;panel++)w.Shape("Brushed steel wall panel",transform,new Vector3(side*4.35f,1.8f,-9.3f+panel*1.18f),new Vector3(.035f,3.3f,1.14f),metal);
                w.Shape("Continuous handrail",transform,new Vector3(side*4.12f,1.05f,side>0?-7.55f:-6.6f),new Vector3(.065f,.07f,side>0?3.5f:5.4f),Workshop.Cream);
                for(int bracket=0;bracket<(side>0?3:4);bracket++)w.Shape("Handrail mounting bracket",transform,new Vector3(side*4.23f,1.02f,-9+bracket*1.6f),new Vector3(.23f,.06f,.06f),metal);
                w.Shape("Cabin skirting",transform,new Vector3(side*4.3f,.16f,-6.2f),new Vector3(.05f,.25f,7.2f),Workshop.Ink);
            }
            for(int i=0;i<7;i++)w.Shape("Rear wall panel",transform,new Vector3(-3.84f+i*1.28f,1.8f,-9.84f),new Vector3(1.23f,3.3f,.035f),metal);
            w.Shape("Rear handrail",transform,new Vector3(0,1.05f,-9.58f),new Vector3(8.1f,.07f,.07f),Workshop.Cream);
            w.Shape("Recessed ceiling diffuser",transform,new Vector3(0,3.54f,-6),new Vector3(5.5f,.07f,3.8f),Workshop.Cream);
            for(int i=0;i<3;i++)w.Shape("Door sill track",transform,new Vector3(0,.018f,-2.25f+i*.12f),new Vector3(5,.016f,.022f),metal);
            glass=w.Own(new Material(Shader.Find("Elevator/FrostedDoor")));
            foreach(Transform door in new[]{left,right})
            {
                door.GetComponent<Renderer>().sharedMaterial=glass;
                for(int side=-1;side<=1;side+=2)w.Shape("Door stainless stile",door,new Vector3(side*.485f,0,-.54f),new Vector3(.03f,1,.15f),metal,PrimitiveType.Cube,false);
                for(int side=-1;side<=1;side+=2)w.Shape("Door stainless rail",door,new Vector3(0,side*.48f,-.54f),new Vector3(1,.04f,.15f),metal,PrimitiveType.Cube,false);
            }
            Transform board=w.Group("Floor selection 0 through 50",transform,new Vector3(4.26f,0,-4.7f));board.localRotation=Quaternion.Euler(0,90,0);
            w.Shape("Number panel steel backplate",board,new Vector3(0,1.7f,0),new Vector3(1.42f,2.28f,.07f),Workshop.Ink);
            for(int n=0;n<=50;n++)
            {
                Vector3 p=new Vector3(-.54f+(n%5)*.27f,2.6f-(n/5)*.18f,-.065f);
                GameObject button=w.Shape("Floor button "+n,board,p,new Vector3(.20f,.125f,.06f),metal);
                button.AddComponent<ElevatorButton>().Initialize(game,n);
                w.Label(n.ToString(),board,p+Vector3.back*.035f,.014f,Workshop.Cream);
            }
            w.Label("FLOORS",board,new Vector3(0,2.76f,-.041f),.035f,Workshop.Cream);
            w.Label("0  /  HUB RESERVED",board,new Vector3(0,.67f,-.041f),.018f,Workshop.Cream);
            Transform table=w.Group("Instruction table",transform,new Vector3(-3.55f,0,-8.1f));
            w.Shape("Notebook table top",table,new Vector3(0,.8f,0),new Vector3(1.1f,.09f,.8f),Workshop.Steel);
            foreach(float x in new[]{-.45f,.45f})foreach(float z in new[]{-.3f,.3f})w.Shape("Table leg",table,new Vector3(x,.4f,z),new Vector3(.045f,.8f,.045f),metal);
            Transform book=w.Group("Elevator field notebook",table,new Vector3(0,.87f,0));book.gameObject.AddComponent<FieldNotebook>().Build(game,w);
        }
        void Update(){if(glass)glass.SetFloat("_Travel",game.Phase==DescentGame.RunPhase.Transit?1:0);}
    }
}

