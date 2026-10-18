using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(115), DisallowMultipleComponent]
    public sealed class PlayerButtonInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SimulatedBody2D body;
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[16];
        public InteractableButton InteractionTarget { get; private set; }
        public float TargetDistanceSquared => InteractionTarget != null && body != null
            ? (InteractionTarget.InteractionPoint - (Vector2)body.Shape.bounds.center).sqrMagnitude : float.PositiveInfinity;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
        }

        private void FixedUpdate() => RefreshInteractionTarget();

        private void Update()
        {
            if (Time.timeScale <= 0f || input == null || !input.isActiveAndEnabled || !input.PressButtonPressed) return;
            InteractableButton selected = InteractionTarget;
            if (selected == null || body == null) return;
            Physics2D.SyncTransforms();
            if (!selected.Contains(body.Shape) || !HasLineOfSight(selected.InteractionPoint)) return;
            // Claim the shared press before invoking an event that may pause or change the scene.
            input.ConsumeInteraction();
            if (selected.TryPress()) InteractionTarget = null;
        }

        public void RefreshInteractionTarget()
        {
            InteractionTarget = null;
            GameManager manager = GameManager.Instance;
            if (!isActiveAndEnabled || body == null || !body.Shape.enabled
                || (manager != null && manager.State != GameManager.LoopState.Playing)) return;
            Physics2D.SyncTransforms();
            float best = float.PositiveInfinity;
            Vector2 center = body.Shape.bounds.center;
            for (int index = 0; index < InteractableButton.ActiveButtons.Count; index++)
            {
                InteractableButton button = InteractableButton.ActiveButtons[index];
                if (!button.Contains(body.Shape)) continue;
                float squared = (button.InteractionPoint - center).sqrMagnitude;
                if (squared >= best || !HasLineOfSight(button.InteractionPoint)) continue;
                best = squared;
                InteractionTarget = button;
            }
        }

        private bool HasLineOfSight(Vector2 point)
        {
            Vector2 origin = body.Shape.bounds.center;
            Vector2 offset = point - origin;
            float distance = offset.magnitude;
            if (distance <= 0.001f) return true;
            int count = Physics2D.Raycast(origin, offset / distance, body.SolidFilter, obstacles, distance);
            for (int index = 0; index < count; index++)
            {
                Collider2D obstacle = obstacles[index].collider;
                if (obstacle == body.Shape || obstacle.transform.IsChildOf(transform)) continue;
                if (obstacles[index].distance < distance - 0.01f) return false;
            }
            return true;
        }

        private void OnDisable() => InteractionTarget = null;
    }
}
