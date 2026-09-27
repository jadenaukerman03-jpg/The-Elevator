using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using TheElevator.Generation;

public static class GenerationChecks
{
    static int checks;
    static void Check(bool pass, string label) { checks++; if (!pass) throw new Exception("FAIL: " + label); }
    public static int Main()
    {
        Stopwatch watch = Stopwatch.StartNew();
        int maps = 0, retries = 0;
        foreach (MapSize size in Enum.GetValues(typeof(MapSize)))
        {
            HashSet<string> signatures = new HashSet<string>();
            for (int seed = -20; seed < 80; seed++)
            {
                MapRecipe recipe = MapRecipe.Default(seed, size);
                MapManifest a = new MacroLayoutGenerator().Generate(recipe);
                MapManifest b = new MacroLayoutGenerator().Generate(recipe);
                Check(a.StructureHash == b.StructureHash && a.ContentHash == b.ContentHash, "Deterministic replay " + size + " / " + seed);
                Check(MapValidator.Validate(a).Count == 0, "All topology constraints " + size + " / " + seed);
                foreach (MapRoom room in a.Rooms)
                {
                    Check(a.FindPath(0, room.Id).Count > 0, "Every room has an extraction route");
                    if (room.Has(RoomRole.Objective)) Check(room.Distance >= recipe.Settings.ObjectiveMinDistance, "Distant objective");
                }
                signatures.Add(a.StructureHash); maps += 2; retries += a.Attempt;
            }
            Check(signatures.Count >= 95, "Seed variation creates different layouts");
            Console.WriteLine("PASS: " + size + " — 100 seeds, deterministic replay, connectivity, loops, zones, vertical links, socket IDs.");
        }
        MapRecipe original = MapRecipe.Default(12345, MapSize.Standard);
        MapManifest before = new MacroLayoutGenerator().Generate(original);
        original.Settings.PropDensityPercent = 0;
        original.Settings.HazardDensityPercent = 0;
        original.Settings.EnemySpawnCapacity = 0;
        MapManifest after = new MacroLayoutGenerator().Generate(original);
        Check(before.StructureHash == after.StructureHash, "Decorative/content settings cannot change structural layout");
        Check(before.ConfigurationHash != after.ConfigurationHash, "Configuration fingerprints include content settings");
        Check(before.ContentHash != after.ContentHash, "Content changes are fingerprinted separately");
        CultureInfo old = CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            MapManifest french = new MacroLayoutGenerator().Generate(original);
            Check(after.StructureHash == french.StructureHash && after.ConfigurationHash == french.ConfigurationHash, "Locale-independent replay");
        }
        finally { System.Threading.Thread.CurrentThread.CurrentCulture = old; }
        Array.Reverse(original.Modules);
        MapManifest reordered = new MacroLayoutGenerator().Generate(original);
        Check(after.StructureHash == reordered.StructureHash && after.ConfigurationHash == reordered.ConfigurationHash, "Catalog ordering does not change map");
        MapRecipe themed = MapRecipe.Default(12345,MapSize.Standard);
        themed.Districts = new[] { new DistrictSpec { Id = "archive", DisplayName = "THE ARCHIVE", ModuleIds = new[] { "records" } } };
        MapManifest archive = new MacroLayoutGenerator().Generate(themed);
        foreach (MapRoom room in archive.Rooms)
        {
            Check(room.DistrictId == "archive" && !room.WetFloor,"New department is data-driven");
            if (!room.IsStair && !room.Has(RoomRole.Entrance | RoomRole.Landmark)) Check(room.ModuleId == "records","Department restricts ordinary modules");
        }
        Check(archive.ConfigurationHash != before.ConfigurationHash,"Theme selection participates in fingerprint");
        themed.Settings.CellSizeMillimeters = 18000;
        MapManifest wide = new MacroLayoutGenerator().Generate(themed);
        Check(wide.EstimatedWalkableSquareMeters > archive.EstimatedWalkableSquareMeters,"Area arithmetic cannot overflow at supported cell sizes");
        MapRecipe invalid = MapRecipe.Default(1, MapSize.Standard); invalid.GenerationVersion = 999;
        bool rejected = false;
        try { new MacroLayoutGenerator().Generate(invalid); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Unknown generation version rejected");
        invalid = MapRecipe.Default(1, MapSize.Standard); invalid.Settings.DoorWidthMillimeters = 1000;
        rejected = false;
        try { new MacroLayoutGenerator().Generate(invalid); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Unsafe doorway clearance rejected");
        invalid = MapRecipe.Default(1, MapSize.Standard); invalid.Settings.OperationBudget = 100; invalid.Settings.MaxGenerationAttempts = 2;
        rejected = false;
        try { new MacroLayoutGenerator().Generate(invalid); } catch (InvalidOperationException e) { rejected = e.Message.Contains("2 bounded attempts"); }
        Check(rejected, "Impossible budget terminates with failed-seed diagnostics");
        Console.WriteLine("PASS: stream isolation, config fingerprints, catalog ordering, locale, invalid settings and bounded failures.");
        Console.WriteLine(checks + " assertions; " + maps + " maps; " + retries + " retry attempts; " + watch.ElapsedMilliseconds + " ms.");
        return 0;
    }
}
