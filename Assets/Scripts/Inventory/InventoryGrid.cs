using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlacedItem
{
    public int itemId;        // 唯一ID
    public int originX;
    public int originY;
    public int width;
    public int height;
    public ItemData itemData; // 引用 ScriptableObject（名字、图标、宽高）
    public bool rotated;      // 是否旋转
}

/// <summary>
/// 背包网格数据结构。只管数据，不管UI
/// </summary>
public class InventoryGrid
{
    public int Width { get ;private set; } 
    public int Height { get ;private set; } 
    private int[,] _cells;//0=空，>0=占用
    private Dictionary<int, PlacedItem> _placedItems = new Dictionary<int, PlacedItem>();

    private int _nextItemId = 1; //物品唯一ID
    public InventoryGrid(int width,int height)
    {
        Width=width;
        Height=height;
        _cells=new int[width,height];
    }

    /// <summary>
    /// 检查某个坐标是否在网格范围内
    /// </summary>
    public bool IsInBounds(int x,int y)
    {
        return x>=0 && x<Width && y>=0 && y<Height;
    }

    /// <summary>
    /// 判断是否为空（空才可放置）
    /// </summary>
    public bool IsEmpty(int x,int y)
    {
        if(!IsInBounds(x,y)) return false;
        return _cells[x,y]==0;
    }

    /// <summary>
    /// 核心方法：检查物体能不能放在初始坐标（originX，originY）位置
    /// originX/originY 是物品左下角格子在网格中的坐标
    /// </summary>
    /// <param name="itemWidth">物体宽度</param>
    /// <param name="itemHeight">物体高度</param>
    /// <returns></returns>
    public bool CanPlace(int originX, int originY , int itemWidth , int itemHeight)
    {
        for(int x=0; x < itemWidth; x++)
        {
            for(int y = 0; y < itemHeight; y++)
            {
                int gridx = originX +x;
                int gridy = originY +y;

                if(!IsInBounds(gridx,gridy)) return false; //越界
                if(_cells[gridx,gridy]!=0) return false;  //已经占用
            }
        }

        return true;
    }

    /// <summary>
    /// 将物品放入网格并且放入字典记住物品名称
    /// </summary>
    public int Place(int originX,int originY,int itemWidth,int itemHeight,ItemData itemData)
    {
        if(!CanPlace(originX,originY,itemWidth,itemHeight)) return -1;

        int itemId = _nextItemId++;
        for(int x=0; x < itemWidth; x++)
        {
            for(int y = 0; y < itemHeight; y++)
            {
                _cells[originX+x,originY+y]=itemId;
            }
        }

        //映射到PlacedItem(更新字典)
        _placedItems[itemId] = new PlacedItem
        {
            itemId = itemId,
            originX = originX, originY = originY,
            width = itemWidth, height = itemHeight,
            itemData = itemData,
            rotated = false
        };

        return itemId;
    }


    /// <summary>
    /// 查这个格子属于哪个物品
    /// </summary>
    public bool TryGetItemAt(int gx, int gy, out PlacedItem item)
    {
        item = default;
        if (!IsInBounds(gx, gy)) return false;
        int itemId = _cells[gx, gy];
        if (itemId == 0) return false;
        return _placedItems.TryGetValue(itemId, out item);
    } 

    /// <summary>
    /// 根据 itemId 查字典
    /// </summary>
    public bool TryGetItemById(int itemId, out PlacedItem item)
    {
        return _placedItems.TryGetValue(itemId, out item);
    }

    /// <summary>
    /// 清空该物品占用格子
    /// </summary>
    public void ClearItem(PlacedItem item)
    {
        for (int x = 0; x < item.width; x++)
            for (int y = 0; y < item.height; y++)
                _cells[item.originX + x, item.originY + y] = 0;
    }

    /// <summary>
    /// 移动已有物品到新位置（先清再查再写）
    /// </summary>
    public bool MoveItem(int itemId, int newOriginX, int newOriginY)
    {
        if (!_placedItems.TryGetValue(itemId, out var item)) return false;

        // 先清掉旧位置
        ClearItem(item);

        // 检查新位置是否可放（旧位置已清，可以放回原位）
        if (!CanPlace(newOriginX, newOriginY, item.width, item.height))
        {
            // 放回去，恢复原位
            for (int x = 0; x < item.width; x++)
                for (int y = 0; y < item.height; y++)
                    _cells[item.originX + x, item.originY + y] = itemId;
            return false;
        }

        // 写入新位置
        for (int x = 0; x < item.width; x++)
            for (int y = 0; y < item.height; y++)
                _cells[newOriginX + x, newOriginY + y] = itemId;

        // 更新字典
        _placedItems[itemId] = new PlacedItem
        {
            itemId = itemId,
            originX = newOriginX, originY = newOriginY,
            width = item.width, height = item.height,
            itemData = item.itemData,
            rotated = item.rotated
        };

        return true;
    }
}

