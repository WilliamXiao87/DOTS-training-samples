using Unity.Entities;
using UnityEngine;

namespace ECS.Scripts
{
    public struct AntSpawner : IComponentData
    {
        public Entity Prefab;
    }


    public class AntSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        private class AntSpawnerBaker : Baker<AntSpawnerAuthoring>
        {
            public override void Bake(AntSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AntSpawner spawner = new AntSpawner
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Renderable),
                };
                AddComponent(entity, spawner);
            }
        }
    }
}