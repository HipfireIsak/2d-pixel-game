using UnityEngine;
using AetherEcho.World;

namespace AetherEcho.World
{
    public class QuestNpcInteractable : MonoBehaviour
    {
        [SerializeField] private string questGiverName = "Chrono Sage";
        [SerializeField] private float interactRadiusMeters = 2.8f;

        public string QuestGiverName => questGiverName;
        public float InteractRadiusMeters => interactRadiusMeters;

        public void Configure(string giverName)
        {
            questGiverName = giverName;
        }
    }
}
