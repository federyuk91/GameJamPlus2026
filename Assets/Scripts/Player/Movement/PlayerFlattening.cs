using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerFlattening : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private AnimationClip flattenClip;
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private Vector2 standingSize;
        private Vector2 standingOffset;
        private bool downHeld;
        private float transitionRemaining;
        private bool standingUp;
        public bool IsFlattened { get; private set; }
        public bool IsFlattening => IsFlattened && transitionRemaining > 0f;
        public bool IsStandingUp => standingUp && transitionRemaining > 0f;
        public float SpeedMultiplier => IsFlattened ? 0.5f : 1f;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
        }
        public void Tick(float vertical, bool inWater, bool canFlatten, float dt)
        {
            transitionRemaining = Mathf.Max(0f, transitionRemaining - dt);
            bool pressedDown = vertical < -0.5f && !downHeld;
            downHeld = vertical < -0.5f;
            if (IsFlattened)
            {
                // Entrando in acqua si tenta il ripristino, senza espandere il corpo dentro un soffitto.
                if (vertical > 0.5f || inWater) TryStand();
                return;
            }
            if (!pressedDown || inWater || !canFlatten) return;
            BoxCollider2D shape = body.Shape;
            standingSize = shape.size;
            standingOffset = shape.offset;
            Vector2 size = standingSize;
            size.y *= 0.5f;
            shape.size = size;
            shape.offset = standingOffset + Vector2.down * (standingSize.y - size.y) * 0.5f;
            IsFlattened = true;
            standingUp = false;
            transitionRemaining = flattenClip != null ? flattenClip.length : 0.25f;
            Physics2D.SyncTransforms();
            body.RefreshGround();
        }
        private bool TryStand()
        {
            BoxCollider2D shape = body.Shape;
            Vector2 offset = standingOffset;
            offset.x = shape.offset.x;
            Vector3 scale = shape.transform.lossyScale;
            Vector2 size = Vector2.Scale(standingSize, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            size = new Vector2(Mathf.Max(0.001f, size.x - 0.001f), Mathf.Max(0.001f, size.y - 0.001f));
            Physics2D.SyncTransforms();
            overlaps.Clear();
            Physics2D.OverlapBox(shape.transform.TransformPoint(offset), size, shape.transform.eulerAngles.z, body.SolidFilter, overlaps);
            foreach (Collider2D obstacle in overlaps)
                if (obstacle != shape && !obstacle.transform.IsChildOf(transform)) return false;
            shape.size = standingSize;
            shape.offset = offset;
            IsFlattened = false;
            standingUp = true;
            transitionRemaining = flattenClip != null ? flattenClip.length : 0.25f;
            Physics2D.SyncTransforms();
            body.RefreshGround();
            return true;
        }
        public void ResetPosture()
        {
            if (IsFlattened && body != null)
            {
                Vector2 offset = standingOffset;
                offset.x = body.Shape.offset.x;
                body.Shape.size = standingSize;
                body.Shape.offset = offset;
            }
            IsFlattened = false;
            standingUp = false;
            downHeld = false;
            transitionRemaining = 0f;
        }
        private void OnDisable() => ResetPosture();
    }
}
