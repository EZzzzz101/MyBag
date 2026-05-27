using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPanel _panel;
    [SerializeField] private int _gridWidth = 8;
    [SerializeField] private int _gridHeight = 6;

    private InventoryGrid _grid;
    [SerializeField]
    private DragItem _currentDragItem;
    private bool _rotated;

    [SerializeField]
    private Canvas _canvas;
    private Vector2Int _dragOffset; // ← 关键：拾取偏移

    void Awake()
    {
        _grid = new InventoryGrid(_gridWidth, _gridHeight);
        _panel.Initialize(_grid);

    }

    void Start()
    {
        // 把预制体克隆到场景里
        _currentDragItem = Instantiate(_currentDragItem, _canvas.transform);
        _currentDragItem.OnPickup+=HandlePickup;

        int w = _currentDragItem.itemData.GetWidth(false);
        int h = _currentDragItem.itemData.GetHeight(false);

        ItemData itemData =_currentDragItem.itemData;

        // 计算物品的像素尺寸（cellSize=60, spacing=2）
        var rt = _currentDragItem.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w * 60 + (w - 1) * 2, h * 60 + (h - 1) * 2);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panel.transform as RectTransform);
        // 物品中心 = 左下角格子 + 右上角格子 的中点
        Vector2 bottomLeft = _panel.GridToScreen(0, 0);
        Vector2 topRight = _panel.GridToScreen(w - 1, h - 1);
        rt.position = (bottomLeft + topRight) / 2f;

        // 数据层放置，记住 itemId
        _currentDragItem.itemId = _grid.Place(0, 0, w, h, itemData);
        _panel.RefreshAll();
    }

     void Update()
    {
        if (_currentDragItem == null) return;

        bool isDragging = _currentDragItem.IsDragging;

        if (isDragging)
        {
            UpdatePlacementPreview();

            if (Input.GetKeyDown(KeyCode.R))
            {
                _rotated = !_rotated;
                float angle = _rotated ? 90f : 0f;
                _currentDragItem.transform.rotation = Quaternion.Euler(0, 0, -angle);
            }
        }
    }

    // ===== 拖拽偏移的核心计算 =====
    //获取网格坐标后计算
    public void SetDragOffset(int originX, int originY, Vector2 mouseScreenPos)
    {
        if (_panel.ScreenToGrid(mouseScreenPos, out int mouseGX, out int mouseGY))
        {
            _dragOffset.x = originX - mouseGX;
            _dragOffset.y = originY - mouseGY;
        }
    }

    // 每帧更新：鼠标在哪个格子 → 反推物品原点 → 画绿/红预览
    private void UpdatePlacementPreview()
    {
        int w = _currentDragItem.itemData.GetWidth(_rotated);
        int h = _currentDragItem.itemData.GetHeight(_rotated);


        // 鼠标屏幕坐标 → 网格坐标
        if (_panel.ScreenToGrid(Input.mousePosition, out int cx, out int cy))
        {
            // 鼠标格子 + 拾取偏移 = 物品应该画在哪
            int originX = cx + _dragOffset.x;
            int originY = cy + _dragOffset.y;
            _panel.ShowPlacementPreview(originX, originY, w, h);
        }
        else
        {
            // 鼠标在背包外面，清除预览
            _panel.RefreshAll();
        }
    }

    void HandlePickup(DragItem item, PointerEventData eventData)
    {
        // 1. 从字典查（走 itemId，不依赖像素命中）
        if (!_grid.TryGetItemById(item.itemId, out PlacedItem placed))
            return;

        // 2. 记下拾取偏移，拿到鼠标格子
        if (!_panel.ScreenToGrid(eventData.position, out int cx, out int cy))
            return; // 没命中格子，不处理

        _dragOffset.x = placed.originX - cx;
        _dragOffset.y = placed.originY - cy;

        // 3. 清掉数据层旧位置
        _grid.ClearItem(placed);

        // 4. 刷新显示
        _panel.RefreshAll();
    }


    // 放下物品：鼠标格子 + 偏移 → 反推原点 → 移动已有物品
    public bool TryPlaceItem(DragItem item)
    {
        if (!_panel.ScreenToGrid(Input.mousePosition, out int cx, out int cy))
        {
            RestoreItemToOriginal(item);
            return false;
        }

        int originX = cx + _dragOffset.x;
        int originY = cy + _dragOffset.y;

        if (_grid.MoveItem(item.itemId, originX, originY))
        {
            // 放下后把图标对齐到新格子
            int w = item.itemData.GetWidth(_rotated);
            int h = item.itemData.GetHeight(_rotated);
            Vector2 bl = _panel.GridToScreen(originX, originY);
            Vector2 tr = _panel.GridToScreen(originX + w - 1, originY + h - 1);
            item.transform.position = (bl + tr) / 2f;

            _rotated = false;
            _panel.RefreshAll();
            return true;
        }
        // 没放成功（红区），也恢复原位
        RestoreItemToOriginal(item);
        return false;
    }

    void RestoreItemToOriginal(DragItem item)
    {
        if (!_grid.TryGetItemById(item.itemId, out PlacedItem original)) return;

        // MoveItem 恢复到原位（ClearItem 后原位是空的，CanPlace 会通过）
        _grid.MoveItem(item.itemId, original.originX, original.originY);
        // 恢复视觉位置
        Vector2 bl = _panel.GridToScreen(original.originX, original.originY);
        Vector2 tr = _panel.GridToScreen(original.originX + original.width - 1, original.originY + original.height - 1);
        item.transform.position = (bl + tr) / 2f;
        _panel.RefreshAll();
    }


}
