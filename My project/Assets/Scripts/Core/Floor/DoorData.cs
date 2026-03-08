using UnityEngine;

namespace HitWaves.Core.Floor
{
    public class DoorData
    {
        private const string LOG_TAG = "DoorData";

        public RoomData RoomA { get; }
        public RoomData RoomB { get; }
        public Vector2 WorldPosition { get; }
        public WallSide SideInA { get; }
        public WallSide SideInB { get; }
        public float Width { get; }

        public DoorData(RoomData roomA, RoomData roomB, Vector2 worldPosition,
            WallSide sideInA, WallSide sideInB, float width)
        {
            RoomA = roomA;
            RoomB = roomB;
            WorldPosition = worldPosition;
            SideInA = sideInA;
            SideInB = sideInB;
            Width = width;

            DebugLogger.Log(LOG_TAG,
                $"생성 — #{roomA.Id} ({sideInA}) ↔ #{roomB.Id} ({sideInB}), " +
                $"pos: {worldPosition}, width: {width}", null);
        }

        /// <summary>
        /// 이 문을 통해 반대편 방을 반환한다.
        /// currentRoom이 RoomA면 RoomB를, RoomB면 RoomA를 반환.
        /// </summary>
        public RoomData GetOtherRoom(RoomData currentRoom)
        {
            if (currentRoom == RoomA) return RoomB;
            if (currentRoom == RoomB) return RoomA;

            Debug.LogWarning($"[{LOG_TAG}] GetOtherRoom: 해당 방(#{currentRoom.Id})이 이 문에 속하지 않음");
            return null;
        }

        /// <summary>
        /// 지정한 방 기준으로 문이 있는 벽면을 반환한다.
        /// </summary>
        public WallSide GetSideFor(RoomData room)
        {
            if (room == RoomA) return SideInA;
            if (room == RoomB) return SideInB;

            Debug.LogWarning($"[{LOG_TAG}] GetSideFor: 해당 방(#{room.Id})이 이 문에 속하지 않음");
            return SideInA;
        }
    }
}
