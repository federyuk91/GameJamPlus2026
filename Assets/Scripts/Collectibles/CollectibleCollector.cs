using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(120), DisallowMultipleComponent]
    public sealed class CollectibleCollector : MonoBehaviour
    {
        [SerializeField] private Collider2D collectionArea;
        [SerializeField] private PlayerInputReader input;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private ContactFilter2D filter;
        private readonly Dictionary<CollectibleType, int> counts = new Dictionary<CollectibleType, int>();

        public event Action<CollectibleType, int> CountChanged;
        public Collectible InteractionTarget { get; private set; }
        public void ConsumeInteractionInput() => input?.ConsumeCollectibleInteraction();

        public int GetCount(CollectibleType type)
        {
            return counts.TryGetValue(type, out int count) ? count : 0;
        }

        internal void RecordCollection(CollectibleType type)
        {
            int count = GetCount(type) + 1;
            counts[type] = count;
            CountChanged?.Invoke(type, count);
        }

        private void Awake()
        {
            if (collectionArea == null) collectionArea = GetComponent<Collider2D>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            filter = ContactFilter2D.noFilter;
            filter.useTriggers = true;
        }

        private void FixedUpdate()
        {
            CollectOverlapping(false);
        }

        private void Update()
        {
            if (Time.timeScale > 0f && input != null && input.isActiveAndEnabled && input.InteractionPressed)
                CollectOverlapping(true);
        }

        private void CollectOverlapping(bool interactionPressed)
        {
            InteractionTarget = null;
            if (collectionArea == null || !collectionArea.enabled) return;

            // The player moves its Transform directly and automatic physics synchronization is disabled.
            Physics2D.SyncTransforms();
            overlaps.Clear();
            collectionArea.Overlap(filter, overlaps);
            Collectible nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (Collider2D candidate in overlaps)
            {
                if (candidate != null && candidate.TryGetComponent(out Collectible collectible)
                    && collectible.CanBeCollected)
                {
                    if (!collectible.RequiresInteraction && !interactionPressed) collectible.TryCollect(this);
                    else if (collectible.RequiresInteraction)
                    {
                        float distance = (candidate.bounds.center - collectionArea.bounds.center).sqrMagnitude;
                        if (distance < nearestDistance)
                        {
                            nearest = collectible;
                            nearestDistance = distance;
                        }
                    }
                }
                if (!isActiveAndEnabled || Time.timeScale <= 0f) return;
            }
            InteractionTarget = nearest;
            if (interactionPressed && nearest != null && nearest.TryCollect(this, true))
            {
                if (input != null) input.ConsumeCollectibleInteraction();
                InteractionTarget = null;
            }
        }

        private void Reset()
        {
            collectionArea = GetComponent<Collider2D>();
        }
    }
}
