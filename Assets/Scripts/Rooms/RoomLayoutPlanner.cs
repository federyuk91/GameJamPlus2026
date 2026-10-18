using System;
using System.Collections.Generic;
using UnityEngine;

namespace MousePlatformer
{
    public readonly struct RoomPlacement
    {
        public RoomDefinition Prefab { get; }
        public Vector2 Position { get; }
        public RoomPlacement(RoomDefinition prefab, Vector2 position)
        {
            Prefab = prefab;
            Position = position;
        }
        public Rect Bounds
        {
            get
            {
                Rect bounds = Prefab.WorldFootprint();
                bounds.position += Position - (Vector2)Prefab.transform.position;
                return bounds;
            }
        }
    }

    // Plan entirely from prefab data before modifying any scene objects.
    public sealed class RoomLayoutPlanner
    {
        private readonly struct OpenPort
        {
            public RoomDirection Direction { get; }
            public Vector2 Position { get; }
            public OpenPort(RoomDirection direction, Vector2 position)
            {
                Direction = direction;
                Position = position;
            }
        }
        private readonly struct Candidate
        {
            public RoomDefinition Room { get; }
            public int PortIndex { get; }
            public Candidate(RoomDefinition room, int portIndex)
            {
                Room = room;
                PortIndex = portIndex;
            }
        }
        private readonly List<RoomDefinition> catalog = new List<RoomDefinition>();
        private readonly List<RoomPlacement> plan = new List<RoomPlacement>();
        private System.Random random;
        private int targetCount;
        private int steps;
        private int limit;
        private Vector2 cellSize;

        public bool TryPlan(RoomLayoutGenerator settings, out List<RoomPlacement> result, out string report)
        {
            result = null;
            catalog.Clear();
            plan.Clear();
            steps = 0;
            targetCount = Mathf.Clamp(settings.RoomCount, 1, 100);
            limit = Mathf.Max(1, settings.SearchLimit);
            random = new System.Random(settings.Seed);
            cellSize = settings.CellSize;
            if (cellSize.x <= 0f || cellSize.y <= 0f)
            {
                report = "Shared Grid cells must have positive dimensions.";
                return false;
            }
            if (!ValidateRoom(settings.StartRoom, out report)) return false;
            foreach (RoomDefinition room in settings.Rooms ?? Array.Empty<RoomDefinition>())
            {
                if (!ValidateRoom(room, out report)) return false;
                if (!catalog.Contains(room)) catalog.Add(room);
            }
            foreach (RoomDefinition room in settings.TerminalRooms ?? Array.Empty<RoomDefinition>())
            {
                if (!ValidateRoom(room, out report)) return false;
                if (room.PortCount != 1)
                {
                    report = room.name + ": a terminal room must have exactly one exit.";
                    return false;
                }
                if (!catalog.Contains(room)) catalog.Add(room);
            }
            RoomPlacement start = new RoomPlacement(settings.StartRoom, -settings.StartRoom.GridOffset);
            plan.Add(start);
            List<OpenPort> ports = PortsFor(start, -1);
            if (!Search(ports))
            {
                report = steps >= limit ? "Search limit reached. Change the seed, catalog or Search Limit."
                    : "No complete layout found: check room count, footprints and terminals for all required directions.";
                return false;
            }
            result = new List<RoomPlacement>(plan);
            report = "Generate " + plan.Count + " rooms, including terminals; all exits connected. Seed " + settings.Seed + ".";
            return true;
        }

        private bool ValidateRoom(RoomDefinition room, out string report)
        {
            if (room == null)
            {
                report = "Assign all room prefabs.";
                return false;
            }
            if (!room.Validate(out string error))
            {
                report = room.name + ": " + error;
                return false;
            }
            if (((Vector2)room.LayoutGrid.cellSize - cellSize).sqrMagnitude > 0.000001f)
            {
                report = room.name + ": cell size does not match the shared Grid.";
                return false;
            }
            report = string.Empty;
            return true;
        }

        private bool Search(List<OpenPort> ports)
        {
            if (plan.Count == targetCount) return ports.Count == 0;
            if (ports.Count == 0 || ports.Count > targetCount - plan.Count || steps >= limit) return false;
            int portIndex = random.Next(ports.Count);
            OpenPort socket = ports[portIndex];
            List<Candidate> candidates = new List<Candidate>();
            foreach (RoomDefinition room in catalog)
            for (int i = 0; i < room.PortCount; i++)
                if (room.Port(i).Direction == RoomPort.Opposite(socket.Direction)) candidates.Add(new Candidate(room, i));
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                Candidate swap = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = swap;
            }
            foreach (Candidate candidate in candidates)
            {
                if (++steps > limit) return false;
                Vector2 offset = candidate.Room.PortOffset(candidate.PortIndex);
                Vector2 position = socket.Position - offset;
                Vector2 gridOrigin = position + candidate.Room.GridOffset;
                gridOrigin.x = Mathf.Round(gridOrigin.x / cellSize.x) * cellSize.x;
                gridOrigin.y = Mathf.Round(gridOrigin.y / cellSize.y) * cellSize.y;
                RoomPlacement placement = new RoomPlacement(candidate.Room, gridOrigin - candidate.Room.GridOffset);
                if (Overlaps(placement.Bounds)) continue;
                List<OpenPort> next = new List<OpenPort>(ports);
                next.RemoveAt(portIndex);
                next.AddRange(PortsFor(placement, candidate.PortIndex));
                plan.Add(placement);
                if (Search(next)) return true;
                plan.RemoveAt(plan.Count - 1);
            }
            return false;
        }

        private bool Overlaps(Rect bounds)
        {
            foreach (RoomPlacement placement in plan)
            {
                Rect other = placement.Bounds;
                float width = Mathf.Min(bounds.xMax, other.xMax) - Mathf.Max(bounds.xMin, other.xMin);
                float height = Mathf.Min(bounds.yMax, other.yMax) - Mathf.Max(bounds.yMin, other.yMin);
                // Shared borders are allowed; positive area overlap is not.
                if (width > 0.001f && height > 0.001f) return true;
            }
            return false;
        }

        private static List<OpenPort> PortsFor(RoomPlacement placement, int connectedPort)
        {
            List<OpenPort> ports = new List<OpenPort>();
            for (int i = 0; i < placement.Prefab.PortCount; i++)
            {
                if (i == connectedPort) continue;
                RoomPort port = placement.Prefab.Port(i);
                Vector2 offset = placement.Prefab.PortOffset(i);
                ports.Add(new OpenPort(port.Direction, placement.Position + offset));
            }
            return ports;
        }
    }
}
