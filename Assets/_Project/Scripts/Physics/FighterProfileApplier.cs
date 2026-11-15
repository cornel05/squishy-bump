using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Physics
{
    /// <summary>
    /// Applies the weight class of the picked fruit at spawn: mass, radius, and through the motor
    /// v_max and traction (PF02). The class never changes afterwards.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class FighterProfileApplier : MonoBehaviour
    {
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private FruitDefinition fruit;

        private bool applied;

        public FruitDefinition Fruit => fruit;

        private void Awake()
        {
            if (fruit != null) Apply(fruit);
        }

        /// <summary>For a spawner that picks the fruit at runtime. Ignored once a fruit is applied.</summary>
        public void Apply(FruitDefinition picked)
        {
            if (applied) return;
            applied = true;
            fruit = picked;

            var fighterClass = picked.Class;
            GetComponent<Rigidbody>().mass = fighterClass.Mass;
            GetComponent<SphereCollider>().radius = fighterClass.Radius;
            GetComponent<PlayerMotor>().Configure(balance, fighterClass);
        }
    }
}
