using UnityEngine;

namespace SquishyBump.Presentation
{
    /// <summary>Gentle bob and slow spin for decorative floats on water. Presentation only.</summary>
    public sealed class FloatBob : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.15f;
        [SerializeField] private float speed = 1.2f;
        [SerializeField] private float spinDegPerSec = 8f;
        [SerializeField] private float phase;

        private Vector3 basePos;

        public void Configure(float bobAmplitude, float bobSpeed, float spinDegreesPerSecond, float phaseOffset)
        {
            amplitude = bobAmplitude;
            speed = bobSpeed;
            spinDegPerSec = spinDegreesPerSecond;
            phase = phaseOffset;
        }

        private void Start() => basePos = transform.position;

        private void Update()
        {
            transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * speed + phase) * amplitude);
            transform.Rotate(0f, spinDegPerSec * Time.deltaTime, 0f, Space.World);
        }
    }
}
