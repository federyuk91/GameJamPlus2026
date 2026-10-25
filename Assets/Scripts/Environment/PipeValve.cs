using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

namespace MousePlatformer
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-480)]
    public sealed class PipeValve : MonoBehaviour
    {
        public enum BindingMode { Nearest, ManualSelection }
        public enum InitialState { PaintedTile, Open, Closed }

        [SerializeField] private BindingMode bindingMode;
        [SerializeField, Tooltip("Optional search filter in the editor. Empty searches all Tilemaps in this scene.")]
        private Tilemap searchTilemap;
        [SerializeField, Min(0f)] private float maximumSearchDistance = 3f;
        [SerializeField] private InitialState initialState;
        [SerializeField, Min(0.01f)] private float secondsPerFrame = 0.1f;
        [SerializeField, Tooltip("Closing order: 241, 240, 226, 225, 224. Opening plays backwards.")]
        private TileBase[] closingFrames = new TileBase[5];
        [SerializeField] private WaterConnection waterConnection;
        [SerializeField, Tooltip("Automatically assign the nearest water link in the editor. Disable to assign Water Connection manually.")]
        private bool autoWaterConnection = true;
        [SerializeField] private UnityEvent onOpened = new UnityEvent();
        [SerializeField] private UnityEvent onClosed = new UnityEvent();
        [SerializeField, HideInInspector] private Tilemap targetTilemap;
        [SerializeField, HideInInspector] private Vector3Int targetCell;
        [SerializeField, HideInInspector] private bool hasBinding;
        private TilemapCollider2D tilemapCollider;
        private CompositeCollider2D[] composites = Array.Empty<CompositeCollider2D>();
        private Matrix4x4 cellTransform;
        private Color cellColor;
        private TileFlags cellFlags;
        private int frameIndex;
        private bool targetOpen;
        private bool initialized;
        private float elapsed;

        public BindingMode Mode => bindingMode;
        public Tilemap SearchTilemap => searchTilemap;
        public float MaximumSearchDistance => maximumSearchDistance;
        public Tilemap TargetTilemap => targetTilemap;
        public Vector3Int TargetCell => targetCell;
        public bool HasBinding => hasBinding && targetTilemap != null;
        public WaterConnection Connection => waterConnection;
        public bool AutoWaterConnection => autoWaterConnection;
        public bool IsOpen => initialized && isActiveAndEnabled && frameIndex == 0 && targetOpen;
        public bool InitiallyOpen => initialState == InitialState.Open || (initialState == InitialState.PaintedTile
            && HasBinding && FrameIndex(targetTilemap.GetTile(targetCell)) == 0);

        public int FrameIndex(TileBase tile) => tile == null ? -1 : Array.IndexOf(closingFrames, tile);

        private void Awake()
        {
            if (!HasBinding || closingFrames.Length < 2 || Array.Exists(closingFrames, tile => tile == null))
            {
                Debug.LogWarning("Pipe Valve needs valid animation frames and an editor-bound Tilemap cell.", this);
                return;
            }
            int painted = FrameIndex(targetTilemap.GetTile(targetCell));
            if (painted < 0)
            {
                Debug.LogWarning("Pipe Valve's bound cell no longer contains a valve tile. Refresh its binding in the Inspector.", this);
                return;
            }
            cellTransform = targetTilemap.GetTransformMatrix(targetCell);
            cellColor = targetTilemap.GetColor(targetCell);
            cellFlags = targetTilemap.GetTileFlags(targetCell);
            tilemapCollider = targetTilemap.GetComponent<TilemapCollider2D>();
            if (tilemapCollider != null && tilemapCollider.attachedRigidbody != null)
                composites = tilemapCollider.attachedRigidbody.GetComponents<CompositeCollider2D>();
            frameIndex = initialState == InitialState.PaintedTile ? painted : initialState == InitialState.Open ? 0 : closingFrames.Length - 1;
            targetOpen = frameIndex == 0;
            initialized = true;
            ApplyFrame();
            SynchronizeConnection();
        }

        private void OnEnable() { if (initialized) SynchronizeConnection(); }
        private void OnDisable() { if (initialized && waterConnection != null) waterConnection.Close(); }

        public void Open() => SetTarget(true);
        public void Close() => SetTarget(false);
        public void Toggle() => SetTarget(!targetOpen);

        private void SetTarget(bool opening)
        {
            if (!initialized || !isActiveAndEnabled || targetOpen == opening) return;
            targetOpen = opening;
            elapsed = 0f;
            SynchronizeConnection();
        }

        private void Update()
        {
            if (!initialized || targetTilemap == null) return;
            int destination = targetOpen ? 0 : closingFrames.Length - 1;
            if (frameIndex == destination) return;
            elapsed += Time.deltaTime;
            float duration = Mathf.Max(0.01f, secondsPerFrame);
            while (elapsed >= duration && frameIndex != destination)
            {
                elapsed -= duration;
                frameIndex += targetOpen ? -1 : 1;
                ApplyFrame();
                if (frameIndex != destination) continue;
                SynchronizeConnection();
                if (targetOpen) onOpened.Invoke(); else onClosed.Invoke();
            }
        }

        private void SynchronizeConnection()
        {
            if (waterConnection == null) return;
            if (IsOpen) waterConnection.Open(); else waterConnection.Close();
        }

        private void ApplyFrame()
        {
            targetTilemap.SetTile(targetCell, closingFrames[frameIndex]);
            // Preserve the painted cell's rotation, flip and tint across tile replacements.
            targetTilemap.SetTileFlags(targetCell, TileFlags.None);
            targetTilemap.SetTransformMatrix(targetCell, cellTransform);
            targetTilemap.SetColor(targetCell, cellColor);
            targetTilemap.SetTileFlags(targetCell, cellFlags);
            targetTilemap.RefreshTile(targetCell);
            if (tilemapCollider == null || !tilemapCollider.enabled) return;
            tilemapCollider.ProcessTilemapChanges();
            foreach (CompositeCollider2D composite in composites)
                if (composite.enabled && composite.generationType == CompositeCollider2D.GenerationType.Manual) composite.GenerateGeometry();
            Physics2D.SyncTransforms();
        }

        private void OnDrawGizmosSelected()
        {
            Color previous = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maximumSearchDistance);
            if (HasBinding)
            {
                Vector3 center = targetTilemap.GetCellCenterWorld(targetCell);
                Gizmos.DrawLine(transform.position, center);
                Gizmos.DrawWireSphere(center, 0.15f);
            }
            Gizmos.color = previous;
        }
    }
}
