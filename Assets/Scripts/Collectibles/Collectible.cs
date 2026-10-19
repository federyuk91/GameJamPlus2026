using UnityEngine;
using UnityEngine.Events;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class Collectible : MonoBehaviour
    {
        [SerializeField] private CollectibleType type = CollectibleType.Cheese;
        [SerializeField] private bool requiresInteraction = true;
        [SerializeField, Tooltip("Adjust the assigned collider's Size/Radius and Offset to change the green area. Interaction requires overlap with the mouse collider.")]
        private Collider2D pickupArea;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private CollectibleRemains remains;
        [SerializeField] private UnityEvent onCollected = new UnityEvent();
        private bool collected;

        public CollectibleType Type => type;
        public bool ShowGizmos => showGizmos;
        public Collider2D PickupArea => pickupArea;
        public bool RequiresInteraction => requiresInteraction;
        public bool CanBeCollected => !collected && isActiveAndEnabled && (pickupArea == null || pickupArea.enabled)
            && (remains == null || !remains.IsConsumed);

        private void Awake()
        {
            if (remains == null) remains = GetComponent<CollectibleRemains>();
        }

        private void Start()
        {
            if (remains == null || !remains.IsConsumed) return;
            collected = true;
            if (pickupArea != null) pickupArea.enabled = false;
            remains.Show();
            enabled = false;
        }

        public bool TryCollect(CollectibleCollector collector, bool interactionPressed = false)
        {
            if (!CanBeCollected || collector == null || !collector.isActiveAndEnabled
                || (requiresInteraction && !interactionPressed))
                return false;

            collected = true;
            if (remains != null) remains.MarkConsumed();
            if (pickupArea != null) pickupArea.enabled = false;
            try
            {
                collector.RecordCollection(type);
                onCollected.Invoke();
            }
            finally
            {
                if (remains != null)
                {
                    remains.Show();
                    enabled = false;
                }
                else gameObject.SetActive(false);
            }
            return true;
        }

        private void Reset()
        {
            pickupArea = GetComponent<Collider2D>();
            if (pickupArea != null) pickupArea.isTrigger = true;
        }

        private void OnValidate()
        {
            if (pickupArea != null) pickupArea.isTrigger = true;
        }
    }
}
