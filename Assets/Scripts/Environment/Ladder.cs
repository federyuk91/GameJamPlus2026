using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class Ladder : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Number of one-unit pieces. The ladder extends downward.")]
        private int length = 5;
        [SerializeField] private BoxCollider2D shape;
        [SerializeField] private SpriteRenderer pieceTemplate;
        [SerializeField, HideInInspector] private SpriteRenderer[] pieces = new SpriteRenderer[0];
        private bool rebuildPending;
        public int Length => length;
        public SpriteRenderer PieceTemplate => pieceTemplate;
        public int PieceCount => pieces != null ? pieces.Length : 0;
        public SpriteRenderer Piece(int index) => pieces[index];
        public BoxCollider2D Shape => shape != null ? shape : shape = GetComponent<BoxCollider2D>();
        public bool Available => isActiveAndEnabled && Shape.enabled && Shape.isTrigger && AxisScale >= 0.0001f;
        public float Bottom => Shape.offset.y - Shape.size.y * 0.5f;
        public float Top => Shape.offset.y + Shape.size.y * 0.5f;
        public float AxisScale => transform.TransformVector(Vector3.up).magnitude;
        public Vector2 Point(float height) => transform.TransformPoint(new Vector3(Shape.offset.x, height, 0f));
        public float ClosestHeight(Vector2 point) => Mathf.Clamp(transform.InverseTransformPoint(point).y, Bottom, Top);

        public void SetLength(int value)
        {
            length = Mathf.Max(1, value);
            rebuildPending = true;
            if (Application.IsPlaying(gameObject)) RebuildRuntime();
        }

        public void SetPieces(SpriteRenderer[] value)
        {
            pieces = value;
            if (PieceCount > 0) pieceTemplate = pieces[0];
            AlignPiecesAndCollider();
        }

        public void AlignPiecesAndCollider()
        {
            length = Mathf.Max(1, length);
            for (int i = 0; i < PieceCount; i++)
            {
                if (pieces[i] == null) continue;
                Transform piece = pieces[i].transform;
                piece.localPosition = new Vector3(0f, -i, 0f);
                piece.localRotation = Quaternion.identity;
                piece.localScale = Vector3.one;
                piece.name = i == 0 ? "Stair_piece" : "Stair_piece (" + i + ")";
            }
            Shape.isTrigger = true;
            Shape.size = new Vector2(Shape.size.x, length);
            Shape.offset = new Vector2(Shape.offset.x, -(length - 1) * 0.5f);
            rebuildPending = false;
        }

        private void RebuildRuntime()
        {
            rebuildPending = false;
            // Legacy ladders without a visual template retain their existing collider setup.
            if (pieceTemplate == null) return;
            List<SpriteRenderer> updated = new List<SpriteRenderer>();
            for (int i = 0; i < PieceCount; i++) if (pieces[i] != null) updated.Add(pieces[i]);
            if (updated.Count == 0) updated.Add(pieceTemplate);
            while (updated.Count < length)
            {
                GameObject piece = Instantiate(pieceTemplate.gameObject, transform);
                piece.SetActive(true);
                updated.Add(piece.GetComponent<SpriteRenderer>());
            }
            while (updated.Count > length)
            {
                int index = updated.Count - 1;
                GameObject piece = updated[index].gameObject;
                updated.RemoveAt(index);
                piece.SetActive(false);
                Destroy(piece);
            }
            SetPieces(updated.ToArray());
        }

        private void Reset() => Shape.isTrigger = true;
        private void Awake()
        {
            Shape.isTrigger = true;
            if (pieceTemplate != null) RebuildRuntime();
        }
        private void OnValidate()
        {
            length = Mathf.Max(1, length);
            rebuildPending = true;
        }
        private void Update()
        {
            if (rebuildPending) RebuildRuntime();
        }
    }
}
