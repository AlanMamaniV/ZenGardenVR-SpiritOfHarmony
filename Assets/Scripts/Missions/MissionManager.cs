using System;
using UnityEngine;
using ZenGardenVR.Core;

namespace ZenGardenVR.Missions
{
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Mission Status")]
        [SerializeField] private bool guardianMet = false;
        [SerializeField] private bool lanternLit = false;
        [SerializeField] private bool bonsaiDiscovered = false;
        [SerializeField] private bool fountainActivated = false;
        [SerializeField] private bool magicStonePlaced = false;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip stepCompleteSound;
        [SerializeField] private AudioClip missionCompleteSound;

        [Header("Mission Completion FX")]
        [SerializeField] private ParticleSystem completeParticles;

        public event Action<string, int, int> OnMissionProgressChanged;
        public event Action OnMissionCompleted;

        public bool IsGuardianMet => guardianMet;
        public bool IsLanternLit => lanternLit;
        public bool IsBonsaiDiscovered => bonsaiDiscovered;
        public bool IsFountainActivated => fountainActivated;
        public bool IsMagicStonePlaced => magicStonePlaced;

        public int CompletedCount
        {
            get
            {
                int c = 0;
                if (guardianMet) c++;
                if (lanternLit) c++;
                if (bonsaiDiscovered) c++;
                if (fountainActivated) c++;
                if (magicStonePlaced) c++;
                return c;
            }
        }

        public int TotalSteps => 5;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }
        }

        private void Start()
        {
            UpdateProgress("¡Bienvenido al Jardín Zen! Habla con el Guardián o explora las zonas.");
        }

        public void CompleteGuardianMeet()
        {
            if (guardianMet) return;
            guardianMet = true;
            OnStepDone("Has conversado con el Guardián del Jardín.");
        }

        public void CompleteLantern()
        {
            if (lanternLit) return;
            lanternLit = true;
            OnStepDone("Has encendido el Farol de Piedra ancestral.");
        }

        public void CompleteBonsai()
        {
            if (bonsaiDiscovered) return;
            bonsaiDiscovered = true;
            OnStepDone("Has contemplado y activado la floración del Bonsái Sagrado.");
        }

        public void CompleteFountain()
        {
            if (fountainActivated) return;
            fountainActivated = true;
            OnStepDone("El agua sagrada de la Fuente Tsukubai vuelve a fluir.");
        }

        public void CompleteMagicStone()
        {
            if (magicStonePlaced) return;
            magicStonePlaced = true;
            OnStepDone("La Piedra Mágica ha sido consagrada en el Altar de Armonía.");
        }

        private void OnStepDone(string message)
        {
            PlaySound(stepCompleteSound);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowNotification(message);
            }

            UpdateProgress(message);

            if (CompletedCount >= TotalSteps)
            {
                TriggerMissionComplete();
            }
        }

        private void UpdateProgress(string lastActionMsg)
        {
            OnMissionProgressChanged?.Invoke(lastActionMsg, CompletedCount, TotalSteps);
        }

        private void TriggerMissionComplete()
        {
            PlaySound(missionCompleteSound);
            if (completeParticles != null)
            {
                completeParticles.Play();
            }

            string completeMsg = "¡Felicidades! Has restaurado la armonía total y descubierto todos los secretos del Jardín Zen.";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowNotification(completeMsg);
            }

            OnMissionCompleted?.Invoke();
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
