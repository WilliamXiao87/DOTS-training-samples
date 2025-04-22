using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Scripts.Job
{
    [BurstCompile]
    [WithAll(typeof(Ant))]
    public partial struct AntJob :IJobEntity
    {
	    public float deltaTime;
	    [NativeDisableUnsafePtrRestriction]public RefRW<RandomSingleton> random;
	    [NativeDisableUnsafePtrRestriction] public RefRW<MapSetting> mapSetting;
	    [ReadOnly]
	    public DynamicBuffer<Pheromone> pheromones;
	    [ReadOnly]
	    public DynamicBuffer<Bucket> buckets;
	    public int mapSize;
	    public float randomSteering;
	    public float pheromoneSteerStrength;
	    public float wallSteerStrength;
	    public float antAccel;
	    public float goalSteerStrength;
	    public float obstacleRadius;
	    public float outwardStrength;
	    public float inwardStrength;
	    public float antSpeed;
	    public float trailDecay;
	    public float2 resourcePosition;
	    public float2 colonyPosition;

	    [BurstCompile]
	    public void Execute(ref Ant ant, ref LocalTransform transform)
	    {
		    float targetSpeed = antSpeed;

		    ant.facingAngle += random.ValueRW.random.NextFloat(-randomSteering, randomSteering);

		    float pheroSteering = PheromoneSteering(ant, 3f, mapSize, mapSetting, pheromones);
		    int wallSteering = WallSteering(ant, 1.5f, mapSize, mapSetting, buckets);
		    ant.facingAngle += pheroSteering * pheromoneSteerStrength;
		    ant.facingAngle += wallSteering * wallSteerStrength;
		    //Debug.Log(
		    //	$"OnUpdate pheroSteering：{pheroSteering} wallSteering:{wallSteering} targetSpeed:{targetSpeed}");
		    targetSpeed *= 1f - (Mathf.Abs(pheroSteering) + Mathf.Abs(wallSteering)) / 3f;

		    ant.speed += (targetSpeed - ant.speed) * antAccel;

		    float2 targetPos;
		    if (ant.holdingResource == false)
		    {
			    targetPos = resourcePosition;
		    }
		    else
		    {
			    targetPos = colonyPosition;
		    }

		    if (Linecast(ant.position, targetPos, mapSize, mapSetting, buckets) == false)
		    {
			    Color color = Color.green;
			    float targetAngle = Mathf.Atan2(targetPos.y - ant.position.y,
				    targetPos.x - ant.position.x);
			    if (targetAngle - ant.facingAngle > Mathf.PI)
			    {
				    ant.facingAngle += Mathf.PI * 2f;
			    }
			    else if (targetAngle - ant.facingAngle < -Mathf.PI)
			    {
				    ant.facingAngle -= Mathf.PI * 2f;
			    }
			    else
			    {
				    if (Mathf.Abs(targetAngle - ant.facingAngle) < Mathf.PI * .5f)
					    ant.facingAngle += (targetAngle - ant.facingAngle) * goalSteerStrength;
			    }

			    //Debug.DrawLine(ant.position/mapSize,targetPos/mapSize,color);
		    }

		    Vector2 dir = new Vector2((ant.position - targetPos).x, (ant.position - targetPos).y);
		    if (dir.sqrMagnitude < 4f * 4f)
		    {
			    ant.holdingResource = !ant.holdingResource;
			    ant.facingAngle += Mathf.PI;
		    }

		    float vx = Mathf.Cos(ant.facingAngle) * ant.speed;
		    float vy = Mathf.Sin(ant.facingAngle) * ant.speed;
		    float ovx = vx;
		    float ovy = vy;

		    if (ant.position.x + vx < 0f || ant.position.x + vx > mapSize)
		    {
			    vx = -vx;
		    }
		    else
		    {
			    ant.position.x += vx;
		    }

		    if (ant.position.y + vy < 0f || ant.position.y + vy > mapSize)
		    {
			    vy = -vy;
		    }
		    else
		    {
			    ant.position.y += vy;
		    }

		    float dx, dy, dist;

		    var nearbyObstacles = GetObstacleBucket(ant.position, mapSize, mapSetting, buckets);
		    for (int j = 0; j < nearbyObstacles.Length; j++)
		    {
			    float2 obstacle = nearbyObstacles[j];
			    dx = ant.position.x - obstacle.x;
			    dy = ant.position.y - obstacle.y;
			    float sqrDist = dx * dx + dy * dy;
			    if (sqrDist < obstacleRadius * obstacleRadius)
			    {
				    dist = Mathf.Sqrt(sqrDist);
				    dx /= dist;
				    dy /= dist;
				    ant.position.x = obstacle.x + dx * obstacleRadius;
				    ant.position.y = obstacle.y + dy * obstacleRadius;

				    vx -= dx * (dx * vx + dy * vy) * 1.5f;
				    vy -= dy * (dx * vx + dy * vy) * 1.5f;
			    }
		    }

		    float inwardOrOutward = -outwardStrength;
		    float pushRadius = mapSize * .4f;
		    if (ant.holdingResource)
		    {
			    inwardOrOutward = inwardStrength;
			    pushRadius = mapSize;
		    }

		    dx = colonyPosition.x - ant.position.x;
		    dy = colonyPosition.y - ant.position.y;
		    dist = Mathf.Sqrt(dx * dx + dy * dy);
		    inwardOrOutward *= 1f - Mathf.Clamp01(dist / pushRadius);
		    vx += dx / dist * inwardOrOutward;
		    vy += dy / dist * inwardOrOutward;

		    if (ovx != vx || ovy != vy)
		    {
			    ant.facingAngle = Mathf.Atan2(vy, vx);
		    }

		    //if (ant.holdingResource == false) {
		    //float excitement = 1f-Mathf.Clamp01((targetPos - ant.position).magnitude / (mapSize * 1.2f));
		    float excitement = .3f;
		    if (ant.holdingResource)
		    {
			    excitement = 1f;
		    }

		    excitement *= ant.speed / antSpeed;
		    //DropPheromones(ant.position, excitement, mapSize, mapSetting, pheromones,deltaTime);
		    //}

		    transform.Position = new Vector3(ant.position.x / mapSize, ant.position.y / mapSize, 0);
		    var directionRad = ant.facingAngle; //
		    transform.Rotation = quaternion.Euler(0, 0, directionRad);
		    //Debug.Log(
		    //	$"OnUpdate=> facingAngle：{ant.facingAngle} speed:{ant.speed} position:{ant.position}");
	    }


	    // 计算信息素索引
		public static int PheromoneIndex(int x, int y, int mapSize)
		{
			return x + y * mapSize;
		}

		// 释放信息素
		public static void DropPheromones(Vector2 position, float strength, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Pheromone> pheromones, float deltaTime)
		{
			int x = Mathf.FloorToInt(position.x);
			int y = Mathf.FloorToInt(position.y);
			// 检查位置是否有效
			if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
			{
				return;
			}

			int index = PheromoneIndex(x, y, mapSize);
			// 更新信息素浓度
			var pheromone = pheromones[index];
			pheromone.Strength += (mapSetting.ValueRO.trailAddSpeed * strength * deltaTime) *
			                      (1f - pheromone.Strength);
			if (pheromone.Strength > 1f)
			{
				pheromone.Strength = 1f;
			}

			pheromones[index] = pheromone;
		}

		// 信息素操控
		public float PheromoneSteering(Ant ant, float distance, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Pheromone> pheromones)
		{
			float output = 0;
			for (int i = -1; i <= 1; i += 2)
			{
				float angle = ant.facingAngle + i * Mathf.PI * .25f;
				float testX = ant.position.x + Mathf.Cos(angle) * distance;
				float testY = ant.position.y + Mathf.Sin(angle) * distance;

				if (testX < 0 || testY < 0 || testX >= mapSize || testY >= mapSize)
				{

				}
				else
				{
					int index = PheromoneIndex((int)testX, (int)testY, mapSize);
					float value = pheromones[index].Strength;
					output += value * i;
				}
			}

			// 返回操控方向
			return Mathf.Sign(output);
		}

		// 墙壁操控
		public static int WallSteering(Ant ant, float distance, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Bucket> bucket)
		{
			int output = 0;

			for (int i = -1; i <= 1; i += 2)
			{
				float angle = ant.facingAngle + i * Mathf.PI * .25f;
				float testX = ant.position.x + Mathf.Cos(angle) * distance;
				float testY = ant.position.y + Mathf.Sin(angle) * distance;

				if (testX < 0 || testY < 0 || testX >= mapSize || testY >= mapSize)
				{
					
				}
				else
				{
					int value = GetObstacleBucket(testX, testY, mapSize, mapSetting, bucket).Length;
					if (value > 0)
					{
						output -= i;
					}
				}
			}
			// 返回操控方向
			return output;
		}

		public static UnsafeList<float2> GetObstacleBucket(Vector2 pos, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Bucket> bucket)
		{
			return GetObstacleBucket(pos.x, pos.y, mapSize, mapSetting, bucket);
		}

		public static UnsafeList<float2> GetObstacleBucket(float2 pos, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Bucket> bucket)
		{
			return GetObstacleBucket(pos.x, pos.y, mapSize, mapSetting, bucket);
		}

		public static UnsafeList<float2> emptyBucket = new UnsafeList<float2>(0, Allocator.Persistent);

		public static UnsafeList<float2> GetObstacleBucket(float posX, float posY, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Bucket> bucket)
		{
			int bucketResolution = mapSetting.ValueRO.bucketResolution;
			int x = (int)(posX / mapSize * bucketResolution);
			int y = (int)(posY / mapSize * bucketResolution);
			if (x < 0 || y < 0 || x >= bucketResolution || y >= bucketResolution)
			{
				//Debug.LogWarning("WallSteering 2");
				return emptyBucket;
			}
			else
			{
				return bucket[x + y * bucketResolution].Obstacle;
			}
		}


		// 线性检测
		public static bool Linecast(float2 point1, float2 point2, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Bucket> bucket)
		{
			float dx = point2.x - point1.x;
			float dy = point2.y - point1.y;
			float dist = Mathf.Sqrt(dx * dx + dy * dy);

			int stepCount = Mathf.CeilToInt(dist * .5f);
			for (int i = 0; i < stepCount; i++)
			{
				float t = (float)i / stepCount;
				if (GetObstacleBucket(point1.x + dx * t, point1.y + dy * t, mapSize, mapSetting, bucket).Length > 0)
				{
					return true;
				}
			}

			return false;
		}

    }
}