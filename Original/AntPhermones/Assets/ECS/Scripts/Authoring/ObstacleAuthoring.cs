using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECS.Scripts
{
    public struct ObstacleSpawner : IComponentData
    {
        public Entity Prefab;
    }

    public class ObstacleAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var spawner = new ObstacleSpawner()
                {
                    Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Renderable),
                };
                AddComponent(entity, spawner);
            }
        }
    }
}


