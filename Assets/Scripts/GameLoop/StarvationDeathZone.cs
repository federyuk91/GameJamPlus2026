using System.Collections.Generic;
using MousePlatformer;
using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class StarvationDeathZone : MonoBehaviour
    {
        public Animator anim;
        private static readonly List<StarvationDeathZone> activeZones = new List<StarvationDeathZone>();
        internal static IReadOnlyList<StarvationDeathZone> ActiveZones => activeZones;
        [SerializeField] private BoxCollider2D area;
        [SerializeField, Min(0.1f)] private float drainInterval = 1f;
        [SerializeField] private Transform mountPoint;
        [SerializeField] private Transform exitPoint;
        private PlayerWheelInteraction occupant;
        private static readonly int IsActive = Animator.StringToHash("isActive");
        public bool CanUse => isActiveAndEnabled && occupant == null;
        public Vector2 InteractionPoint => area != null ? (Vector2)area.bounds.center : (Vector2)transform.position;
        public Vector2 MountPosition(Vector2 fallback) => mountPoint != null ? (Vector2)mountPoint.position : fallback;
        public Vector2 ExitPosition(Vector2 fallback) => exitPoint != null ? (Vector2)exitPoint.position : fallback;
        [SerializeField] private bool showGizmos = true;
        public float DrainInterval => Mathf.Max(0.1f, drainInterval);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => activeZones.Clear();

        private void Awake()
        {
            if (area == null) area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        private void OnEnable()
        {
            if (!activeZones.Contains(this)) activeZones.Add(this);
            if (anim != null) anim.SetBool(IsActive, false);
        }

        private void OnDisable()
        {
            activeZones.Remove(this);
            if (occupant != null) occupant.StopUsing();
        }

        public bool TryUse(PlayerWheelInteraction player, Collider2D shape)
        {
            if (!CanUse || player == null || !Contains(shape)) return false;
            occupant = player;
            if (anim != null) anim.SetBool(IsActive, true);
            return true;
        }

        public void Release(PlayerWheelInteraction player)
        {
            if (occupant != player) return;
            occupant = null;
            if (anim != null) anim.SetBool(IsActive, false);
        }

        public bool Contains(Collider2D playerCollider)
        {
            if (!isActiveAndEnabled || area == null || !area.enabled || playerCollider == null
                || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) return false;
            ColliderDistance2D distance = area.Distance(playerCollider);
            return distance.isValid && distance.distance <= 0f;
        }

        private void Reset()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        private void OnValidate()
        {
            if (area != null) area.isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || area == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = area.transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.06f);
            Gizmos.DrawCube(area.offset, area.size);
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.5f);
            Gizmos.DrawWireCube(area.offset, area.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
