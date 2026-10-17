using UnityEngine;

namespace GameJamPlus.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SpawnPoint : MonoBehaviour
    {
        public Vector2 Position => transform.position;
        // The manager evaluates spawn points in Awake, before child OnEnable callbacks.
        public bool CanSpawn => enabled && gameObject.activeInHierarchy && gameObject.scene.IsValid();

        private void OnDrawGizmos()
        {
            Color previous = Gizmos.color;
            Gizmos.color = new Color(0.2f, 1f, 0.75f);
            Vector3 center = transform.position;
            Gizmos.DrawWireSphere(center, 0.15f);
            Gizmos.DrawLine(center + Vector3.left * 0.3f, center + Vector3.right * 0.3f);
            Gizmos.DrawLine(center + Vector3.down * 0.3f, center + Vector3.up * 0.3f);
            Gizmos.color = previous;
        }
    }
}
