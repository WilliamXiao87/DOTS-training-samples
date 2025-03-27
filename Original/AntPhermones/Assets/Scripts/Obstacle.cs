using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结构体Obstacle用于在二维空间中表示障碍物的信息。
/// 它定义了障碍物的位置和大小，以便在游戏中进行碰撞检测或路径规划。
/// </summary>
public struct Obstacle {
    /// <summary>
    /// 障碍物的位置，使用二维向量表示。
    /// </summary>
	public Vector2 position;
	
	/// <summary>
    /// 障碍物的半径，用于定义障碍物的大小。
    /// </summary>
	public float radius;
}