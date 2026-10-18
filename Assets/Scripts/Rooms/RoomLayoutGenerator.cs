using UnityEngine;

namespace MousePlatformer
{
    [DisallowMultipleComponent]
    public sealed class RoomLayoutGenerator : MonoBehaviour
    {
        [SerializeField] private RoomDefinition startRoom;
        [SerializeField] private RoomDefinition[] rooms = new RoomDefinition[0];
        [SerializeField] private RoomDefinition[] terminalRooms = new RoomDefinition[0];
        [SerializeField, Range(1, 100)] private int roomCount = 10;
        [SerializeField] private int seed = 12345;
        [SerializeField, Min(1)] private int searchLimit = 50000;
        [SerializeField, Tooltip("Shared Grid cell size. Must match the prefabs.")]
        private Vector2 cellSize = Vector2.one;
        public Vector2 CellSize => cellSize;
        [SerializeField, HideInInspector] private Transform generatedRoot;
        [SerializeField, HideInInspector] private string lastReport;
        public RoomDefinition StartRoom => startRoom;
        public RoomDefinition[] Rooms => rooms;
        public RoomDefinition[] TerminalRooms => terminalRooms;
        public int RoomCount => roomCount;
        public int Seed => seed;
        public int SearchLimit => searchLimit;
        public Transform GeneratedRoot => generatedRoot;
        public string LastReport => lastReport;

        public void SetResult(Transform root, string report)
        {
            generatedRoot = root;
            lastReport = report;
        }
    }
}
