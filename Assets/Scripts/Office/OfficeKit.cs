using UnityEngine;
using TheElevator.Generation;

namespace TheElevator.Office
{
    public sealed partial class OfficeKit
    {
        public readonly OfficeArt A;
        public readonly OfficePlan Plan;
        readonly GeneratedFloor floor;
        readonly System.Collections.Generic.HashSet<int> recoverableRooms=new System.Collections.Generic.HashSet<int>();
        public OfficeKit(Workshop w,GeneratedFloor target)
        { A=new OfficeArt(w); floor=target; Plan=OfficePlan.Build(target.Manifest); }
        public void Dress(Transform geometry,MapRoom room,float height)
        {
            OfficeRoomPlan info=Plan.Rooms[room.Id];
            Material departmentAccent=A.DepartmentAccents[info.Department];
            bool publicArea=info.Kind==OfficeRoomKind.Lobby||info.Kind==OfficeRoomKind.Reception;
            bool service=info.Kind==OfficeRoomKind.Server||info.Kind==OfficeRoomKind.Maintenance||info.Kind==OfficeRoomKind.Records;
            foreach(MeshRenderer renderer in geometry.GetComponentsInChildren<MeshRenderer>())
                if (!renderer.GetComponent<TextMesh>())
                renderer.sharedMaterial=renderer.name.Contains("Floor")||renderer.name.Contains("landing")||renderer.name.Contains("gallery") ? (publicArea?A.Tile:A.Carpet)
                    :renderer.name.Contains("stripe")||renderer.name.Contains("frame") ? departmentAccent :renderer.name.Contains("Ceiling")?A.Plaster:A.Plaster;
            Edge[] edges=Edges(room);
            // Finished wall bays on every real wall: wainscot, skirting, crown, inset panels and outlets.
            int annexSide=room.Id==Plan.MeetingRoom?Plan.MeetingAnnexSide:-1;
            for(int side=0;side<4;side++)
            {
                if(side==annexSide||edges[side]==Edge.Open)continue;
                Transform wall=A.Group(geometry,"Finished wall bay",Vector3.zero,side*90);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    A.Box(wall,"Walnut wainscot",new Vector3(sign*4.05f,.52f,5.82f),new Vector3(3.65f,.95f,.08f),publicArea?A.Wood:A.Plaster);
                    A.Box(wall,"Brass skirting",new Vector3(sign*4.05f,.1f,5.74f),new Vector3(3.65f,.18f,.045f),A.Dark);
                    A.Box(wall,"Wall picture rail",new Vector3(sign*4.05f,1.02f,5.74f),new Vector3(3.65f,.035f,.05f),A.Brass);
                    A.Box(wall,"Ceiling perimeter trim",new Vector3(sign*4.05f,height-.14f,5.78f),new Vector3(3.65f,.12f,.13f),A.Dark);
                    A.Box(wall,"Outlet plate",new Vector3(sign*3.1f,.35f,5.71f),new Vector3(.18f,.12f,.035f),A.Plastic);
                    for(int socket=0;socket<2;socket++) A.Box(wall,"Socket",new Vector3(sign*3.1f-.04f+socket*.08f,.35f,5.687f),new Vector3(.025f,.045f,.012f),A.Dark);
                    A.Box(wall,"Inset acoustic wall panel",new Vector3(sign*4.3f,2.05f,5.79f),new Vector3(2.65f,1.52f,.04f),service?A.Metal:A.Upholstery);
                    for(int rib=0;rib<4;rib++) A.Box(wall,"Acoustic rib",new Vector3(sign*4.3f-1.17f+rib*.72f,2.05f,5.745f),new Vector3(.12f,1.20f,.085f),publicArea?A.Brass:A.Dark);
                }
               if(edges[side]==Edge.Door&&!(room.Has(RoomRole.Entrance)&&side==2))
                {
                    // A plain cased opening; the one real door hangs in the passage beyond it.
                    for(int sign=-1;sign<=1;sign+=2)A.Box(wall,"Finished door jamb",new Vector3(sign*1.45f,1.4f,5.73f),new Vector3(.20f,2.8f,.26f),A.Dark);
                    A.Box(wall,"Access terminal housing",new Vector3(1.68f,1.38f,5.72f),new Vector3(.23f,.38f,.12f),A.Dark);
                    A.Box(wall,"Access status",new Vector3(1.68f,1.46f,5.64f),new Vector3(.15f,.08f,.015f),info.Clearance>0?A.WarmLight:A.Screen);
                }
            }
            if(!room.Has(RoomRole.StairUp))
            {
                // Broad soft ceiling islands keep the room readable without a dense tile grid.
                for(int ix=-1;ix<=1;ix++)for(int iz=-1;iz<=1;iz++)
                    A.Box(geometry,"Rounded ceiling island",new Vector3(ix*3.7f,height-.06f,iz*3.7f),new Vector3(3.35f,.14f,3.35f),A.Plaster);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    A.Box(geometry,"Recessed luminaire",new Vector3(sign*3,height-.14f,0),new Vector3(1.24f,.1f,2.44f),A.Dark);
                    A.Box(geometry,"Opal light diffuser",new Vector3(sign*3,height-.2f,0),new Vector3(1.1f,.035f,2.3f),publicArea?A.WarmLight:A.CoolLight);
                    A.W.Lamp(floor.RoomRoots[room.Id],new Vector3(sign*3,height-.45f,0),publicArea?new Color(1,.95f,.86f):service?new Color(.78f,.88f,.95f):new Color(.93f,.97f,.95f),publicArea?1.7f:1.5f,9);
                    A.Box(geometry,"HVAC grille",new Vector3(sign*4,height-.13f,4),new Vector3(1.3f,.06f,.65f),A.Dark);
                    for(int slat=0;slat<12;slat++) A.Box(geometry,"Vent louver",new Vector3(sign*4-.56f+slat*.1f,height-.18f,4),new Vector3(.035f,.05f,.57f),A.Metal);
                }
                A.Round(geometry,"Smoke detector",new Vector3(0,height-.13f,3),new Vector3(.24f,.045f,.24f),A.Plastic);
                A.Round(geometry,"Sprinkler",new Vector3(3,height-.23f,3),new Vector3(.1f,.06f,.1f),A.Brass);
            }
            if(room.IsStair) return;
            if(room.Has(RoomRole.Landmark))
            {
                Transform landmark=A.Group(geometry,"Landmark / corporate memory",new Vector3(0,height-.8f,0));
                // Suspended sculpture keeps the walking lanes unobstructed below 2.8 m.
                A.Round(landmark,"Corporate memory disc",Vector3.zero,new Vector3(1.35f,.045f,1.35f),departmentAccent);
                for(int i=0;i<4;i++){Transform vane=A.Group(landmark,"Sculpture vane",Vector3.zero,i*90+room.Id%30);A.Box(vane,"Brass memory fin",new Vector3(.55f,.1f,0),new Vector3(.07f,.4f,.8f),A.Brass);}
           }
            if(annexSide>=0)MeetingRoom(geometry,room.Id,annexSide,height);
            else Furnish(geometry,room,info,height);
        }
        public void DressConnector(Transform root,float length,float width,MapLink link)
        {
            foreach(MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=renderer.name.Contains("deck")?A.Tile:A.Plaster;
            for(int sign=-1;sign<=1;sign+=2)
            {
                A.Box(root,"Connector skirting",new Vector3(sign*(width/2-.02f),.08f,0),new Vector3(.06f,.16f,length),A.Dark);
                A.Box(root,"Connector dado",new Vector3(sign*(width/2-.04f),1.05f,0),new Vector3(.04f,.035f,length),A.Brass);
                A.Box(root,"Inset wayfinding band",new Vector3(sign*(width/2-.04f),2.5f,0),new Vector3(.04f,.2f,length*.8f),A.DepartmentAccents[Plan.Rooms[link.B].Department]);
                A.Box(root,"Connector wall pilaster",new Vector3(sign*(width/2-.03f),1.5f,0),new Vector3(.07f,3,.09f),A.Metal);
            }
            A.Box(root,"Connector light recess",new Vector3(0,3.18f,0),new Vector3(.5f,.06f,Mathf.Min(1.4f,length)),A.Dark);
            A.Box(root,"Connector opal fixture",new Vector3(0,3.14f,0),new Vector3(.4f,.018f,Mathf.Min(1.3f,length)),A.CoolLight);
        }
        public void Desk(Transform t,int room,bool cubicle)
        {
            A.Box(t,"Desk slab / radiused edge",new Vector3(0,.76f,.15f),new Vector3(2.15f,.12f,1.0f),A.Wood,true);
            for(int side=-1;side<=1;side+=2)
            {
                A.Box(t,"Desk trestle",new Vector3(side*.85f,.36f,.2f),new Vector3(.16f,.72f,.65f),A.Metal);
                A.Box(t,"Desk foot",new Vector3(side*.85f,.055f,.15f),new Vector3(.16f,.1f,.9f),A.Dark);
            }
            if(cubicle)
            {
                A.Box(t,"Cubicle acoustic divider",new Vector3(0,.88f,.72f),new Vector3(2.3f,1.76f,.18f),A.Upholstery,true);
                A.Box(t,"Divider cap",new Vector3(0,1.77f,.72f),new Vector3(2.34f,.035f,.10f),A.Metal);
                A.Box(t,"Divider side",new Vector3(-1.12f,.88f,.18f),new Vector3(.18f,1.76f,1.05f),A.Upholstery,true);
            }
            Monitor(t,new Vector3(.05f,.81f,.34f),(room==2||room%3==0)&&recoverableRooms.Add(room));
            A.Box(t,"Keyboard",new Vector3(.03f,.815f,-.19f),new Vector3(.45f,.023f,.17f),A.Plastic);
            if(Plan.Config.Quality>=OfficeQuality.Medium) for(int row=0;row<5;row++) for(int key=0;key<14;key++)
                A.Box(t,"Keycap",new Vector3(-.177f+key*.030f,.832f,-.25f+row*.030f),new Vector3(.024f,.009f,.023f),key%5==0?A.Dark:A.Paper);
            A.Box(t,"Mouse pad",new Vector3(.59f,.802f,-.17f),new Vector3(.3f,.006f,.28f),A.Dark);
            A.Round(t,"Mouse",new Vector3(.59f,.83f,-.17f),new Vector3(.07f,.045f,.12f),A.Plastic,PrimitiveType.Sphere);
            Chair(t,new Vector3(0,0,-.83f));
            Paperwork(t,new Vector3(-.66f,.81f,-.12f));
            Mug(t,new Vector3(.81f,.81f,.25f));
            A.Box(t,"Personal name plate",new Vector3(-.68f,.88f,.47f),new Vector3(.55f,.12f,.1f),A.Brass);
            // The tower stands on the floor under the desk.
            A.Box(t,"PC tower",new Vector3(.78f,.31f,.33f),new Vector3(.3f,.62f,.47f),A.Dark);
            for(int slot=0;slot<10;slot++)A.Box(t,"Tower intake grille",new Vector3(.78f,.12f+slot*.025f,.090f),new Vector3(.21f,.009f,.006f),A.Metal);
            for(int usb=0;usb<2;usb++)A.Box(t,"Front USB socket",new Vector3(.735f+usb*.085f,.52f,.086f),new Vector3(.04f,.018f,.008f),A.Metal);
            A.Round(t,"Power button",new Vector3(.78f,.58f,.085f),new Vector3(.02f,.02f,.008f),A.Metal,PrimitiveType.Sphere);
            A.Box(t,"PC status strip",new Vector3(.78f,.43f,.085f),new Vector3(.15f,.014f,.015f),A.Screen);
            A.Round(t,"Waste bin",new Vector3(-.84f,.21f,-.42f),new Vector3(.3f,.21f,.3f),A.Metal);
            A.Box(t,"Cable channel",new Vector3(0,.678f,.46f),new Vector3(1.6f,.045f,.045f),A.Dark);
            DeskDetails(t,room);
            Task(t,room,OfficeTask.Typing,new Vector3(0,0,-.83f),true);
        }
        public void Monitor(Transform t,Vector3 p,bool recoverable=false)
        {
            Transform screen=A.Group(t,"Office LCD workstation",p);screen.gameObject.AddComponent<OfficeEquipment>().Recoverable=recoverable;
            A.Box(screen,"Weighted monitor base",new Vector3(0,.016f,0),new Vector3(.29f,.032f,.23f),A.Dark);
            A.Box(screen,"Height adjustment column",new Vector3(0,.15f,.055f),new Vector3(.048f,.28f,.055f),A.Metal);
            A.Round(screen,"VESA pivot",new Vector3(0,.32f,.04f),new Vector3(.085f,.06f,.065f),A.Dark,PrimitiveType.Sphere);
            A.Box(screen,"Injection molded rear shell",new Vector3(0,.37f,.035f),new Vector3(.65f,.402f,.11f),A.Dark);
            A.Box(screen,"LCD inner bezel",new Vector3(0,.376f,-.024f),new Vector3(.626f,.365f,.010f),A.Metal);
            A.Box(screen,"Matte display panel",new Vector3(0,.38f,-.030f),new Vector3(.613f,.348f,.005f),A.Screen);
            A.Box(screen,"Application toolbar",new Vector3(0,.523f,-.034f),new Vector3(.606f,.053f,.003f),A.Dark);
            A.Label(screen,"MORROW / BUDGET WORKBOOK",new Vector3(0,.524f,-.037f),.006f);
            A.Box(screen,"Spreadsheet heading",new Vector3(.047f,.475f,-.034f),new Vector3(.48f,.03f,.003f),A.Upholstery);
            for(int col=0;col<6;col++)A.Box(screen,"Spreadsheet column",new Vector3(-.18f+col*.088f,.36f,-.034f),new Vector3(.0015f,.2f,.003f),A.Plastic);
            for(int row=0;row<7;row++)A.Box(screen,"Spreadsheet row",new Vector3(.045f,.45f-row*.029f,-.034f),new Vector3(.46f,.001f,.003f),A.Plastic);
            for(int row=0;row<5;row++)A.Label(screen,(1042+row*113).ToString()+"       "+(73+row*9)+"       APPROVED",new Vector3(.055f,.435f-row*.029f,-.037f),.0045f);
            A.Box(screen,"Sidebar",new Vector3(-.26f,.38f,-.034f),new Vector3(.074f,.23f,.003f),A.Dark);
            A.Round(screen,"Standby LED",new Vector3(.277f,.184f,-.025f),new Vector3(.003f,.003f,.003f),A.CoolLight,PrimitiveType.Sphere);
            A.Label(screen,"M O R R O W",new Vector3(0,.181f,-.026f),.004f);
            for(int vent=0;vent<12;vent++)A.Box(screen,"Rear cooling slots",new Vector3(-.23f+vent*.042f,.44f,.023f),new Vector3(.018f,.045f,.003f),A.Metal);
            A.Box(screen,"Rear serial label",new Vector3(0,.33f,.026f),new Vector3(.18f,.055f,.003f),A.Paper);
            A.Box(screen,"Video cable plug",new Vector3(-.07f,.21f,.035f),new Vector3(.025f,.043f,.025f),A.Dark);
            A.Box(screen,"Cable to desk grommet",new Vector3(-.07f,.1f,.063f),new Vector3(.009f,.21f,.009f),A.Dark);
        }        public void Chair(Transform t,Vector3 p)
        {
            Transform c=A.Group(t,"Ergonomic chair",p);
            // Castors on the floor, spokes on the castors, gas lift on the spokes, seat on the lift.
            A.Round(c,"Chair gas lift",new Vector3(0,.2f,0),new Vector3(.07f,.12f,.07f),A.Metal);
            for(int i=0;i<5;i++)
            {
                Transform leg=A.Group(c,"Chair spoke",Vector3.zero,i*72);
                A.Box(leg,"Castor arm",new Vector3(0,.08f,.2f),new Vector3(.04f,.045f,.42f),A.Metal);
                A.Round(leg,"Castor",new Vector3(0,.03f,.38f),new Vector3(.085f,.06f,.085f),A.Dark,PrimitiveType.Sphere);
            }
            A.Box(c,"Sculpted seat",new Vector3(0,BeanRig.SeatSurface-.085f,0),new Vector3(.60f,.17f,.56f),A.Upholstery);
            A.Box(c,"Lumbar back",new Vector3(0,.705f,-.24f),new Vector3(.59f,.58f,.17f),A.Upholstery);
            for(int s=-1;s<=1;s+=2) A.Box(c,"Chair armrest",new Vector3(s*.31f,.595f,.015f),new Vector3(.065f,.05f,.4f),A.Dark);
            c.gameObject.AddComponent<OfficeSeat>();
            // Solid to players (you can't walk through a chair); employees ignore this layer so they can sit in it.
            BoxCollider solid=c.gameObject.AddComponent<BoxCollider>();solid.center=new Vector3(0,.48f,-.04f);solid.size=new Vector3(.62f,.96f,.62f);
            c.gameObject.layer=PhysicsLayers.Seats;
        }
        public void Paperwork(Transform t,Vector3 p)
        {
            Transform papers=A.Group(t,"Desk story / rejected requisitions",p,-8);
            A.Box(papers,"Report stack",new Vector3(0,.018f,0),new Vector3(.31f,.035f,.23f),A.Paper);
            if(Plan.Config.Quality>=OfficeQuality.Medium) for(int i=0;i<5;i++) A.Box(papers,"Printed report line",new Vector3(-.025f,.038f,-.07f+i*.026f),new Vector3(.19f,.001f,.005f),A.Dark);
            A.Box(papers,"Rejected rubber stamp",new Vector3(.2f,.04f,.015f),new Vector3(.08f,.075f,.05f),A.Red);
            A.Box(papers,"Company pen",new Vector3(-.2f,.01f,0),new Vector3(.012f,.013f,.16f),A.Brass);
        }
        public void Mug(Transform t,Vector3 p)
        {
            A.Round(t,"Employee mug",p+Vector3.up*.065f,new Vector3(.12f,.065f,.12f),A.Plastic);
            A.Round(t,"Coffee surface",p+Vector3.up*.133f,new Vector3(.102f,.002f,.102f),A.Wood);
            A.Box(t,"Mug handle",p+new Vector3(.075f,.07f,0),new Vector3(.055f,.075f,.026f),A.Plastic);
        }
        public void Plant(Transform t,Vector3 p)
        {
            Transform plant=A.Group(t,"Tagged synthetic ficus",p);
            A.Round(plant,"Ceramic planter",new Vector3(0,.25f,0),new Vector3(.48f,.25f,.48f),A.Plastic);
            A.Round(plant,"Soil",new Vector3(0,.51f,0),new Vector3(.41f,.01f,.41f),A.Wood);
            A.Round(plant,"Artificial trunk",new Vector3(0,.95f,0),new Vector3(.045f,.5f,.045f),A.Wood);
            for(int i=0;i<9;i++)
            {
                float angle=i*137.5f*Mathf.Deg2Rad;
                GameObject leaf=A.Round(plant,"Molded foliage",new Vector3(Mathf.Sin(angle)*.23f,1.05f+i*.067f,Mathf.Cos(angle)*.23f),new Vector3(.18f,.045f,.46f),A.Upholstery,PrimitiveType.Sphere);
                leaf.transform.localRotation=Quaternion.Euler(15,i*137.5f,-20);
            }
        }
        void Reception(Transform t,int room)
        {
            A.Box(t,"Reception curved counter",new Vector3(0,.57f,.3f),new Vector3(2.25f,1.14f,.8f),A.Wood,true);
            A.Box(t,"Stone counter cap",new Vector3(0,1.16f,.3f),new Vector3(2.38f,.08f,.92f),A.Tile);
            for(int i=0;i<14;i++) A.Box(t,"Reception vertical fluting",new Vector3(-1.05f+i*.16f,.61f,-.13f),new Vector3(.035f,.94f,.025f),A.Brass);
            Transform terminal=A.Group(t,"Reception terminal facing employee",new Vector3(-.58f,1.21f,.36f),180);Monitor(terminal,Vector3.zero);
            A.Box(t,"Reception keyboard",new Vector3(0,1.22f,.63f),new Vector3(.6f,.035f,.18f),A.Plastic);
            A.Label(t,"RECEPTION",new Vector3(.2f,.7f,-.16f),.04f);
            Task(t,room,OfficeTask.Reception,new Vector3(0,0,1.05f),false,180);
        }
        void Lounge(Transform t)
        {
            // The settee base, back and arms all stand on the floor; the cushions sit on the base.
            A.Box(t,"Upholstered settee",new Vector3(0,.18f,.2f),new Vector3(2.05f,.36f,.78f),A.Upholstery,true);
            A.Box(t,"Settee back",new Vector3(0,.48f,.54f),new Vector3(2.08f,.96f,.17f),A.Upholstery);
            for(int s=-1;s<=1;s+=2) A.Box(t,"Settee arm",new Vector3(s*.97f,.31f,.18f),new Vector3(.14f,.62f,.79f),A.Wood);
            A.Box(t,"Coffee table",new Vector3(0,.35f,-.86f),new Vector3(1.15f,.065f,.53f),A.Wood,true);
            A.Box(t,"Coffee table pedestal",new Vector3(0,.16f,-.86f),new Vector3(.16f,.32f,.2f),A.Brass);
            Paperwork(t,new Vector3(.2f,.383f,-.85f));
            for(int i=-1;i<=1;i++)
            {
                A.Box(t,"Individual settee cushion",new Vector3(i*.62f,(.36f+BeanRig.SeatSurface)*.5f,.18f),new Vector3(.59f,BeanRig.SeatSurface-.36f,.65f),A.Upholstery);
                A.Box(t,"Settee stitched back seam",new Vector3(i*.62f,.72f,.451f),new Vector3(.006f,.36f,.006f),A.Dark);
                // One place to sit per cushion, facing away from the back.
                A.Group(t,"Settee place",new Vector3(i*.62f,0,.15f),180).gameObject.AddComponent<OfficeSeat>();
            }
            A.Box(t,"Company magazine",new Vector3(-.31f,.39f,-.87f),new Vector3(.29f,.02f,.37f),A.Red);
            A.Box(t,"Magazine title band",new Vector3(-.31f,.403f,-.93f),new Vector3(.24f,.004f,.05f),A.Paper);
        }
        void Kitchen(Transform t,int room)
        {
            A.Box(t,"Cabinet mounting backboard",new Vector3(0,1.17f,.825f),new Vector3(2.13f,2.34f,.06f),A.Wood);
            A.Box(t,"Kitchen base cabinetry",new Vector3(0,.45f,.35f),new Vector3(2.05f,.9f,.67f),A.Plastic,true);
            A.Box(t,"Kitchen worktop",new Vector3(0,.94f,.35f),new Vector3(2.13f,.07f,.73f),A.Tile);
            for(int s=-1;s<=1;s+=2) A.Box(t,"Cabinet handle",new Vector3(s*.5f,.72f,-.02f),new Vector3(.26f,.035f,.04f),A.Metal);
            A.Box(t,"Coffee dispenser",new Vector3(-.55f,1.24f,.36f),new Vector3(.52f,.54f,.45f),A.Dark);
            A.Box(t,"Coffee status display",new Vector3(-.55f,1.34f,.12f),new Vector3(.25f,.11f,.015f),A.Screen);
            A.Round(t,"Coffee spout",new Vector3(-.55f,1.1f,.1f),new Vector3(.055f,.08f,.055f),A.Metal);
            Mug(t,new Vector3(.5f,.99f,.2f));
            A.Box(t,"Microwave",new Vector3(.5f,1.2f,.38f),new Vector3(.75f,.4f,.44f),A.Plastic);
            A.Box(t,"Microwave window",new Vector3(.45f,1.2f,.148f),new Vector3(.48f,.26f,.014f),A.Dark);
            KitchenDetails(t);
            Task(t,room,OfficeTask.Coffee,new Vector3(-.55f,0,-.4f),false);
        }
        public void Server(Transform t)
        {
            A.Box(t,"Rack cabinet",new Vector3(0,1.08f,.25f),new Vector3(1.05f,2.16f,.85f),A.Dark,true);
            for(int i=0;i<10;i++)
            {
                A.Box(t,"Rack module",new Vector3(0,.22f+i*.19f,-.19f),new Vector3(.91f,.15f,.055f),A.Metal);
                for(int led=0;led<3;led++) A.Box(t,"Server activity LED",new Vector3(.21f+led*.08f,.22f+i*.19f,-.225f),new Vector3(.025f,.02f,.012f),led==0?A.WarmLight:A.Screen);
                A.Box(t,"Rack service label",new Vector3(-.16f,.22f+i*.19f,-.224f),new Vector3(.22f,.035f,.01f),A.Paper);
                for(int side=-1;side<=1;side+=2)A.Box(t,"Rack grab handle",new Vector3(side*.40f,.22f+i*.19f,-.24f),new Vector3(.025f,.095f,.04f),A.Dark);
                if(Plan.Config.Quality>=OfficeQuality.High)for(int vent=0;vent<5;vent++)A.Box(t,"Rack cooling grille",new Vector3(-.29f+vent*.065f,.26f+i*.19f,-.226f),new Vector3(.03f,.012f,.012f),A.Dark);
                A.Box(t,"Cable loom",new Vector3(.55f,.23f+i*.17f,.1f),new Vector3(.06f,.06f,.42f),A.Red);
            }
        }
        void Shelves(Transform t)
        {
            for(int side=-1;side<=1;side+=2) A.Box(t,"Archive upright",new Vector3(side*.92f,1.1f,.3f),new Vector3(.07f,2.2f,.78f),A.Metal,true);
            for(int shelf=0;shelf<5;shelf++)
            {
                A.Box(t,"Archive shelf",new Vector3(0,.15f+shelf*.43f,.3f),new Vector3(1.85f,.04f,.8f),A.Metal);
                for(int box=0;box<4;box++)
                {
                    A.Box(t,"Filed asset carton",new Vector3(-.7f+box*.47f,.34f+shelf*.43f,.3f),new Vector3(.41f,.33f,.6f),box%2==0?A.Plastic:A.Wood);
                    A.Box(t,"Archive handle recess",new Vector3(-.7f+box*.47f,.44f+shelf*.43f,-.007f),new Vector3(.11f,.035f,.014f),A.Dark);
                    A.Box(t,"Carton lid",new Vector3(-.7f+box*.47f,.51f+shelf*.43f,.3f),new Vector3(.425f,.028f,.62f),A.Paper);
                    A.Box(t,"Archive index label",new Vector3(-.7f+box*.47f,.36f+shelf*.43f,-.008f),new Vector3(.22f,.07f,.012f),A.Paper);
                }
            }
        }
        void Washroom(Transform t)
        {
            // Mirrors, dispensers and the sign mount on a full-height splashback, never in mid air.
            A.Box(t,"Washroom splashback",new Vector3(0,1.3f,.8f),new Vector3(2.15f,2.6f,.06f),A.Tile);
            A.Box(t,"Vanity counter",new Vector3(0,.82f,.3f),new Vector3(1.95f,.12f,.72f),A.Tile,true);
            A.Box(t,"Vanity cabinet",new Vector3(0,.38f,.36f),new Vector3(1.85f,.76f,.62f),A.Wood,true);
            for(int side=-1;side<=1;side+=2)
            {
                A.Round(t,"Inset basin",new Vector3(side*.5f,.89f,.24f),new Vector3(.55f,.045f,.43f),A.Plastic);
                A.Box(t,"Automatic faucet",new Vector3(side*.5f,1.01f,.49f),new Vector3(.045f,.26f,.16f),A.Metal);
                A.Box(t,"Mirror",new Vector3(side*.5f,1.65f,.755f),new Vector3(.74f,.94f,.025f),A.Metal);
            }
            A.Box(t,"Soap dispenser",new Vector3(0,1.3f,.70f),new Vector3(.22f,.32f,.14f),A.Plastic);
            A.Box(t,"Soap level window",new Vector3(0,1.29f,.625f),new Vector3(.06f,.17f,.015f),A.Screen);
            A.Box(t,"Towel dispenser",new Vector3(.94f,1.46f,.68f),new Vector3(.26f,.35f,.18f),A.Metal);
            A.Box(t,"Paper towel",new Vector3(.94f,1.2f,.66f),new Vector3(.20f,.19f,.015f),A.Paper);
            A.Label(t,"PLEASE WASH YOUR HANDS",new Vector3(0,2.3f,.765f),.027f);
        }
        public void Vending(Transform t)
        {
            A.Box(t,"Nourish-9 housing",new Vector3(0,1.03f,0),new Vector3(1.24f,2.05f,.88f),A.Red);
            A.Box(t,"Vending face",new Vector3(0,1.02f,-.454f),new Vector3(1.12f,1.94f,.035f),A.Dark);
            A.Box(t,"Product cavity",new Vector3(-.15f,1.25f,-.48f),new Vector3(.76f,1.12f,.025f),A.Plastic);
            for(int row=0;row<4;row++) for(int col=0;col<4;col++)
            {
                A.Round(t,"Visible nutrient can",new Vector3(-.44f+col*.18f,.79f+row*.26f,-.51f),new Vector3(.115f,.085f,.115f),(col+row)%2==0?A.Brass:A.Upholstery);
                A.Box(t,"Product shelf",new Vector3(-.15f,.69f+row*.26f,-.53f),new Vector3(.75f,.025f,.1f),A.Metal);
            }
            A.Box(t,"Product safety glass",new Vector3(-.15f,1.24f,-.59f),new Vector3(.78f,1.14f,.012f),A.Glass,false,false);
            A.Box(t,"Reader display",new Vector3(.43f,1.5f,-.49f),new Vector3(.18f,.17f,.022f),A.Screen);
            for(int i=0;i<6;i++) A.Box(t,"Selection button",new Vector3(.38f+i%2*.09f,1.25f-i/2*.1f,-.5f),new Vector3(.055f,.048f,.03f),A.Plastic);
            A.Box(t,"Delivery chute",new Vector3(0,.37f,-.49f),new Vector3(.83f,.25f,.07f),A.Metal);
            A.Label(t,"NOURISH / 9",new Vector3(0,1.88f,-.485f),.056f);
            VendingDetails(t);
        }
        void Task(Transform t,int room,OfficeTask activity,Vector3 p,bool seated,float yaw=0)
        {
            Transform point=A.Group(t,"Task / "+activity,p,yaw); OfficeTaskPoint task=point.gameObject.AddComponent<OfficeTaskPoint>();
            task.RoomId=room; task.Activity=activity; task.Seated=seated;
        }
    }
}
