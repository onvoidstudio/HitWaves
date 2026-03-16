using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HitWaves.Core.Floor
{
    public class FloorRenderer : MonoBehaviour
    {
        private const string LOG_TAG = "FloorRenderer";

        [Header("바닥 텍스처")]
        [Tooltip("큰 바닥 텍스처 (256x256 단위로 자동 슬라이스)")]
        [SerializeField] private Texture2D _floorTexture;

        [Tooltip("슬라이스 크기 (픽셀)")]
        [SerializeField] private int _sliceSize = 256;

        [Header("정렬")]
        [Tooltip("바닥 Tilemap의 Sorting Layer 이름")]
        [SerializeField] private string _sortingLayerName = "Default";

        [Tooltip("바닥 Tilemap의 Order in Layer")]
        [SerializeField] private int _orderInLayer = -10;

        private Grid _grid;
        private Tile[] _slicedTiles;
        private int _cols;
        private int _rows;

        private Dictionary<int, GameObject> _roomTilemaps = new Dictionary<int, GameObject>();

        /// <summary>
        /// 큰 텍스처를 sliceSize x sliceSize 크기로 잘라서 Tile 배열을 생성한다.
        /// </summary>
        private void SliceTexture()
        {
            if (_floorTexture == null)
            {
                DebugLogger.LogWarning(LOG_TAG, "바닥 텍스처가 할당되지 않음", this);
                return;
            }

            _cols = _floorTexture.width / _sliceSize;
            _rows = _floorTexture.height / _sliceSize;

            if (_cols <= 0 || _rows <= 0)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    $"텍스처 크기({_floorTexture.width}x{_floorTexture.height})가 " +
                    $"슬라이스 크기({_sliceSize})보다 작음", this);
                return;
            }

            _slicedTiles = new Tile[_cols * _rows];

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _cols; x++)
                {
                    int pixelX = x * _sliceSize;
                    int pixelY = y * _sliceSize;

                    Rect rect = new Rect(pixelX, pixelY, _sliceSize, _sliceSize);
                    Vector2 pivot = new Vector2(0.5f, 0.5f);

                    Sprite sprite = Sprite.Create(
                        _floorTexture, rect, pivot, _sliceSize);
                    sprite.name = $"floor_slice_{x}_{y}";

                    Tile tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;

                    _slicedTiles[y * _cols + x] = tile;
                }
            }

            DebugLogger.Log(LOG_TAG,
                $"텍스처 슬라이스 완료 — {_cols}x{_rows} = {_slicedTiles.Length}조각 " +
                $"(원본: {_floorTexture.width}x{_floorTexture.height}, 슬라이스: {_sliceSize}px)", this);
        }

        /// <summary>
        /// 셀 좌표에 맞는 타일 조각을 반환한다.
        /// </summary>
        private Tile GetTileForCell(int cellX, int cellY)
        {
            int tx = ((cellX % _cols) + _cols) % _cols;
            int ty = ((cellY % _rows) + _rows) % _rows;

            return _slicedTiles[ty * _cols + tx];
        }

        /// <summary>
        /// 방 하나의 바닥을 별도 Tilemap에 채운다.
        /// </summary>
        private void RenderRoom(RoomData room)
        {
            if (_slicedTiles == null || _slicedTiles.Length == 0) return;

            GameObject tilemapGo = new GameObject($"FloorTilemap_Room{room.Id}");
            tilemapGo.transform.SetParent(_grid.transform);

            Tilemap tilemap = tilemapGo.AddComponent<Tilemap>();

            TilemapRenderer renderer = tilemapGo.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = _orderInLayer;

            Rect worldRect = room.WorldRect;

            int minX = Mathf.FloorToInt(worldRect.xMin);
            int minY = Mathf.FloorToInt(worldRect.yMin);
            int maxX = Mathf.CeilToInt(worldRect.xMax);
            int maxY = Mathf.CeilToInt(worldRect.yMax);

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    int localX = x - minX;
                    int localY = y - minY;
                    Tile tile = GetTileForCell(localX, localY);
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            tilemapGo.SetActive(false);
            _roomTilemaps[room.Id] = tilemapGo;

            DebugLogger.Log(LOG_TAG,
                $"RenderRoom #{room.Id} [{room.Label}] — " +
                $"셀 범위: ({minX},{minY})~({maxX},{maxY})", this);
        }

        /// <summary>
        /// 모든 방의 바닥을 렌더링한다. 외부에서 호출하는 진입점.
        /// </summary>
        public void RenderAll(List<RoomData> rooms)
        {
            Clear();

            if (_slicedTiles == null)
            {
                SliceTexture();
            }

            CreateGrid();

            DebugLogger.Log(LOG_TAG, $"RenderAll 시작 — {rooms.Count}개 방", this);

            for (int i = 0; i < rooms.Count; i++)
            {
                RenderRoom(rooms[i]);
            }

            DebugLogger.Log(LOG_TAG, "RenderAll 완료", this);
        }

        /// <summary>
        /// 지정한 방의 바닥만 보이게 하고 나머지는 숨긴다.
        /// </summary>
        public void ShowRoom(int roomId)
        {
            foreach (var kvp in _roomTilemaps)
            {
                kvp.Value.SetActive(kvp.Key == roomId);
            }

            DebugLogger.Log(LOG_TAG, $"ShowRoom #{roomId} — 나머지 숨김", this);
        }

        /// <summary>
        /// Grid를 런타임에 생성한다.
        /// </summary>
        private void CreateGrid()
        {
            GameObject gridGo = new GameObject("FloorGrid");
            gridGo.transform.SetParent(transform);

            _grid = gridGo.AddComponent<Grid>();
            _grid.cellSize = new Vector3(1f, 1f, 0f);

            DebugLogger.Log(LOG_TAG, "Grid 생성 완료", this);
        }

        /// <summary>
        /// 기존 렌더링된 바닥을 전부 제거한다.
        /// </summary>
        public void Clear()
        {
            if (_grid != null)
            {
                Destroy(_grid.gameObject);
                _grid = null;
                _roomTilemaps.Clear();
                DebugLogger.Log(LOG_TAG, "Clear — 기존 바닥 제거", this);
            }
        }
    }
}
