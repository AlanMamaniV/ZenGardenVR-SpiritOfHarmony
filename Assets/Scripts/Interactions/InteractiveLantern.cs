using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZenGardenVR.Missions;
using ZenGardenVR.UI;

namespace ZenGardenVR.Interactions
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractiveLantern : MonoBehaviour
    {
        [Header("Lantern Settings")]
        [SerializeField] private Light lanternLight;
        [SerializeField] private ParticleSystem flameParticles;
        [SerializeField] private MeshRenderer lightCoverRenderer;
        [SerializeField] private Material litMaterial;
        [SerializeField] private Material unlitMaterial;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip igniteSound;
        [SerializeField] private AudioClip extinguishSound;

        [Header("Info Display")]
        [SerializeField] private string itemName = "Farol Japonés Ancestral (Tōrō)";
        [SerializeField] [TextArea] private string itemDescription = "Farol de piedra tradicional. Ilumina el camino de los espíritus errantes hacia la armonía.";

        [Header("Initial State")]
        [SerializeField] private bool isLit = false;

        private XRBaseInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnSelectEntered);
                interactable.activated.AddListener(OnActivated);
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.hoverExited.AddListener(OnHoverExited);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelectEntered);
                interactable.activated.RemoveListener(OnActivated);
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
                interactable.hoverExited.RemoveListener(OnHoverExited);
            }
        }

        private void Start()
        {
            ApplyState(isLit, false);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            ToggleState();
        }

        private void OnActivated(ActivateEventArgs args)
        {
            ToggleState();
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ShowInspection(itemName, itemDescription, isLit ? "Seleccionar: Apagar" : "Seleccionar: Encender fuego sagrado");
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ClearInspection();
            }
        }

        public void ToggleState()
        {
            isLit = !isLit;
            ApplyState(isLit, true);

            if (isLit && MissionManager.Instance != null)
            {
                MissionManager.Instance.CompleteLantern();
            }

            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ShowInspection(itemName, itemDescription, isLit ? "Seleccionar: Apagar" : "Seleccionar: Encender");
            }
        }

        private void ApplyState(bool lit, bool playFx)
        {
            if (lanternLight != null)
            {
                lanternLight.enabled = lit;
            }

            if (flameParticles != null)
            {
                if (lit) flameParticles.Play();
                else flameParticles.Stop();
            }

            if (lightCoverRenderer != null)
            {
                if (lit && litMaterial != null)
                {
                    lightCoverRenderer.material = litMaterial;
                }
                else if (!lit && unlitMaterial != null)
                {
                    lightCoverRenderer.material = unlitMaterial;
                }
            }

            if (playFx && audioSource != null)
            {
                AudioClip clip = lit ? igniteSound : extinguishSound;
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }
    }
}
