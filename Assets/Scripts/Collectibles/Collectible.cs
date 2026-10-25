using GameJamPlus.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class Collectible : MonoBehaviour
    {
        [SerializeField] private CollectibleType type = CollectibleType.Cheese;
        [SerializeField] private bool requiresInteraction = true;
        [SerializeField, Tooltip("Unique identity of this placed item across the run. Shared by collection, photos and remains; duplicates must use different IDs. Empty IDs do not persist.")]
        private string collectionId;
        [SerializeField, Tooltip("Adjust the assigned collider's Size/Radius and Offset to change the pickup area.")]
        private Collider2D pickupArea;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private CollectibleRemains remains;
        [SerializeField] private UnityEvent onCollected = new UnityEvent();
        private bool collected;

        public CollectibleType Type => type;
        public string CollectionId => collectionId;
        public CollectibleCollector Collector { get; private set; }
        public bool ShowGizmos => showGizmos;
        public Collider2D PickupArea => pickupArea;
        public bool RequiresInteraction => requiresInteraction;
        public bool CanBeCollected => !collected && isActiveAndEnabled
            && (pickupArea == null || pickupArea.enabled) && !GameSession.Collections.HasCollected(collectionId);

        private void Awake()
        {
            if (remains == null) remains = GetComponent<CollectibleRemains>();
        }

        private void Start()
        {
            // Session initialization in GameManager.Awake precedes all collectible Start callbacks.
            if (!GameSession.Collections.HasCollected(collectionId)) return;
            collected = true;
            ShowCollectedState();
        }

        public bool TryCollect(CollectibleCollector collector, bool interactionPressed = false)
        {
            if (!CanBeCollected || collector == null || !collector.CanCollect
                || (requiresInteraction && !interactionPressed)
                || !GameSession.Collections.TryRemember(collectionId)) return false;
            collected = true;
            Collector = collector;
            if (pickupArea != null) pickupArea.enabled = false;
            try
            {
                // Effects and goal decisions commit before presentation events can release pressure plates.
                collector.RecordCollection(this);
                onCollected.Invoke();
            }
            finally
            {
                ShowCollectedState();
            }
            return true;
        }

        private void ShowCollectedState()
        {
            if (pickupArea != null) pickupArea.enabled = false;
            if (remains != null)
            {
                remains.Show();
                enabled = false;
            }
            else gameObject.SetActive(false);
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
