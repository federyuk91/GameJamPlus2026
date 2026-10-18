using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class PeriodicSpriteEffect : MonoBehaviour
    {
        [SerializeField] private Animator effectPrefab;
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField, Min(0.1f)] private float interval = 10f;
        [SerializeField] private string animationState = "Base Layer.Loop";
        private Animator effect;
        private float timeUntilNext;
        private int stateHash;
        private bool playing;

        private void Awake() => stateHash = Animator.StringToHash(animationState);

        private void OnEnable() => timeUntilNext = Mathf.Max(0.1f, interval);

        private void LateUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (Time.deltaTime <= 0f || (manager != null && manager.State != GameManager.LoopState.Playing)) return;
            timeUntilNext -= Time.deltaTime;
            if (playing)
            {
                // Read the actual Animator progress, so changing the clip speed still plays exactly one cycle.
                if (effect == null || effect.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f) return;
                effect.gameObject.SetActive(false);
                effect.transform.position = transform.position + positionOffset;
                playing = false;
            }
            if (timeUntilNext > 0f || effectPrefab == null) return;
            if (effect == null)
                effect = Instantiate(effectPrefab, transform.position + positionOffset, Quaternion.identity);
            // Keep the effect independent of the swimming fish during its animation.
            effect.transform.position = transform.position + positionOffset;
            effect.gameObject.SetActive(true);
            effect.Play(stateHash, 0, 0f);
            effect.Update(0f);
            playing = true;
            timeUntilNext = Mathf.Max(0.1f, interval);
        }

        private void OnDisable()
        {
            playing = false;
            if (effect != null) effect.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (effect != null) Destroy(effect.gameObject);
        }
    }
}
