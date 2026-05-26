using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        int w = _currentDragItem.itemData.GetWidth(false);
        int h = _currentDragItem.itemData.GetHeight(false);

        // 计算物品的像素尺寸（cellSize=60, spacing=2）
        var rt = _currentDragItem.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w * 60 + (w - 1) * 2, h * 60 + (h - 1) * 2);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panel.transform as RectTransform);
        // 物品中心 = 左下角格子 + 右上角格子 的中点
        Vector2 bottomLeft = _panel.GridToScreen(0, 0);
        Vector2 topRight = _panel.GridToScreen(w - 1, h - 1);
        rt.position = (bottomLeft + topRight) / 2f;

        // 数据层放置
        _grid.Place(0, 0, w, h);
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


    // 放下物品：鼠标格子 + 偏移 → 反推原点 → 尝试 Place
    public bool TryPlaceItem(DragItem item)
    {
        int w = item.itemData.GetWidth(_rotated);
        int h = item.itemData.GetHeight(_rotated);

        if (!_panel.ScreenToGrid(Input.mousePosition, out int cx, out int cy))
            return false;

        int originX = cx + _dragOffset.x;
        int originY = cy + _dragOffset.y;

        if (_grid.Place(originX, originY, w, h))
        {
            Destroy(item.gameObject);
            _rotated = false;
            _panel.RefreshAll();
            return true;
        }
        return false;
    }
}
