using UnityEngine;

namespace MousePlatformer
{
    // Crates settle before the player tick; horizontal motion comes only from interaction.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D))]
    public sealed class PushableCrate : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField, Min(0.01f), Tooltip("Divisore della velocità di spinta e tiro. 1 = velocità base, 2 = metà velocità.")]
        private float weight = 1f;
        private GravitySettings Gravity => physicsSettings != null ? physicsSettings.gravity : PhysicsSettings.DefaultGravity;
        private float verticalSpeed;
        public SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        public float Weight => Mathf.Max(0.01f, weight);

        private void FixedUpdate()
        {
            verticalSpeed = Mathf.Max(verticalSpeed - Gravity.crateGravity * Time.fixedDeltaTime, -Gravity.crateTerminalSpeed);
            Body.Move(Vector2.up * (verticalSpeed * Time.fixedDeltaTime));
            if (Body.Grounded) verticalSpeed = 0f;
        }

        private void OnDisable() => verticalSpeed = 0f;
    }
}
