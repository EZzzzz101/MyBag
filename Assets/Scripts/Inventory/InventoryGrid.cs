using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 背包网格数据结构。只管数据，不管UI
/// </summary>
public class InventoryGrid
{
    public int Width { get ;private set; } 
    public int Height { get ;private set; } 
    private int[,] _cells;//0=空，>0=占用

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
    /// 将物品放入网格并且记住ID
    /// </summary>
    public bool Place(int originX,int originY,int itemWidth,int itemHeight,int itemId = 1)
    {
        if(!CanPlace(originX,originY,itemWidth,itemHeight)) return false;

        for(int x=0; x < itemWidth; x++)
        {
            for(int y = 0; y < itemHeight; y++)
            {
                _cells[originX+x,originY+y]=itemId;
            }
        }

        return true;
    }

    public bool Remove(int originX, int originY, int itemWidth, int itemHeight)
    {
        for (int x = 0; x < itemWidth; x++)
        {      
            for(int y = 0; y < itemHeight; y++)
            {
                _cells[originX+x,originY+y]=0;
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

