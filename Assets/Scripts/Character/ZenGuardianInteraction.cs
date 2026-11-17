using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using ZenGardenVR.UI;

namespace ZenGardenVR.Character
{
    /// <summary>
    /// Gestión de interacción humana recíproca entre el usuario y el Zen Guardian.
    /// Administra proximidad proxémica, secuencias de bienvenida (Greeting/Waving),
    /// diálogo interactivo contextual del jardín Zen, y retroalimentación visual/auditiva.
    /// </summary>
    [RequireComponent(typeof(ZenGuardianController))]
    public class ZenGuardianInteraction : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private ZenGuardianController controller;
        [SerializeField] private ZenGuardianGaze gaze;
        [SerializeField] private Transform playerTransform;

        [Header("Distancias de Proximidad (Proxémica)")]
        [Tooltip("Distancia a la que el Guardián percibe la llegada del jugador y realiza la reverencia de bienvenida.")]
        [SerializeField] private float greetingDistance = 5.0f;
        [Tooltip("Distancia para entablar conversación interactiva directa.")]
        [SerializeField] private float conversationDistance = 3.0f;
        [Tooltip("Distancia a partir de la cual el usuario se aleja y se cierra la interacción.")]
        [SerializeField] private float exitDistance = 6.5f;

        [Header("Diálogo del Jardín Zen")]
        [TextArea(2, 4)]
        [SerializeField] private string[] dialogueLines = new string[]
        {
            "Bienvenido al Jardín Zen. Soy el Guardián de este lugar sagrado.",
            "Cada piedra karesansui, los cerezos sakura y las aguas del río representan la armonía entre la naturaleza y el espíritu.",
            "¿Te gustaría descubrir los secretos de este jardín? Enciende los cuatro faroles sagrados y deposita la gema en el altar.",
            "Que la paz y la serenidad guíen tus pasos en este espacio de contemplación."
        };

        [Header("UI & Retroalimentación")]
        [SerializeField] private TextMeshPro speechBubbleText;
        [SerializeField] private GameObject speechBubbleCanvas;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip chimeSound;

        // Banderas de estado
        private bool hasGreeted = false;
        private bool inConversationRange = false;
        private int currentDialogueIndex = 0;
        private Coroutine dialogueCoroutine;

        public bool InConversation => inConversationRange;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<ZenGuardianController>();
            if (gaze == null) gaze = GetComponent<ZenGuardianGaze>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            FindPlayer();
            SetSpeechText("");
        }

        private void FindPlayer()
        {
            if (playerTransform == null && Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float distance = Vector3.Distance(transform.position, playerTransform.position);

            // 1. Proximidad de llegada: Saludo y reverencia (StandingGreeting -> Waving)
            if (!hasGreeted && distance <= greetingDistance)
            {
                hasGreeted = true;
                TriggerArrivalSequence();
            }

            // 2. Zona de conversación interactiva
            if (distance <= conversationDistance)
            {
                if (!inConversationRange)
                {
                    inConversationRange = true;
                    OnEnterConversationZone();
                }
            }
            else if (distance > exitDistance)
            {
                if (inConversationRange || hasGreeted)
                {
                    OnExitConversationZone();
                }
            }

            // Controles de prueba rápidos en teclado (Editor y XR Device Simulator)
            HandleTestingInput();
        }

        private void HandleTestingInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Barra espaciadora o tecla E: Avanzar diálogo / Iniciar conversación
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
            {
                AdvanceDialogue();
            }

            // Y: Asentir afirmativamente (HeadNodYes)
            if (keyboard.yKey.wasPressedThisFrame)
            {
                UserNoddedYes();
            }

            // N: Negación o movimiento de cabeza reflexivo (SarcasticHeadNod)
            if (keyboard.nKey.wasPressedThisFrame)
            {
                UserNoddedNo();
            }

            // G: Forzar saludo manual
            if (keyboard.gKey.wasPressedThisFrame)
            {
                TriggerArrivalSequence();
            }
        }

        /// <summary>
        /// Secuencia inicial de bienvenida al aproximarse el jugador.
        /// Reproduce StandingGreeting (reverencia) y luego Waving (saludo con la mano).
        /// </summary>
        public void TriggerArrivalSequence()
        {
            PlayChime();
            SetSpeechText("¡Bienvenido viajero!");
            NotifyUI("Guardián Zen", "El Guardián del Jardín Zen te saluda con reverencia.");

            controller.PlayGreeting(3.2f, () =>
            {
                controller.PlayWave(2.6f, () =>
                {
                    controller.StartListening();
                    ShowDialogueLine(0);
                });
            });
        }

        private void OnEnterConversationZone()
        {
            controller.StartListening();
            if (currentDialogueIndex == 0)
            {
                ShowDialogueLine(0);
            }
        }

        private void OnExitConversationZone()
        {
            inConversationRange = false;
            hasGreeted = false;
            currentDialogueIndex = 0;
            SetSpeechText("");
            controller.ResetToIdle();
        }

        /// <summary>
        /// Avanza la conversación con el personaje.
        /// </summary>
        public void AdvanceDialogue()
        {
            if (controller.IsBusy) return;

            currentDialogueIndex++;
            if (currentDialogueIndex >= dialogueLines.Length)
            {
                currentDialogueIndex = 0;
            }

            ShowDialogueLine(currentDialogueIndex);
        }

        private void ShowDialogueLine(int index)
        {
            if (index < 0 || index >= dialogueLines.Length) return;

            string line = dialogueLines[index];
            SetSpeechText(line);
            NotifyUI("Guardián Zen", line);

            // Iniciar animación de gesticulación/habla (Talking)
            controller.StartTalking();

            if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = StartCoroutine(TalkingRoutine(line.Length * 0.08f + 2.0f));
        }

        private IEnumerator TalkingRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            // Al terminar la explicación, regresa a la postura atenta de escucha
            controller.StopTalking(returnToListening: true);
            dialogueCoroutine = null;
        }

        /// <summary>
        /// Respuesta cuando el usuario asiente (afirmativo).
        /// </summary>
        public void UserNoddedYes()
        {
            SetSpeechText("El Guardián asiente con sabiduría en respuesta.");
            NotifyUI("Guardián Zen", "Veo que comprendes la armonía de este lugar sagrado.");
            controller.PlayNod(2.0f, () =>
            {
                controller.StartListening();
            });
        }

        /// <summary>
        /// Respuesta cuando el usuario niega o realiza un gesto reflexivo.
        /// </summary>
        public void UserNoddedNo()
        {
            SetSpeechText("El Guardián reflexiona con paciencia.");
            NotifyUI("Guardián Zen", "La paciencia y la serenidad llegarán a su debido tiempo.");
            controller.PlayExtraNod(2.2f, () =>
            {
                controller.StartListening();
            });
        }

        private void NotifyUI(string speaker, string message)
        {
            if (VRUIManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(speaker))
                {
                    VRUIManager.Instance.ShowGuardianDialogue(speaker, message);
                }
                else
                {
                    VRUIManager.Instance.ShowNotification(message);
                }
            }
        }

        private void SetSpeechText(string text)
        {
            if (speechBubbleText != null)
            {
                speechBubbleText.text = text;
            }

            if (speechBubbleCanvas != null)
            {
                speechBubbleCanvas.SetActive(!string.IsNullOrEmpty(text));
            }
        }

        private void PlayChime()
        {
            if (audioSource != null && chimeSound != null)
            {
                audioSource.PlayOneShot(chimeSound, 0.7f);
            }
        }
    }
}
