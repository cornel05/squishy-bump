using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>What App needs to put a match together: profiles, the five levels and the roster prefabs.</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Match Setup")]
    public sealed class MatchSetup : ScriptableObject
    {
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private SquishyBump.Presentation.PresentationProfile presentation;
        [SerializeField] private LevelDefinition[] levels;
        [Tooltip("Roster slots in order: host player, remote players, then idle slots.")]
        [SerializeField] private GameObject[] roster;
        [SerializeField] private string mainMenuScene;

        public BalanceProfile Balance => balance;
        public SquishyBump.Presentation.PresentationProfile Presentation => presentation;
        public LevelDefinition[] Levels => levels;
        public GameObject[] Roster => roster;
        public string MainMenuScene => mainMenuScene;
    }
}
