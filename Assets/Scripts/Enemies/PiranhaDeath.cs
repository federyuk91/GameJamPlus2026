using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(PropsMotion), typeof(PropsBuoyancy))]
    public sealed class PiranhaDeath : MonoBehaviour
    {
        private static readonly List<PiranhaDeath> activePiranhas = new List<PiranhaDeath>();
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Animator animator;
        [SerializeField] private PiranhaPatrol patrol;
        [SerializeField] private PiranhaChase chase;
        [SerializeField, FormerlySerializedAs("lethalContact")] private PiranhaBite bite;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private PropsMotion floatingMotion;

        public static IReadOnlyList<PiranhaDeath> ActivePiranhas => activePiranhas;
        public bool IsDead { get; private set; }
        public Collider2D Shape => body != null ? body.Shape : null;

        private void Awake()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            if (patrol == null) patrol = GetComponent<PiranhaPatrol>();
            if (chase == null) chase = GetComponent<PiranhaChase>();
            if (bite == null) bite = GetComponent<PiranhaBite>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (floatingMotion == null) floatingMotion = GetComponent<PropsMotion>();
            floatingMotion.enabled = false;
        }

        private void OnEnable() => activePiranhas.Add(this);
        private void OnDisable() => activePiranhas.Remove(this);

        public void Die()
        {
            if (IsDead || body == null || floatingMotion == null) return;
            IsDead = true;
            if (animator != null) animator.speed = 0f;
            if (chase != null) chase.enabled = false;
            if (patrol != null) patrol.enabled = false;
            if (bite != null) bite.enabled = false;
            if (visual != null) visual.flipY = !visual.flipY;
            // The living fish uses a bite trigger; its corpse must support the player.
            body.Shape.isTrigger = false;
            Vector2 offset = body.Shape.offset;
            offset.y = -offset.y;
            body.Shape.offset = offset;
            floatingMotion.enabled = true;
            Physics2D.SyncTransforms();
        }
    }
}
