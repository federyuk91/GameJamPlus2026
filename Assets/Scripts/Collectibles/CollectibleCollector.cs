using System;
using System.Collections.Generic;
using GameJamPlus.Gameplay;
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

        public event Action<CollectibleType, int> CountChanged;
        public Collectible InteractionTarget { get; private set; }
        public float TargetDistanceSquared => InteractionTarget != null && collectionArea != null
            ? ((InteractionTarget.PickupArea != null ? InteractionTarget.PickupArea.bounds.center : InteractionTarget.transform.position)
                - collectionArea.bounds.center).sqrMagnitude : float.PositiveInfinity;
        public void ConsumeInteractionInput() => input?.ConsumeCollectibleInteraction();

        public int GetCount(CollectibleType type)
        {
            return GameSession.GetCollectibleCount(type);
        }

        internal void RecordCollection(CollectibleType type)
        {
            int count = GameSession.AddCollectibleCount(type, 1);
            CountChanged?.Invoke(type, count);
        }

        public void AddKnowledge(int amount)
        {
            if (amount <= 0) return;
            int count = GameSession.AddCollectibleCount(CollectibleType.Knowledge, amount);
            CountChanged?.Invoke(CollectibleType.Knowledge, count);
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
            CollectOverlapping();
        }

        private void Update()
        {
            if (Time.timeScale > 0f && input != null && input.isActiveAndEnabled && input.InteractionPressed)
                CollectSelectedTarget();
        }

        private void CollectSelectedTarget()
        {
            Collectible selected = InteractionTarget;
            if (selected == null || !selected.CanBeCollected || collectionArea == null || !collectionArea.enabled) return;
            Physics2D.SyncTransforms();
            overlaps.Clear();
            collectionArea.Overlap(filter, overlaps);
            foreach (Collider2D candidate in overlaps)
            {
                if (candidate == null || !candidate.TryGetComponent(out Collectible collectible) || collectible != selected) continue;
                if (selected.TryCollect(this, true))
                {
                    input.ConsumeCollectibleInteraction();
                    InteractionTarget = null;
                }
                return;
            }
        }

        public void RefreshInteractionTarget() => CollectOverlapping(false);

        private void CollectOverlapping(bool collectAutomatic = true)
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
                    if (!collectible.RequiresInteraction)
                    {
                        if (collectAutomatic) collectible.TryCollect(this);
                    }
                    else
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
        }

        private void Reset()
        {
            collectionArea = GetComponent<Collider2D>();
        }
    }
}
