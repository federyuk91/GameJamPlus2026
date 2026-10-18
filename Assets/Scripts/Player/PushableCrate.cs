using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(CrateMotion))]
    public sealed class PushableCrate : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private bool showGizmos = true;
        [SerializeField, Min(0.01f), Tooltip("Push/pull speed divisor. 1 = base speed, 2 = half speed.")]
        private float weight = 1f;
        public SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        public float Weight => Mathf.Max(0.01f, weight);
        public bool ShowGizmos => showGizmos;
    }
}
