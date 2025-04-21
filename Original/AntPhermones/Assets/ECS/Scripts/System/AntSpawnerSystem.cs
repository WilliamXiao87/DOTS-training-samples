using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering.UI;
using Random = UnityEngine.Random;

namespace ECS.Scripts
{
	[RequireMatchingQueriesForUpdate]
	public partial struct AntSpawnerSystem : ISystem, ISystemStartStop
	{
		// 每批处理的实例数
		const int instancesPerBatch = 1023;
		
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<AntSpawner>();
			state.RequireForUpdate<MapSetting>();
			state.RequireForUpdate<Pheromone>();
		}

		[BurstCompile]
		public void OnStartRunning(ref SystemState state)
		{
			var antSpawner = SystemAPI.GetSingletonRW<AntSpawner>();
			var mapSetting = SystemAPI.GetSingletonRW<MapSetting>();
			for (int i = 0; i < mapSetting.ValueRO.antCount; i++)
			{
				var entity = state.EntityManager.Instantiate(antSpawner.ValueRO.Prefab);
			}

			InitAnt(ref state, mapSetting);
			//Debug.Log("antSpawner.ValueRO.Prefab ==");
			//state.Enabled = false;
		}

		private void InitAnt(ref SystemState state, RefRW<MapSetting> mapSetting)
		{
			var random = SystemAPI.GetSingletonRW<RandomSingleton>();
			float mapSize = mapSetting.ValueRO.mapSize;
			foreach (var (localTransform, ant) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Ant>>())
			{
				ant.ValueRW.position = new float2(
					(random.ValueRW.random.NextFloat(-5f, 5f) + mapSize * .5f),
					(random.ValueRW.random.NextFloat(-5f, 5f) + mapSize * .5f));
				ant.ValueRW.facingAngle = random.ValueRW.random.NextFloat(0, 360);
				localTransform.ValueRW.Position =
					new Vector3(ant.ValueRW.position.x / mapSize, ant.ValueRW.position.y / mapSize, 0);

				var directionRad = ant.ValueRW.facingAngle / 180f * math.PI;
				localTransform.ValueRW.Rotation = quaternion.Euler(0, 0, directionRad);
				localTransform.ValueRW.Scale = 2.0f / mapSize;
				ant.ValueRW.brightness = random.ValueRW.random.NextFloat(.75f, 1.25f);
			}
		}

		// 计算信息素索引
		int PheromoneIndex(int x, int y, int mapSize)
		{
			return x + y * mapSize;
		}

		// 释放信息素
		void DropPheromones(Vector2 position, float strength, int mapSize, RefRW<MapSetting> mapSetting, DynamicBuffer<Pheromone> pheromones)
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
			pheromone.Strength += (mapSetting.ValueRO.trailAddSpeed * strength * Time.fixedDeltaTime) *
			                      (1f - pheromone.Strength);
			if (pheromone.Strength > 1f)
			{
				pheromone.Strength = 1f;
			}

			pheromones[index] = pheromone;
		}

		// 信息素操控
		float PheromoneSteering(RefRW<Ant> ant, float distance, int mapSize, RefRW<MapSetting> mapSetting)
		{
			float output = 0;
			var pheromones = SystemAPI.GetSingletonBuffer<Pheromone>();
			for (int i = -1; i <= 1; i += 2)
			{
				float angle = ant.ValueRO.facingAngle + i * Mathf.PI * .25f;
				float testX = ant.ValueRO.position.x + Mathf.Cos(angle) * distance;
				float testY = ant.ValueRO.position.y + Mathf.Sin(angle) * distance;

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
		int WallSteering(RefRW<Ant> ant, float distance, int mapSize, RefRW<MapSetting> mapSetting)
		{
			int output = 0;

			for (int i = -1; i <= 1; i += 2)
			{
				float angle = ant.ValueRO.facingAngle + i * Mathf.PI * .25f;
				float testX = ant.ValueRO.position.x + Mathf.Cos(angle) * distance;
				float testY = ant.ValueRO.position.y + Mathf.Sin(angle) * distance;

				if (testX < 0 || testY < 0 || testX >= mapSize || testY >= mapSize)
				{
					
				}
				else
				{
					int value = GetObstacleBucket(testX, testY, mapSize, mapSetting).Length;
					if (value > 0)
					{
						output -= i;
					}
				}
			}
			// 返回操控方向
			return output;
		}

		UnsafeList<float2> GetObstacleBucket(Vector2 pos, int mapSize, RefRW<MapSetting> mapSetting)
		{
			return GetObstacleBucket(pos.x, pos.y, mapSize, mapSetting);
		}

		UnsafeList<float2> GetObstacleBucket(float2 pos, int mapSize, RefRW<MapSetting> mapSetting)
		{
			return GetObstacleBucket(pos.x, pos.y, mapSize, mapSetting);
		}

		static UnsafeList<float2> emptyBucket = new UnsafeList<float2>(0, Allocator.Persistent);

		UnsafeList<float2> GetObstacleBucket(float posX, float posY, int mapSize, RefRW<MapSetting> mapSetting)
		{
			var bucket = SystemAPI.GetSingletonBuffer<Bucket>();
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
		bool Linecast(float2 point1, float2 point2, int mapSize, RefRW<MapSetting> mapSetting)
		{
			float dx = point2.x - point1.x;
			float dy = point2.y - point1.y;
			float dist = Mathf.Sqrt(dx * dx + dy * dy);

			int stepCount = Mathf.CeilToInt(dist * .5f);
			for (int i = 0; i < stepCount; i++)
			{
				float t = (float)i / stepCount;
				if (GetObstacleBucket(point1.x + dx * t, point1.y + dy * t, mapSize, mapSetting).Length > 0)
				{
					return true;
				}
			}

			return false;
		}


		public void OnUpdate(ref SystemState state)
		{
			/*Debug.Log("OnUpdate ==11111");*/
			var random = SystemAPI.GetSingletonRW<RandomSingleton>();
			var mapSetting = SystemAPI.GetSingletonRW<MapSetting>();
			int mapSize = mapSetting.ValueRO.mapSize;
			float randomSteering = mapSetting.ValueRO.randomSteering;
			float pheromoneSteerStrength = mapSetting.ValueRO.pheromoneSteerStrength;
			float wallSteerStrength = mapSetting.ValueRO.wallSteerStrength;
			float antAccel = mapSetting.ValueRO.antAccel;
			float goalSteerStrength = mapSetting.ValueRO.goalSteerStrength;
			float obstacleRadius = mapSetting.ValueRO.obstacleRadius;
			float outwardStrength = mapSetting.ValueRO.outwardStrength;
			float inwardStrength = mapSetting.ValueRO.inwardStrength;
			float antSpeed = mapSetting.ValueRO.antSpeed;
			float trailDecay = mapSetting.ValueRO.trailDecay;
			float2 resourcePosition =
				new float2(mapSetting.ValueRO.resourcePosition.x, mapSetting.ValueRO.resourcePosition.y);
			float2 colonyPosition =
				new float2(mapSetting.ValueRO.colonyPosition.x, mapSetting.ValueRO.colonyPosition.y);
			float4 searchColor = new float4(mapSetting.ValueRO.searchColor.r, mapSetting.ValueRO.searchColor.g,
				mapSetting.ValueRO.searchColor.b, 1);
			float4 carryColor = new float4(mapSetting.ValueRO.carryColor.r, mapSetting.ValueRO.carryColor.g,
				mapSetting.ValueRO.carryColor.b, 1);
			var pheromones = SystemAPI.GetSingletonBuffer<Pheromone>();
			if(pheromones.Length == 0)
				return;
			foreach (var (localTransform, ant) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Ant>>())
			{
				//Debug.Log(
				//	$"OnUpdate facingAngle：{ant.ValueRW.facingAngle} speed:{ant.ValueRW.speed} position:{ant.ValueRW.position}");
				float targetSpeed = antSpeed;

				ant.ValueRW.facingAngle += random.ValueRW.random.NextFloat(-randomSteering, randomSteering);

				float pheroSteering = PheromoneSteering(ant, 3f, mapSize, mapSetting);
				int wallSteering = WallSteering(ant, 1.5f, mapSize, mapSetting);
				ant.ValueRW.facingAngle += pheroSteering * pheromoneSteerStrength;
				ant.ValueRW.facingAngle += wallSteering * wallSteerStrength;
				//Debug.Log(
				//	$"OnUpdate pheroSteering：{pheroSteering} wallSteering:{wallSteering} targetSpeed:{targetSpeed}");
				targetSpeed *= 1f - (Mathf.Abs(pheroSteering) + Mathf.Abs(wallSteering)) / 3f;

				ant.ValueRW.speed += (targetSpeed - ant.ValueRO.speed) * antAccel;

				float2 targetPos;
				if (ant.ValueRO.holdingResource == false)
				{
					targetPos = resourcePosition;
					ant.ValueRW.color += (searchColor * ant.ValueRO.brightness - ant.ValueRO.color) * .05f;
				}
				else
				{
					targetPos = colonyPosition;
					ant.ValueRW.color += (carryColor * ant.ValueRO.brightness - ant.ValueRO.color) * .05f;
				}

				if (Linecast(ant.ValueRO.position, targetPos, mapSize, mapSetting) == false)
				{
					Color color = Color.green;
					float targetAngle = Mathf.Atan2(targetPos.y - ant.ValueRO.position.y,
						targetPos.x - ant.ValueRO.position.x);
					if (targetAngle - ant.ValueRO.facingAngle > Mathf.PI)
					{
						ant.ValueRW.facingAngle += Mathf.PI * 2f;
						color = Color.red;
					}
					else if (targetAngle - ant.ValueRO.facingAngle < -Mathf.PI)
					{
						ant.ValueRW.facingAngle -= Mathf.PI * 2f;
						color = Color.red;
					}
					else
					{
						if (Mathf.Abs(targetAngle - ant.ValueRO.facingAngle) < Mathf.PI * .5f)
							ant.ValueRW.facingAngle += (targetAngle - ant.ValueRO.facingAngle) * goalSteerStrength;
					}

					//Debug.DrawLine(ant.position/mapSize,targetPos/mapSize,color);
				}

				Vector2 dir = new Vector2((ant.ValueRO.position - targetPos).x, (ant.ValueRO.position - targetPos).y);
				if (dir.sqrMagnitude < 4f * 4f)
				{
					ant.ValueRW.holdingResource = !ant.ValueRO.holdingResource;
					ant.ValueRW.facingAngle += Mathf.PI;
				}

				float vx = Mathf.Cos(ant.ValueRO.facingAngle) * ant.ValueRO.speed;
				float vy = Mathf.Sin(ant.ValueRO.facingAngle) * ant.ValueRO.speed;
				float ovx = vx;
				float ovy = vy;

				if (ant.ValueRO.position.x + vx < 0f || ant.ValueRO.position.x + vx > mapSize)
				{
					vx = -vx;
				}
				else
				{
					ant.ValueRW.position.x += vx;
				}

				if (ant.ValueRO.position.y + vy < 0f || ant.ValueRO.position.y + vy > mapSize)
				{
					vy = -vy;
				}
				else
				{
					ant.ValueRW.position.y += vy;
				}

				float dx, dy, dist;

				var nearbyObstacles = GetObstacleBucket(ant.ValueRO.position, mapSize, mapSetting);
				for (int j = 0; j < nearbyObstacles.Length; j++)
				{
					float2 obstacle = nearbyObstacles[j];
					dx = ant.ValueRO.position.x - obstacle.x;
					dy = ant.ValueRO.position.y - obstacle.y;
					float sqrDist = dx * dx + dy * dy;
					if (sqrDist < obstacleRadius * obstacleRadius)
					{
						dist = Mathf.Sqrt(sqrDist);
						dx /= dist;
						dy /= dist;
						ant.ValueRW.position.x = obstacle.x + dx * obstacleRadius;
						ant.ValueRW.position.y = obstacle.y + dy * obstacleRadius;

						vx -= dx * (dx * vx + dy * vy) * 1.5f;
						vy -= dy * (dx * vx + dy * vy) * 1.5f;
					}
				}

				float inwardOrOutward = -outwardStrength;
				float pushRadius = mapSize * .4f;
				if (ant.ValueRO.holdingResource)
				{
					inwardOrOutward = inwardStrength;
					pushRadius = mapSize;
				}

				dx = colonyPosition.x - ant.ValueRO.position.x;
				dy = colonyPosition.y - ant.ValueRO.position.y;
				dist = Mathf.Sqrt(dx * dx + dy * dy);
				inwardOrOutward *= 1f - Mathf.Clamp01(dist / pushRadius);
				vx += dx / dist * inwardOrOutward;
				vy += dy / dist * inwardOrOutward;

				if (ovx != vx || ovy != vy)
				{
					ant.ValueRW.facingAngle = Mathf.Atan2(vy, vx);
				}

				//if (ant.holdingResource == false) {
				//float excitement = 1f-Mathf.Clamp01((targetPos - ant.position).magnitude / (mapSize * 1.2f));
				float excitement = .3f;
				if (ant.ValueRW.holdingResource)
				{
					excitement = 1f;
				}

				excitement *= ant.ValueRO.speed / antSpeed;
				DropPheromones(ant.ValueRO.position, excitement, mapSize, mapSetting, pheromones);
				//}

				localTransform.ValueRW.Position = new Vector3(ant.ValueRO.position.x/mapSize, ant.ValueRO.position.y/mapSize, 0);
				var directionRad = ant.ValueRO.facingAngle;//
				localTransform.ValueRW.Rotation = quaternion.Euler(0, 0, directionRad);
				//Debug.Log(
				//	$"OnUpdate=> facingAngle：{ant.ValueRW.facingAngle} speed:{ant.ValueRW.speed} position:{ant.ValueRW.position}");
				//state.Enabled = false;
			}

			for (int x = 0; x < mapSize; x++)
			{
				for (int y = 0; y < mapSize; y++)
				{
					int index = PheromoneIndex(x, y, mapSize);
					var pheromone = pheromones[index];
					pheromone.Strength *= trailDecay;
					pheromones[index] = pheromone;
				}
			}
		}


		[BurstCompile]
		public void OnStopRunning(ref SystemState state)
		{

		}
	}
}