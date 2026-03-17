using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HitWaves.UI
{
    /// <summary>
    /// 인벤토리 슬롯 1개의 드래그 앤 드롭 처리.
    /// 슬롯 간 교환 + UI 밖 드롭 시 월드에 아이템 버리기.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private int _slotIndex;
        private InventoryUI _inventoryUI;
        private Image _iconImage;
        private Canvas _canvas;

        private GameObject _dragGhost;
        private bool _dropReceived;

        private static InventorySlotUI _currentDragSource;

        public int SlotIndex => _slotIndex;

        /// <summary>
        /// InventoryUI에서 슬롯 생성 시 호출.
        /// </summary>
        public void Setup(int slotIndex, InventoryUI inventoryUI, Image iconImage, Canvas canvas)
        {
            _slotIndex = slotIndex;
            _inventoryUI = inventoryUI;
            _iconImage = iconImage;
            _canvas = canvas;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 빈 슬롯은 드래그 불가
            if (_iconImage.sprite == null) return;

            _currentDragSource = this;
            _dropReceived = false;

            // 드래그 고스트 생성 (캔버스 위에 반투명 아이콘)
            _dragGhost = new GameObject("DragGhost");
            _dragGhost.transform.SetParent(_canvas.transform);

            Image ghostImage = _dragGhost.AddComponent<Image>();
            ghostImage.sprite = _iconImage.sprite;
            ghostImage.color = new Color(1f, 1f, 1f, 0.7f);
            ghostImage.raycastTarget = false;
            ghostImage.preserveAspect = true;

            RectTransform ghostRT = ghostImage.rectTransform;
            ghostRT.sizeDelta = new Vector2(60f, 60f);
            ghostRT.position = eventData.position;

            // 원본 아이콘 반투명
            _iconImage.color = new Color(1f, 1f, 1f, 0.3f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragGhost == null) return;
            _dragGhost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
            }

            if (_currentDragSource != this) return;

            // 다른 슬롯에 드롭되지 않았으면 → 월드에 버리기
            if (!_dropReceived)
            {
                _inventoryUI.DropItemToWorld(_slotIndex);
            }

            _inventoryUI.RefreshAll();
            _currentDragSource = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_currentDragSource == null) return;
            if (_currentDragSource == this) return;

            _currentDragSource._dropReceived = true;
            _inventoryUI.SwapSlots(_currentDragSource._slotIndex, _slotIndex);
        }
    }
}
