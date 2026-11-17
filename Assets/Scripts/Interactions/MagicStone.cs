using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZenGardenVR.UI;

namespace ZenGardenVR.Interactions
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class MagicStone : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private MeshRenderer stoneRenderer;
        [SerializeField] private Color normalColor = new Color(0.4f, 0.7f, 1f, 1f);
        [SerializeField] private Color grabColor = new Color(0.9f, 0.5f, 1f, 1f);
        [SerializeField] private Color placedColor = new Color(0.4f, 1f, 0.6f, 1f);
        [SerializeField] private Light auraLight;
        [SerializeField] private ParticleSystem auraParticles;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip grabSound;
        [SerializeField] private AudioClip releaseSound;

        [Header("Info Display")]
        [SerializeField] private string itemName = "Piedra Mágica del Espíritu";
        [SerializeField] [TextArea] private string itemDescription = "Un mineral flotante cargado de energía espiritual. Vibra cuando se acerca a un Altar Zen.";

        private XRGrabInteractable grabInteractable;
        private Material materialInstance;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        private bool isPlaced = false;

        public bool IsPlaced => isPlaced;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (stoneRenderer != null)
            {
                materialInstance = stoneRenderer.material;
            }
        }

        private void Start()
        {
            UpdateColor(normalColor);
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrab);
                grabInteractable.selectExited.AddListener(OnRelease);
                grabInteractable.hoverEntered.AddListener(OnHover);
                grabInteractable.hoverExited.AddListener(OnHoverExit);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrab);
                grabInteractable.selectExited.RemoveListener(OnRelease);
                grabInteractable.hoverEntered.RemoveListener(OnHover);
                grabInteractable.hoverExited.RemoveListener(OnHoverExit);
            }
        }

        private void OnHover(HoverEnterEventArgs args)
        {
            if (VRUIManager.Instance != null && !isPlaced)
            {
                VRUIManager.Instance.ShowInspection(itemName, itemDescription, "Grip: Agarrar, Mover y Rotar con la mano.");
            }
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            if (VRUIManager.Instance != null && !grabInteractable.isSelected)
            {
                VRUIManager.Instance.ClearInspection();
            }
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            if (!isPlaced)
            {
                UpdateColor(grabColor);
                if (auraLight != null) auraLight.color = grabColor;
                if (audioSource != null && grabSound != null)
                {
                    audioSource.PlayOneShot(grabSound);
                }
            }
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            if (!isPlaced)
            {
                UpdateColor(normalColor);
                if (auraLight != null) auraLight.color = normalColor;
                if (audioSource != null && releaseSound != null)
                {
                    audioSource.PlayOneShot(releaseSound);
                }
            }
        }

        public void PlaceOnAltar(Transform snapPoint)
        {
            isPlaced = true;
            grabInteractable.enabled = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (snapPoint != null)
            {
                transform.position = snapPoint.position;
                transform.rotation = snapPoint.rotation;
                transform.SetParent(snapPoint);
            }

            UpdateColor(placedColor);
            if (auraLight != null)
            {
                auraLight.color = placedColor;
                auraLight.range = 3.5f;
            }

            if (auraParticles != null)
            {
                var main = auraParticles.main;
                main.startColor = placedColor;
                auraParticles.Play();
            }
        }

        private void UpdateColor(Color c)
        {
            if (materialInstance != null)
            {
                if (materialInstance.HasProperty(BaseColorProp))
                {
                    materialInstance.SetColor(BaseColorProp, c);
                }
                if (materialInstance.HasProperty(EmissionColorProp))
                {
                    materialInstance.EnableKeyword("_EMISSION");
                    materialInstance.SetColor(EmissionColorProp, c * 2f);
                }
            }
        }
    }
}
