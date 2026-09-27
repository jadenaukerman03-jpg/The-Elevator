using UnityEngine;

namespace TheElevator.Generation
{
    public sealed class ObjectiveTerminal : MonoBehaviour
    {
        public GeneratedFloor Floor;
        public string SocketId;
        public bool Complete { get; private set; }
        public void Use(DescentGame game)
        {
            if (Complete || !Floor) return;
            Complete = Floor.CompleteObjective(SocketId);
            if (!Complete) return;
            game.Notify("Survey recorded. " + Floor.CompletedObjectives + "/" + Floor.Manifest.Recipe.Settings.ObjectiveCount + " remote terminals checked.");
            game.Sound.Play(880, 0.25f, 0.14f);
        }
    }
}
