using System;
using System.Collections;
using UnityEngine;

namespace ZenGardenVR.Character
{
    /// <summary>
    /// Control central de animaciones y estados del personaje Zen Guardian.
    /// Gestiona transiciones suaves, parámetros del Animator Controller y temporizadores.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ZenGuardianController : MonoBehaviour
    {
        public enum GuardianState
        {
            Idle,
            Greeting,
            Waving,
            Listening,
            Talking,
            Nodding,
            ExtraNod
        }

        [Header("Referencias")]
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;

        [Header("Estado Actual")]
        [SerializeField] private GuardianState currentState = GuardianState.Idle;

        // Hash de parámetros del Animator para máximo rendimiento
        private static readonly int AnimParamIsListening = Animator.StringToHash("IsListening");
        private static readonly int AnimParamIsTalking = Animator.StringToHash("IsTalking");
        private static readonly int AnimParamGreeting = Animator.StringToHash("Greeting");
        private static readonly int AnimParamWave = Animator.StringToHash("Wave");
        private static readonly int AnimParamNod = Animator.StringToHash("Nod");
        private static readonly int AnimParamExtraNod = Animator.StringToHash("ExtraNod");

        private Coroutine activeStateCoroutine;

        public GuardianState CurrentState => currentState;
        public bool IsBusy => currentState == GuardianState.Greeting || currentState == GuardianState.Waving;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            ResetToIdle();
        }

        /// <summary>
        /// Reproduce la animación de reverencia / bienvenida (StandingGreeting).
        /// </summary>
        public void PlayGreeting(float duration = 3.2f, Action onComplete = null)
        {
            SetState(GuardianState.Greeting);
            if (animator != null)
            {
                animator.SetBool(AnimParamIsListening, false);
                animator.SetBool(AnimParamIsTalking, false);
                animator.SetTrigger(AnimParamGreeting);
            }

            StartManagedTimer(duration, () =>
            {
                ResetToIdle();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Reproduce el saludo con la mano (Waving).
        /// </summary>
        public void PlayWave(float duration = 2.8f, Action onComplete = null)
        {
            SetState(GuardianState.Waving);
            if (animator != null)
            {
                animator.SetBool(AnimParamIsListening, false);
                animator.SetBool(AnimParamIsTalking, false);
                animator.SetTrigger(AnimParamWave);
            }

            StartManagedTimer(duration, () =>
            {
                ResetToIdle();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Cambia al estado de escucha atenta (Listening / HeadNodYes en loop).
        /// </summary>
        public void StartListening()
        {
            SetState(GuardianState.Listening);
            if (animator != null)
            {
                animator.SetBool(AnimParamIsTalking, false);
                animator.SetBool(AnimParamIsListening, true);
            }
        }

        /// <summary>
        /// Finaliza la escucha y regresa al reposo si no hay otra acción.
        /// </summary>
        public void StopListening()
        {
            if (currentState == GuardianState.Listening)
            {
                ResetToIdle();
            }
            else if (animator != null)
            {
                animator.SetBool(AnimParamIsListening, false);
            }
        }

        /// <summary>
        /// Activa la animación de conversación y gesticulación (Talking).
        /// </summary>
        public void StartTalking(AudioClip speechClip = null)
        {
            SetState(GuardianState.Talking);
            if (animator != null)
            {
                animator.SetBool(AnimParamIsListening, false);
                animator.SetBool(AnimParamIsTalking, true);
            }

            if (speechClip != null && audioSource != null)
            {
                audioSource.clip = speechClip;
                audioSource.Play();
            }
        }

        /// <summary>
        /// Detiene la gesticulación de habla y regresa a escuchar o a Idle.
        /// </summary>
        public void StopTalking(bool returnToListening = true)
        {
            if (animator != null)
            {
                animator.SetBool(AnimParamIsTalking, false);
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (returnToListening)
            {
                StartListening();
            }
            else
            {
                ResetToIdle();
            }
        }

        /// <summary>
        /// Asentimiento afirmativo (Nod).
        /// </summary>
        public void PlayNod(float duration = 1.8f, Action onComplete = null)
        {
            SetState(GuardianState.Nodding);
            if (animator != null)
            {
                animator.SetTrigger(AnimParamNod);
            }

            StartManagedTimer(duration, () =>
            {
                ResetToIdle();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Movimiento de cabeza adicional / reflexivo (SarcasticHeadNod).
        /// </summary>
        public void PlayExtraNod(float duration = 2.2f, Action onComplete = null)
        {
            SetState(GuardianState.ExtraNod);
            if (animator != null)
            {
                animator.SetTrigger(AnimParamExtraNod);
            }

            StartManagedTimer(duration, () =>
            {
                ResetToIdle();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Vuelve de manera suave y segura al estado base Idle.
        /// </summary>
        public void ResetToIdle()
        {
            if (activeStateCoroutine != null)
            {
                StopCoroutine(activeStateCoroutine);
                activeStateCoroutine = null;
            }

            SetState(GuardianState.Idle);
            if (animator != null)
            {
                animator.SetBool(AnimParamIsListening, false);
                animator.SetBool(AnimParamIsTalking, false);
            }
        }

        private void SetState(GuardianState newState)
        {
            currentState = newState;
        }

        private void StartManagedTimer(float duration, Action callback)
        {
            if (activeStateCoroutine != null)
            {
                StopCoroutine(activeStateCoroutine);
            }
            activeStateCoroutine = StartCoroutine(TimerRoutine(duration, callback));
        }

        private IEnumerator TimerRoutine(float time, Action callback)
        {
            yield return new WaitForSeconds(time);
            activeStateCoroutine = null;
            callback?.Invoke();
        }
    }
}
