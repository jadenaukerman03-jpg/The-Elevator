using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    public static class OfficeNavigation
    {
        // Navigation queries ignore people and movable doors; door passage is reserved separately.
        const int Mask=~((1<<2)|(1<<8));
        static readonly Collider[] overlaps=new Collider[32];
        static OfficeEmployee planning;
        public static bool Clear(Vector3 point)
        {
            if(planning)foreach(OfficeEmployee other in planning.Office.Employees)
                if(other!=planning&&!other.Travelling&&other.AtStation&&Mathf.Abs(other.transform.position.y-point.y)<.6f&&Vector3.Distance(point,other.transform.position)<.76f)return false;
            int count=Physics.OverlapCapsuleNonAlloc(point+Vector3.up*.36f,point+Vector3.up*1.49f,.33f,overlaps,Mask,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length)return false;
            for(int i=0;i<count;i++)if(!overlaps[i].GetComponentInParent<OfficeDoor>())return false;
            return true;
        }
        public static bool Segment(Vector3 a,Vector3 b)
        {
            Vector3 delta=b-a;if(delta.sqrMagnitude<.002f)return true;
            if(planning)foreach(OfficeEmployee other in planning.Office.Employees)
            {
                if(other==planning||other.Travelling||!other.AtStation||Mathf.Abs(other.transform.position.y-a.y)>.6f)continue;
                float t=Mathf.Clamp01(Vector3.Dot(other.transform.position-a,delta)/delta.sqrMagnitude);
                if(Vector3.Distance(a+delta*t,other.transform.position)<.76f)return false;
            }
            foreach(RaycastHit hit in Physics.CapsuleCastAll(a+Vector3.up*.36f,a+Vector3.up*1.49f,.33f,delta.normalized,delta.magnitude,Mask,QueryTriggerInteraction.Ignore))
                if(!hit.collider.GetComponentInParent<OfficeDoor>())return false;
            return true;
        }
        public static List<int> RoomPath(OfficeEmployee employee,int from,int to)
        {
            OfficeFloor office=employee.Office;int count=office.Plan.Rooms.Count;
            float[] cost=new float[count];int[] previous=new int[count];bool[] closed=new bool[count];
            for(int i=0;i<count;i++){cost[i]=float.MaxValue;previous[i]=-1;}cost[from]=0;
            for(int step=0;step<count;step++)
            {
                int best=-1;for(int i=0;i<count;i++)if(!closed[i]&&(best<0||cost[i]<cost[best]))best=i;
                if(best<0||cost[best]==float.MaxValue)break;if(best==to)break;closed[best]=true;
                foreach(int next in office.Map.Manifest.Neighbors(best))
                {
                    if(next==office.Plan.TargetRoom||closed[next])continue;
                    float noise=((employee.EmployeeId*37+next*13)%17)*.035f;
                    float candidate=cost[best]+1+office.TravellersInRoom(next)*2+noise;
                    if(candidate<cost[next]){cost[next]=candidate;previous[next]=best;}
                }
            }
            List<int> path=new List<int>();if(cost[to]==float.MaxValue)return path;
            for(int at=to;at>=0;at=previous[at]){path.Add(at);if(at==from)break;}path.Reverse();return path;
        }
        public static bool Build(OfficeEmployee employee,OfficeTaskPoint target,List<Vector3> result)
        {
            planning=employee;
            try{return BuildRoute(employee,target,result);}finally{planning=null;}
        }
        static bool BuildRoute(OfficeEmployee employee,OfficeTaskPoint target,List<Vector3> result)
        {
            result.Clear();OfficeFloor office=employee.Office;int from=office.Map.NearestRoom(employee.transform.position).Id;
            List<int> rooms=RoomPath(employee,from,target.RoomId);if(rooms.Count==0)return false;
            List<Vector3> coarse=new List<Vector3>();coarse.Add(employee.transform.position);
            coarse.Add(office.Map.Anchor(office.Map.Manifest.Rooms[from]));
            for(int i=1;i<rooms.Count;i++)coarse.AddRange(office.Map.Route(rooms[i-1],rooms[i]));
            coarse.Add(office.Map.Anchor(office.Map.Manifest.Rooms[target.RoomId]));coarse.Add(target.transform.position);
            Vector3 last=coarse[0];
            for(int i=1;i<coarse.Count;i++)
            {
                Vector3 goal=coarse[i];
                // Keep right on wide, level corridor sections. Opposing traffic gets a different lane.
                if(i<coarse.Count-1&&Mathf.Abs(goal.y-last.y)<.2f)
                {
                    Vector3 travel=goal-last;travel.y=0;
                    Vector3 lane=goal+Vector3.Cross(Vector3.up,travel.normalized)*(.38f+employee.EmployeeId%3*.045f);
                    if(Clear(lane))goal=lane;
                }
                if((goal-last).sqrMagnitude<.03f)continue;
                if(Segment(last,goal)){result.Add(goal);last=goal;continue;}
                if(Mathf.Abs(goal.y-last.y)>.3f)return false;
                List<Vector3> detour=LocalPath(last,goal);if(detour==null){result.Clear();return false;}
                result.AddRange(detour);last=goal;
            }
            return result.Count>0;
        }
        // Bounded local A*: furniture detours are searched within the current segment, not the whole map.
        static List<Vector3> LocalPath(Vector3 start,Vector3 end)
        {
            const float cell=.45f;Vector3 min=new Vector3(Mathf.Min(start.x,end.x)-3,start.y,Mathf.Min(start.z,end.z)-3);
            int width=Mathf.CeilToInt((Mathf.Abs(start.x-end.x)+6)/cell)+1,height=Mathf.CeilToInt((Mathf.Abs(start.z-end.z)+6)/cell)+1;
            if(width*height>3600)return null;
            int sx=Mathf.RoundToInt((start.x-min.x)/cell),sz=Mathf.RoundToInt((start.z-min.z)/cell),ex=Mathf.RoundToInt((end.x-min.x)/cell),ez=Mathf.RoundToInt((end.z-min.z)/cell);
            int source=sx+sz*width,dest=ex+ez*width,n=width*height;
            float[] cost=new float[n];int[] prev=new int[n];bool[] closed=new bool[n];sbyte[] clear=new sbyte[n];List<int> open=new List<int>{source};
            for(int i=0;i<n;i++){cost[i]=float.MaxValue;prev[i]=-1;}cost[source]=0;
            for(int iteration=0;iteration<1800&&open.Count>0;iteration++)
            {
                int pick=0;float score=float.MaxValue;
                for(int i=0;i<open.Count;i++){int p=open[i];float f=cost[p]+Mathf.Abs(p%width-ex)+Mathf.Abs(p/width-ez);if(f<score){score=f;pick=i;}}
                int current=open[pick];open.RemoveAt(pick);if(closed[current])continue;closed[current]=true;
                Vector3 here=current==source?start:min+new Vector3(current%width*cell,0,current/width*cell);
                if(current==dest||Vector3.Distance(here,end)<.7f&&Segment(here,end))
                {
                    List<Vector3> path=new List<Vector3>{end};for(int p=current;p!=source&&p>=0;p=prev[p])path.Add(min+new Vector3(p%width*cell,0,p/width*cell));path.Reverse();return path;
                }
                for(int d=0;d<4;d++)
                {
                    int x=current%width+(d==0?1:d==1?-1:0),z=current/width+(d==2?1:d==3?-1:0);
                    if(x<0||z<0||x>=width||z>=height)continue;int next=x+z*width;if(closed[next])continue;
                    Vector3 point=min+new Vector3(x*cell,0,z*cell);
                    if(clear[next]==0)clear[next]=(sbyte)(Clear(point)?1:-1);
                    if(clear[next]<0||cost[next]<=cost[current]+1||!Segment(here,point))continue;
                    cost[next]=cost[current]+1;prev[next]=current;open.Add(next);
                }
            }
            return null;
        }
    }
}


