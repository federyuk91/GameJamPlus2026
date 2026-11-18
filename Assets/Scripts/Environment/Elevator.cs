using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-110), DisallowMultipleComponent]
    public sealed class Elevator : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D platform;
        [SerializeField, Min(0f), Tooltip("Travel speed in world units per second.")]
        private float speed = 1f;
        [SerializeField, Tooltip("Bottom height relative to the Elevator root, along world Y.")]
        private float bottomOffset = 0.778f;
        [SerializeField, Tooltip("Top height relative to the Elevator root, along world Y.")]
        private float topOffset = 2.5f;

        private readonly List<SimulatedBody2D> passengers = new List<SimulatedBody2D>();
        private bool isOn;
        private bool movingUp = true;

        public Vector3 BottomPosition => GetPoint(bottomOffset);
        public Vector3 TopPosition => GetPoint(Mathf.Max(topOffset, bottomOffset));

        public void On() => isOn = true;
        public void Off() => isOn = false;

        private void FixedUpdate()
        {
            if (!isOn || speed <= 0f || platform == null || !platform.isActiveAndEnabled) return;

            Vector3 position = platform.transform.position;
            float bottom = BottomPosition.y;
            float top = TopPosition.y;
            if (position.y >= top) movingUp = false;
            else if (position.y <= bottom) movingUp = true;

            float destination = movingUp ? top : bottom;
            float nextY = Mathf.MoveTowards(position.y, destination, speed * Time.fixedDeltaTime);
            Vector2 displacement = Vector2.up * (nextY - position.y);
            if (displacement.y == 0f) return;

            passengers.Clear();
            foreach (SimulatedBody2D body in SimulatedBody2D.ActiveBodies)
            {
                if (!body.isActiveAndEnabled || !body.Shape.enabled || !body.Grounded || body.GroundCollider != platform)
                    continue;

                passengers.Add(body);
                // Respect the passenger's movement limit and collision clearance.
                displacement = Vector2.ClampMagnitude(displacement, body.MaxSpeed * Time.fixedDeltaTime);
                if (Mathf.Abs(displacement.y) > 0.00001f)
                    displacement = body.AllowedDisplacement(displacement, platform);
            }

            if (displacement.y == 0f) return;
            position.y += displacement.y;
            platform.transform.position = position;
            Physics2D.SyncTransforms();
            foreach (SimulatedBody2D passenger in passengers)
            {
                passenger.Move(displacement, ignored: platform);
                passenger.RefreshGround();
            }
        }

        private Vector3 GetPoint(float offset)
        {
            Vector3 point = platform != null ? platform.transform.position : transform.position;
            point.y = transform.position.y + offset;
            return point;
        }

        private void OnValidate()
        {
            topOffset = Mathf.Max(topOffset, bottomOffset);
        }

        private void OnDrawGizmosSelected()
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(BottomPosition, TopPosition);
            Gizmos.DrawWireSphere(BottomPosition, 0.08f);
            Gizmos.DrawWireSphere(TopPosition, 0.08f);
            Gizmos.color = previousColor;
        }
    }
}
