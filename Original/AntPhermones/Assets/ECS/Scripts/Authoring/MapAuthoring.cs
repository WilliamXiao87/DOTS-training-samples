using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Scripts
{
    public struct MapSetting :IComponentData
    {
        public Entity colony;
        public Entity resource;
        public int mapSize;
        public int obstacleRingCount;
        public float obstacleRadius;
        public float obstaclesPerRing;
        public int bucketResolution;
        // 搜索颜色
        public Color searchColor;
        // 携带颜色
        public Color carryColor;
        // 蚂蚁数量
        public int antCount;
        // 蚂蚁尺寸
        public Vector3 antSize;
        // 蚂蚁速度
        public float antSpeed;
        // 蚂蚁加速度，范围在0到1之间
        [Range(0f,1f)]
        public float antAccel;
        
        // 信息素添加速度
        public float trailAddSpeed;
        // 信息素衰减率，范围在0到1之间
        [Range(0f,1f)]
        public float trailDecay;
        // 随机操控性
        public float randomSteering;
        // 信息素操控强度
        public float pheromoneSteerStrength;
        // 墙壁操控强度
        public float wallSteerStrength;
        // 目标操控强度
        public float goalSteerStrength;
        // 外向强度
        public float outwardStrength;
        // 内向强度
        public float inwardStrength;
        
        public Vector3 colonyPosition;
        public Vector3 resourcePosition;
    }

    [ChunkSerializable]
    public struct Bucket : IBufferElementData
    {
        public UnsafeList<float2> Obstacle;
    }

    public struct Pheromone : IBufferElementData
    {
        public float Strength;
    }
    

    public class MapAuthoring : MonoBehaviour
    {
        public GameObject colony;
        public GameObject resource;
        public int mapSize;
        // 障碍物环数
        public int obstacleRingCount;
        // 每环障碍物比例，范围在0到1之间
        [Range(0f,1f)]
        public float obstaclesPerRing;
        // 障碍物半径
        public float obstacleRadius;
        // 桶的分辨率
        public int bucketResolution;
        // 搜索颜色
        public Color searchColor;
        // 携带颜色
        public Color carryColor;
        // 蚂蚁数量
        public int antCount;
        // 蚂蚁尺寸
        public Vector3 antSize;
        // 蚂蚁速度
        public float antSpeed;
        // 蚂蚁加速度，范围在0到1之间
        [Range(0f,1f)]
        public float antAccel;
        
        // 信息素添加速度
        public float trailAddSpeed;
        // 信息素衰减率，范围在0到1之间
        [Range(0f,1f)]
        public float trailDecay;
        // 随机操控性
        public float randomSteering;
        // 信息素操控强度
        public float pheromoneSteerStrength;
        // 墙壁操控强度
        public float wallSteerStrength;
        // 目标操控强度
        public float goalSteerStrength;
        // 外向强度
        public float outwardStrength;
        // 内向强度
        public float inwardStrength;
        
        private class MapBaker : Baker<MapAuthoring>
        {
            public override void Bake(MapAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                MapSetting setting = new MapSetting()
                {
                    colony = GetEntity(authoring.colony, TransformUsageFlags.Renderable),
                    resource = GetEntity(authoring.resource, TransformUsageFlags.Renderable),
                    mapSize = authoring.mapSize,
                    obstacleRingCount = authoring.obstacleRingCount,
                    obstaclesPerRing = authoring.obstaclesPerRing,
                    obstacleRadius = authoring.obstacleRadius,
                    bucketResolution = authoring.bucketResolution,
                    searchColor = authoring.searchColor,
                    carryColor = authoring.carryColor,
                    antCount = authoring.antCount,
                    antSize = authoring.antSize,
                    antSpeed = authoring.antSpeed,
                    antAccel = authoring.antAccel,
                    trailAddSpeed = authoring.trailAddSpeed,
                    trailDecay = authoring.trailDecay,
                    randomSteering = authoring.randomSteering,
                    pheromoneSteerStrength = authoring.pheromoneSteerStrength,
                    wallSteerStrength = authoring.wallSteerStrength,
                    goalSteerStrength = authoring.goalSteerStrength,
                    outwardStrength = authoring.outwardStrength,
                    inwardStrength = authoring.inwardStrength,
                };
                AddComponent(entity, setting);
                AddBuffer<Bucket>(entity);
            }
        }
    }
}