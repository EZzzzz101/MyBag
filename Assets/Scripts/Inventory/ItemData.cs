using UnityEngine;

/// <summary>
/// 物品数据。矩形物品，旋转就是 w 和 h 互换。
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int itemWidth = 1;   // 不旋转时的宽度（占几格）
    public int itemHeight = 1;  // 不旋转时的高度（占几格）
    public int maxStack = 1;

    /// <summary>
    /// 获取旋转后的宽
    /// </summary>
    /// <param name="rotated">是否旋转（旋转 = 宽高互换）</param>
    public int GetWidth(bool rotated)
    {
        return rotated ? itemHeight : itemWidth;
    }

    /// <summary>
    /// 获取旋转后的高
    /// </summary>
    public int GetHeight(bool rotated)
    {
        return rotated ? itemWidth : itemHeight;
    }
}