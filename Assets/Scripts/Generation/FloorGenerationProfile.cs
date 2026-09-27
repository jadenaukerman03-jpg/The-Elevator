using UnityEngine;

namespace TheElevator.Generation
{
    [CreateAssetMenu(menuName = "The Elevator/Generation profile")]
    public sealed class FloorGenerationProfile : ScriptableObject
    {
        public GenerationSettings Settings = new GenerationSettings();
    }
}
