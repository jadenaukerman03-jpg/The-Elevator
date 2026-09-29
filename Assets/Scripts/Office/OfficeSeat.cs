using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    // A place anyone can sit: desk and boardroom chairs, each sofa cushion, armchairs. The transform sits on the
    // floor under the sitter's hips and its +Z is the way the sitter faces. Every seat surface is BeanRig.SeatSurface high.
    public sealed class OfficeSeat : MonoBehaviour
    {
        public static readonly List<OfficeSeat> All=new List<OfficeSeat>();
        public WorkerController Sitter;
        void OnEnable(){All.Add(this);}
        void OnDisable(){All.Remove(this);}
        // The employee station on this seat, if any: a seated player reserves it so its owner waits.
        public OfficeTaskPoint Station(OfficeFloor office)
        {
            if(!office)return null;
            return office.Stations.Find(s=>s&&s.Seated&&(s.transform.position-transform.position).sqrMagnitude<.3f*.3f);
        }
        public bool Free(OfficeFloor office)
        {
            if(Sitter)return false;
            if(office)foreach(OfficeEmployee employee in office.Employees)
            {
                Vector3 delta=employee.transform.position-transform.position;delta.y=0;
                if(delta.sqrMagnitude<.45f*.45f)return false;
            }
            return true;
        }
        // The seat you are looking at: nearest free seat to the aim point, within reach.
        public static OfficeSeat Aimed(Vector3 eye,Vector3 aim,OfficeFloor office)
        {
            OfficeSeat best=null;float bestDistance=.8f;
            foreach(OfficeSeat seat in All)
            {
                if(!seat)continue;
                Vector3 cushion=seat.transform.position+Vector3.up*BeanRig.SeatSurface;
                float distance=Vector3.Distance(cushion,aim);
                if(distance<bestDistance&&Vector3.Distance(eye,cushion)<2.6f&&seat.Free(office)){best=seat;bestDistance=distance;}
            }
            return best;
        }
    }
}
