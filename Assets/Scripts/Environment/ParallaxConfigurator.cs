using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class ParallaxConfigurator : MonoBehaviour
    {
        public enum Depth
        {
            ForegroundDeskDecorations,
            Terrain,
            WaterAndDiggableGround,
            GameplayDecorations,
            PlayerAndCharacters,
            CollectiblesAndProps,
            FixedDesk,
            BackgroundDeskDecorations,
            BackgroundScientists,
            WallElements,
            BehindWindow,
            Lens
        }

        [Serializable]
        public sealed class DepthPlane
        {
            [SerializeField, Tooltip("Category ordered from the foreground to the farthest background.")]
            private Depth depth;
            [SerializeField, Tooltip("Parent containing the objects assigned to this plane, including inactive children.")]
            private GameObject parent;
            [SerializeField, Tooltip("World Z assigned to the parent. Descendant local Z values are reset to zero.")]
            private float worldZ;
            [SerializeField, Tooltip("Sorting Layer shared by the renderers and Sorting Groups in this plane.")]
            private string sortingLayer;
            [SerializeField, Range(-32768, 32767), Tooltip("Plane priority within its Sorting Layer. Higher values draw in front.")]
            private int sortingOrder;
            [SerializeField, Tooltip("Group the whole plane for stable ordering of its renderers.")]
            private bool useSortingGroup;
            [SerializeField, Tooltip("Rest decorations in this plane on Desk Surface using the same generic anchor, including foreground and background objects.")]
            private bool restOnDesk;

            public Depth Category => depth;
            public GameObject Parent => parent;
            public float WorldZ => worldZ;
            public string SortingLayer => sortingLayer;
            public int SortingOrder => sortingOrder;
            public bool UseSortingGroup => useSortingGroup;
            public bool RestOnDesk => restOnDesk;

            public DepthPlane(Depth depth, float worldZ, string sortingLayer, int sortingOrder, bool useSortingGroup = true, bool restOnDesk = false)
            {
                this.depth = depth;
                this.worldZ = worldZ;
                this.sortingLayer = sortingLayer;
                this.sortingOrder = sortingOrder;
                this.useSortingGroup = useSortingGroup;
                this.restOnDesk = restOnDesk;
            }
        }

        [SerializeField, Tooltip("Desk surface used by planes with Rest On Desk enabled. Gameplay props are not affected unless explicitly assigned to such a plane.")]
        private SpriteRenderer deskSurface;

        [SerializeField, Tooltip("Assign one independent parent per depth. Configuration is applied only through the Inspector button, with Undo support.")]
        private DepthPlane[] depths =
        {
            new DepthPlane(Depth.ForegroundDeskDecorations, -4f, "Default", 600, restOnDesk: true),
            new DepthPlane(Depth.Terrain, -3f, "Default", 500),
            new DepthPlane(Depth.WaterAndDiggableGround, -2f, "Default", 400),
            new DepthPlane(Depth.GameplayDecorations, -1f, "Default", 300),
            new DepthPlane(Depth.PlayerAndCharacters, 0f, "Default", 200),
            new DepthPlane(Depth.CollectiblesAndProps, 1f, "Default", 100),
            new DepthPlane(Depth.Lens, 1.5f, "Default", 50),
            new DepthPlane(Depth.BackgroundDeskDecorations, 2f, "Lens Background", 500, restOnDesk: true),
            new DepthPlane(Depth.FixedDesk, 3f, "Lens Background", 400),
            new DepthPlane(Depth.BackgroundScientists, 4f, "Lens Background", 300, restOnDesk: true),
            new DepthPlane(Depth.WallElements, 5f, "Lens Background", 200),
            new DepthPlane(Depth.BehindWindow, 6f, "Lens Background", 100)
        };

        public IReadOnlyList<DepthPlane> Depths => depths;
        public SpriteRenderer DeskSurface => deskSurface;
    }
}
