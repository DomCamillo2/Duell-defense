using System;

namespace DefenseDuell.Core
{
    [Serializable]
    public readonly struct MapRectangle
    {
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }
        public MapRectangle(int minX, int minY, int maxX, int maxY)
        { MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; }
    }

    public readonly struct TowerPlacement
    {
        public MapPoint Position { get; }
        public int Slot { get; }
        public int SiteId { get; }
        public TowerPlacement(MapPoint position, int slot, int siteId)
        { Position = position; Slot = slot; SiteId = siteId; }
    }

    /// <summary>Shared authoritative geometry for preview, purchases and decoded snapshots.</summary>
    public static class PlacementValidator
    {
        public const int BuildingRadius = 30;
        public const int PathClearance = 70;
        public const int BuildingSeparation = 60;
        // Legacy content/pool sizing metadata only. These values never limit purchases, slots or live entities.
        public const int CombatCapacity = 30;
        public const int MineCapacity = 2;
        public const int RenderCapacity = 32;

        public static ErrorCode ValidateBuild(GameContent content, MatchState state, PlayerSlot owner,
            int kind, int target, MapPoint requested, out TowerPlacement placement)
        {
            placement = default;
            if (!state.ValidPlayer(owner)) return ErrorCode.InvalidPlayer;
            if (state.Phase != MatchPhase.Playing) return ErrorCode.WrongPhase;
            TowerDefinition[] definitions = content.TowerData;
            if (kind < 0 || kind >= definitions.Length) return ErrorCode.InvalidKind;
            if ((state.Players[(int)owner].AvailableTowerMask & (1 << kind)) == 0) return ErrorCode.TowerLocked;
            bool economy = definitions[kind].IsEconomy;
            bool freeBuilding = content.Map.FreePlacement;
            MapPoint position;
            int siteId;
            if (freeBuilding)
            {
                if (target != -1) return ErrorCode.InvalidTarget;
                position = requested; siteId = -1;
            }
            else
            {
                if (!content.Map.CanBuildAt(target, economy)) return ErrorCode.InvalidTarget;
                siteId = target; position = content.Map.SlotData[target];
            }
            ErrorCode geometry = ValidatePosition(content, state, owner, kind, siteId, position);
            if (geometry != ErrorCode.None) return geometry;
            int renderSlot = siteId;
            if (content.Map.FreePlacement)
            {
                // Find an unused stable slot; initial render-pool size is not a game limit.
                var used = new System.Collections.Generic.HashSet<int>();
                foreach (TowerState tower in state.Towers) if (tower.Owner == owner) used.Add(tower.Slot);
                renderSlot = 0;
                while (used.Contains(renderSlot)) renderSlot = checked(renderSlot + 1);
            }
            if (state.Players[(int)owner].Gold < definitions[kind].Price) return ErrorCode.InsufficientGold;
            placement = new TowerPlacement(position, renderSlot, siteId);
            return ErrorCode.None;
        }

        /// <summary>No phase, economy or purchase-cap checks. Ignores the inspected entity when decoding a snapshot.</summary>
        public static ErrorCode ValidatePosition(GameContent content, MatchState state, PlayerSlot owner,
            int kind, int siteId, MapPoint position, int ignoredTowerId = -1)
        {
            if (!state.ValidPlayer(owner)) return ErrorCode.InvalidPlayer;
            TowerDefinition[] definitions = content.TowerData;
            if (kind < 0 || kind >= definitions.Length) return ErrorCode.InvalidKind;
            MapDefinition map = content.Map;
            bool economy = definitions[kind].IsEconomy;
            if (!map.FreePlacement)
            {
                if (!map.CanBuildAt(siteId, economy)) return ErrorCode.InvalidTarget;
                MapPoint fixedPosition = map.SlotData[siteId];
                if (position.X != fixedPosition.X || position.Y != fixedPosition.Y) return ErrorCode.InvalidTarget;
                foreach (TowerState tower in state.Towers)
                    if (tower.Owner == owner && tower.Id != ignoredTowerId && tower.SiteId == siteId)
                        return ErrorCode.Occupied;
                // Fixed legacy positions have their original 40-unit route clearance.
                if (!map.FreePlacement) return ErrorCode.None;
            }
            else if (siteId != -1) return ErrorCode.InvalidTarget;

            ErrorCode terrain = ValidateTerrain(map, position);
            if (terrain != ErrorCode.None) return terrain;
            foreach (TowerState tower in state.Towers)
            {
                if (tower.Owner != owner || tower.Id == ignoredTowerId) continue;
                if (DistanceSquared(position, tower.Position) < BuildingSeparation * BuildingSeparation)
                    return ErrorCode.TooClose;
            }
            return ErrorCode.None;
        }

        internal static ErrorCode ValidateTerrain(MapDefinition map, MapPoint position)
        {
            if (position.X < BuildingRadius || position.Y < BuildingRadius
                || position.X > map.Width - BuildingRadius || position.Y > map.Height - BuildingRadius)
                return ErrorCode.OutsideMap;
            MapPoint[] route = map.WaypointData;
            for (int i = 0; i + 1 < route.Length; i++)
                if (!SegmentClearance(position, route[i], route[i + 1], PathClearance)) return ErrorCode.OnPath;
            foreach (MapRectangle rectangle in map.BlockerData)
            {
                long dx = position.X < rectangle.MinX ? (long)rectangle.MinX - position.X
                    : position.X > rectangle.MaxX ? (long)position.X - rectangle.MaxX : 0;
                long dy = position.Y < rectangle.MinY ? (long)rectangle.MinY - position.Y
                    : position.Y > rectangle.MaxY ? (long)position.Y - rectangle.MaxY : 0;
                if (dx * dx + dy * dy < BuildingRadius * BuildingRadius) return ErrorCode.BlockedArea;
            }
            return ErrorCode.None;
        }

        internal static bool SegmentClearance(MapPoint point, MapPoint start, MapPoint end, int clearance)
        {
            long dx = (long)end.X - start.X, dy = (long)end.Y - start.Y;
            long px = (long)point.X - start.X, py = (long)point.Y - start.Y;
            long lengthSquared = dx * dx + dy * dy, projection = px * dx + py * dy;
            long radiusSquared = (long)clearance * clearance;
            if (projection <= 0) return px * px + py * py >= radiusSquared;
            if (projection >= lengthSquared)
            { px = (long)point.X - end.X; py = (long)point.Y - end.Y; return px * px + py * py >= radiusSquared; }
            long cross = dx * py - dy * px;
            return cross * cross >= radiusSquared * lengthSquared;
        }

        private static long DistanceSquared(MapPoint a, MapPoint b)
        { long dx = (long)a.X - b.X, dy = (long)a.Y - b.Y; return dx * dx + dy * dy; }
    }
}
