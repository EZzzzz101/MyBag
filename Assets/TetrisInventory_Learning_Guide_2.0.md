# Unity 2D 三角洲类型背包 — 学习指南 2.0

## 改进点（相比 1.0）

| 1.0 | 2.0 |
|-----|-----|
| 俄罗斯方块式任意形状 | 三角洲式矩形物品（w × h） |
| 没有架构分层 | MVC + 事件驱动 |
| 拖拽物品会"跳" | 拾取时记偏移，自然跟随 |
| 防御性代码过多（Remove 里 IsInBounds） | 精简，只保留必要的 |
| 学习指南里的参考代码 | 和你当前代码状态对齐 |

---

## 架构总览

```
┌──────────────────────────────────────────────────┐
│            InventoryController (Controller)        │
│  - 持有 InventoryGrid (Model)                     │
│  - 持有 InventoryPanel (View) 引用                │
│  - 订阅 View 的事件                               │
│  - 所有对 Model 的修改都由 Controller 发起        │
└────────┬────────────────────────┬────────────────┘
         │ 调用方法               │ 订阅事件
         ▼                       ▼
┌─────────────────┐    ┌──────────────────────────┐
│  InventoryGrid   │    │    InventoryPanel (View)  │
│    (Model)       │    │    - 只管画格子           │
│  纯 C# 类        │    │    - 只读 Model 来渲染    │
│  不继承 MB       │    │    - 触发事件，不管逻辑   │
└─────────────────┘    └──────────────────────────┘
```

**数据流：**
```
用户操作 → View 触发事件 → Controller 收到 → 调 Model → Controller 调 View.Refresh
```

View 永远不写 Model，Controller 是唯一的桥梁。

---

## 项目结构（完整）

```
Assets/
  Scripts/
    Inventory/
      InventoryGrid.cs          # Model — 网格数据
      ItemData.cs               # 物品数据（ScriptableObject）
      InventoryPanel.cs         # View — 格子 UI + 事件
      DragItem.cs               # 可拖拽物品（IBeginDragHandler 等）
      InventoryController.cs    # Controller — 订阅事件 + 调 Model
```

---

# 第一阶段：网格数据结构（纯逻辑）

## InventoryGrid.cs — Model 层

纯 C# 类，不继承 MonoBehaviour。只管数据。

```csharp
using UnityEngine;

/// <summary>
/// 背包网格数据结构。只负责数据，不管 UI。
/// </summary>
public class InventoryGrid
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    private int[,] _cells; // 0=空, >0=占用（同时也是物品ID）

    public InventoryGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new int[width, height];
    }

    /// <summary>
    /// 检查某个坐标是否在网格范围内
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// 检查指定位置是否为空（越界视为不可用）
    /// </summary>
    public bool IsEmpty(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        return _cells[x, y] == 0;
    }

    /// <summary>
    /// 核心方法：检查宽 w 高 h 的物品能否放在 (originX, originY) 位置
    /// originX/originY 是物品左下角格子在网格中的坐标
    /// </summary>
    public bool CanPlace(int originX, int originY, int itemWidth, int itemHeight)
    {
        for (int x = 0; x < itemWidth; x++)
        {
            for (int y = 0; y < itemHeight; y++)
            {
                int gridX = originX + x;
                int gridY = originY + y;

                if (!IsInBounds(gridX, gridY)) return false; // 越界
                if (_cells[gridX, gridY] != 0) return false;  // 已被占用
            }
        }
        return true;
    }

    /// <summary>
    /// 将物品放入网格
    /// </summary>
    public bool Place(int originX, int originY, int itemWidth, int itemHeight, int itemId = 1)
    {
        if (!CanPlace(originX, originY, itemWidth, itemHeight)) return false;

        for (int x = 0; x < itemWidth; x++)
        {
            for (int y = 0; y < itemHeight; y++)
            {
                _cells[originX + x, originY + y] = itemId;
            }
        }
        return true;
    }

    /// <summary>
    /// 从网格中移除物品（简化版：不检查越界，相信调用方）
    /// </summary>
    public bool Remove(int originX, int originY, int itemWidth, int itemHeight)
    {
        for (int x = 0; x < itemWidth; x++)
        {
            for (int y = 0; y < itemHeight; y++)
            {
                _cells[originX + x, originY + y] = 0;
            }
        }
        return true;
    }

    /// <summary>
    /// Debug：在 Console 打印当前网格状态
    /// </summary>
    public void DebugPrint()
    {
        string result = "";
        for (int y = Height - 1; y >= 0; y--) // 从上到下打印（Y轴在上）
        {
            for (int x = 0; x < Width; x++)
            {
                result += _cells[x, y] == 0 ? "[ ]" : "[■]";
            }
            result += "\n";
        }
        Debug.Log(result);
    }
}
```

## 第一阶段验证（Console 测试）

```csharp
using UnityEngine;

public class TestPhase1 : MonoBehaviour
{
    void Start()
    {
        var grid = new InventoryGrid(5, 5);

        // 放入一个 2×3 的物品（比如步枪）
        Debug.Log("Can place 2x3 at (0,0)? " + grid.CanPlace(0, 0, 2, 3)); // True
        grid.Place(0, 0, 2, 3);
        grid.DebugPrint();

        Debug.Log("Can place 2x2 at (1,0)? " + grid.CanPlace(1, 0, 2, 2)); // False（重叠）
        Debug.Log("Can place 3x3 at (3,3)? " + grid.CanPlace(3, 3, 3, 3)); // False（越界）

        grid.Remove(0, 0, 2, 3);
        Debug.Log("After remove:");
        grid.DebugPrint();
    }
}
```

**注意陷阱：** IsInBounds 里是 `y >= 0`，不是 `y > 0`，否则最底下一行永远用不了。

---

# 第二阶段：物品数据 + 旋转

## ItemData.cs — ScriptableObject

矩形物品不需要旋转矩阵，旋转 = 宽高互换。

```csharp
using UnityEngine;

/// <summary>
/// 物品数据。矩形物品，旋转就是 w 和 h 互换。
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int itemWidth = 1;   // 不旋转时的宽度
    public int itemHeight = 1;  // 不旋转时的高度
    public int maxStack = 1;

    public int GetWidth(bool rotated)
    {
        return rotated ? itemHeight : itemWidth;
    }

    public int GetHeight(bool rotated)
    {
        return rotated ? itemWidth : itemHeight;
    }
}
```

**创建物品：** 右键 Project → Create → Inventory → Item Data，填名字和宽高即可。

---

# 第三阶段：可视化网格（UGUI + MVC）

## 场景搭建

```
Canvas (Screen Space - Overlay)
  └── Panel (挂 GridLayoutGroup + ContentSizeFitter + InventoryPanel)
        └── Slot 预制体（右键 → UI → Image，拖成预制体后删掉场景里的）
```

**Panel 组件配置：**

| 组件 | 设置 |
|------|------|
| GridLayoutGroup | Constraint: FixedColumnCount, CellSize: (60,60), Spacing: (2,2) |
| ContentSizeFitter | Horizontal: PreferredSize, Vertical: PreferredSize |
| InventoryPanel | 拖入 Slot 预制体 |

## InventoryPanel.cs — View 层

**View 的职责：**
- 接收 Model 引用（Initialize 传进来，不要 new）
- 读取 Model 来渲染（RefreshAll、ShowPlacementPreview）
- 触发事件通知 Controller（用户操作时）
- 绝不调用 Model 的写方法（Place / Remove）

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View 层：只负责显示，不修改 Model。
/// 用户操作通过事件通知 Controller。
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject _slotPrefab;
    [SerializeField] private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color _occupiedColor = new Color(0.3f, 0.5f, 0.3f, 0.8f);
    [SerializeField] private Color _highlightValidColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color _highlightInvalidColor = new Color(1, 0, 0, 0.5f);

    private InventoryGrid _grid;
    private Image[,] _slotImages;
    private GridLayoutGroup _layout;

    // ===== 事件（Controller 订阅） =====
    public event System.Action<int, int> OnCellEnter;      // 拖拽中鼠标进入某格
    public event System.Action<int, int> OnCellDrop;       // 物品放到某格
    public event System.Action<int, int> OnCellRightClick; // 右键某格

    // ===== 初始化 =====
    public void Initialize(InventoryGrid grid)
    {
        _grid = grid;  // 不 new！用 Controller 传过来的
        _slotImages = new Image[_grid.Width, _grid.Height];
        _layout = GetComponent<GridLayoutGroup>();

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

    // ===== 刷新渲染 =====
    public void RefreshAll()
    {
        if (_grid == null || _slotImages == null) return; // 防崩

        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                _slotImages[x, y].color = _grid.IsEmpty(x, y) ? _emptyColor : _occupiedColor;
            }
        }
    }

    // ===== 悬停预览 =====
    public void ShowPlacementPreview(int originX, int originY, int itemWidth, int itemHeight)
    {
        RefreshAll();  // 先清除上次的预览

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

    // ===== 坐标转换 =====
    public bool ScreenToGrid(Vector2 screenPos, out int gridX, out int gridY)
    {
        gridX = -1;
        gridY = -1;

        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                var rt = _slotImages[x, y].rectTransform;
                if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                {
                    gridX = x;
                    gridY = y;
                    return true;
                }
            }
        }
        return false;
    }
}
```

## InventoryController.cs — Controller 层（初版）

Controller 负责：创建 Model → 注入 View → 所有数据修改。

```csharp
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPanel _panel;
    [SerializeField] private int _gridWidth = 8;
    [SerializeField] private int _gridHeight = 6;

    private InventoryGrid _grid;

    void Awake()
    {
        _grid = new InventoryGrid(_gridWidth, _gridHeight);
        _panel.Initialize(_grid);
    }

    void Start()
    {
        // 测试：放入一个 2×3 的物品
        _grid.Place(0, 0, 2, 3);
        _panel.RefreshAll();
    }
}
```

**挂载方式：** Controller 挂在一个空 GameObject 上，拖 Panel 到 `_panel` 字段。Panel 不需要 Controller 组件，Controller 也不需要 Panel 的组件，直接拖引用就行。

---

# 第四阶段：拖拽 + 放下

## 关键概念：拖拽偏移（Drag Offset）

**问题：** 鼠标只提供一个格子坐标，但物品占 W×H 的矩形。物品原点应该是哪个格子？

**没有偏移：** 拾取瞬间物品"跳"到鼠标为左下角，手感差。

**有偏移：** 拾取时记住"鼠标格子 到 物品原点"的差值，拖拽全程用这个差值反推原点。

```
拾取 2×3 物品，鼠标在物品的第 2 格（originX=2, mouseGX=3）：
  dragOffset = 2 - 3 = -1

拖拽过程中，鼠标在 gridX=7：
  originX = 7 + (-1) = 6  ← 物品始终和拾取时保持一致相对位置
```

## DragItem.cs — 拖拽物品

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可拖拽的物品图标。只负责拖拽视觉，逻辑全交给 Controller。
/// </summary>
[RequireComponent(typeof(Image))]
public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData itemData;
    public bool rotated;

    private Vector2 _offset;
    private Canvas _canvas;
    private RectTransform _rt;
    private Transform _originalParent;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        transform.SetParent(_canvas.transform);  // 脱离父节点，渲染在最上层
        _canvasGroup.blocksRaycasts = false;     // 射线穿透到背包格子
        _offset = (Vector2)_rt.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rt.position = eventData.position + _offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        var controller = FindObjectOfType<InventoryController>();
        if (controller != null && controller.TryPlaceItem(this))
            return; // 放成功了，物品已销毁

        ReturnToOriginal(); // 没放成功，回到原位
    }

    public void ReturnToOriginal()
    {
        transform.SetParent(_originalParent);
        _rt.anchoredPosition = Vector2.zero;
    }
}
```

## InventoryController.cs — 完整版（第四阶段）

Controller 升级：订阅 View 事件 + 处理拖拽偏移 + 旋转。

```csharp
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPanel _panel;
    [SerializeField] private int _gridWidth = 8;
    [SerializeField] private int _gridHeight = 6;

    private InventoryGrid _grid;
    private DragItem _currentDragItem;
    private bool _rotated;
    private Vector2Int _dragOffset; // ← 关键：拾取偏移

    void Awake()
    {
        _grid = new InventoryGrid(_gridWidth, _gridHeight);
        _panel.Initialize(_grid);
    }

    void Update()
    {
        if (_currentDragItem == null)
            _currentDragItem = FindObjectOfType<DragItem>();

        if (_currentDragItem == null) return;

        bool isDragging = _currentDragItem.transform.parent == GetComponentInParent<Canvas>().transform;

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
    public void SetDragOffset(int originX, int originY, Vector2 mouseScreenPos)
    {
        if (_panel.ScreenToGrid(mouseScreenPos, out int mouseGX, out int mouseGY))
        {
            _dragOffset.x = originX - mouseGX;
            _dragOffset.y = originY - mouseGY;
        }
    }

    private void UpdatePlacementPreview()
    {
        int w = _currentDragItem.itemData.GetWidth(_rotated);
        int h = _currentDragItem.itemData.GetHeight(_rotated);

        if (_panel.ScreenToGrid(Input.mousePosition, out int cx, out int cy))
        {
            int originX = cx + _dragOffset.x;
            int originY = cy + _dragOffset.y;
            _panel.ShowPlacementPreview(originX, originY, w, h);
        }
        else
        {
            _panel.RefreshAll();
        }
    }

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
```

## 从背包往外拖（已放入的物品）

拖拽已放置的物品需要额外处理：
- 拾取时：调 `_grid.Remove()` 把原位置清掉，新建一个 DragItem 的 GameObject 来拖
- 如果放置失败（新位置放不下，或者拖到背包外）：**物品会丢失**，因为已经从格子里删了
- 需要"放回原位"逻辑：在 Remove 之前备份 `(originX, originY, w, h)`，如果 TryPlaceItem 返回 false 就重新 Place 回去

---

# 第五阶段：右键移除 + 物品追踪

## 核心问题

`_cells[x, y]` 存的是 `itemId`，但四阶段为止都用默认值 `1`，没法区分"这个格子属于哪个物品"。右键点一格想删整个物品，需要根据 `itemId` 查出这个物品的所有信息。

## 在 InventoryGrid 中增加物品字典

```csharp
// ===== 添加到 InventoryGrid 类 =====

private Dictionary<int, PlacedItem> _placedItems = new Dictionary<int, PlacedItem>();
private int _nextItemId = 1;

public struct PlacedItem
{
    public int originX;
    public int originY;
    public int width;
    public int height;
    public string itemName;
}

// 改造 Place：返回 itemId（之前返回 bool 不够用了）
public int Place(int originX, int originY, int itemWidth, int itemHeight, string itemName = "")
{
    if (!CanPlace(originX, originY, itemWidth, itemHeight)) return -1;

    int itemId = _nextItemId++;
    for (int x = 0; x < itemWidth; x++)
        for (int y = 0; y < itemHeight; y++)
            _cells[originX + x, originY + y] = itemId;

    _placedItems[itemId] = new PlacedItem
    {
        originX = originX, originY = originY,
        width = itemWidth, height = itemHeight,
        itemName = itemName
    };
    return itemId;
}

// 右键移除：根据格格坐标找到物品，删除整个物品
public bool RemoveByCell(int gx, int gy, out PlacedItem removedItem)
{
    removedItem = default;
    if (!IsInBounds(gx, gy)) return false;

    int itemId = _cells[gx, gy];
    if (itemId == 0) return false;
    if (!_placedItems.TryGetValue(itemId, out var item)) return false;

    // 清空该物品占用的所有格子
    for (int x = 0; x < item.width; x++)
        for (int y = 0; y < item.height; y++)
            _cells[item.originX + x, item.originY + y] = 0;

    _placedItems.Remove(itemId);
    removedItem = item;
    return true;
}

// 自动整理：把所有物品往左下方压缩
public void Compact()
{
    var sorted = _placedItems
        .OrderBy(kv => kv.Value.originY)
        .ThenBy(kv => kv.Value.originX)
        .ToList();

    foreach (var kv in sorted)
    {
        var item = kv.Value;
        int itemId = kv.Key;

        // 先移除
        for (int x = 0; x < item.width; x++)
            for (int y = 0; y < item.height; y++)
                _cells[item.originX + x, item.originY + y] = 0;

        // 从最左下往当前位置扫描，找第一个能放的位置
        bool found = false;
        int bestX = 0, bestY = 0;
        for (int y = 0; y < Height && !found; y++)
        {
            for (int x = 0; x < Width && !found; x++)
            {
                if (CanPlace(x, y, item.width, item.height))
                {
                    bestX = x; bestY = y;
                    found = true;
                }
            }
        }

        // 放到最佳位置
        for (int x = 0; x < item.width; x++)
            for (int y = 0; y < item.height; y++)
                _cells[bestX + x, bestY + y] = itemId;

        _placedItems[itemId] = new PlacedItem
        {
            originX = bestX, originY = bestY,
            width = item.width, height = item.height,
            itemName = item.itemName
        };
    }
}
```

---

# 总结

| 阶段 | 核心技能 | 新增文件 |
|------|---------|---------|
| 1 | 二维数组、矩形碰撞检测 | `InventoryGrid.cs` |
| 2 | ScriptableObject、旋转=宽高互换 | `ItemData.cs` |
| 3 | GridLayoutGroup + ContentSizeFitter + MVC | `InventoryPanel.cs`, `InventoryController.cs` |
| 4 | IBeginDragHandler、拖拽偏移 | `DragItem.cs` |
| 5 | 物品 ID 追踪、右键移除、自动整理 | 补充 Grid 和 Controller |

## MVC 规则速查

| 层 | 能做什么 | 不能做什么 |
|----|---------|-----------|
| **Model** (Grid) | 存数据、提供查询 | 不能引用 View/Controller |
| **View** (Panel) | 渲染格子、触发事件 | **不能 new Model**、不能调 Place/Remove |
| **Controller** | 创建 Model、订阅事件、修改数据 | 不操作 UI 组件 |

## 拖拽偏移速查

```
拾取时：  dragOffset = origin - mouseGrid
拖拽时：  origin = mouseGrid + dragOffset
放下时：  origin = mouseGrid + dragOffset
```

## 注意事项

- `IsInBounds` 必须是 `y >= 0`，不能是 `y > 0`
- `Remove` 不需要 IsInBounds 检查，调用方保证合法
- `RefreshAll` 加 null 守卫防止未初始化调用
- `ShowPlacementPreview` 的 `RefreshAll` 要放在 for 循环**前面**（先清再画）
- DragItem 的 `OnEndDrag` 如果放成功了要 return，否则才走 ReturnToOriginal
