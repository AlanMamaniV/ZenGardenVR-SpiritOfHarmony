using UnityEngine;

namespace ZenGardenVR.Character
{
    /// <summary>
    /// Sistema de mirada mutua (Mutual Gaze / Head Tracking) para el Zen Guardian en VR.
    /// Orienta la cabeza suavemente hacia el HMD/Cámara del usuario sin requerir hardware de eye-tracking,
    /// aplicando límites angulares anatómicos realistas y amortiguación continua.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ZenGuardianGaze : MonoBehaviour
    {
        [Header("Objetivo de Mirada")]
        [Tooltip("Transform de la cabeza/cámara del jugador. Si es null, buscará Camera.main automáticamente.")]
        [SerializeField] private Transform target;

        [Header("Configuración de Alcance")]
        [Tooltip("Distancia máxima en metros a la que el Guardián reaccionará dirigiendo su mirada.")]
        [SerializeField] private float maxGazeDistance = 8.0f;
        [Tooltip("Distancia mínima recomendada de interacción.")]
        [SerializeField] private float minGazeDistance = 0.8f;

        [Header("Límites Angulares (Grados)")]
        [Tooltip("Ángulo horizontal máximo respecto al frente del guardián (+/- grados).")]
        [Range(20f, 85f)]
        [SerializeField] private float maxHorizontalAngle = 65f;
        [Tooltip("Ángulo vertical máximo arriba/abajo (+/- grados).")]
        [Range(10f, 45f)]
        [SerializeField] private float maxVerticalAngle = 30f;

        [Header("Suavizado y Damping")]
        [Tooltip("Velocidad de transición al girar la cabeza.")]
        [SerializeField] private float gazeDamping = 3.5f;

        [Header("IK Weights")]
        [Range(0f, 1f)]
        [SerializeField] private float lookAtWeight = 0.85f;
        [Range(0f, 1f)]
        [SerializeField] private float bodyWeight = 0.25f;
        [Range(0f, 1f)]
        [SerializeField] private float headWeight = 0.90f;
        [Range(0f, 1f)]
        [SerializeField] private float eyesWeight = 0.0f; // 0.0 porque no asumimos hardware de eye-tracking
        [Range(0f, 1f)]
        [SerializeField] private float clampWeight = 0.5f;

        private Animator animator;
        private Vector3 currentLookPosition;
        private float currentWeight = 0f;
        private bool isPlayerInSight = false;

        public bool IsPlayerInSight => isPlayerInSight;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            LocatePlayerTarget();
            currentLookPosition = transform.position + transform.forward * 3f + Vector3.up * 1.6f;
        }

        private void LocatePlayerTarget()
        {
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (target == null)
            {
                LocatePlayerTarget();
                if (target == null) return;
            }

            Vector3 guardianChest = transform.position + Vector3.up * 1.45f;
            Vector3 directionToTarget = target.position - guardianChest;
            float distance = directionToTarget.magnitude;

            // Comprobar si el usuario está dentro del rango de distancia
            bool inDistance = distance >= minGazeDistance && distance <= maxGazeDistance;

            // Comprobar si está en el cono de visión frontal
            Vector3 flatDirection = Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized;
            float horizontalAngle = Vector3.Angle(transform.forward, flatDirection);

            isPlayerInSight = inDistance && horizontalAngle <= maxHorizontalAngle;

            float targetWeight = isPlayerInSight ? lookAtWeight : 0f;
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * gazeDamping * 0.5f);

            // Calcular posición de mirada limitada
            if (isPlayerInSight)
            {
                Vector3 clampedDir = ClampDirection(directionToTarget.normalized, transform.forward, maxHorizontalAngle, maxVerticalAngle);
                Vector3 desiredLookPos = guardianChest + clampedDir * Mathf.Min(distance, 4.0f);
                currentLookPosition = Vector3.Lerp(currentLookPosition, desiredLookPos, Time.deltaTime * gazeDamping);
            }
            else
            {
                Vector3 naturalRestLook = guardianChest + transform.forward * 4.0f;
                currentLookPosition = Vector3.Lerp(currentLookPosition, naturalRestLook, Time.deltaTime * gazeDamping * 0.7f);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;

            if (currentWeight > 0.001f)
            {
                animator.SetLookAtWeight(currentWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
                animator.SetLookAtPosition(currentLookPosition);
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }

        /// <summary>
        /// Restringe el vector de dirección dentro de un cono elíptico seguro para el cuello humano.
        /// </summary>
        private Vector3 ClampDirection(Vector3 direction, Vector3 forward, float maxHoriz, float maxVert)
        {
            Quaternion toLocal = Quaternion.Inverse(Quaternion.LookRotation(forward, Vector3.up));
            Vector3 localDir = toLocal * direction;

            // Proyección esférica de ángulos
            float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

            yaw = Mathf.Clamp(yaw, -maxHoriz, maxHoriz);
            pitch = Mathf.Clamp(pitch, -maxVert, maxVert);

            Quaternion clampedRot = Quaternion.Euler(-pitch, yaw, 0f);
            Quaternion toWorld = Quaternion.LookRotation(forward, Vector3.up);
            return toWorld * (clampedRot * Vector3.forward);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
