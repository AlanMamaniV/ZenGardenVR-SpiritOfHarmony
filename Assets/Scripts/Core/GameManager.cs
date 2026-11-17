using System;
using UnityEngine;

namespace ZenGardenVR.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private bool isInitialized = false;

        public event Action<string> OnNotificationMessage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            isInitialized = true;
        }

        public void ShowNotification(string message)
        {
            Debug.Log($"[GameManager Notification]: {message}");
            OnNotificationMessage?.Invoke(message);
        }
    }
}
