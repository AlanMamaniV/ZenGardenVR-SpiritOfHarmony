using UnityEngine;
using ZenGardenVR.Core;
using ZenGardenVR.Missions;

namespace ZenGardenVR.Interactions
{
    public class AltarZone : MonoBehaviour
    {
        [Header("Altar Settings")]
        [SerializeField] private Transform stoneSnapPoint;
        [SerializeField] private ParticleSystem altarParticles;
        [SerializeField] private Light altarLight;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip activationSound;

        [Header("State")]
        [SerializeField] private bool isActivated = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isActivated) return;

            MagicStone stone = other.GetComponent<MagicStone>();
            if (stone == null)
            {
                stone = other.GetComponentInParent<MagicStone>();
            }

            if (stone != null && !stone.IsPlaced)
            {
                ActivateAltar(stone);
            }
        }

        private void ActivateAltar(MagicStone stone)
        {
            isActivated = true;
            stone.PlaceOnAltar(stoneSnapPoint != null ? stoneSnapPoint : transform);

            if (altarParticles != null)
            {
                altarParticles.Play();
            }

            if (altarLight != null)
            {
                altarLight.enabled = true;
            }

            if (audioSource != null && activationSound != null)
            {
                audioSource.PlayOneShot(activationSound);
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.CompleteMagicStone();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowNotification("¡Has canalizado la energía espiritual en el Altar!");
            }
        }
    }
}
