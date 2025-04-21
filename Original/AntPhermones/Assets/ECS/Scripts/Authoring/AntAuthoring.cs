using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace ECS.Scripts
{
    public struct Position : IComponentData
    {
        public float2 position;
    }
    
    public struct Speed : IComponentData
    {
        public float speed;
        public float maxSpeed;
        public float accel;
    }

    public struct Direction : IComponentData
    {
        public float direction; // angle Z
    }

    public struct Ant : IComponentData
    {
        /// <summary>
        /// 蚂蚁的位置。
        /// </summary>
        public float2 position;
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

        public float4 color;
    }

    public class AntAuthoring : MonoBehaviour
    {
        private class AntBaker : Baker<AntAuthoring>
        {
            public override void Bake(AntAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                Ant ant = new Ant();
                Position pos = new Position();
                Direction direction = new Direction();
                Speed speed = new Speed();
                AddComponent(entity, ant);
                AddComponent(entity, pos);
                AddComponent(entity, direction);
                AddComponent(entity, speed);
                AddComponent(entity, new URPMaterialPropertyBaseColor{ Value = new float4(0,0,1, 1) });
            }
        }
    }
}