using System;
using TheElevator;

public static class RulesChecks
{
    static int checks;
    static void Assert(bool condition, string description)
    {
        if (!condition) throw new Exception("FAIL: " + description);
        checks++;
        Console.WriteLine("PASS: " + description);
    }
    static bool Near(float a, float b) { return Math.Abs(a - b) < 0.001f; }
    public static int Main()
    {
        Assert(Near(RunRules.DepartureCost(180), 24), "At capacity uses base power");
        Assert(RunRules.CanDepart(24, 180), "Exact power budget permits departure");
        Assert(!RunRules.CanDepart(23.99f, 180), "Insufficient power blocks departure");
        Assert(RunRules.DepartureCost(240) > RunRules.DepartureCost(180), "Overload costs extra power");
        Assert(RunRules.DoorSeconds(240) > RunRules.DoorSeconds(180), "Overload slows doors");
        Assert(Near(RunRules.DepartureCost(70), RunRules.DepartureCost(180)), "Light loads do not pay an overload penalty");
        Assert(RunRules.CarrySpeed(85) < RunRules.CarrySpeed(18), "Heavy safe is slower than briefcase");
        Assert(RunRules.CarrySpeed(500) > 0, "Even extreme weight cannot immobilize a player");
        Assert(Near(RunRules.Recharge(90), 100), "Recharge cannot exceed capacity");
        Assert(Near(RunRules.Recharge(0), 32), "Power cell restores expected charge");
        float power = RunRules.StartingPower;
        for (int floor = 0; floor < RunRules.FloorCount; floor++)
        {
            if (floor == 1) power = RunRules.Recharge(power);
            Assert(RunRules.CanDepart(power, 180), "One battery makes a normal run solvable: floor " + (floor + 1));
            power -= RunRules.DepartureCost(180);
        }
        Assert(RunRules.StartingPower < RunRules.DepartureCost(180) * RunRules.FloorCount,
            "Skipping all batteries creates a real resource failure");
        Assert(RunRules.Grade(1100) != RunRules.Grade(0), "Recovered cargo changes completion grade");
        Console.WriteLine(checks + " checks passed.");
        return 0;
    }
}
