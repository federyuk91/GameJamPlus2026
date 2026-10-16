using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PlayerRopeInteraction : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private RopeSimulation[] ropes;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private Vector2 handOffset = new Vector2(0f, 0.2f);
        private PlayerInteractionSettings Settings => physicsSettings != null ? physicsSettings.interactions : PhysicsSettings.DefaultInteractions;
        private RopeSimulation rope;
        private float distance;
        private float cooldown;
        public bool Attached => rope != null;

        private void Awake()
        {
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (ropes == null || ropes.Length == 0)
                ropes = FindObjectsByType<RopeSimulation>(FindObjectsSortMode.None);
        }

        // True means the rope handled this tick, including a possible release velocity.
        public bool Tick(float horizontal, float vertical, bool grab, bool jump,
            Vector2 incomingVelocity, float dt, out Vector2 velocity, out bool released)
        {
            velocity = incomingVelocity;
            released = false;
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (rope != null && !rope.isActiveAndEnabled)
            {
                Detach();
                released = true;
                return true;
            }
            if (rope == null && cooldown <= 0f && !jump && (grab || vertical > 0.1f))
                TryAttach(incomingVelocity);
            if (rope == null) return false;

            velocity = rope.Chain.Velocity(distance);
            if (jump)
            {
                Vector2 tangent = rope.Chain.Tangent(distance);
                Vector2 swing = new Vector2(-tangent.y, tangent.x);
                float direction = Vector2.Dot(velocity, swing);
                if (Mathf.Abs(direction) < 0.1f) direction = Mathf.Abs(horizontal) > 0.1f ? horizontal : 1f;
                GravitySettings gravity = physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;
                velocity += swing * (Mathf.Sign(direction) * gravity.ropeJumpSwingBoost) + Vector2.up * gravity.ropeJumpUpSpeed;
                Detach();
                released = true;
                return true;
            }

            // Distance is measured down the rope: up input decreases it.
            distance = Mathf.Clamp(distance - vertical * Settings.ropeClimbSpeed * dt,
                Mathf.Min(0.3f, rope.Chain.Length), rope.Chain.Length);
            rope.SetDrive(this, distance, horizontal * Settings.ropeSwingAcceleration);
            Vector2 desired = rope.Chain.Point(distance) - handOffset;
            Vector2 start = transform.position;
            body.Move(desired - start);
            velocity = ((Vector2)transform.position - start) / dt;
            if (Vector2.Distance(transform.position, desired) > 0.06f)
            {
                // The player must never be dragged through a solid wall by the rope.
                Detach();
                released = true;
            }
            return true;
        }

        private void TryAttach(Vector2 incomingVelocity)
        {
            RopeSimulation nearest = null;
            float best = Settings.ropeGrabRadius;
            float grip = 0f;
            foreach (var candidate in ropes)
            {
                if (candidate == null || !candidate.Available) continue;
                float along = candidate.Chain.ClosestDistance((Vector2)transform.position + handOffset, out float separation);
                if (separation > best) continue;
                best = separation;
                nearest = candidate;
                grip = Mathf.Clamp(along, Mathf.Min(0.3f, candidate.Chain.Length), candidate.Chain.Length);
            }
            if (nearest != null && nearest.Attach(this, grip, incomingVelocity))
            {
                rope = nearest;
                distance = grip;
            }
        }

        private void Detach()
        {
            if (rope != null) rope.Release(this);
            rope = null;
            cooldown = Settings.ropeRegrabDelay;
        }
        private void OnDisable() => Detach();
    }
}
