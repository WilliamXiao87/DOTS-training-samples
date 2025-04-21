using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Scripts
{
    [RequireMatchingQueriesForUpdate]
    public partial struct ObstacleSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ObstacleSpawner>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            RefRW<RandomSingleton> random = SystemAPI.GetSingletonRW<RandomSingleton>();
            RefRW<MapSetting> mapSetting = SystemAPI.GetSingletonRW<MapSetting>();
            RefRW<ObstacleSpawner> obstacleSpawner = SystemAPI.GetSingletonRW<ObstacleSpawner>();
            
            float radius = mapSetting.ValueRO.obstacleRadius;
            int mapSize = mapSetting.ValueRO.mapSize;
            int bucketResolution = mapSetting.ValueRO.bucketResolution;
            
            var bucket = SystemAPI.GetSingletonBuffer<Bucket>();
            bucket.Length = bucketResolution * bucketResolution;
            for (int i = 0; i < bucket.Length; i++)
            {
                bucket[i] = new Bucket{Obstacle = new UnsafeList<float2>(0, Allocator.Persistent)};
            }

            // 遍历每个障碍物环
            for (int i=1;i<=mapSetting.ValueRO.obstacleRingCount;i++) {
                // 计算当前环的半径
                float ringRadius = (i / (mapSetting.ValueRO.obstacleRingCount+1f)) * (mapSetting.ValueRO.mapSize * .5f);
                // 计算当前环的周长
                float circumference = ringRadius * 2f * Mathf.PI;
                // 计算当前环上障碍物的最大数量
                int maxCount = Mathf.CeilToInt(circumference / (2f * mapSetting.ValueRO.obstacleRadius) * 2f);
                // 生成一个随机偏移量，用于在环上错开障碍物的位置
                int offset = random.ValueRW.random.NextInt(0,maxCount);
                // 随机决定当前环上的洞的数量
                int holeCount = random.ValueRW.random.NextInt(1,3);
                
                
                // 遍历当前环上的每个可能的障碍物位置
                for (int j=0;j<maxCount;j++) {
                    float t = (float)j / maxCount;
                    // 根据洞的数量决定是否在当前位置放置障碍物
                    if ((t * holeCount)%1f < mapSetting.ValueRO.obstaclesPerRing) {
                        // 计算障碍物的角度位置
                        float angle = (j + offset) / (float)maxCount * (2f * Mathf.PI);


                        float2 position = new float2(mapSize * .5f + Mathf.Cos(angle) * ringRadius,
                            mapSize * .5f + Mathf.Sin(angle) * ringRadius);
                        var obstalce = state.EntityManager.Instantiate(obstacleSpawner.ValueRO.Prefab);
                        var localTransform = SystemAPI.GetComponentRW<LocalTransform>(obstalce);
                        Vector3 localPosition = new Vector3(position.x / mapSize,
                            position.y / mapSize, 0);
                        localTransform.ValueRW.Position = localPosition;
                        localTransform.ValueRW.Scale = radius * 2f / mapSize;
                        
                        // 遍历障碍物影响的区域
                        for (int x = Mathf.FloorToInt((position.x - radius)/mapSize*bucketResolution); x <= Mathf.FloorToInt((position.x + radius)/mapSize*bucketResolution); x++) {
                            if (x < 0 || x >= bucketResolution) {
                                continue;
                            }
                            for (int y = Mathf.FloorToInt((position.y - radius) / mapSize * bucketResolution); y <= Mathf.FloorToInt((position.y + radius) / mapSize * bucketResolution); y++) {
                                if (y<0 || y>=bucketResolution) {
                                    continue;
                                }
                                int id = x + y * bucketResolution;
                                var list = bucket[id].Obstacle;
                                list.Add(position);
                                bucket[id] = new Bucket { Obstacle = list };
                            }
                        }
                    }
                }
            }
            state.Enabled = false;
        }
        
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            
        }
    }
}