using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenGardenVR.Core;
using ZenGardenVR.Missions;

namespace ZenGardenVR.UI
{
    public class VRUIManager : MonoBehaviour
    {
        public static VRUIManager Instance { get; private set; }

        [Header("World Space Canvas / UI Panel")]
        [SerializeField] private Canvas worldCanvas;

        [Header("Object Inspection Elements")]
        [SerializeField] private GameObject inspectionPanel;
        [SerializeField] private TextMeshProUGUI inspectionTitleText;
        [SerializeField] private TextMeshProUGUI inspectionDescText;
        [SerializeField] private TextMeshProUGUI inspectionActionText;

        [Header("Mission Elements")]
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI missionStepsText;
        [SerializeField] private TextMeshProUGUI missionProgressText;
        [SerializeField] private Slider missionProgressBar;

        [Header("Guardian Dialogue Elements")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueContentText;

        [Header("Notifications")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("Wrist / Follow HUD Target")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private bool followCameraSmoothly = true;
        [SerializeField] private float followDistance = 2.4f;
        [SerializeField] private float followHeight = -0.75f;

        private Coroutine dialogueHideCoroutine;
        private Coroutine notificationHideCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNotificationMessage += ShowNotification;
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionProgressChanged += UpdateMissionDisplay;
                MissionManager.Instance.OnMissionCompleted += DisplayMissionCompletedUI;
                UpdateMissionDisplay("Bienvenido al Jardín Zen", MissionManager.Instance.CompletedCount, MissionManager.Instance.TotalSteps);
            }

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (notificationPanel != null) notificationPanel.SetActive(false);
            if (inspectionPanel != null) inspectionPanel.SetActive(false);
            if (missionPanel != null) missionPanel.SetActive(false); // Oculto por defecto para no bloquear la visión
        }

        private void Update()
        {
            // Permitir al usuario alternar el panel de misiones con M o H si lo desea
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.mKey.wasPressedThisFrame || keyboard.hKey.wasPressedThisFrame))
            {
                if (missionPanel != null)
                {
                    missionPanel.SetActive(!missionPanel.activeSelf);
                }
            }

            if (followCameraSmoothly)
            {
                if (followTarget == null && Camera.main != null)
                {
                    followTarget = Camera.main.transform;
                }

                if (followTarget != null)
                {
                    Vector3 targetPos = followTarget.position + followTarget.forward * followDistance;
                    targetPos.y = followTarget.position.y + followHeight;

                    transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3.5f);
                    Vector3 lookDir = transform.position - followTarget.position;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 4f);
                    }
                }
            }
        }

        public void ShowInspection(string title, string description, string actionHint)
        {
            if (inspectionPanel != null)
            {
                inspectionPanel.SetActive(true);
                if (inspectionTitleText != null) inspectionTitleText.text = title;
                if (inspectionDescText != null) inspectionDescText.text = description;
                if (inspectionActionText != null) inspectionActionText.text = actionHint;
            }
        }

        public void ClearInspection()
        {
            if (inspectionPanel != null)
            {
                inspectionPanel.SetActive(false);
            }
        }

        public void ShowGuardianDialogue(string speaker, string dialogue)
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                if (speakerNameText != null) speakerNameText.text = speaker;
                if (dialogueContentText != null) dialogueContentText.text = dialogue;

                if (dialogueHideCoroutine != null)
                {
                    StopCoroutine(dialogueHideCoroutine);
                }
                dialogueHideCoroutine = StartCoroutine(HideDialogueAfterDelay(9f));
            }
        }

        private IEnumerator HideDialogueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        public void ShowNotification(string message)
        {
            if (notificationPanel != null && notificationText != null)
            {
                notificationPanel.SetActive(true);
                notificationText.text = message;

                if (notificationHideCoroutine != null)
                {
                    StopCoroutine(notificationHideCoroutine);
                }
                notificationHideCoroutine = StartCoroutine(HideNotificationAfterDelay(4.5f));
            }
        }

        private IEnumerator HideNotificationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
        }

        public void UpdateMissionDisplay(string lastStatus, int completedCount, int totalCount)
        {
            if (missionTitleText != null)
            {
                missionTitleText.text = "Misión: Secretos del Jardín Zen";
            }

            if (missionProgressText != null)
            {
                missionProgressText.text = $"Progreso Armónico: {completedCount} / {totalCount}";
            }

            if (missionProgressBar != null)
            {
                missionProgressBar.value = (float)completedCount / totalCount;
            }

            if (missionStepsText != null)
            {
                var mm = MissionManager.Instance;
                string s1 = (mm != null && mm.IsGuardianMet) ? "<color=#7CFF79>[✓]</color>" : "<color=#D0D0D0>[ ]</color>";
                string s2 = (mm != null && mm.IsLanternLit) ? "<color=#7CFF79>[✓]</color>" : "<color=#D0D0D0>[ ]</color>";
                string s3 = (mm != null && mm.IsBonsaiDiscovered) ? "<color=#7CFF79>[✓]</color>" : "<color=#D0D0D0>[ ]</color>";
                string s4 = (mm != null && mm.IsFountainActivated) ? "<color=#7CFF79>[✓]</color>" : "<color=#D0D0D0>[ ]</color>";
                string s5 = (mm != null && mm.IsMagicStonePlaced) ? "<color=#7CFF79>[✓]</color>" : "<color=#D0D0D0>[ ]</color>";

                missionStepsText.text =
                    $"{s1} 1. Encontrarse con el Guardián\n" +
                    $"{s2} 2. Encender un farol japonés\n" +
                    $"{s3} 3. Despertar el bonsái sagrado\n" +
                    $"{s4} 4. Activar el flujo de la fuente\n" +
                    $"{s5} 5. Colocar piedra mágica en el altar";
            }
        }

        private void DisplayMissionCompletedUI()
        {
            if (missionTitleText != null)
            {
                missionTitleText.text = "¡Misión Cumplida! Armonía Alcanzada";
            }
            ShowNotification("¡Enhorabuena! Has dominado el equilibrio y los secretos del Jardín Zen.");
        }
    }
}
