using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using TMPro;
using HitWaves.Entity.Player;
using HitWaves.Core.Item;

namespace HitWaves.UI
{
    /// <summary>
    /// E키로 토글하는 인벤토리 패널.
    /// 장비 슬롯을 그리드 형태로 표시한다.
    /// 드래그 앤 드롭으로 슬롯 교환 + UI 밖 드롭 시 월드에 버리기.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        private const string LOG_TAG = "InventoryUI";

        [Header("참조")]
        [Tooltip("플레이어의 Inventory 컴포넌트")]
        [SerializeField] private Inventory _inventory;

        [Header("입력")]
        [Tooltip("인벤토리 토글 키 (Player > Inventory 액션)")]
        [SerializeField] private InputActionReference _toggleAction;

        [Header("슬롯 외형")]
        [Tooltip("슬롯 크기 (px, 기준 해상도 1920×1080)")]
        [SerializeField] private float _slotSize = 80f;

        [Tooltip("슬롯 간 간격 (px)")]
        [SerializeField] private float _slotSpacing = 10f;

        [Tooltip("슬롯 테두리 색상")]
        [SerializeField] private Color _slotBorderColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Tooltip("슬롯 배경 색상")]
        [SerializeField] private Color _slotBgColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        [Tooltip("패널 배경 색상")]
        [SerializeField] private Color _panelBgColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        [Tooltip("패널 패딩 (px)")]
        [SerializeField] private float _panelPadding = 15f;

        private Canvas _canvas;
        private GameObject _panel;
        private Image[] _slotImages;
        private TextMeshProUGUI[] _slotLabels;
        private InventorySlotUI[] _slotUIs;
        private bool _isOpen;

        private void Awake()
        {
            EnsureEventSystem();
            CreateUI();
            _panel.SetActive(false);
            _isOpen = false;
        }

        private void OnEnable()
        {
            if (_toggleAction != null && _toggleAction.action != null)
            {
                _toggleAction.action.Enable();
                _toggleAction.action.performed += OnTogglePerformed;
            }

            if (_inventory != null)
            {
                _inventory.OnSlotChanged += HandleSlotChanged;
            }
        }

        private void OnDisable()
        {
            if (_toggleAction != null && _toggleAction.action != null)
            {
                _toggleAction.action.performed -= OnTogglePerformed;
            }

            if (_inventory != null)
            {
                _inventory.OnSlotChanged -= HandleSlotChanged;
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext ctx)
        {
            Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _panel.SetActive(_isOpen);

            if (_isOpen)
            {
                RefreshAll();
            }

            DebugLogger.Log(LOG_TAG, $"인벤토리 {(_isOpen ? "열림" : "닫힘")}", this);
        }

        /// <summary>
        /// 모든 슬롯 UI를 현재 인벤토리 상태로 갱신한다.
        /// </summary>
        public void RefreshAll()
        {
            if (_inventory == null) return;

            for (int i = 0; i < _slotImages.Length && i < _inventory.Slots.Count; i++)
            {
                RefreshSlot(i);
            }
        }

        /// <summary>
        /// 두 슬롯을 교환한다 (InventorySlotUI에서 호출).
        /// </summary>
        public void SwapSlots(int fromIndex, int toIndex)
        {
            if (_inventory == null) return;
            _inventory.SwapSlots(fromIndex, toIndex);
            RefreshAll();
        }

        /// <summary>
        /// 슬롯의 아이템을 월드에 드롭한다 (InventorySlotUI에서 호출).
        /// </summary>
        public void DropItemToWorld(int slotIndex)
        {
            if (_inventory == null) return;

            ItemInstance item = _inventory.DropFromSlot(slotIndex);
            if (item == null) return;

            Vector2 dropPos = (Vector2)_inventory.transform.position;
            DroppedItem.Create(item, dropPos);

            DebugLogger.Log(LOG_TAG,
                $"아이템 월드 드롭 — {item.Data?.ItemName ?? "Unknown"}", this);
        }

        private void RefreshSlot(int index)
        {
            if (_inventory == null || index >= _inventory.Slots.Count) return;

            EquipmentSlot slot = _inventory.Slots[index];

            if (slot.IsEmpty)
            {
                _slotImages[index].sprite = null;
                _slotImages[index].color = _slotBgColor;
            }
            else
            {
                ItemInstance item = slot.EquippedItem;
                Sprite icon = item.Data != null ? item.Data.Icon : null;

                if (icon != null)
                {
                    _slotImages[index].sprite = icon;
                    _slotImages[index].color = Color.white;
                }
                else
                {
                    _slotImages[index].sprite = null;
                    _slotImages[index].color = new Color(0.4f, 0.6f, 0.4f, 1f);
                }
            }
        }

        private void HandleSlotChanged(EquipmentSlot slot)
        {
            if (!_isOpen) return;
            RefreshAll();
        }

        /// <summary>
        /// EventSystem이 없으면 생성한다 (드래그 앤 드롭에 필수).
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            DebugLogger.Log(LOG_TAG, "EventSystem 런타임 생성", this);
        }

        /// <summary>
        /// UI를 런타임에 생성한다.
        /// </summary>
        private void CreateUI()
        {
            // Canvas
            GameObject canvasGo = new GameObject("InventoryCanvas");
            canvasGo.transform.SetParent(transform);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 패널 (화면 중앙)
            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGo.transform);

            Image panelImage = _panel.AddComponent<Image>();
            panelImage.color = _panelBgColor;
            panelImage.raycastTarget = true;

            RectTransform panelRT = panelImage.rectTransform;
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);

            // 슬롯 개수에 따라 패널 크기 계산
            int slotCount = 2; // 오른손 + 왼손
            float panelWidth = _panelPadding * 2f + _slotSize * slotCount + _slotSpacing * (slotCount - 1);
            float panelHeight = _panelPadding * 2f + _slotSize + 25f; // 라벨 높이 포함
            panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);

            _slotImages = new Image[slotCount];
            _slotLabels = new TextMeshProUGUI[slotCount];
            _slotUIs = new InventorySlotUI[slotCount];

            // 화면 왼쪽 = 왼손, 오른쪽 = 오른손
            string[] labels = { "왼손", "오른손" };
            int[] slotMap = { 1, 0 }; // 화면 위치 i → 인벤토리 슬롯 인덱스
            float startX = -((slotCount - 1) * (_slotSize + _slotSpacing)) * 0.5f;

            for (int i = 0; i < slotCount; i++)
            {
                int inventoryIndex = slotMap[i];
                float xPos = startX + i * (_slotSize + _slotSpacing);

                // 슬롯 컨테이너
                GameObject slotGo = new GameObject($"Slot_{labels[i]}");
                slotGo.transform.SetParent(_panel.transform);

                // 슬롯 테두리 (약간 큰 이미지) — 드래그 앤 드롭 타겟
                Image borderImage = slotGo.AddComponent<Image>();
                borderImage.color = _slotBorderColor;
                borderImage.raycastTarget = true;

                RectTransform borderRT = borderImage.rectTransform;
                borderRT.anchorMin = new Vector2(0.5f, 0.5f);
                borderRT.anchorMax = new Vector2(0.5f, 0.5f);
                borderRT.anchoredPosition = new Vector2(xPos, 5f);
                borderRT.sizeDelta = new Vector2(_slotSize + 4f, _slotSize + 4f);

                // 슬롯 배경 (내부)
                GameObject bgGo = new GameObject("Background");
                bgGo.transform.SetParent(slotGo.transform);

                Image bgImage = bgGo.AddComponent<Image>();
                bgImage.color = _slotBgColor;
                bgImage.raycastTarget = false;

                RectTransform bgRT = bgImage.rectTransform;
                bgRT.anchorMin = new Vector2(0.5f, 0.5f);
                bgRT.anchorMax = new Vector2(0.5f, 0.5f);
                bgRT.anchoredPosition = Vector2.zero;
                bgRT.sizeDelta = new Vector2(_slotSize, _slotSize);

                // 아이템 아이콘 (빈 상태)
                GameObject iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(slotGo.transform);

                Image iconImage = iconGo.AddComponent<Image>();
                iconImage.color = _slotBgColor;
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;

                RectTransform iconRT = iconImage.rectTransform;
                iconRT.anchorMin = new Vector2(0.5f, 0.5f);
                iconRT.anchorMax = new Vector2(0.5f, 0.5f);
                iconRT.anchoredPosition = Vector2.zero;
                iconRT.sizeDelta = new Vector2(_slotSize - 8f, _slotSize - 8f);

                _slotImages[inventoryIndex] = iconImage;

                // InventorySlotUI 컴포넌트 (드래그 앤 드롭)
                InventorySlotUI slotUI = slotGo.AddComponent<InventorySlotUI>();
                slotUI.Setup(inventoryIndex, this, iconImage, _canvas);
                _slotUIs[inventoryIndex] = slotUI;

                // 라벨 (슬롯 아래)
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(slotGo.transform);

                TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = labels[i];
                label.fontSize = 16f;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;

                RectTransform labelRT = label.rectTransform;
                labelRT.anchorMin = new Vector2(0.5f, 0.5f);
                labelRT.anchorMax = new Vector2(0.5f, 0.5f);
                labelRT.anchoredPosition = new Vector2(0f, -(_slotSize * 0.5f + 15f));
                labelRT.sizeDelta = new Vector2(_slotSize + 20f, 25f);

                _slotLabels[inventoryIndex] = label;
            }

            DebugLogger.Log(LOG_TAG, "UI 런타임 생성 완료 (드래그 앤 드롭 지원)", this);
        }
    }
}
