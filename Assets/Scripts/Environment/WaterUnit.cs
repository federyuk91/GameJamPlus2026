using GameJamPlus.Gameplay;
using UnityEngine;

namespace MousePlatformer
{
    [DefaultExecutionOrder(-450), DisallowMultipleComponent, RequireComponent(typeof(WaterVolume))]
    public sealed class WaterUnit : MonoBehaviour
    {
        [SerializeField] private WaterVolume water;
        [SerializeField] private WaterGeometryBaker geometry;
        [SerializeField, Min(0.01f)] private float waterAmount = 1f;
        [SerializeField, Min(0.01f)] private float width = 1f;
        [SerializeField, Min(0f)] private float fallAcceleration = 12f;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 8f;
        [SerializeField, Min(0f)] private float surfaceFlowSpeed = 2f;
        [SerializeField, Min(1f)] private float maximumSpreadWidth = 3f;
        private float currentWidth;
        private float flowDirection;
        private float amount;
        private float fallSpeed;
        private float restBottom;
        private bool resting;

        private void Awake()
        {
            if (water == null) water = GetComponent<WaterVolume>();
        }

        private void OnEnable()
        {
            amount = waterAmount;
            currentWidth = width;
            flowDirection = 0f;
            fallSpeed = 0f;
            resting = false;
        }

        private void FixedUpdate()
        {
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.State != GameManager.LoopState.Playing) return;
            if (geometry == null || !geometry.isActiveAndEnabled) geometry = WaterGeometryBaker.ForPoint(transform.position, gameObject.scene);
            if (geometry == null || !geometry.HasBake) return;
            if (!resting) currentWidth = Mathf.MoveTowards(currentWidth, width, Time.fixedDeltaTime * 2f);
            Rect bounds = new Rect((Vector2)transform.position - new Vector2(currentWidth, amount / currentWidth) * 0.5f,
                new Vector2(currentWidth, amount / currentWidth));
            if (resting)
            {
                bounds.y = restBottom;
                FlowOnSurface(ref bounds);
            }
            fallSpeed = Mathf.Min(maximumFallSpeed, fallSpeed + fallAcceleration * Time.fixedDeltaTime);
            float requested = fallSpeed * Time.fixedDeltaTime;
            float travel = geometry.DownwardTravel(bounds, requested);
            bounds.y -= travel;
            bool landed = travel + 0.00001f < requested;
            Vector2 foot = new Vector2(bounds.center.x, bounds.yMin + 0.01f);
            WaterBasin basin = geometry.BasinAt(foot);
            if (basin != null && (landed || bounds.yMin <= basin.SurfaceHeight))
            {
                amount -= basin.ReceiveWater(amount);
                basin.NotifyFlow(1f, foot, Vector2.down);
                if (amount <= 0.0001f)
                {
                    gameObject.SetActive(false);
                    return;
                }
                bounds.height = amount / currentWidth;
            }
            if (landed)
            {
                fallSpeed = 0f;
                resting = true;
                restBottom = bounds.yMin;
            }
            else resting = false;
            water.SetWorldBounds(bounds);
            water.SetPresentation(bounds.yMax, bounds.yMin, flowDirection, foot);
        }

        private void FlowOnSurface(ref Rect bounds)
        {
            float preferred = flowDirection == 0f ? -1f : Mathf.Sign(flowDirection);
            for (int side = 0; side < 2; side++)
            {
                float direction = side == 0 ? preferred : -preferred;
                for (int step = 1; step <= 8; step++)
                {
                    float offset = direction * step * 0.25f;
                    if (Mathf.Abs(geometry.HorizontalTravel(bounds, offset)) + 0.001f < Mathf.Abs(offset)) break;
                    Rect candidate = bounds;
                    candidate.x += offset;
                    if (geometry.DownwardTravel(candidate, 0.25f) < 0.1f) continue;
                    flowDirection = direction;
                    bounds.x += geometry.HorizontalTravel(bounds, direction * Mathf.Min(Mathf.Abs(offset), surfaceFlowSpeed * Time.fixedDeltaTime));
                    return;
                }
            }
            flowDirection = 0f;
            float expansion = Mathf.Min(Mathf.Max(width, maximumSpreadWidth) - currentWidth, surfaceFlowSpeed * Time.fixedDeltaTime);
            if (expansion <= 0f) return;
            float left = -geometry.HorizontalTravel(bounds, -expansion * 0.5f);
            float right = geometry.HorizontalTravel(bounds, expansion * 0.5f);
            bounds.xMin -= left;
            bounds.xMax += right;
            currentWidth = bounds.width;
            bounds.height = amount / currentWidth;
        }
    }
}
