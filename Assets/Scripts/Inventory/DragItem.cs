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
    public int itemId;  // 在背包里的唯一ID，从字典反查用

    private Vector2 _offset;
    private Canvas _canvas;
    private RectTransform _rt;
    private Transform _originalParent;
    private CanvasGroup _canvasGroup;

    public bool IsDragging { get; private set; }

    // 拖拽开始事件，Controller 订阅
    public event System.Action<DragItem, PointerEventData> OnPickup;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDragging = true;
        _originalParent = transform.parent;
        transform.SetParent(_canvas.transform);  // 脱离父节点，渲染在最上层
        _canvasGroup.blocksRaycasts = false;     // 射线穿透到背包格子
        _offset = (Vector2)_rt.position - eventData.position;
        OnPickup?.Invoke(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rt.position = eventData.position + _offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        _canvasGroup.blocksRaycasts = true;

        var controller = FindObjectOfType<InventoryController>();
        if (controller != null)
        {
            controller.TryPlaceItem(this); // 成功或失败都由 Controller 处理定位
            return;
        }

        ReturnToOriginal();
    }

    public void ReturnToOriginal()
    {
        transform.SetParent(_originalParent);
        _rt.anchoredPosition = Vector2.zero;
    }
}