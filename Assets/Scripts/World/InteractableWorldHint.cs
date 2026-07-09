using UnityEngine;

namespace AetherEcho.World
{
    public class InteractableWorldHint : MonoBehaviour
    {
        [SerializeField] private string hintText = "Press E";

        public string HintText => hintText;

        public void Configure(string text)
        {
            hintText = text;
        }
    }
}
