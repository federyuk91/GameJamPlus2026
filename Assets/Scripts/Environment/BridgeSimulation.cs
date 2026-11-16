using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(80), DisallowMultipleComponent]
    public sealed class BridgeSimulation : MonoBehaviour
    {
        [Header("Composition")]
        [SerializeField] private SpriteRenderer bridgeStart;
        [SerializeField] private SpriteRenderer bridgeEnd;
        [SerializeField] private SpriteRenderer sliceTemplate;
        [SerializeField, Min(1)] private int sliceCount = 1;
        [SerializeField, HideInInspector] private SpriteRenderer[] slices = Array.Empty<SpriteRenderer>();
        [SerializeField, HideInInspector] private BoxCollider2D[] surfaces = Array.Empty<BoxCollider2D>();
        [SerializeField, Tooltip("Deck height relative to each sprite center, in units.")]
        private float deckSurfaceHeight = -0.09375f;
        [SerializeField, Min(0.03125f)] private float deckThickness = 0.125f;
        [Header("Flexibility")]
        [SerializeField, Min(0f), Tooltip("Unloaded sag at the middle of the bridge, in units.")]
        private float restSag = 0.02f;
        [SerializeField, Min(0f), Tooltip("Additional middle-span sag per kilogram, in units/kg.")]
        private float sagPerKilogram = 0.1f;
        [SerializeField, Min(0f)] private float maximumSag = 0.2f;
        [SerializeField, Range(0.1f, 12f)] private float frequency = 2.5f;
        [SerializeField, Range(0.05f, 2f)] private float dampingRatio = 0.45f;
        [SerializeField, Min(0f), Tooltip("Maximum vertical node speed, in units/s.")]
        private float maximumVerticalSpeed = 0.5f;

        private struct Passenger
        {
            public SimulatedBody2D Body;
            public BoxCollider2D Surface;
            public Vector3 LocalPoint;
            public Vector3 WorldPoint;
        }

        private readonly Dictionary<Collider2D, int> surfaceIndices = new Dictionary<Collider2D, int>();
        private readonly List<Passenger> passengers = new List<Passenger>(8);
        private SpriteRenderer[] parts;
        private Vector3[] restPositions;
        private float[] nodes;
        private float deckY;
        private BridgeFlexMotion motion;

        public SpriteRenderer StartPiece => bridgeStart;
        public SpriteRenderer EndPiece => bridgeEnd;
        public SpriteRenderer SliceTemplate => sliceTemplate;
        public int SliceCount => Mathf.Max(1, sliceCount);
        public int ExistingSliceCount => slices.Length;
        public SpriteRenderer Slice(int index) => slices[index];
        public bool HasSurface(BoxCollider2D surface) => Array.IndexOf(surfaces, surface) >= 0;

        internal bool HasClearedDeck(BoxCollider2D passenger, float skin)
        {
            foreach (BoxCollider2D surface in surfaces)
            {
                if (surface != null && surface.enabled && surface.gameObject.activeInHierarchy
                    && surface.TryGetComponent(out OneWayPlatform2D platform) && platform.isActiveAndEnabled
                    && platform.AllowsDropThrough && !platform.HasCleared(surface, passenger, skin)) return false;
            }
            return true;
        }

        private void OnValidate()
        {
            sliceCount = Mathf.Max(1, sliceCount);
            deckThickness = Mathf.Max(0.03125f, deckThickness);
        }

        public void SetPieces(SpriteRenderer[] newSlices, BoxCollider2D[] newSurfaces)
        {
            slices = newSlices;
            surfaces = newSurfaces;
            ArrangePieces();
        }

        public void ArrangePieces()
        {
            if (!TryGetParts(out SpriteRenderer[] layout) || surfaces.Length != layout.Length) return;
            Vector3 startCenter = bridgeStart.transform.localPosition + bridgeStart.sprite.bounds.center;
            float left = startCenter.x - bridgeStart.sprite.bounds.size.x * 0.5f;
            for (int index = 0; index < layout.Length; index++)
            {
                SpriteRenderer part = layout[index];
                Bounds bounds = part.sprite.bounds;
                Transform piece = part.transform;
                piece.localRotation = Quaternion.identity;
                piece.localScale = Vector3.one;
                piece.localPosition = new Vector3(left + bounds.size.x * 0.5f - bounds.center.x,
                    startCenter.y - bounds.center.y, piece.localPosition.z);
                BoxCollider2D surface = surfaces[index];
                if (surface != null)
                {
                    surface.isTrigger = false;
                    surface.offset = new Vector2(bounds.center.x, bounds.center.y + deckSurfaceHeight - deckThickness * 0.5f);
                    surface.size = new Vector2(bounds.size.x, Mathf.Max(0.03125f, deckThickness));
                }
                left += bounds.size.x;
            }
        }

        private bool TryGetParts(out SpriteRenderer[] result)
        {
            result = null;
            if (bridgeStart == null || bridgeEnd == null || slices.Length == 0) return false;
            SpriteRenderer[] layout = new SpriteRenderer[slices.Length + 2];
            layout[0] = bridgeStart;
            layout[layout.Length - 1] = bridgeEnd;
            Array.Copy(slices, 0, layout, 1, slices.Length);
            foreach (SpriteRenderer part in layout)
                if (part == null || part.sprite == null || part.transform.parent != transform) return false;
            result = layout;
            return true;
        }

        private void Awake()
        {
            if (!TryGetParts(out parts) || surfaces.Length != parts.Length)
            {
                Debug.LogError("Bridge: assign Start, End, slices and surfaces, then rebuild outside Play Mode.", this);
                enabled = false;
                return;
            }
            ArrangePieces();
            restPositions = new Vector3[parts.Length];
            nodes = new float[parts.Length + 1];
            for (int index = 0; index < parts.Length; index++)
            {
                BoxCollider2D surface = surfaces[index];
                if (surface == null || surface.transform != parts[index].transform)
                {
                    Debug.LogError("Bridge: every piece needs its assigned BoxCollider2D on the same object.", this);
                    enabled = false;
                    return;
                }
                restPositions[index] = parts[index].transform.localPosition;
                nodes[index] = restPositions[index].x + surface.offset.x - surface.size.x * 0.5f;
                surfaceIndices.Add(surface, index);
            }
            BoxCollider2D last = surfaces[surfaces.Length - 1];
            nodes[nodes.Length - 1] = restPositions[restPositions.Length - 1].x + last.offset.x + last.size.x * 0.5f;
            deckY = restPositions[0].y + surfaces[0].offset.y + surfaces[0].size.y * 0.5f;
            motion = new BridgeFlexMotion(nodes);
        }

        private void FixedUpdate()
        {
            if (motion == null) return;
            passengers.Clear();
            motion.BeginStep(restSag);
            foreach (SimulatedBody2D body in SimulatedBody2D.ActiveBodies)
            {
                if (!body.isActiveAndEnabled || !body.Shape.enabled || !body.Grounded
                    || body.GroundCollider == null || !surfaceIndices.TryGetValue(body.GroundCollider, out int index)) continue;
                BoxCollider2D surface = surfaces[index];
                if (!surface.enabled || !surface.gameObject.activeInHierarchy) continue;
                Bounds bounds = body.Shape.bounds;
                Vector3 point = new Vector3(bounds.center.x, bounds.min.y, body.transform.position.z);
                motion.AddLoad(transform.InverseTransformPoint(point).x, body.Weight, sagPerKilogram);
                passengers.Add(new Passenger
                {
                    Body = body, Surface = surface,
                    LocalPoint = surface.transform.InverseTransformPoint(point), WorldPoint = point
                });
            }
            motion.Step(Time.fixedDeltaTime, frequency, dampingRatio, maximumSag, maximumVerticalSpeed);
            for (int index = 0; index < parts.Length; index++) ApplyPose(index);
            Physics2D.SyncTransforms();
            foreach (Passenger passenger in passengers)
            {
                Vector2 displacement = passenger.Surface.transform.TransformPoint(passenger.LocalPoint) - passenger.WorldPoint;
                passenger.Body.Move(displacement, ignored: passenger.Surface);
            }
        }

        private void ApplyPose(int index)
        {
            Vector2 first = new Vector2(nodes[index], deckY + motion.Offset(index));
            Vector2 second = new Vector2(nodes[index + 1], deckY + motion.Offset(index + 1));
            Vector2 direction = second - first;
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            float stretch = direction.magnitude / (nodes[index + 1] - nodes[index]);
            BoxCollider2D surface = surfaces[index];
            Vector3 topOffset = new Vector3(surface.offset.x * stretch, surface.offset.y + surface.size.y * 0.5f, 0f);
            Vector3 position = (Vector3)((first + second) * 0.5f) - rotation * topOffset;
            position.z = restPositions[index].z;
            Transform piece = parts[index].transform;
            piece.localScale = new Vector3(stretch, 1f, 1f);
            piece.localRotation = rotation;
            piece.localPosition = position;
        }

        private void OnDisable()
        {
            if (motion == null || parts == null) return;
            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index] == null) continue;
                parts[index].transform.localPosition = restPositions[index];
                parts[index].transform.localRotation = Quaternion.identity;
                parts[index].transform.localScale = Vector3.one;
            }
        }
    }
}
