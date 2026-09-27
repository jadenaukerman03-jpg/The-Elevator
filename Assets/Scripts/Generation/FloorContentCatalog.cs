using System;
using UnityEngine;

namespace TheElevator.Generation
{
    [Serializable]
    public sealed class ModulePrefabOverride
    {
        public string ModuleId;
        [Tooltip("Decorative content only. Shell, doorways, stairs, and clear transit lanes belong to the geometry resolver.")]
        public GameObject InteriorPrefab;
    }

    [CreateAssetMenu(menuName = "The Elevator/Floor content catalog")]
    public sealed class FloorContentCatalog : ScriptableObject
    {
        public string ThemeId = "civic-works";
        [TextArea(3,12)] public string ThemePayload;
        [Tooltip("Bump when authored geometry or prefab content changes, so old generation records can be rejected safely.")]
        public int ThemeRevision = 1;
        public ModuleSpec[] Modules = MapRecipe.DefaultModules();
        public DistrictSpec[] Districts = MapRecipe.DefaultDistricts();
        public ModulePrefabOverride[] PrefabOverrides = new ModulePrefabOverride[0];
        public Color WallColor = new Color(0.27f, 0.34f, 0.33f);
        public Color FloorColor = new Color(0.20f, 0.24f, 0.23f);
        public Color RecordsAccent = new Color(0.95f, 0.67f, 0.21f);
        public Color UtilitiesAccent = new Color(0.21f, 0.67f, 0.78f);
        public Color DispatchAccent = new Color(0.83f, 0.35f, 0.25f);

        public GameObject FindPrefab(string id)
        {
            foreach (ModulePrefabOverride entry in PrefabOverrides)
                if (entry != null && entry.ModuleId == id) return entry.InteriorPrefab;
            return null;
        }
    }
}
