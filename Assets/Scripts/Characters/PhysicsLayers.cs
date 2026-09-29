using UnityEngine;

namespace TheElevator
{
    // Physics layers used by the game, and who collides with whom.
    //   2  Ignore Raycast: the player's own capsule (interaction rays pass through it).
    //   8  Employees.
    //   9  Seat furniture (chairs): solid for players, ignored by employees, who sit in them.
    //  10  Ragdolls: bodies lying on the floor; they rest on the world but nobody trips over them.
    public static class PhysicsLayers
    {
        public const int Player = 2, Employees = 8, Seats = 9, Ragdolls = 10;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Configure()
        {
            Physics.IgnoreLayerCollision(Employees, Seats, true);
            Physics.IgnoreLayerCollision(Ragdolls, Player, true);
            Physics.IgnoreLayerCollision(Ragdolls, Employees, true);
            Physics.IgnoreLayerCollision(Ragdolls, Ragdolls, true);
        }
    }
}
