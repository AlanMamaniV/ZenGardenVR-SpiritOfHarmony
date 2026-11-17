using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZenGardenVR.Missions;
using ZenGardenVR.UI;

namespace ZenGardenVR.Interactions
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractiveFountain : MonoBehaviour
    {
        [Header("Fountain Components")]
        [SerializeField] private ParticleSystem waterStreamParticles;
        [SerializeField] private ParticleSystem splashParticles;
        [SerializeField] private AudioSource waterAudioSource;
        [SerializeField] private Transform bambooShishi;
        [SerializeField] private Light waterGlowLight;

        [Header("State")]
        [SerializeField] private bool isFlowing = true;

        [Header("Info Display")]
        [SerializeField] private string itemName = "Fuente Sagrada Tsukubai";
        [SerializeField] [TextArea] private string itemDescription = "Fuente de purificación tradicional de bambú y piedra. El agua purifica el corazón de quien visita el santuario.";

        private XRBaseInteractable interactable;
        private float shishiTimer = 0f;
        private bool shishiTilted = false;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (waterAudioSource == null) waterAudioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            ApplyFlowState(isFlowing);
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnSelect);
                interactable.activated.AddListener(OnActivate);
                interactable.hoverEntered.AddListener(OnHover);
                interactable.hoverExited.AddListener(OnHoverExit);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelect);
                interactable.activated.RemoveListener(OnActivate);
                interactable.hoverEntered.RemoveListener(OnHover);
                interactable.hoverExited.RemoveListener(OnHoverExit);
            }
        }

        private void Update()
        {
            if (isFlowing && bambooShishi != null)
            {
                shishiTimer += Time.deltaTime;
                if (shishiTimer > 3f)
                {
                    shishiTimer = 0f;
                    shishiTilted = !shishiTilted;
                }

                float targetRot = shishiTilted ? -25f : 0f;
                bambooShishi.localRotation = Quaternion.Slerp(bambooShishi.localRotation, Quaternion.Euler(targetRot, 0, 0), Time.deltaTime * 5f);
            }
        }

        private void OnHover(HoverEnterEventArgs args)
        {
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ShowInspection(itemName, itemDescription, isFlowing ? "Seleccionar: Detener flujo de agua" : "Seleccionar: Activar agua sagrada");
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
            ToggleWater();
        }

        private void OnActivate(ActivateEventArgs args)
        {
            ToggleWater();
        }

        public void ToggleWater()
        {
            isFlowing = !isFlowing;
            ApplyFlowState(isFlowing);

            if (isFlowing && MissionManager.Instance != null)
            {
                MissionManager.Instance.CompleteFountain();
            }

            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ShowInspection(itemName, itemDescription, isFlowing ? "Seleccionar: Detener agua" : "Seleccionar: Activar agua");
            }
        }

        private void ApplyFlowState(bool flow)
        {
            if (waterStreamParticles != null)
            {
                if (flow) waterStreamParticles.Play();
                else waterStreamParticles.Stop();
            }

            if (splashParticles != null)
            {
                if (flow) splashParticles.Play();
                else splashParticles.Stop();
            }

            if (waterAudioSource != null)
            {
                if (flow)
                {
                    if (!waterAudioSource.isPlaying) waterAudioSource.Play();
                }
                else
                {
                    waterAudioSource.Stop();
                }
            }

            if (waterGlowLight != null)
            {
                waterGlowLight.enabled = flow;
            }
        }
    }
}
