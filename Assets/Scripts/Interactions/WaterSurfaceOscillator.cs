using UnityEngine;

namespace ZenGardenVR.Interactions
{
    public class WaterSurfaceOscillator : MonoBehaviour
    {
        [SerializeField] private float waveSpeed = 1.2f;
        [SerializeField] private float waveHeight = 0.025f;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.localPosition;
        }

        private void Update()
        {
            float newY = startPos.y + Mathf.Sin(Time.time * waveSpeed) * waveHeight;
            transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        }
    }
}
