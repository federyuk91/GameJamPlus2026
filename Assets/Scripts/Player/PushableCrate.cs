using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(CrateMotion))]
    public sealed class PushableCrate : MonoBehaviour
    {
        [SerializeField] private SimulatedBody2D body;
        [SerializeField, Min(0.01f), Tooltip("Divisore della velocità di spinta e tiro. 1 = velocità base, 2 = metà velocità.")]
        private float weight = 1f;
        public SimulatedBody2D Body => body != null ? body : body = GetComponent<SimulatedBody2D>();
        public float Weight => Mathf.Max(0.01f, weight);
    }
}
