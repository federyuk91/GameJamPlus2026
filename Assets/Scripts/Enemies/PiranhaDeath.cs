using System.Collections.Generic;
using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(SimulatedBody2D), typeof(CrateMotion), typeof(CrateBuoyancy))]
    public sealed class PiranhaDeath : MonoBehaviour
    {
        private static readonly List<PiranhaDeath> activePiranhas = new List<PiranhaDeath>();
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private PiranhaPatrol patrol;
        [SerializeField] private PiranhaChase chase;
        [SerializeField] private LethalContact lethalContact;
        [SerializeField] private SimulatedBody2D body;
        [SerializeField] private CrateMotion floatingMotion;
        [SerializeField, Min(0f)] private float passengerDownwardAcceleration = 45f;

        public static IReadOnlyList<PiranhaDeath> ActivePiranhas => activePiranhas;
        public bool IsDead { get; private set; }
        public Collider2D Shape => body != null ? body.Shape : null;

        private void Awake()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (patrol == null) patrol = GetComponent<PiranhaPatrol>();
            if (chase == null) chase = GetComponent<PiranhaChase>();
            if (lethalContact == null) lethalContact = GetComponent<LethalContact>();
            if (body == null) body = GetComponent<SimulatedBody2D>();
            if (floatingMotion == null) floatingMotion = GetComponent<CrateMotion>();
            floatingMotion.enabled = false;
        }

        private void OnEnable() => activePiranhas.Add(this);
        private void OnDisable() => activePiranhas.Remove(this);

        public void Die()
        {
            if (IsDead || body == null || floatingMotion == null) return;
            IsDead = true;
            if (chase != null) chase.enabled = false;
            if (patrol != null) patrol.enabled = false;
            if (lethalContact != null) lethalContact.enabled = false;
            if (visual != null) visual.flipY = !visual.flipY;
            // The living fish is a lethal trigger; its corpse must support the player.
            body.Shape.isTrigger = false;
            Vector2 offset = body.Shape.offset;
            offset.y = -offset.y;
            body.Shape.offset = offset;
            floatingMotion.PassengerDownwardAcceleration = passengerDownwardAcceleration;
            floatingMotion.enabled = true;
            Physics2D.SyncTransforms();
        }
    }
}
