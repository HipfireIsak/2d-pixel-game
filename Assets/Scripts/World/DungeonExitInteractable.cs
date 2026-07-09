using UnityEngine;

namespace AetherEcho.World
{
    public class DungeonExitInteractable : MonoBehaviour
    {
        [SerializeField] private float interactRadiusMeters = 2.5f;

        public float InteractRadiusMeters => interactRadiusMeters;
    }
}
