using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZenGardenVR.Missions;
using ZenGardenVR.UI;

namespace ZenGardenVR.Interactions
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractiveBonsai : MonoBehaviour
    {
        [Header("Bonsai Settings")]
        [SerializeField] private ParticleSystem petalParticles;
        [SerializeField] private Animator bonsaiAnimator;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip chimeSound;
        [SerializeField] private Transform branchRoot;

        [Header("Info Display")]
        [SerializeField] private string itemName = "Bonsái Sagrado de los Ancestros";
        [SerializeField] [TextArea] private string itemDescription = "Pino y cerezo milenario moldeado por generaciones de monjes. Al interactuar con él, despierta su florecimiento espiritual.";

        private XRBaseInteractable interactable;
        private bool isBlossomed = false;
        private float swayTimer = 0f;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnSelect);
                interactable.hoverEntered.AddListener(OnHover);
                interactable.hoverExited.AddListener(OnHoverExit);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelect);
                interactable.hoverEntered.RemoveListener(OnHover);
                interactable.hoverExited.RemoveListener(OnHoverExit);
            }
        }

        private void Update()
        {
            if (branchRoot != null)
            {
                swayTimer += Time.deltaTime * 1.5f;
                float angle = Mathf.Sin(swayTimer) * 1.5f;
                branchRoot.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void OnHover(HoverEnterEventArgs args)
        {
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ShowInspection(itemName, itemDescription, isBlossomed ? "Seleccionar: Hacer florecer de nuevo" : "Seleccionar: Despertar pétalos de cerezo");
            }
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ClearInspection();
            }
        }

        private void OnSelect(SelectEnterEventArgs args)
        {
            TriggerBlossom();
        }

        public void TriggerBlossom()
        {
            isBlossomed = true;

            if (petalParticles != null)
            {
                petalParticles.Stop();
                petalParticles.Play();
            }

            if (bonsaiAnimator != null)
            {
                bonsaiAnimator.SetTrigger("Blossom");
            }

            if (audioSource != null && chimeSound != null)
            {
                audioSource.PlayOneShot(chimeSound);
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.CompleteBonsai();
            }

            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ShowInspection(itemName, itemDescription, "¡Bonsái en floración espiritual!");
            }
        }
    }
}
