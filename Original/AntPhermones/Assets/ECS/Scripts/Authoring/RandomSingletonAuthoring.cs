using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ECS.Scripts
{
    public struct RandomSingleton : IComponentData
    {
        public Random random;
    }

    public class RandomSingletonAuthoring : MonoBehaviour
    {
        public uint seed = 1;
        private class RandomSingletonBaker : Baker<RandomSingletonAuthoring>
        {
            public override void Bake(RandomSingletonAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var randomSingleton = new RandomSingleton
                {
                    random = new Random(authoring.seed),
                };
                AddComponent(entity, randomSingleton);
            }
        }
    }
}