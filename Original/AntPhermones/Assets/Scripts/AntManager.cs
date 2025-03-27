using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntManager : MonoBehaviour {
	public Material basePheromoneMaterial;
		// 蚂蚁信息素渲染器
	public Renderer pheromoneRenderer;
	// 蚂蚁材质
	public Material antMaterial;
	// 障碍物材质
	public Material obstacleMaterial;
	// 资源材质
	public Material resourceMaterial;
	// 殖民地材质
	public Material colonyMaterial;
	// 蚂蚁模型网格
	public Mesh antMesh;
	// 障碍物模型网格
	public Mesh obstacleMesh;
	// 殖民地模型网格
	public Mesh colonyMesh;
	// 资源模型网格
	public Mesh resourceMesh;
	// 搜索颜色
	public Color searchColor;
	// 携带颜色
	public Color carryColor;
	// 蚂蚁数量
	public int antCount;
	// 地图大小
	public int mapSize = 128;
	// 桶的分辨率
	public int bucketResolution;
	// 蚂蚁尺寸
	public Vector3 antSize;
	// 蚂蚁速度
	public float antSpeed;
	// 蚂蚁加速度，范围在0到1之间
	[Range(0f,1f)]
	public float antAccel;
	// 信息素添加速度
	public float trailAddSpeed;
	// 信息素衰减率，范围在0到1之间
	[Range(0f,1f)]
	public float trailDecay;
	// 随机操控性
	public float randomSteering;
	// 信息素操控强度
	public float pheromoneSteerStrength;
	// 墙壁操控强度
	public float wallSteerStrength;
	// 目标操控强度
	public float goalSteerStrength;
	// 外向强度
	public float outwardStrength;
	// 内向强度
	public float inwardStrength;
	// 旋转分辨率
	public int rotationResolution = 360;
	// 障碍物环数
	public int obstacleRingCount;
	// 每环障碍物比例，范围在0到1之间
	[Range(0f,1f)]
	public float obstaclesPerRing;
	// 障碍物半径
	public float obstacleRadius;
	
	// 信息素纹理
	Texture2D pheromoneTexture;
	// 自定义信息素材质
	Material myPheromoneMaterial;
	
	// 信息素颜色数组
	Color[] pheromones;
	// 蚂蚁数组
	Ant[] ants;
	// 矩阵数组
	Matrix4x4[][] matrices;
	// 蚂蚁颜色数组
	Vector4[][] antColors;
	// 材质属性块数组
	MaterialPropertyBlock[] matProps;
	// 障碍物数组
	Obstacle[] obstacles;
	// 障碍物矩阵数组
	Matrix4x4[][] obstacleMatrices;
	// 障碍物桶数组
	Obstacle[,][] obstacleBuckets;
	
	// 资源模型矩阵
	Matrix4x4 resourceMatrix;
	// 殖民地模型矩阵
	Matrix4x4 colonyMatrix;
	
	// 资源位置
	Vector2 resourcePosition;
	// 殖民地位置
	Vector2 colonyPosition;
	
	// 每批处理的实例数
	const int instancesPerBatch = 1023;
	
	// 旋转矩阵查找表
	Matrix4x4[] rotationMatrixLookup;
	
	// 获取旋转矩阵
	Matrix4x4 GetRotationMatrix(float angle) {
	    // 角度归一化
	    angle /= Mathf.PI * 2f;
	    angle -= Mathf.Floor(angle);
	    angle *= rotationResolution;
	    // 返回对应的旋转矩阵
	    return rotationMatrixLookup[((int)angle)%rotationResolution];
	}
	
	// 计算信息素索引
	int PheromoneIndex(int x, int y) {
	    return x + y * mapSize;
	}
	
	// 释放信息素
	void DropPheromones(Vector2 position,float strength) {
	    int x = Mathf.FloorToInt(position.x);
	    int y = Mathf.FloorToInt(position.y);
	    // 检查位置是否有效
	    if (x < 0 || y < 0 || x >= mapSize || y >= mapSize) {
	        return;
	    }
	
	    int index = PheromoneIndex(x,y);
	    // 更新信息素浓度
	    pheromones[index].r += (trailAddSpeed*strength*Time.fixedDeltaTime)*(1f-pheromones[index].r);
	    if (pheromones[index].r>1f) {
	        pheromones[index].r = 1f;
	    }
	}
	
	// 信息素操控
	float PheromoneSteering(Ant ant,float distance) {
	    float output = 0;
	
	    for (int i=-1;i<=1;i+=2) {
	        float angle = ant.facingAngle + i * Mathf.PI*.25f;
	        float testX = ant.position.x + Mathf.Cos(angle) * distance;
	        float testY = ant.position.y + Mathf.Sin(angle) * distance;
	
	        if (testX <0 || testY<0 || testX>=mapSize || testY>=mapSize) {
	
	        } else {
	            int index = PheromoneIndex((int)testX,(int)testY);
	            float value = pheromones[index].r;
	            output += value*i;
	        }
	    }
	    // 返回操控方向
	    return Mathf.Sign(output);
	}
	
	// 墙壁操控
	int WallSteering(Ant ant,float distance) {
	    int output = 0;
	
	    for (int i = -1; i <= 1; i+=2) {
	        float angle = ant.facingAngle + i * Mathf.PI*.25f;
	        float testX = ant.position.x + Mathf.Cos(angle) * distance;
	        float testY = ant.position.y + Mathf.Sin(angle) * distance;
	
	        if (testX < 0 || testY < 0 || testX >= mapSize || testY >= mapSize) {
	
	        } else {
	            int value = GetObstacleBucket(testX,testY).Length;
	            if (value > 0) {
	                output -= i;
	            }
	        }
	    }
	    // 返回操控方向
	    return output;
	}
	
	// 线性检测
	bool Linecast(Vector2 point1, Vector2 point2) {
	    float dx = point2.x - point1.x;
	    float dy = point2.y - point1.y;
	    float dist = Mathf.Sqrt(dx * dx + dy * dy);
	
	    int stepCount = Mathf.CeilToInt(dist*.5f);
	    for (int i=0;i<stepCount;i++) {
	        float t = (float)i / stepCount;
	        if (GetObstacleBucket(point1.x+dx*t,point1.y+dy*t).Length>0) {
	            return true;
	        }
	    }
	
	    return false;
	}
	
	// 生成障碍物
	void GenerateObstacles() {	    // 初始化障碍物列表
	    List<Obstacle> output = new List<Obstacle>();
	    
	    // 遍历每个障碍物环
	    for (int i=1;i<=obstacleRingCount;i++) {
	        // 计算当前环的半径
	        float ringRadius = (i / (obstacleRingCount+1f)) * (mapSize * .5f);
	        // 计算当前环的周长
	        float circumference = ringRadius * 2f * Mathf.PI;
	        // 计算当前环上障碍物的最大数量
	        int maxCount = Mathf.CeilToInt(circumference / (2f * obstacleRadius) * 2f);
	        // 生成一个随机偏移量，用于在环上错开障碍物的位置
	        int offset = Random.Range(0,maxCount);
	        // 随机决定当前环上的洞的数量
	        int holeCount = Random.Range(1,3);
	        
	        // 遍历当前环上的每个可能的障碍物位置
	        for (int j=0;j<maxCount;j++) {
	            float t = (float)j / maxCount;
	            // 根据洞的数量决定是否在当前位置放置障碍物
	            if ((t * holeCount)%1f < obstaclesPerRing) {
	                // 计算障碍物的角度位置
	                float angle = (j + offset) / (float)maxCount * (2f * Mathf.PI);
	                // 创建并初始化障碍物
	                Obstacle obstacle = new Obstacle();
	                // 设置障碍物的位置
	                obstacle.position = new Vector2(mapSize * .5f + Mathf.Cos(angle) * ringRadius,mapSize * .5f + Mathf.Sin(angle) * ringRadius);
	                // 设置障碍物的半径
	                obstacle.radius = obstacleRadius;
	                // 将障碍物添加到输出列表中
	                output.Add(obstacle);
	                //Debug.DrawRay(obstacle.position / mapSize,-Vector3.forward * .05f,Color.green,10000f);
	            }
	        }
	    }
	    
	    // 初始化障碍物的矩阵数组，用于批量绘制
	    obstacleMatrices = new Matrix4x4[Mathf.CeilToInt((float)output.Count / instancesPerBatch)][];
	    // 为每个批量绘制的障碍物生成矩阵
	    for (int i=0;i<obstacleMatrices.Length;i++) {
	        obstacleMatrices[i] = new Matrix4x4[Mathf.Min(instancesPerBatch,output.Count - i * instancesPerBatch)];
	        // 为当前批次的每个障碍物生成变换矩阵
	        for (int j=0;j<obstacleMatrices[i].Length;j++) {
	            obstacleMatrices[i][j] = Matrix4x4.TRS(output[i * instancesPerBatch + j].position / mapSize,Quaternion.identity,new Vector3(obstacleRadius*2f,obstacleRadius*2f,1f)/mapSize);
	        }
	    }
	    
	    // 将输出列表转换为数组
	    obstacles = output.ToArray();
	    
	    // 初始化临时的障碍物桶，用于空间划分
	    List<Obstacle>[,] tempObstacleBuckets = new List<Obstacle>[bucketResolution,bucketResolution];
	    
	    // 初始化每个桶
	    for (int x = 0; x < bucketResolution; x++) {
	        for (int y = 0; y < bucketResolution; y++) {
	            tempObstacleBuckets[x,y] = new List<Obstacle>();
	        }
	    }
	    
	    // 将每个障碍物添加到相应的桶中
	    for (int i = 0; i < obstacles.Length; i++) {
	        Vector2 pos = obstacles[i].position;
	        float radius = obstacles[i].radius;
	        // 遍历障碍物影响的区域
	        for (int x = Mathf.FloorToInt((pos.x - radius)/mapSize*bucketResolution); x <= Mathf.FloorToInt((pos.x + radius)/mapSize*bucketResolution); x++) {
	            if (x < 0 || x >= bucketResolution) {
	                continue;
	            }
	            for (int y = Mathf.FloorToInt((pos.y - radius) / mapSize * bucketResolution); y <= Mathf.FloorToInt((pos.y + radius) / mapSize * bucketResolution); y++) {
	                if (y<0 || y>=bucketResolution) {
	                    continue;
	                }
	                tempObstacleBuckets[x,y].Add(obstacles[i]);
	            }
	        }
	    }
	    
	    // 初始化最终的障碍物桶数组
	    obstacleBuckets = new Obstacle[bucketResolution,bucketResolution][];
	    // 将临时障碍物桶转换为数组并赋值给最终的障碍物桶
	    for (int x = 0; x < bucketResolution; x++) {
	        for (int y = 0; y < bucketResolution; y++) {
	            obstacleBuckets[x,y] = tempObstacleBuckets[x,y].ToArray();
	        }
	    }
	}
	
	// 空障碍物桶
	Obstacle[] emptyBucket = new Obstacle[0];
	// 获取障碍物桶
	Obstacle[] GetObstacleBucket(Vector2 pos) {
	    return GetObstacleBucket(pos.x,pos.y);
	}
	Obstacle[] GetObstacleBucket(float posX, float posY) {
	    int x = (int)(posX / mapSize * bucketResolution);
	    int y = (int)(posY / mapSize * bucketResolution);
	    if (x<0 || y<0 || x>=bucketResolution || y>=bucketResolution) {
	        return emptyBucket;
	    } else {
	        return obstacleBuckets[x,y];
	    }
	}
	void Start () {

		GenerateObstacles();

		colonyPosition = Vector2.one * mapSize * .5f;
		colonyMatrix = Matrix4x4.TRS(colonyPosition/mapSize,Quaternion.identity,new Vector3(4f,4f,.1f)/mapSize);
		float resourceAngle = Random.value * 2f * Mathf.PI;
		resourcePosition = Vector2.one * mapSize * .5f + new Vector2(Mathf.Cos(resourceAngle) * mapSize * .475f,Mathf.Sin(resourceAngle) * mapSize * .475f);
		resourceMatrix = Matrix4x4.TRS(resourcePosition / mapSize,Quaternion.identity,new Vector3(4f,4f,.1f) / mapSize);

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
		}
	}

	void FixedUpdate() {
		for (int i = 0; i < ants.Length; i++) {
			Ant ant = ants[i];
			float targetSpeed = antSpeed;

			ant.facingAngle += Random.Range(-randomSteering,randomSteering);

			float pheroSteering = PheromoneSteering(ant,3f);
			int wallSteering = WallSteering(ant,1.5f);
			ant.facingAngle += pheroSteering * pheromoneSteerStrength;
			ant.facingAngle += wallSteering * wallSteerStrength;

			targetSpeed *= 1f - (Mathf.Abs(pheroSteering) + Mathf.Abs(wallSteering)) / 3f;

			ant.speed += (targetSpeed - ant.speed) * antAccel;

			Vector2 targetPos;
			int index1 = i / instancesPerBatch;
			int index2 = i % instancesPerBatch;
			if (ant.holdingResource == false) {
				targetPos = resourcePosition;

				antColors[index1][index2] += ((Vector4)searchColor * ant.brightness - antColors[index1][index2])*.05f;
			} else {
				targetPos = colonyPosition;
				antColors[index1][index2] += ((Vector4)carryColor * ant.brightness - antColors[index1][index2]) * .05f;
			}
			if (Linecast(ant.position,targetPos)==false) {
				Color color = Color.green;
				float targetAngle = Mathf.Atan2(targetPos.y-ant.position.y,targetPos.x-ant.position.x);
				if (targetAngle - ant.facingAngle > Mathf.PI) {
					ant.facingAngle += Mathf.PI * 2f;
					color = Color.red;
				} else if (targetAngle - ant.facingAngle < -Mathf.PI) {
					ant.facingAngle -= Mathf.PI * 2f;
					color = Color.red;
				} else {
					if (Mathf.Abs(targetAngle-ant.facingAngle)<Mathf.PI*.5f)
					ant.facingAngle += (targetAngle-ant.facingAngle)*goalSteerStrength;
				}

				//Debug.DrawLine(ant.position/mapSize,targetPos/mapSize,color);
			}
			if ((ant.position - targetPos).sqrMagnitude < 4f * 4f) {
				ant.holdingResource = !ant.holdingResource;
				ant.facingAngle += Mathf.PI;
			}

			float vx = Mathf.Cos(ant.facingAngle) * ant.speed;
			float vy = Mathf.Sin(ant.facingAngle) * ant.speed;
			float ovx = vx;
			float ovy = vy;

			if (ant.position.x + vx < 0f || ant.position.x + vx > mapSize) {
				vx = -vx;
			} else {
				ant.position.x += vx;
			}
			if (ant.position.y + vy < 0f || ant.position.y + vy > mapSize) {
				vy = -vy;
			} else {
				ant.position.y += vy;
			}

			float dx, dy, dist;

			Obstacle[] nearbyObstacles = GetObstacleBucket(ant.position);
			for (int j=0;j<nearbyObstacles.Length;j++) {
				Obstacle obstacle = nearbyObstacles[j];
				dx = ant.position.x - obstacle.position.x;
				dy = ant.position.y - obstacle.position.y;
				float sqrDist = dx * dx + dy * dy;
				if (sqrDist<obstacleRadius*obstacleRadius) {
					dist = Mathf.Sqrt(sqrDist);
					dx /= dist;
					dy /= dist;
					ant.position.x = obstacle.position.x + dx * obstacleRadius;
					ant.position.y = obstacle.position.y + dy * obstacleRadius;

					vx -= dx * (dx * vx + dy * vy) * 1.5f;
					vy -= dy * (dx * vx + dy * vy) * 1.5f;
				}
			}

			float inwardOrOutward = -outwardStrength;
			float pushRadius = mapSize * .4f;
			if (ant.holdingResource) {
				inwardOrOutward = inwardStrength;
				pushRadius = mapSize;
			}
			dx = colonyPosition.x - ant.position.x;
			dy = colonyPosition.y - ant.position.y;
			dist = Mathf.Sqrt(dx * dx + dy * dy);
			inwardOrOutward *= 1f-Mathf.Clamp01(dist / pushRadius);
			vx += dx / dist * inwardOrOutward;
			vy += dy / dist * inwardOrOutward;

			if (ovx != vx || ovy != vy) {
				ant.facingAngle = Mathf.Atan2(vy,vx);
			}

			//if (ant.holdingResource == false) {
			//float excitement = 1f-Mathf.Clamp01((targetPos - ant.position).magnitude / (mapSize * 1.2f));
			float excitement = .3f;
			if (ant.holdingResource) {
				excitement = 1f;
			}
			excitement *= ant.speed / antSpeed;
			DropPheromones(ant.position,excitement);
			//}

			Matrix4x4 matrix = GetRotationMatrix(ant.facingAngle);
			matrix.m03 = ant.position.x / mapSize;
			matrix.m13 = ant.position.y / mapSize;
			matrices[i / instancesPerBatch][i % instancesPerBatch] = matrix;
		}

		for (int x = 0; x < mapSize; x++) {
			for (int y = 0; y < mapSize; y++) {
				int index = PheromoneIndex(x,y);
				pheromones[index].r *= trailDecay;
			}
		}

		pheromoneTexture.SetPixels(pheromones);
		pheromoneTexture.Apply();

		for (int i=0;i<matProps.Length;i++) {
			matProps[i].SetVectorArray("_Color",antColors[i]);
		}
	}
	private void Update() {

		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			Time.timeScale = 1f;
		} else if (Input.GetKeyDown(KeyCode.Alpha2)) {
			Time.timeScale = 2f;
		} else if (Input.GetKeyDown(KeyCode.Alpha3)) {
			Time.timeScale = 3f;
		} else if (Input.GetKeyDown(KeyCode.Alpha4)) {
			Time.timeScale = 4f;
		} else if (Input.GetKeyDown(KeyCode.Alpha5)) {
			Time.timeScale = 5f;
		} else if (Input.GetKeyDown(KeyCode.Alpha6)) {
			Time.timeScale = 6f;
		} else if (Input.GetKeyDown(KeyCode.Alpha7)) {
			Time.timeScale = 7f;
		} else if (Input.GetKeyDown(KeyCode.Alpha8)) {
			Time.timeScale = 8f;
		} else if (Input.GetKeyDown(KeyCode.Alpha9)) {
			Time.timeScale = 9f;
		}

		for (int i = 0; i < matrices.Length; i++) {
			Graphics.DrawMeshInstanced(antMesh,0,antMaterial,matrices[i],matrices[i].Length,matProps[i]);
		}
		for (int i=0;i<obstacleMatrices.Length;i++) {
			Graphics.DrawMeshInstanced(obstacleMesh,0,obstacleMaterial,obstacleMatrices[i]);
		}

		Graphics.DrawMesh(colonyMesh,colonyMatrix,colonyMaterial,0);
		Graphics.DrawMesh(resourceMesh,resourceMatrix,resourceMaterial,0);
	}
}
