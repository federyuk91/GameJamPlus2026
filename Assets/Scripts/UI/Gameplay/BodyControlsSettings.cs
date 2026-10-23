using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    [CreateAssetMenu(menuName = "Game/UI/Body Controls Settings")]
    public sealed class BodyControlsSettings : ScriptableObject
    {
        [SerializeField] private VisualTreeAsset layout;
        public VisualTreeAsset Layout => layout;


    }
}
