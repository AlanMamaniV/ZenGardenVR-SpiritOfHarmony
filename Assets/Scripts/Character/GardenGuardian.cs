using System;
using System.Collections;
using UnityEngine;
using ZenGardenVR.Missions;
using ZenGardenVR.UI;

namespace ZenGardenVR.Character
{
    /// <summary>
    /// Guardián del Jardín Zen implementado como un Agente Virtual Autónomo (Embodied Conversational Agent).
    /// Incorpora los principios de animación corporal, seguimiento de mirada (gaze / mutual orientation),
    /// proxémica y reconocimiento gestual de asentimiento y negación de la cabeza.
    /// </summary>
    public class GardenGuardian : MonoBehaviour
    {
        [Header("Animator & Character Controller")]
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private Transform headLookTarget;

        [Header("Head Joint for Dynamic Nod/Shake Animation")]
        [SerializeField] private Transform headJoint;
        [SerializeField] private Transform leftArmJoint;
        [SerializeField] private Transform rightArmJoint;

        [Header("Gaze & Orientation Settings (Social VR)")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float encounterDistance = 5.0f;
        [SerializeField] private float personalSpaceDistance = 1.8f;
        [SerializeField] private float turnSpeed = 3.5f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem spiritAuraParticles;
        [SerializeField] private Light spiritualLight;

        [Header("Gesture Detection Reference")]
        [SerializeField] private HeadGestureDetector gestureDetector;

        private enum DialogueState
        {
            Unmet,
            GreetingDelivered,
            WaitingForQuestionAnswer,
            QuestExplained,
            Encouraging
        }

        private DialogueState currentState = DialogueState.Unmet;
        private bool hasMetUser = false;
        private float breathingTimer = 0f;

        // Dynamic gesture reaction animation
        private bool isNoddingAnim = false;
        private bool isShakingAnim = false;
        private float gestureAnimTimer = 0f;

        private void Start()
        {
            if (gestureDetector == null)
            {
                gestureDetector = FindFirstObjectByType<HeadGestureDetector>();
            }

            if (gestureDetector != null)
            {
                gestureDetector.OnNodDetected += HandlePlayerNod;
                gestureDetector.OnShakeDetected += HandlePlayerShake;
            }

            if (spiritAuraParticles != null)
            {
                spiritAuraParticles.Play();
            }
        }

        private void OnDestroy()
        {
            if (gestureDetector != null)
            {
                gestureDetector.OnNodDetected -= HandlePlayerNod;
                gestureDetector.OnShakeDetected -= HandlePlayerShake;
            }
        }

        private void Update()
        {
            if (playerTransform == null && Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }

            // Procedural subtle breathing and idle float
            breathingTimer += Time.deltaTime * 1.4f;
            float floatOffset = Mathf.Sin(breathingTimer) * 0.035f;
            
            // 1. Social Orientation / Mutual Gaze Tracking
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);

                if (dist <= encounterDistance * 1.5f)
                {
                    Vector3 lookPos = playerTransform.position;
                    lookPos.y = transform.position.y;
                    Quaternion targetRot = Quaternion.LookRotation(lookPos - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                }

                if (!hasMetUser && dist <= encounterDistance)
                {
                    TriggerEncounter();
                }
            }

            // 2. Procedural Facial/Head Nodding & Shaking Animation for the Guardian
            AnimateGuardianHead();
        }

        private void AnimateGuardianHead()
        {
            if (headJoint == null) return;

            if (isNoddingAnim)
            {
                gestureAnimTimer += Time.deltaTime * 6f;
                float nodPitch = Mathf.Sin(gestureAnimTimer) * 16f;
                headJoint.localRotation = Quaternion.Euler(nodPitch, 0, 0);

                if (gestureAnimTimer >= Mathf.PI * 2.5f)
                {
                    isNoddingAnim = false;
                    headJoint.localRotation = Quaternion.identity;
                }
            }
            else if (isShakingAnim)
            {
                gestureAnimTimer += Time.deltaTime * 5f;
                float shakeYaw = Mathf.Sin(gestureAnimTimer) * 18f;
                headJoint.localRotation = Quaternion.Euler(0, shakeYaw, 0);

                if (gestureAnimTimer >= Mathf.PI * 2.5f)
                {
                    isShakingAnim = false;
                    headJoint.localRotation = Quaternion.identity;
                }
            }
            else
            {
                // Idle gentle tilt
                float idlePitch = Mathf.Sin(breathingTimer * 0.8f) * 2.5f;
                headJoint.localRotation = Quaternion.Euler(idlePitch, 0, 0);
            }
        }

        public void TriggerEncounter()
        {
            hasMetUser = true;
            currentState = DialogueState.GreetingDelivered;

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.CompleteGuardianMeet();
            }

            // Japanese formal greeting bow
            PlayGuardianNod();
            StartCoroutine(GreetingDialogueSequence());
        }

        private IEnumerator GreetingDialogueSequence()
        {
            SpeakText("Konnichiwa. Bienvenido al Jardín Zen. Soy el Guardián espiritual de este santuario.");
            yield return new WaitForSeconds(4.5f);

            SpeakText("Cada piedra, cada gota de agua y cada cerezo representan la armonía entre la naturaleza y el espíritu.");
            yield return new WaitForSeconds(5.0f);

            currentState = DialogueState.WaitingForQuestionAnswer;
            SpeakText("¿Deseas restaurar el equilibrio y descubrir todos los secretos del jardín? (Asiente con la cabeza [SÍ / Y] o niega [NO / N])");
        }

        private void HandlePlayerNod()
        {
            if (currentState == DialogueState.WaitingForQuestionAnswer)
            {
                currentState = DialogueState.QuestExplained;
                PlayGuardianNod();
                StartCoroutine(AcceptQuestSequence());
            }
            else if (currentState == DialogueState.QuestExplained || currentState == DialogueState.Encouraging)
            {
                PlayGuardianNod();
                SpeakText("Tu espíritu refleja sabiduría y serenidad. Sigue adelante con las misiones del jardín.");
            }
        }

        private void HandlePlayerShake()
        {
            PlayGuardianShake();
            if (currentState == DialogueState.WaitingForQuestionAnswer)
            {
                SpeakText("Comprendo. Tómate tu tiempo para contemplar el río, las rocas y las flores a tu propio ritmo.");
            }
            else
            {
                SpeakText("La paciencia es la mayor virtud del jardín. Respira profundo y contempla.");
            }
        }

        private IEnumerator AcceptQuestSequence()
        {
            SpeakText("¡Excelente! Tus ancestros sonríen. El jardín necesita tu toque armonioso.");
            yield return new WaitForSeconds(4.0f);

            SpeakText("Enciende los faroles de piedra, haz florecer el bonsái sagrado, activa el agua de la fuente y consagra la piedra mágica en el altar.");
            yield return new WaitForSeconds(6.0f);

            currentState = DialogueState.Encouraging;
            SpeakText("Que los pétalos de sakura guíen tus pasos hacia la paz interior.");
        }

        private void SpeakText(string text)
        {
            Debug.Log($"[Guardián]: {text}");
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.ShowGuardianDialogue("Guardián del Jardín", text);
            }
        }

        public void PlayGuardianNod()
        {
            isNoddingAnim = true;
            isShakingAnim = false;
            gestureAnimTimer = 0f;
            if (animator != null)
            {
                animator.SetTrigger("Nod");
            }
        }

        public void PlayGuardianShake()
        {
            isShakingAnim = true;
            isNoddingAnim = false;
            gestureAnimTimer = 0f;
            if (animator != null)
            {
                animator.SetTrigger("Shake");
            }
        }
    }
}
