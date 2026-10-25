using System;
using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-500), DisallowMultipleComponent]
    public sealed class WaterConnection : MonoBehaviour
    {
        [SerializeField] private WaterBasin first;
        [SerializeField] private WaterBasin second;
        [SerializeField, Tooltip("Water cannot cross below this world-space height. Place this object at the passage floor.")]
        private float sillOffset;
        [SerializeField, Min(0f), Tooltip("Transferred water area per second at one unit of level difference. Flow scales with the square root of the head difference.")]
        private float flowRate = 3f;
        [SerializeField] private bool startsOpen = true;
        [SerializeField, Tooltip("Optional door: flow is blocked while this door is closed.")]
        private Door blockingDoor;
        [SerializeField, Tooltip("Optional animated Tilemap valve controlling this passage.")]
        private PipeValve blockingValve;
        [SerializeField] private bool showGizmos = true;
        private bool open;
        public float SillHeight => transform.position.y + sillOffset;
        public bool IsOpen => open && (blockingDoor == null || blockingDoor.IsOpen)
            && (blockingValve == null || blockingValve.IsOpen);

        private void Awake() => open = startsOpen;
        public void Open() => open = true;
        public void Close() => open = false;
        public void Toggle() => open = !open;

        public void Configure(WaterBasin firstBasin, WaterBasin secondBasin, float sillHeight)
        {
            first = firstBasin;
            second = secondBasin;
            sillOffset = sillHeight - transform.position.y;
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.State != GameManager.LoopState.Playing) return;
            if (!IsOpen || first == null || second == null || first == second
                || !first.isActiveAndEnabled || !second.isActiveAndEnabled
                || first.Model == null || second.Model == null) return;
            WaterBasin source = first.SurfaceHeight >= second.SurfaceHeight ? first : second;
            WaterBasin target = source == first ? second : first;
            float head = source.SurfaceHeight - Mathf.Max(SillHeight, target.SurfaceHeight);
            if (head <= 0.0001f) return;
            double transferred = WaterBasinModel.CalculateTransfer(source.Model, target.Model, SillHeight, flowRate, Time.fixedDeltaTime);
            if (transferred <= 0d) return;
            source.Model.SetVolume(source.Model.Volume - transferred);
            target.Model.SetVolume(target.Model.Volume + transferred);
            Vector2 direction = ((Vector2)target.transform.position - (Vector2)source.transform.position).normalized;
            float strength = Mathf.Clamp01((float)(transferred / Math.Max(0.0001f, Time.fixedDeltaTime)) / Mathf.Max(0.01f, flowRate));
            source.NotifyFlow(strength, transform.position, direction);
            target.NotifyFlow(strength, transform.position, direction);
            source.RefreshWater();
            target.RefreshWater();
            Physics2D.SyncTransforms();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            Gizmos.color = Color.yellow;
            Vector3 sill = new Vector3(transform.position.x, SillHeight, transform.position.z);
            Gizmos.DrawWireSphere(sill, 0.08f);
            Gizmos.DrawLine(sill + Vector3.left * 0.3f, sill + Vector3.right * 0.3f);
            if (first == null || second == null) return;
            Vector3 direction = (second.transform.position - first.transform.position).normalized;
            Vector3 side = new Vector3(-direction.y, direction.x, 0f);
            Vector3 start = sill - direction * 0.35f;
            Vector3 end = sill + direction * 0.35f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawLine(start, start + direction * 0.12f + side * 0.08f);
            Gizmos.DrawLine(start, start + direction * 0.12f - side * 0.08f);
            Gizmos.DrawLine(end, end - direction * 0.12f + side * 0.08f);
            Gizmos.DrawLine(end, end - direction * 0.12f - side * 0.08f);
        }
    }
}
