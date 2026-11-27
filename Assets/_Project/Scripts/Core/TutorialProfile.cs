using UnityEngine;

namespace SquishyBump.Core
{
    public enum TutorialStep { Move, Jump, Dash, ClassCard, Push, Completed, Skipped }

    /// <summary>Bố trí đồ tập; các số này không thay balance của motor hay trận thật.</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Tutorial Profile")]
    public sealed class TutorialProfile : ScriptableObject
    {
        [SerializeField] private Vector3 trainingPoint = Vector3.zero;
        [SerializeField] private Vector3 moveMarker = new(0, 0, 3);
        [SerializeField] private Vector3 dashMarker = new(0, 0, 6);
        [SerializeField] private float markerRadius = 1f;
        [SerializeField] private float toyInset = 1.5f;
        [SerializeField] private float approachGap = 2.5f;
        public Vector3 TrainingPoint => trainingPoint;
        public Vector3 MoveMarker => moveMarker;
        public Vector3 DashMarker => dashMarker;
        public float MarkerRadius => markerRadius;
        public float ToyInset => toyInset;
        public float ApproachGap => approachGap;
    }
}
