using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class AirBubblesPickup : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D pickupArea;

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.LoopState.Playing
                || pickupArea == null || !pickupArea.enabled) return;

            Collider2D player = manager.PlayerCollider;
            PlayerStats stats = manager.PlayerStats;
            if (player == null || !player.enabled || !player.gameObject.activeInHierarchy
                || stats == null || !stats.isActiveAndEnabled || stats.IsDead) return;

            // The simulated player moves its Transform, so trigger callbacks are not required.
            Physics2D.SyncTransforms();
            ColliderDistance2D contact = pickupArea.Distance(player);
            if (!contact.isValid || contact.distance > 0f) return;

            pickupArea.enabled = false;
            stats.RestoreOxygenNotch();
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            pickupArea = GetComponent<BoxCollider2D>();
            pickupArea.isTrigger = true;
        }
    }
}
