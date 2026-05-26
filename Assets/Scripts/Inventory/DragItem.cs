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

    public bool IsDragging { get; private set; }

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