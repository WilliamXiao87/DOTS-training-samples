using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace ECS.Scripts
{
    [RequireMatchingQueriesForUpdate]
    [CreateAfter(typeof(AntSpawnerSystem))]
    [UpdateAfter(typeof(AntSpawnerSystem))]
    public partial struct RenderingSystem : ISystem, ISystemStartStop
    {
        private static Texture2D texture;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Ant>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var mapSetting = SystemAPI.GetSingletonRW<MapSetting>();
            var gameObject = GameObject.Find("Ant Manager/Pheromone Renderer");
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            var material = meshRenderer.material;
            texture = new Texture2D(mapSetting.ValueRO.mapSize,mapSetting.ValueRO.mapSize, TextureFormat.RFloat, false);
            material.mainTexture = texture;
        }

        public void OnStopRunning(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var antRenderingJob = new AntRenderingJob();
            state.Dependency = antRenderingJob.Schedule(state.Dependency);
            
            /*// 绘制信息素
            var gameObject = GameObject.Find("Ant Manager/Pheromone Renderer");
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            var material = meshRenderer.material;
            var texture2D = material.mainTexture as Texture2D;*/

            var pheromones = SystemAPI.GetSingletonBuffer<Pheromone>();
            if (texture == null)
            {
                Debug.LogError("texture is null");
                return;
            }

            if (pheromones.Length == 0)
            {
                Debug.LogError("pheromones is null");
                return;
            }

            texture.SetPixelData(pheromones.AsNativeArray(), 0, 0);
            texture.Apply();
        }
    }
    
    [BurstCompile]
    [WithAll(typeof(Ant))]
    public partial struct AntRenderingJob : IJobEntity
    {
        [BurstCompile]
        public void Execute(in Ant ant, ref URPMaterialPropertyBaseColor color)
        {
            if(ant.holdingResource)
                color.Value = new Vector4(1, 1, 0, 1);
            else
                color.Value = new Vector4(0, 0, 1, 1);
        }
    }

}