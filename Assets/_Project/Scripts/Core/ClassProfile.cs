using UnityEngine;

namespace SquishyBump.Core
{
    public enum FruitWeightClass { Normal, Heavy, Light }
    /// <summary>Weight-class values of GDD 6.1 (Heavy / Normal / Light).</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Class Profile")]
    public sealed class ClassProfile : ScriptableObject
    {
        [SerializeField] private FruitWeightClass category;
        [SerializeField] private float mass;
        [SerializeField] private float radius;
        [SerializeField] private float maxSpeed;
        [Tooltip("Traction factor c in a_max = c·μ·g.")]
        [SerializeField] private float traction;

        public float Mass => mass;
        public float Radius => radius;
        public float MaxSpeed => maxSpeed;
        public float Traction => traction;
        public FruitWeightClass Category => category;
    }
}
