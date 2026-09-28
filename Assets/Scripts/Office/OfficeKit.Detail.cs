using UnityEngine;
using TheElevator.Generation;

namespace TheElevator.Office
{
    // Deterministic authored detail clusters. Keep floor-level pieces within existing furniture footprints.
    public sealed partial class OfficeKit
    {
        void RoomDetails(Transform root, MapRoom room, OfficeRoomPlan info)
        {
            Transform wall=A.Group(root,"Department identity and daily life",new Vector3(4.1f,0,5.60f));
            for(int side=-1;side<=1;side+=2)A.Box(wall,"Noticeboard floor-supported post",new Vector3(side*1.05f,1.0f,.065f),new Vector3(.045f,2,.045f),A.Metal);
            A.Box(wall,"Noticeboard surround",new Vector3(0,1.95f,0),new Vector3(2.35f,1.3f,.08f),A.Wood);
            A.Box(wall,"Noticeboard felt",new Vector3(0,1.95f,-.05f),new Vector3(2.23f,1.18f,.025f),A.Upholstery);
            string[] notices={"MANDATORY JOY\nTHURSDAY / 09:00","LOST: ONE HAND\nRETURN TO HR","SAFETY RECORD\n003 DAYS","COFFEE IS A\nREVOCABLE PRIVILEGE","PROMOTION LIST\nPENDING FOREVER","REMEMBER TO\nRECHARGE"};
            for(int i=0;i<3;i++)
            {
                Transform sheet=A.Group(wall,"Pinned employee notice",new Vector3(-.72f+i*.72f,1.96f,-.075f));
                sheet.localRotation=Quaternion.Euler(0,0,(i-1)*4);
                A.Box(sheet,"Notice paper",Vector3.zero,new Vector3(.6f,.79f,.012f),A.Paper);
                A.Box(sheet,"Notice department header",new Vector3(0,.27f,-.012f),new Vector3(.5f,.09f,.008f),A.DepartmentAccents[info.Department]);
                A.Label(sheet,notices[(room.Id+i)%notices.Length],new Vector3(0,.07f,-.018f),.019f,Color.black);
                for(int line=0;line<4;line++)A.Box(sheet,"Memo fine print",new Vector3(-.03f,-.1f-line*.045f,-.014f),new Vector3(.38f-line*.025f,.009f,.004f),A.Dark);
                A.Round(sheet,"Notice pin",new Vector3(0,.36f,-.021f),new Vector3(.028f,.028f,.017f),A.Red,PrimitiveType.Sphere);
            }
            Transform clock=A.Group(root,"Oversized department clock",new Vector3(-3.3f,3.35f,5.58f));
            A.Round(clock,"Clock face",Vector3.zero,new Vector3(1.2f,1.2f,.08f),A.Paper,PrimitiveType.Sphere);
            A.W.Soft.Ring(clock,"Plum clock rim",Vector3.zero,.59f,.055f,A.W.Soft.Plum);
            A.Box(clock,"Minute hand",new Vector3(.1f,.12f,-.075f),new Vector3(.075f,.45f,.035f),A.Red).transform.localRotation=Quaternion.Euler(0,0,-32);
            A.Box(clock,"Hour hand",new Vector3(-.11f,0,-.09f),new Vector3(.30f,.085f,.04f),A.Dark);
            for(int mark=0;mark<4;mark++){float angle=mark*Mathf.PI*.5f;A.Round(clock,"Clock marker",new Vector3(Mathf.Cos(angle)*.47f,Mathf.Sin(angle)*.47f,-.07f),new Vector3(.075f,.075f,.025f),A.Brass,PrimitiveType.Sphere);}            if(room.Id!=Plan.MeetingRoom)
            {
                A.Box(root,"Department plaque",new Vector3(3.2f,2.35f,-5.76f),new Vector3(1.55f,.42f,.05f),A.Dark);
                A.Label(root,"M / "+OfficePlan.Departments[info.Department]+"\n"+info.Kind.ToString().ToUpper()+"  "+room.Id.ToString("000"),new Vector3(3.2f,2.35f,-5.73f),.031f,null,180);
            }
            // Slim safety equipment on the perimeter; never in the central transit cross.
            Transform safety=A.Group(root,"Safety station",new Vector3(-5.55f,0,-4.9f),90);
            A.Box(safety,"Fire equipment backing",new Vector3(0,1.2f,0),new Vector3(.5f,1.2f,.055f),A.Dark);
            A.Round(safety,"Extinguisher tank",new Vector3(0,.95f,-.15f),new Vector3(.24f,.26f,.24f),A.Red);
            A.Box(safety,"Extinguisher grip",new Vector3(0,1.28f,-.15f),new Vector3(.2f,.05f,.07f),A.Metal);
            A.Box(safety,"Extinguisher hose",new Vector3(.14f,1.06f,-.15f),new Vector3(.035f,.39f,.035f),A.Dark);
            A.Label(safety,"FIRE\nAND OTHER FEELINGS",new Vector3(0,1.64f,-.035f),.021f);
            if(info.Kind==OfficeRoomKind.Lobby||info.Kind==OfficeRoomKind.Reception)
            {
                Transform brand=A.Group(root,"Corporate architectural emblem",new Vector3(-4.15f,2.92f,-5.65f),180);
                A.Box(brand,"Brand backplate",Vector3.zero,new Vector3(2.4f,.76f,.06f),A.Wood);
                A.Label(brand,"M O R R O W",new Vector3(0,.09f,-.047f),.075f);
                A.Label(brand,"YOU BELONG TO SOMETHING BIGGER",new Vector3(0,-.2f,-.047f),.019f);
            }
        }
        void DeskDetails(Transform t,int room)
        {
            Transform phone=A.Group(t,"Desk telephone",new Vector3(.74f,.81f,.49f),-12);
            A.Box(phone,"Telephone base",new Vector3(0,.035f,0),new Vector3(.24f,.07f,.2f),A.Dark);
            A.Box(phone,"Handset",new Vector3(0,.09f,.03f),new Vector3(.27f,.055f,.063f),A.Plastic);
            for(int x=-1;x<=1;x++)for(int y=0;y<3;y++)A.Box(phone,"Telephone key",new Vector3(x*.035f,.076f,-.067f+y*.027f),new Vector3(.024f,.008f,.019f),A.Paper);
            if(Plan.Config.Quality>=OfficeQuality.High)
            {
                for(int i=0;i<9;i++) A.Round(phone,"Coiled handset cable",new Vector3(.16f,.026f,-.03f+i*.018f),new Vector3(.025f,.006f,.025f),A.Dark);
                Transform photo=A.Group(t,"Personal framed photograph",new Vector3(-.89f,.82f,.36f),12);
                A.Box(photo,"Photo frame",new Vector3(0,.12f,0),new Vector3(.21f,.24f,.035f),A.Brass);
                A.Box(photo,"Photo print",new Vector3(0,.12f,-.021f),new Vector3(.175f,.2f,.008f),A.DepartmentAccents[room%6]);
                for(int i=-1;i<=1;i++)
                {
                    A.Round(photo,"Family portrait head",new Vector3(i*.046f,.15f,-.028f),new Vector3(.033f,.04f,.01f),A.Plastic,PrimitiveType.Sphere);
                    A.Box(photo,"Family portrait suit",new Vector3(i*.046f,.097f,-.028f),new Vector3(.042f,.058f,.007f),A.Dark);
                }
            }
            A.Box(t,"Underdesk drawer unit",new Vector3(-.73f,.39f,.28f),new Vector3(.49f,.66f,.55f),A.Plastic);
            for(int drawer=0;drawer<3;drawer++)
            {
                A.Box(t,"Drawer front",new Vector3(-.73f,.2f+drawer*.20f,-.007f),new Vector3(.455f,.18f,.025f),A.Plastic);
                A.Box(t,"Drawer recessed pull",new Vector3(-.73f,.23f+drawer*.20f,-.026f),new Vector3(.19f,.022f,.017f),A.Dark);
            }
            A.Box(t,"Desk blotter",new Vector3(.02f,.802f,-.13f),new Vector3(.95f,.003f,.39f),A.Upholstery);
            A.Box(t,"Terminal reminder note",new Vector3(.30f,1.02f,.25f),new Vector3(.12f,.10f,.008f),A.Paper);
            A.Label(t,"SMILE",new Vector3(.30f,1.02f,.241f),.010f,Color.black);
        }
        void KitchenDetails(Transform t)
        {
            for(int i=0;i<6;i++)
            {
                A.Box(t,"Backsplash tile",new Vector3(-.86f+i*.34f,1.15f,.7f),new Vector3(.328f,.32f,.025f),A.Tile);
                if(i!=0)A.Box(t,"Upper cupboard",new Vector3(-.86f+i*.34f,1.99f,.66f),new Vector3(.328f,.56f,.26f),A.Plastic);
                if(i!=0)A.Box(t,"Cupboard pull",new Vector3(-.86f+i*.34f,1.85f,.52f),new Vector3(.11f,.025f,.025f),A.Brass);
            }
            A.Box(t,"Drip tray",new Vector3(-.55f,.99f,.05f),new Vector3(.36f,.025f,.22f),A.Metal);
            for(int i=0;i<4;i++)A.Box(t,"Drip tray slots",new Vector3(-.68f+i*.085f,1.006f,.05f),new Vector3(.012f,.005f,.15f),A.Dark);
            for(int i=0;i<3;i++)A.Round(t,"Coffee supply tin",new Vector3(-.03f+i*.13f,1.08f,.5f),new Vector3(.10f,.10f,.10f),i==0?A.Red:A.Brass);
            A.Box(t,"Microwave controls",new Vector3(.8f,1.2f,.145f),new Vector3(.11f,.29f,.012f),A.Dark);
            A.Box(t,"Microwave timer",new Vector3(.8f,1.27f,.135f),new Vector3(.08f,.045f,.009f),A.Screen);
        }
        void VendingDetails(Transform t)
        {
            for(int side=-1;side<=1;side+=2)
            {
                A.Box(t,"Cabinet corner molding",new Vector3(side*.59f,1.02f,-.445f),new Vector3(.036f,1.96f,.04f),A.Metal);
                for(int y=0;y<3;y++)A.Round(t,"Face retaining screw",new Vector3(side*.58f,.2f+y*.8f,-.472f),new Vector3(.016f,.016f,.012f),A.Brass,PrimitiveType.Sphere);
                A.Box(t,"Rubber leveling foot",new Vector3(side*.46f,.025f,.27f),new Vector3(.14f,.05f,.16f),A.Dark);
            }
            for(int row=0;row<4;row++)
            {
                A.Label(t,"A"+(row+1)+"     B"+(row+1)+"     C"+(row+1),new Vector3(-.15f,.72f+row*.26f,-.592f),.014f);
                for(int col=0;col<4;col++)A.Box(t,"Can nutrition stripe",new Vector3(-.44f+col*.18f,.79f+row*.26f,-.57f),new Vector3(.074f,.04f,.007f),A.Paper);
            }
            A.Box(t,"Coin slot surround",new Vector3(.43f,.83f,-.489f),new Vector3(.17f,.12f,.023f),A.Metal);
            A.Box(t,"Coin slot",new Vector3(.43f,.84f,-.504f),new Vector3(.09f,.012f,.008f),A.Dark);
            A.Label(t,"EXACT CHANGE\nNO REFUNDS",new Vector3(.43f,.65f,-.512f),.012f);
            for(int slat=0;slat<6;slat++)A.Box(t,"Compressor ventilation slot",new Vector3(0,.08f+slat*.021f,-.478f),new Vector3(.87f,.009f,.01f),A.Dark);
            A.Label(t,"PROPERTY OF MORROW / ASSET 009-125",new Vector3(0,.24f,-.541f),.013f);
            Transform service=A.Group(t,"Rear service panel",new Vector3(0,1.05f,.447f),180);
            A.Box(service,"Removable service cover",Vector3.zero,new Vector3(.91f,1.7f,.017f),A.Dark);
            A.Label(service,"CAUTION\nDISCONNECT BEFORE MOVING\n125 KG / TWO PERSON LIFT",new Vector3(0,.28f,-.018f),.024f);
            for(int i=0;i<8;i++)A.Box(service,"Rear condenser louver",new Vector3(0,-.3f+i*.047f,-.014f),new Vector3(.72f,.016f,.01f),A.Metal);
        }
    }
}
