using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// ui层，只管显示逻辑
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject _slotPrefab; // 单个格子预制体（一个带 Image 的 GameObject）
    //空颜色
    [SerializeField] private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    //占用颜色
    [SerializeField] private Color _occupiedColor = new Color(0.3f, 0.5f, 0.3f, 0.8f);
    //悬停高亮有效颜色
    [SerializeField] private Color _highlightValidColor = new Color(0, 1, 0, 0.5f);
    //悬停高亮无效颜色
    [SerializeField] private Color _highlightInvalidColor = new Color(1, 0, 0, 0.5f);

    private InventoryGrid _grid;
    private Image[,] _slotImages;
    private GridLayoutGroup _layout;

    // ===== 事件（Controller 订阅） =====
    // public event System.Action<int, int> OnCellEnter;      // 拖拽中鼠标进入某格
    // public event System.Action<int, int> OnCellRightClick; // 右键某格

    /// <summary>
    /// 初始化背包格子
    /// </summary>
    public void Initialize(InventoryGrid grid)
    {   
        _grid=grid;
        _slotImages = new Image[_grid.Width, _grid.Height];
        _layout = GetComponent<GridLayoutGroup>();

        //设置GridLayoutGroup
        _layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _layout.constraintCount = _grid.Width;
        _layout.cellSize = new Vector2(60, 60);
        _layout.spacing = new Vector2(2, 2);

         // 生成格子（从左上到右下）
        for (int y = _grid.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                var slot = Instantiate(_slotPrefab, transform);
                slot.name = $"Slot_{x}_{y}";
                var img = slot.GetComponent<Image>();
                img.color = _emptyColor;
                _slotImages[x, y] = img;
            }
        }
    }

   /// <summary>
    /// 刷新所有格子的颜色
    /// </summary>
    public void RefreshAll()
    {
        if (_grid == null || _slotImages == null) return;

        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                _slotImages[x, y].color = _grid.IsEmpty(x, y) ? _emptyColor : _occupiedColor;
            }
        }
    }

    /// <summary>
    /// 鼠标悬停预览：显示物品覆盖的矩形区域
    /// </summary>
    public void ShowPlacementPreview(int originX, int originY, int itemWidth, int itemHeight)
    {
        RefreshAll(); // 先清除上次的预览

        bool canPlace = _grid.CanPlace(originX, originY, itemWidth, itemHeight);

        for (int x = 0; x < itemWidth; x++)
        {
            for (int y = 0; y < itemHeight; y++)
            {
                int gx = originX + x;
                int gy = originY + y;
                if (!_grid.IsInBounds(gx, gy)) continue;

                _slotImages[gx, gy].color = canPlace ? _highlightValidColor : _highlightInvalidColor;
            }
        }
    }

    /// <summary>
    /// 网格坐标 → 屏幕像素坐标（格子的中心点）
    /// </summary>
    public Vector2 GridToScreen(int gridX, int gridY)
    {
        return _slotImages[gridX, gridY].rectTransform.position;
    }

    /// <summary>
    /// 根据屏幕坐标获取对应的网格坐标
    /// </summary>
    public bool ScreenToGrid(Vector2 screenPos, out int gridX, out int gridY)
    {
        gridX = gridY = -1;

        // 1. 屏幕坐标 → Panel 本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, screenPos, null, out Vector2 localPos);

        Rect panelRect = ((RectTransform)transform).rect;

        // 原点从中心挪到左上角
        float xFromLeft = localPos.x + panelRect.width / 2f;
        float yFromTop = panelRect.height / 2f-localPos.y;  
        
        
        // 2. 数学计算（高 H 格、间距 2、cellSize 60）
        float cellWithSpacing = 60 + 2;  // cellSize + spacing
        gridX = Mathf.FloorToInt(xFromLeft/ cellWithSpacing);
        int rowFromTop = Mathf.FloorToInt(yFromTop / cellWithSpacing);   // 先算出从上第几排
        gridY = (_grid.Height - 1) - rowFromTop;         // 再翻转到从下算

        // 3. 边界检查
        if (gridX < 0 || gridX >= _grid.Width || gridY < 0 || gridY >= _grid.Height)
            return false;

        Debug.Log($"({gridX}, {gridY})");
        return true;
    }

}
