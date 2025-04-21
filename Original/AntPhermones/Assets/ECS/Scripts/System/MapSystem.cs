using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Scripts
{
    [RequireMatchingQueriesForUpdate]
    public partial struct MapSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapSetting>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            Debug.Log("MapSystem OnStartRunning==");
            RefRW<MapSetting> mapSetting = SystemAPI.GetSingletonRW<MapSetting>();
            int mapSize = mapSetting.ValueRO.mapSize;
            
            // 生成群居点
            Vector3 colonyPosition = Vector3.one * mapSize * .5f;
            colonyPosition = new Vector3(colonyPosition.x, colonyPosition.z, 0);
            var colony = state.EntityManager.Instantiate(mapSetting.ValueRO.colony);
            var colonyLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(colony);
            colonyLocalTransform.ValueRW.Position = colonyPosition / mapSize;
            colonyLocalTransform.ValueRW.Scale = 4f / mapSize;
            mapSetting.ValueRW.colonyPosition = colonyPosition;
            
            // 生成食物点
            float resourceAngle = Random.value * 2f * Mathf.PI;
            Vector3 resourcePosition = Vector3.one * mapSize * .5f + new Vector3(Mathf.Cos(resourceAngle) * mapSize * .475f,Mathf.Sin(resourceAngle) * mapSize * .475f, 0);
            resourcePosition.z = 0;
            var resource = state.EntityManager.Instantiate(mapSetting.ValueRO.resource);
            var resourceLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(resource);
            resourceLocalTransform.ValueRW.Position = resourcePosition /mapSize;
            resourceLocalTransform.ValueRW.Scale = 4f / mapSize;
            mapSetting.ValueRW.resourcePosition = resourcePosition;
            
            // 生成信息素
            var pheromone = state.EntityManager.CreateEntity();
            var pheromones = state.EntityManager.AddBuffer<Pheromone>(pheromone);
            pheromones.Length = mapSize * mapSize;
            for (int i = 0; i < pheromones.Length; i++)
            {
                pheromones[i] = new Pheromone { Strength = 0 };
            }

            /*
            pheromoneTexture = new Texture2D(mapSize,mapSize);
            pheromoneTexture.wrapMode = TextureWrapMode.Mirror;
            pheromones = new Color[mapSize * mapSize];
            myPheromoneMaterial = new Material(basePheromoneMaterial);
            myPheromoneMaterial.mainTexture = pheromoneTexture;
            pheromoneRenderer.sharedMaterial = myPheromoneMaterial;
            ants = new Ant[antCount];
            matrices = new Matrix4x4[Mathf.CeilToInt((float)antCount / instancesPerBatch)][];
            for (int i=0;i<matrices.Length;i++) {
                if (i<matrices.Length-1) {
                    matrices[i] = new Matrix4x4[instancesPerBatch];
                } else {
                    matrices[i] = new Matrix4x4[antCount - i * instancesPerBatch];
                }
            }
            matProps = new MaterialPropertyBlock[matrices.Length];
            antColors = new Vector4[matrices.Length][];
            for (int i=0;i<matProps.Length;i++) {
                antColors[i] = new Vector4[matrices[i].Length];
                matProps[i] = new MaterialPropertyBlock();
            }

            for (int i = 0; i < antCount; i++) {
                ants[i] = new Ant(new Vector2(Random.Range(-5f,5f)+mapSize*.5f,Random.Range(-5f,5f) + mapSize * .5f));
            }

            rotationMatrixLookup = new Matrix4x4[rotationResolution];
            for (int i=0;i<rotationResolution;i++) {
                float angle = (float)i / rotationResolution;
                angle *= 360f;
                rotationMatrixLookup[i] = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0f,0f,angle),antSize);
            }*/
            state.Enabled = false;
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
 
        }
    }
}