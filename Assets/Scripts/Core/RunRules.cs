using System;

namespace TheElevator
{
    // Engine-independent balance rules: shared by the game and the command-line checks.
    public static class RunRules
    {
        public const float Capacity = 180f;
        public const float WorkerMass = 70f;
        public const float StartingPower = 68f;
        public const float BatteryCharge = 32f;
        public const float FloorSeconds = 110f;
        public const int FloorCount = 3;

        public static float Overload(float mass) { return Math.Max(0f, mass - Capacity); }
        public static float DepartureCost(float mass) { return 24f + Overload(mass) * 0.18f; }
        public static float DoorSeconds(float mass) { return 4f + Overload(mass) / 25f; }
        public static float CarrySpeed(float mass) { return Math.Max(0.42f, 1f - mass / 135f); }
        public static float Recharge(float power) { return Math.Min(100f, power + BatteryCharge); }
        public static bool CanDepart(float power, float mass) { return power >= DepartureCost(mass); }
        public static string Grade(int value)
        {
            if (value >= 1100) return "EMPLOYEE OF THE INCIDENT";
            if (value >= 650) return "SUSPICIOUSLY COMPETENT";
            if (value >= 300) return "PROBATION EXTENDED";
            return "AT LEAST YOU CAME BACK";
        }
    }
}
