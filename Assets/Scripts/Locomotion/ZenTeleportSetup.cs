using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace ZenGardenVR.Locomotion
{
    public class ZenTeleportSetup : MonoBehaviour
    {
        [Header("Teleportation Configuration")]
        [SerializeField] private TeleportationArea[] teleportationAreas;

        private void Start()
        {
            if (teleportationAreas == null || teleportationAreas.Length == 0)
            {
                teleportationAreas = FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None);
            }
        }
    }
}
