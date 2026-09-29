using UnityEngine;

namespace TheElevator.Office
{
    // The boardroom: a dead-end room two cells long. One long table with chairs all around it runs from near the door
    // to the far end, where a presenter stands beside the board.
    public sealed partial class OfficeKit
    {
        public Transform MeetingBoard { get; private set; }
        public const float BoardHeight = 1.8f;

        // Local frame: origin at the door cell's centre, +Z runs away from the door into the extension cell.
        void MeetingRoom(Transform geometry,int room,int annexSide,float height)
        {
            float cell=floor.Manifest.Recipe.Settings.CellSizeMillimeters/1000f;
            Transform m=A.Group(geometry,"Boardroom",Vector3.zero,annexSide*90);

            // Ceiling and light for the gap and the extension cell.
            for(int ix=-1;ix<=1;ix++)for(int iz=-1;iz<=1;iz++)
                A.Box(m,"Rounded ceiling island",new Vector3(ix*3.7f,height-.06f,cell+iz*3.7f),new Vector3(3.35f,.14f,3.35f),A.Plaster);
            for(int sign=-1;sign<=1;sign+=2)
                foreach(float z in new[]{cell*.5f,cell})
                {
                    A.Box(m,"Recessed luminaire",new Vector3(sign*3,height-.14f,z),new Vector3(1.24f,.1f,2.44f),A.Dark);
                    A.Box(m,"Opal light diffuser",new Vector3(sign*3,height-.2f,z),new Vector3(1.1f,.035f,2.3f),A.CoolLight);
                    A.W.Lamp(m,new Vector3(sign*3,height-.45f,z),new Color(.93f,.97f,.95f),1.5f,9);
                }

            // The door half of the first cell stays open floor; the table runs from just past its centre to the far end.
            float start=1.4f,end=cell+2.6f,length=end-start;
            A.Box(m,"Boardroom table",new Vector3(0,.74f,(start+end)*.5f),new Vector3(1.9f,.09f,length),A.Wood,true);
            for(float z=start+1.2f;z<=end-1.1f;z+=3.3f)A.Box(m,"Table pedestal",new Vector3(0,.35f,z),new Vector3(.55f,.7f,.8f),A.Dark);
            int seats=Mathf.FloorToInt(length/1.35f);
            float first=start+(length-(seats-1)*1.35f)*.5f;
            for(int i=0;i<seats;i++)
                for(int side=-1;side<=1;side+=2)
                {
                    float z=first+i*1.35f;
                    Transform seat=A.Group(m,"Boardroom seat",new Vector3(side*1.35f,0,z),side<0?90:-90);
                    Chair(seat,Vector3.zero);
                    Task(seat,room,OfficeTask.Meeting,Vector3.zero,true);
                    int prop=(i*2+side+3)%4;
                    if(prop==0)Paperwork(m,new Vector3(side*.5f,.79f,z));
                    else if(prop==2)Mug(m,new Vector3(side*.6f,.79f,z+.25f));
                }

            // The board hangs on the far wall, facing back down the table.
            Transform board=A.Group(m,"Presentation board",new Vector3(0,0,cell+5.84f));
            A.Box(board,"Board frame",new Vector3(0,BoardHeight,0),new Vector3(4.8f,2.3f,.06f),A.Dark);
            A.Box(board,"Board surface",new Vector3(0,BoardHeight,-.035f),new Vector3(4.6f,2.1f,.012f),A.Paper);
            A.Label(board,"Q4 ROADMAP",new Vector3(0,2.62f,-.045f),.05f,Color.black);
            for(int i=0;i<6;i++)
            {
                float bar=.25f+i*.2f;
                A.Box(board,"Chart bar",new Vector3(-1.4f+i*.56f,1.05f+bar*.5f,-.045f),new Vector3(.34f,bar,.012f),i==5?A.Red:A.DepartmentAccents[i%A.DepartmentAccents.Length]);
            }
            A.Label(board,"JAN       FEB       MAR       APR       MAY       JUN",new Vector3(0,.92f,-.045f),.026f,Color.black);
            MeetingBoard=board;
            // The presenter stands beside the board with it on their right, half turned toward the table.
            Task(m,room,OfficeTask.Present,new Vector3(1.9f,0,cell+4.9f),false,215);
        }
    }
}
