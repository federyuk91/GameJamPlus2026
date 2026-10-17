using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class Ladder : MonoBehaviour
    {
        private BoxCollider2D shape;
        public BoxCollider2D Shape => shape != null ? shape : shape = GetComponent<BoxCollider2D>();
        public bool Available => isActiveAndEnabled && Shape.enabled && Shape.isTrigger && AxisScale >= 0.0001f;
        public float Bottom => Shape.offset.y - Shape.size.y * 0.5f;
        public float Top => Shape.offset.y + Shape.size.y * 0.5f;
        public float AxisScale => transform.TransformVector(Vector3.up).magnitude;
        public Vector2 Point(float height) => transform.TransformPoint(new Vector3(Shape.offset.x, height, 0f));

        public float ClosestHeight(Vector2 point) => Mathf.Clamp(transform.InverseTransformPoint(point).y, Bottom, Top);
        private void Reset() => Shape.isTrigger = true;
        private void Awake() => Shape.isTrigger = true;
        private void OnValidate() => Shape.isTrigger = true;
    }
}
