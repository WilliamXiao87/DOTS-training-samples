using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace ECS.Scripts.Job
{
    [BurstCompile]
    [WithAll(typeof(Ant))]
    public partial struct DropPheromonesJob :IJobEntity
    {
        public float deltaTime;
        [NativeDisableUnsafePtrRestriction]public RefRW<MapSetting> mapSetting;
        [NativeDisableParallelForRestriction]
        public NativeArray<Pheromone> pheromones;
        
        public void Execute(ref Ant ant)
        {
            int mapSize = mapSetting.ValueRO.mapSize;
            var position = ant.position;
            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y);
            // 检查位置是否有效
            if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
            {
                return;
            }
            
            float excitement = .3f;
            if (ant.holdingResource)
            {
                excitement = 1f;
            }

            int index = PheromoneIndex(x, y, mapSize);
            // 更新信息素浓度
            var pheromone = pheromones[index];
            pheromone.Strength += (mapSetting.ValueRO.trailAddSpeed * excitement * deltaTime) *
                                  (1f - pheromone.Strength);
            if (pheromone.Strength > 1f)
            {
                pheromone.Strength = 1f;
            }

            pheromones[index] = pheromone;
        }
        
        // 计算信息素索引
        public static int PheromoneIndex(int x, int y, int mapSize)
        {
            return x + y * mapSize;
        }
    }

    [BurstCompile]
    public struct PheromoneDecayJob : IJobParallelFor
    {
        public float trailDecay;
        public NativeArray<Pheromone> pheromones;
        public void Execute(int index)
        {
            var pheromone = pheromones[index];
            pheromone.Strength *= trailDecay;
            pheromones[index] = pheromone;
        }
    }
}