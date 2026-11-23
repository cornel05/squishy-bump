using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>Dữ liệu H06, GDD 7.4–7.5. Editor tạo asset; runtime không thay balance.</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Ice Profile")]
    public sealed class IceProfile : ScriptableObject
    {
        [SerializeField] private int rings = 4;
        [SerializeField] private float flatToFlat = 2f;
        [SerializeField] private int minimumSolid = 37;
        [SerializeField] private float waveSeconds = 3f;
        [SerializeField] private float chaosWaveSeconds = 3.5f;
        [SerializeField] private float secondPhaseSeconds = 30f;
        [SerializeField] private float thirdPhaseSeconds = 60f;
        [SerializeField] private float chaosPhaseSeconds = 45f;
        [SerializeField] private int firstWaveCount = 2;
        [SerializeField] private int secondWaveCount = 3;
        [SerializeField] private int thirdWaveCount = 4;
        [SerializeField] private float fighterWeight = 1.5f;
        [SerializeField] private float crackSeconds = 1.5f;
        [SerializeField] private float returnSeconds = 10f;
        [SerializeField] private float fadeSeconds = 1f;
        [Header("Chỉ trình bày, không thay hình học mặt trên")]
        [SerializeField] private float thickness = 0.25f;
        [SerializeField] private float shakeMeters = 0.025f;
        public int Rings => rings;
        public float FlatToFlat => flatToFlat;
        public int MinimumSolid => minimumSolid;
        public float WaveSeconds => waveSeconds;
        public float ChaosWaveSeconds => chaosWaveSeconds;
        public float SecondPhaseSeconds => secondPhaseSeconds;
        public float ThirdPhaseSeconds => thirdPhaseSeconds;
        public float ChaosPhaseSeconds => chaosPhaseSeconds;
        public int FirstWaveCount => firstWaveCount;
        public int SecondWaveCount => secondWaveCount;
        public int ThirdWaveCount => thirdWaveCount;
        public float FighterWeight => fighterWeight;
        public float CrackSeconds => crackSeconds;
        public float ReturnSeconds => returnSeconds;
        public float FadeSeconds => fadeSeconds;
        public float Thickness => thickness;
        public float ShakeMeters => shakeMeters;
    }
}
