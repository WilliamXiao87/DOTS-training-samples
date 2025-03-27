using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 蚂蚁类，表示一只蚂蚁及其属性。
/// </summary>
public class Ant {
    /// <summary>
    /// 蚂蚁的位置。
    /// </summary>
    public Vector2 position;
    /// <summary>
    /// 蚂蚁面向的角度。
    /// </summary>
    public float facingAngle;
    /// <summary>
    /// 蚂蚁的移动速度。
    /// </summary>
    public float speed;
    /// <summary>
    /// 蚂蚁是否持有资源。
    /// </summary>
    public bool holdingResource;
    /// <summary>
    /// 蚂蚁的亮度。
    /// </summary>
    public float brightness;

    /// <summary>
    /// 初始化蚂蚁类的新实例。
    /// </summary>
    /// <param name="pos">蚂蚁的初始位置。</param>
    public Ant(Vector2 pos) {
        position = pos;
        // 随机初始化面向角度，确保蚂蚁面向各个方向的可能性。
        facingAngle = Random.value * Mathf.PI * 2f;
        speed = 0f;
        holdingResource = false;
        // 随机初始化亮度，以模拟个体差异。
        brightness = Random.Range(.75f,1.25f);
    }
}
