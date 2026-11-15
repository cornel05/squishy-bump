using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>
    /// Hazard timings and telegraphs of GDD chapter 7 that are not in the 6.1 table.
    /// Ring drop times and warning lead stay in <see cref="BalanceProfile"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Hazard Profile")]
    public sealed class HazardProfile : ScriptableObject
    {
        [Header("H01 Ring drop (GDD 7.1)")]
        [Tooltip("Last part of the warning where the seam turns red and blinks.")]
        [SerializeField] private float ringDropFinalWarningSeconds;
        [SerializeField] private float telegraphBlinkHz;

        public float RingDropFinalWarningSeconds => ringDropFinalWarningSeconds;
        public float TelegraphBlinkHz => telegraphBlinkHz;
    }
}
