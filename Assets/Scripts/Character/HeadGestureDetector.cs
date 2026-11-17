using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZenGardenVR.Character
{
    public class HeadGestureDetector : MonoBehaviour
    {
        [Header("Target Camera")]
        [SerializeField] private Transform headCamera;

        [Header("Pitch (Nod / Asentir Sí) Thresholds")]
        [SerializeField] private float nodPitchThreshold = 14f; // degrees of deflection
        [SerializeField] private float nodAngularSpeedThreshold = 25f; // deg/sec

        [Header("Yaw (Shake / Negar No) Thresholds")]
        [SerializeField] private float shakeYawThreshold = 16f;
        [SerializeField] private float shakeAngularSpeedThreshold = 28f;

        [Header("Timing and Cooldown")]
        [SerializeField] private float gestureCooldown = 2.0f;
        [SerializeField] private float minGestureDuration = 0.15f;
        [SerializeField] private float maxGestureDuration = 1.0f;

        [Header("Controller Fallback Actions")]
        [SerializeField] private InputActionReference controllerYesAction;
        [SerializeField] private InputActionReference controllerNoAction;

        public event Action OnNodDetected;
        public event Action OnShakeDetected;

        private float lastPitch;
        private float lastYaw;
        private float cooldownTimer = 0f;

        // Gesture integration tracking
        private float nodAccumulated = 0f;
        private float nodDuration = 0f;
        private float shakeAccumulated = 0f;
        private float shakeDuration = 0f;

        private void Start()
        {
            if (headCamera == null && Camera.main != null)
            {
                headCamera = Camera.main.transform;
            }

            if (headCamera != null)
            {
                Vector3 euler = headCamera.rotation.eulerAngles;
                lastPitch = NormalizeAngle(euler.x);
                lastYaw = NormalizeAngle(euler.y);
            }
        }

        private void OnEnable()
        {
            if (controllerYesAction != null && controllerYesAction.action != null)
            {
                controllerYesAction.action.Enable();
                controllerYesAction.action.performed += OnControllerYes;
            }

            if (controllerNoAction != null && controllerNoAction.action != null)
            {
                controllerNoAction.action.Enable();
                controllerNoAction.action.performed += OnControllerNo;
            }
        }

        private void OnDisable()
        {
            if (controllerYesAction != null && controllerYesAction.action != null)
            {
                controllerYesAction.action.performed -= OnControllerYes;
            }

            if (controllerNoAction != null && controllerNoAction.action != null)
            {
                controllerNoAction.action.performed -= OnControllerNo;
            }
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return;
            }

            // Keyboard fallbacks for testing in Editor: Y (Yes), N (No)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.yKey.wasPressedThisFrame)
                {
                    TriggerNod();
                    return;
                }
                if (Keyboard.current.nKey.wasPressedThisFrame)
                {
                    TriggerShake();
                    return;
                }
            }

            if (headCamera == null)
            {
                if (Camera.main != null) headCamera = Camera.main.transform;
                else return;
            }

            Vector3 euler = headCamera.rotation.eulerAngles;
            float currentPitch = NormalizeAngle(euler.x);
            float currentYaw = NormalizeAngle(euler.y);

            float deltaPitch = Mathf.DeltaAngle(lastPitch, currentPitch);
            float deltaYaw = Mathf.DeltaAngle(lastYaw, currentYaw);

            float pitchSpeed = Mathf.Abs(deltaPitch) / Time.deltaTime;
            float yawSpeed = Mathf.Abs(deltaYaw) / Time.deltaTime;

            // Analyze pitch (Nod - affirmative)
            if (pitchSpeed > nodAngularSpeedThreshold && yawSpeed < pitchSpeed * 0.8f)
            {
                nodAccumulated += Mathf.Abs(deltaPitch);
                nodDuration += Time.deltaTime;

                if (nodAccumulated >= nodPitchThreshold && nodDuration >= minGestureDuration && nodDuration <= maxGestureDuration)
                {
                    TriggerNod();
                    ResetTracking();
                    return;
                }
            }
            else
            {
                nodAccumulated = Mathf.MoveTowards(nodAccumulated, 0, Time.deltaTime * 30f);
                nodDuration = 0f;
            }

            // Analyze yaw (Shake - negative)
            if (yawSpeed > shakeAngularSpeedThreshold && pitchSpeed < yawSpeed * 0.8f)
            {
                shakeAccumulated += Mathf.Abs(deltaYaw);
                shakeDuration += Time.deltaTime;

                if (shakeAccumulated >= shakeYawThreshold && shakeDuration >= minGestureDuration && shakeDuration <= maxGestureDuration)
                {
                    TriggerShake();
                    ResetTracking();
                    return;
                }
            }
            else
            {
                shakeAccumulated = Mathf.MoveTowards(shakeAccumulated, 0, Time.deltaTime * 30f);
                shakeDuration = 0f;
            }

            lastPitch = currentPitch;
            lastYaw = currentYaw;
        }

        private void OnControllerYes(InputAction.CallbackContext ctx)
        {
            if (cooldownTimer <= 0f)
            {
                TriggerNod();
            }
        }

        private void OnControllerNo(InputAction.CallbackContext ctx)
        {
            if (cooldownTimer <= 0f)
            {
                TriggerShake();
            }
        }

        public void TriggerNod()
        {
            cooldownTimer = gestureCooldown;
            Debug.Log("[HeadGestureDetector] ¡Gesto ASENTIMIENTO (SÍ) detectado!");
            OnNodDetected?.Invoke();
        }

        public void TriggerShake()
        {
            cooldownTimer = gestureCooldown;
            Debug.Log("[HeadGestureDetector] ¡Gesto NEGACIÓN (NO) detectado!");
            OnShakeDetected?.Invoke();
        }

        private void ResetTracking()
        {
            nodAccumulated = 0f;
            nodDuration = 0f;
            shakeAccumulated = 0f;
            shakeDuration = 0f;
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
