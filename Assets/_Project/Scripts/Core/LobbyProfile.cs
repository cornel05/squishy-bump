using UnityEngine;

namespace SquishyBump.Core
{
    [CreateAssetMenu(menuName = "Squishy Bump/Lobby Profile")]
    public sealed class LobbyProfile : ScriptableObject
    {
        [SerializeField] private ClassProfile walkingClass;
        [SerializeField] private float countdownSeconds = 5;
        [SerializeField] private Vector3 firstSpawn = new(-5, 0, -6);
        [SerializeField] private float spawnSpacing = 2;
        [SerializeField] private float deskReach = 3;
        public ClassProfile WalkingClass => walkingClass;
        public float CountdownSeconds => countdownSeconds;
        public Vector3 Spawn(int index) => firstSpawn + Vector3.right * (index * spawnSpacing);
        public float DeskReach => deskReach;
    }
}
