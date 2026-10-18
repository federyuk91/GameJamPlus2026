using UnityEngine;
using UnityEngine.Events;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class Collectible : MonoBehaviour
    {
        [SerializeField] private CollectibleType type = CollectibleType.Cheese;
        [SerializeField] private bool requiresInteraction = true;
        [SerializeField, Tooltip("Regola Size/Radius e Offset del collider assegnato per modificare l'area verde. L'interazione richiede la sovrapposizione con il collider del topo.")]
        private Collider2D pickupArea;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private UnityEvent onCollected = new UnityEvent();
        private bool collected;

        public CollectibleType Type => type;
        public bool ShowGizmos => showGizmos;
        public Collider2D PickupArea => pickupArea;
        public bool RequiresInteraction => requiresInteraction;
        public bool CanBeCollected => !collected && isActiveAndEnabled && (pickupArea == null || pickupArea.enabled);

        public bool TryCollect(CollectibleCollector collector, bool interactionPressed = false)
        {
            if (collected || !isActiveAndEnabled || collector == null || !collector.isActiveAndEnabled
                || (requiresInteraction && !interactionPressed))
                return false;

            collected = true;
            if (pickupArea != null) pickupArea.enabled = false;
            try
            {
                collector.RecordCollection(type);
                onCollected.Invoke();
            }
            finally
            {
                gameObject.SetActive(false);
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
