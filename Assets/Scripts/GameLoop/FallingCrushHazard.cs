using System.Collections;
using MousePlatformer;
using UnityEngine;
using GameJamPlus.UI;

namespace GameJamPlus.Gameplay
{
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    [RequireComponent(typeof(PropsMotion), typeof(AudioSource))]
    public sealed class FallingCrushHazard : MonoBehaviour
    {
        [SerializeField] private PropsMotion motion;
        [SerializeField, Tooltip("Conclusion requested from the central StageEndManager. None disables this ending.")]
        private StageEndCase ending = StageEndCase.Crushing;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip impactClip;
        [SerializeField, Range(0f, 1f), Tooltip("Crushing impact volume before Effects and Master volume settings.")]
        private float impactVolume = 0.8f;
        [SerializeField, Min(0f), Tooltip("Minimum downward crate speed in world units per second that can crush the player. Ignores resting contact and small settling movements.")]
        private float minimumFallSpeed = 0.5f;
        [SerializeField, Min(0f), Tooltip("Extra world-space head contact tolerance, added to the crate collision skin.")]
        private float contactTolerance = 0.02f;
        [SerializeField, Min(0f), Tooltip("Seconds continuously inside or outside water before disabling or enabling crushing damage. Returning to the previous medium cancels the pending change.")]
        private float waterTransitionDelay = 2f;
        private bool waterProtectionActive;
        private float waterTransitionElapsed;
        private Bounds previousBounds;
        private CratePose previousPose;
        private float previousFallSpeed;
        private bool triggered;
        private bool completed;
        private Coroutine animationRoutine;
        private StageEndRequest animationRequest;

        private void Awake()
        {
            if (motion == null) motion = GetComponent<PropsMotion>();

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            previousBounds = motion.Body.Shape.bounds;
            previousPose = new CratePose(motion.Body.Shape);
            waterProtectionActive = WaterVolume.SubmergedFraction(previousBounds) > 0f;
            waterTransitionElapsed = 0f;
            previousFallSpeed = 0f;
            triggered = false;
            completed = false;
            GameAudioSettings.EffectsVolumeChanged += ApplyEffectsVolume;
            ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
        }

        private void ApplyEffectsVolume(float volume) => audioSource.volume = impactVolume * volume;

        private void FixedUpdate()
        {
            BoxCollider2D shape = motion.Body.Shape;
            Bounds currentBounds = shape.bounds;
            CratePose currentPose = new CratePose(shape);
            // Check the final bounds: the prop may have entered water during this physics step.
            bool inWater = WaterVolume.SubmergedFraction(currentBounds) > 0f;
            UpdateWaterProtection(inWater);
            float fallSpeed = Mathf.Max(0f, -motion.FrameVelocity.y);
            GameManager manager = GameManager.Instance;
            if (!triggered && !waterProtectionActive && motion.isActiveAndEnabled && shape.enabled
                && manager != null && manager.State == GameManager.LoopState.Playing
                && HitsHead(currentBounds, currentPose, manager.PlayerCollider, Mathf.Max(previousFallSpeed, fallSpeed))
                && StageEndManager.TryBeginAnimated(ending, this, out animationRequest))
            {
                triggered = true;
                if (impactClip != null)
                {
                    ApplyEffectsVolume(GameAudioSettings.EffectsVolume);
                    audioSource.PlayOneShot(impactClip);
                }
                manager.Hud.PlayDamageFlash();
                animationRoutine = StartCoroutine(PlayCrush(manager.PlayerCrushVisual, manager.PlayerCollider.bounds));
            }
            previousBounds = currentBounds;
            previousPose = currentPose;
            previousFallSpeed = waterProtectionActive ? 0f : fallSpeed;
        }

        private void UpdateWaterProtection(bool inWater)
        {
            if (inWater == waterProtectionActive)
            {
                waterTransitionElapsed = 0f;
                return;
            }
            waterTransitionElapsed += Time.fixedDeltaTime;
            if (waterTransitionElapsed < waterTransitionDelay) return;
            waterProtectionActive = inWater;
            waterTransitionElapsed = 0f;
        }

        private IEnumerator PlayCrush(PlayerCrushVisual crushVisual, Bounds head)
        {
            if (crushVisual != null) yield return crushVisual.Play(height => FollowCrushedHead(height, head));
            else Debug.LogError("Assign Player Crush Visual on the Game Manager and the Player prefab to animate crushing.", this);
            completed = true;
            animationRoutine = null;
            animationRequest.Complete();
        }

        private void FollowCrushedHead(float headHeight, Bounds head)
        {
            BoxCollider2D shape = motion.Body.Shape;
            CratePose pose = new CratePose(shape);
            float underside = shape.bounds.min.y;
            if (!pose.AxisAligned && !pose.TryGetUnderside(head.min.x, head.max.x, out underside, out _)) return;
            float descent = underside - headHeight - motion.Body.Skin;
            if (descent <= 0f) return;
            // The game is paused during death, so follow the collapsing collider with a solid cast.
            Vector2 travel = motion.Body.AllowedDisplacement(Vector2.down * descent);
            transform.position += (Vector3)travel;
            Physics2D.SyncTransforms();
        }

        private bool HitsHead(Bounds after, CratePose pose, Collider2D player, float fallSpeed)
        {
            if (player == null || !player.enabled || !player.gameObject.activeInHierarchy) return false;
            Bounds head = player.bounds;
            float tolerance = motion.Body.Skin + contactTolerance;
            if (!previousPose.AxisAligned || !pose.AxisAligned || Mathf.Abs(Mathf.DeltaAngle(previousPose.Angle, pose.Angle)) > 0.01f)
                return SweptEdgeHitsHead(pose, head, tolerance, fallSpeed);
            if (fallSpeed < minimumFallSpeed) return false;

            Bounds before = previousBounds;
            float descent = before.min.y - after.min.y;
            if (descent <= 0.00001f) return false;
            if (before.min.y < head.max.y - tolerance || after.min.y > head.max.y + tolerance) return false;

            // Check the crate's swept position at head height, including casts stopping at the skin gap.
            float impact = Mathf.Clamp01((before.min.y - head.max.y) / descent);
            float left = Mathf.Lerp(before.min.x, after.min.x, impact);
            float right = Mathf.Lerp(before.max.x, after.max.x, impact);
            return Mathf.Min(right, head.max.x) - Mathf.Max(left, head.min.x) > 0.01f;
        }

        private bool SweptEdgeHitsHead(CratePose after, Bounds head, float tolerance, float fallSpeed)
        {
            float left = head.min.x + 0.005f;
            float right = head.max.x - 0.005f;
            if (left >= right) return false;
            float angle = Mathf.Abs(Mathf.DeltaAngle(previousPose.Angle, after.Angle));
            float travel = Vector2.Distance(previousPose.Center, after.Center);
            int samples = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(angle / 2f, travel / (head.size.x * 0.25f))), 1, 16);
            for (int i = 1; i <= samples; i++)
            {
                CratePose sample = CratePose.Interpolate(previousPose, after, (float)i / samples);
                if (!sample.TryGetUnderside(left, right, out float height, out Vector2 localPoint)
                    || height > head.max.y + tolerance) continue;

                // Compare the same point on the crate, since a different corner can be lowest after a turn.
                float startY = previousPose.WorldPoint(localPoint).y;
                float descent = startY - after.WorldPoint(localPoint).y;
                if (startY < head.max.y - tolerance || descent <= 0.00001f) continue;
                if (Mathf.Max(fallSpeed, descent / Time.fixedDeltaTime) >= minimumFallSpeed) return true;
            }
            return false;
        }

        private readonly struct CratePose
        {
            public readonly Vector2 Center;
            public readonly float Angle;
            private readonly Vector2 halfSize;
            private readonly Vector2 right;
            private readonly Vector2 up;
            public bool AxisAligned => Mathf.Abs(Angle - 90f * Mathf.Round(Angle / 90f)) < 0.01f;

            public CratePose(BoxCollider2D shape) : this(shape.transform.TransformPoint(shape.offset),
                shape.transform.eulerAngles.z,
                Vector2.Scale(shape.size * 0.5f, new Vector2(Mathf.Abs(shape.transform.lossyScale.x), Mathf.Abs(shape.transform.lossyScale.y))))
            {
            }

            private CratePose(Vector2 center, float angle, Vector2 halfSize)
            {
                Center = center;
                Angle = angle;
                this.halfSize = halfSize;
                float radians = angle * Mathf.Deg2Rad;
                right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                up = new Vector2(-right.y, right.x);
            }

            public static CratePose Interpolate(CratePose before, CratePose after, float fraction)
                => new CratePose(Vector2.Lerp(before.Center, after.Center, fraction),
                    Mathf.LerpAngle(before.Angle, after.Angle, fraction), Vector2.Lerp(before.halfSize, after.halfSize, fraction));

            public Vector2 WorldPoint(Vector2 local) => Center + right * local.x + up * local.y;

            public bool TryGetUnderside(float minX, float maxX, out float height, out Vector2 localPoint)
            {
                height = float.PositiveInfinity;
                localPoint = Vector2.zero;
                Vector2 bottomLeft = -halfSize;
                Vector2 bottomRight = new Vector2(halfSize.x, -halfSize.y);
                Vector2 topLeft = -bottomRight;
                Vector2 topRight = halfSize;
                CheckEdge(bottomLeft, bottomRight, minX, maxX, ref height, ref localPoint);
                CheckEdge(bottomRight, topRight, minX, maxX, ref height, ref localPoint);
                CheckEdge(topRight, topLeft, minX, maxX, ref height, ref localPoint);
                CheckEdge(topLeft, bottomLeft, minX, maxX, ref height, ref localPoint);
                return !float.IsPositiveInfinity(height);
            }

            private void CheckEdge(Vector2 start, Vector2 end, float minX, float maxX, ref float height, ref Vector2 localPoint)
            {
                Vector2 a = WorldPoint(start);
                Vector2 b = WorldPoint(end);
                float first = 0f;
                float last = 1f;
                float dx = b.x - a.x;
                if (Mathf.Abs(dx) < 0.00001f)
                {
                    if (a.x < minX || a.x > maxX) return;
                }
                else
                {
                    float atMin = (minX - a.x) / dx;
                    float atMax = (maxX - a.x) / dx;
                    first = Mathf.Max(0f, Mathf.Min(atMin, atMax));
                    last = Mathf.Min(1f, Mathf.Max(atMin, atMax));
                    if (first > last) return;
                }
                float fraction = a.y <= b.y ? first : last;
                float y = Mathf.Lerp(a.y, b.y, fraction);
                if (y >= height) return;
                height = y;
                localPoint = Vector2.Lerp(start, end, fraction);
            }
        }

        private void OnDisable()
        {
            GameAudioSettings.EffectsVolumeChanged -= ApplyEffectsVolume;
            if (audioSource != null) audioSource.Stop();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            if (triggered && !completed)
            {
                completed = true;
                animationRequest.Complete();
            }
        }
    }
}
