using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>One selectable fruit (GDD 12.2): its weight class, player colour and body mesh.</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Fruit Definition")]
    public sealed class FruitDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private ClassProfile fighterClass;
        [Tooltip("Body colour, also the player colour (name tag, scoreboard dot).")]
        [SerializeField] private Color32 color;
        [Tooltip("Unit sphere placeholder until the .glb prototype (A15) is imported.")]
        [SerializeField] private Mesh mesh;

        public string DisplayName => displayName;
        public ClassProfile Class => fighterClass;
        public Color32 Color => color;
        public Mesh Mesh => mesh;
    }
}
