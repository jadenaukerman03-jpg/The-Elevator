using UnityEngine;

namespace TheElevator.Generation
{
    public sealed class GenerationSocket : MonoBehaviour
    {
        public string StableId;
        public SocketKind Kind;
        public int RoomId, ContentSeed;
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Kind == SocketKind.Objective ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.35f);
        }
    }
}
