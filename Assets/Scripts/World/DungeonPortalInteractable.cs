using UnityEngine;

namespace AetherEcho.World
{
    public class DungeonPortalInteractable : MonoBehaviour
    {
        [SerializeField] private float interactRadiusMeters = 2.8f;

        public float InteractRadiusMeters => interactRadiusMeters;
    }
}
