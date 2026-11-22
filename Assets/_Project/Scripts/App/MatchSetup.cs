using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>What App needs to put a match together: profiles, the five levels and the roster prefabs.</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Match Setup")]
    public sealed class MatchSetup : ScriptableObject
    {
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private BotProfile defaultBotProfile;
        [SerializeField] private LobbyProfile lobby;
        [SerializeField] private SquishyBump.Presentation.PresentationProfile presentation;
        [SerializeField] private LevelDefinition[] levels;
        [Tooltip("Roster slots in order: host player, remote players, then idle slots.")]
        [SerializeField] private GameObject[] roster;
        [SerializeField] private string mainMenuScene;

        [Header("Arena card and banners (GDD B.3, B.6, 11.2)")]
        [Tooltip("Card line per level, L1 first (B02–B06).")]
        [SerializeField] private string[] cardLines;
        [Tooltip("The card shows this long before the countdown (2–3 s).")]
        [SerializeField] private float cardSeconds;
        [SerializeField] private float bannerSeconds;
        [Tooltip("B07 on L2–L5: banner when this much match time is left.")]
        [SerializeField] private float finalWarningSeconds;

        public BalanceProfile Balance => balance;
        public BotProfile DefaultBotProfile => defaultBotProfile;
        public LobbyProfile Lobby => lobby;
        public SquishyBump.Presentation.PresentationProfile Presentation => presentation;
        public LevelDefinition[] Levels => levels;
        public GameObject[] Roster => roster;
        public string MainMenuScene => mainMenuScene;
        public string CardLine(int level) => cardLines != null && level >= 1 && level <= cardLines.Length ? cardLines[level - 1] : "";
        public float CardSeconds => cardSeconds;
        public float BannerSeconds => bannerSeconds;
        public float FinalWarningSeconds => finalWarningSeconds;
    }
}
