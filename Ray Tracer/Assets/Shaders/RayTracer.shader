Shader "Custom/RayTracer"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //--------------------------------------------------------------------

            struct RayTracingMaterial
            {
                float4 color;
                float4 emissionColor;
                float emissionStrength;
                float smoothness;
                float specularProbability;
            };
           

            struct Triangle
            {
                float3 posA, posB, posC;
                float3 normalA, normalB, normalC;
            };

            struct Node
            {
                float3 boundsMin;
                float3 boundsMax;
                int firstTriangleIndex;
                int triangleCount;
                int childIndex;
            };

            struct Model
            {
                int nodeOffset;
                int triangleOffset;
                float4x4 worldToLocalMatrix;
                float4x4 localToWorldMatrix;
                RayTracingMaterial material;    
            };

            StructuredBuffer<Triangle> Triangles;
            StructuredBuffer<Node> Nodes;
            StructuredBuffer<Model> Models;
            int numModels;

            struct Ray
            {
                float3 origin;
                float3 direction;
                float3 inverseDirection;
            };

            struct HitInfo
            {
                bool didHit;
                float distance;
                float3 hitPoint;
                float3 normal;

                RayTracingMaterial material;
            };

			uint NextRandom(inout uint state)
			{
				state = state * 747796405 + 2891336453;
				uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
				result = (result >> 22) ^ result;
				return result;
			}

			float RandomValue(inout uint state)
			{
				return NextRandom(state) / 4294967295.0; // 2^32 - 1
			}

            // Random value in normal distribution 
			float RandomValueNormalDistribution(inout uint state)
			{
				float theta = 2 * 3.1415926 * RandomValue(state);
				float rho = sqrt(-2 * log(RandomValue(state)));
				return rho * cos(theta);
			}

            float3 RandomDirection(inout uint state)
            {
                float x = RandomValueNormalDistribution(state);
                float y = RandomValueNormalDistribution(state);
                float z = RandomValueNormalDistribution(state);
                return normalize(float3(x,y,z));
            }


            float3 RandomHemisphereDirection(float3 normal, inout uint state)
            {
                float3 dir = RandomDirection(state);
                return dir * sign(dot(normal, dir));
            }

            static const float PI = 3.141526;
            
            float2 RandomPointInCircle(inout uint rngState)
            {
                float angle = RandomValue(rngState) * 2 * PI;
                float2 pointOnCircle = float2(cos(angle), sin(angle));
                return pointOnCircle * sqrt(RandomValue(rngState));
            }

            //Uses following algorithm so check hit
            //https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm

            HitInfo HitTriangle(Triangle tri, Ray ray)
            {
                float3 edgeAB = tri.posB - tri.posA;
                float3 edgeAC = tri.posC - tri.posA;
                float3 normalVector = cross(edgeAB, edgeAC);
                float3 ao = ray.origin - tri.posA;
                float3 dao = cross(ao, ray.direction);

                float determinant = -dot(ray.direction, normalVector);
                float invDet = 1.0/determinant;

                float dst = dot(ao, normalVector) * invDet;
                float u = dot(edgeAC, dao) * invDet;
                float v = -dot(edgeAB, dao) * invDet;
                float w = 1 - u - v;

                HitInfo hitInfo;
                hitInfo.didHit = determinant >= 1e-6 && dst >= 0 && u >= 0 && v >= 0 && w >= 0;
                hitInfo.hitPoint = ray.origin + ray.direction * dst;
                hitInfo.normal = normalize(tri.normalA * w + tri.normalB * u + tri.normalC * v);
                hitInfo.distance = dst;
                return hitInfo;
            }

            float RayBoundingBox(Ray ray, float3 boxMin, float3 boxMax)
            {
                float3 tMin = (boxMin - ray.origin) * ray.inverseDirection;
                float3 tMax = (boxMax - ray.origin) * ray.inverseDirection;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);
                bool didHit = tFar >= tNear && tFar > 0;
                return didHit ? tNear : 1.#INF;
            }

            HitInfo RayTriangleBVH(Ray ray, int nodeOffset, int triangleOffset, inout int2 stats)
            {
                int stack[32];
                int stackIndex = 0;
                stack[stackIndex++] = nodeOffset;

                HitInfo result;
                result.distance = 1.#INF;

                while(stackIndex > 0)
                {
                    Node node = Nodes[stack[--stackIndex]];

                    if(node.childIndex == 0)
                    {
                        for(int i = 0; i < node.triangleCount; i++)
                        {
                            Triangle tri = Triangles[triangleOffset + node.firstTriangleIndex + i];
                            HitInfo hitInfo = HitTriangle(tri, ray);
                            stats[1]++;

                            if(hitInfo.didHit && hitInfo.distance < result.distance)
                            {
                                result = hitInfo;
                            }
                        }
                    }
                    else
                    {

                        int childIndex = nodeOffset + node.childIndex;
                        Node childA = Nodes[childIndex + 0];
                        Node childB = Nodes[childIndex + 1];

                        float dstA = RayBoundingBox(ray, childA.boundsMin, childA.boundsMax);
                        float dstB = RayBoundingBox(ray, childB.boundsMin, childB.boundsMax);
                        stats[0] += 2;

                        //The closest distance should be pushed last, so it can be processed first
                        if(dstA < dstB)
                        {
                            if(dstB < result.distance) stack[stackIndex++] = childIndex + 1; //Far
                            if(dstA < result.distance) stack[stackIndex++] = childIndex + 0; //Near
                        }
                        else
                        {
                            if(dstA < result.distance) stack[stackIndex++] = childIndex + 0; //Far
                            if(dstB < result.distance) stack[stackIndex++] = childIndex + 1; //Near
                        }

                    }
                    
                }

                return result;
            }

            HitInfo CalculateRayCollision(Ray worldRay, inout uint2 stats)
            {
                HitInfo result;
                result.distance = 1.#INF;

                for(int i = 0; i < numModels; i++)
                {
                    Model model = Models[i];

                    Ray ray;
                    ray.origin = mul(model.worldToLocalMatrix, float4(worldRay.origin, 1));
                    ray.direction = mul(model.worldToLocalMatrix, float4(worldRay.direction, 0));
                    ray.inverseDirection = 1 / ray.direction;

                    HitInfo hit = RayTriangleBVH(ray, model.nodeOffset, model.triangleOffset, stats);

                    if(hit.didHit && hit.distance < result.distance)
                    {
                        result = hit;
                        result.material = model.material;
                        result.normal = normalize(mul(model.localToWorldMatrix, float4(hit.normal, 0)));
                        result.hitPoint = worldRay.origin + worldRay.direction * hit.distance;
                    }
                }

                return result;
            }

            float3 skyHorizonColor;
            float3 skyColor;
            float3 groundColor;
            float3 sunLightDirection;
            float sunFocus;
            float sunIntensity;

            float3 EnvironmentLight(Ray ray)
            {
                float skyGradientT = pow(smoothstep(0, 0.4, ray.direction.y), 0.35);
                float3 skyGradient = lerp(skyHorizonColor, skyColor, skyGradientT);
                float sun = pow(max(0, dot(ray.direction, -sunLightDirection)), sunFocus) * sunIntensity;

                float groundToSkyT = smoothstep(-0.01, 0, ray.direction.y);
                float sunMask = groundToSkyT >= 1;
                return lerp(groundColor, skyGradient, groundToSkyT) + sun * sunMask;
            }

            int MaxBounceCount;
            int NumRaysPerPixel;
            float divergeStrength;
            float defocusStrength;

            int debugMode;
            float triangleDebugScale;
            float boxDebugScale;

            float3 Trace(Ray ray, inout int rngState)
            {
                float3 incomingLight = 0;
                float3 rayColor = 1;
                uint2 stats = uint2(0, 0);


                for(int i = 0; i <= MaxBounceCount; i++)
                {
                    HitInfo hitInfo = CalculateRayCollision(ray, stats);

                    if(hitInfo.didHit)
                    {
                        RayTracingMaterial material = hitInfo.material;
                        ray.origin = hitInfo.hitPoint;

                        float3 diffuseDirection = normalize(hitInfo.normal + RandomDirection(rngState));
                        float3 specularDirection = reflect(ray.direction, hitInfo.normal);

                        bool isSpecularBounce = material.specularProbability >= RandomValue(rngState);
                        ray.direction = lerp(diffuseDirection, specularDirection, material.smoothness * isSpecularBounce);
                        ray.inverseDirection = 1 / ray.direction;

                        float3 emittedLight = material.emissionColor * material.emissionStrength;
                        incomingLight += emittedLight * rayColor;
                        rayColor *= material.color;
                        
                        float p = max(rayColor.r, max(rayColor.g, rayColor.b));
                        if(RandomValue(rngState) >= p)
                        {
                            break;
                        }

                        rayColor *= 1.0/p;
                    }
                    else
                    {
                        incomingLight += EnvironmentLight(ray) * rayColor;
                        break;
                    }
                }

                //Box debug mode
                if(debugMode == 1)
                {
                    float debugVis = stats[0] / boxDebugScale;
                    return debugVis < 1 ? debugVis: float3(1,0,0);
                }
                //Triangle debug mode
                else if(debugMode == 2)
                {
                    float debugVis = stats[1] / triangleDebugScale;
                    return debugVis < 1 ? debugVis : float3(1, 0, 0);
                }
                else
                {
                    return incomingLight;
                }
            }

            float3 ViewParams;
            float4x4 CamLocalToWorldMatrix;

            int Frame;

            float4 frag (v2f i) : SV_Target
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;    
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x;
				uint rngState = pixelIndex + Frame * 719393;

                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                float3 camUp = CamLocalToWorldMatrix._m01_m11_m21;
                float3 camRight = CamLocalToWorldMatrix._m00_m10_m20;

                float3 totalIncomingLight = 0;

                for(int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
                {
                    Ray ray;

                    float2 defocusJitter = RandomPointInCircle(rngState) * defocusStrength / numPixels.x;
                    ray.origin = _WorldSpaceCameraPos + camRight * defocusJitter.x + camUp * defocusJitter.y;

                    float2 jitter = RandomPointInCircle(rngState) * divergeStrength / numPixels.x;
                    float3 jitteredViewPoint = viewPoint + camRight * jitter.x + camUp * jitter.y;
                    ray.direction = normalize(jitteredViewPoint - ray.origin);

                    totalIncomingLight += Trace(ray, rngState);
                }

                float3 pixelColor = totalIncomingLight / NumRaysPerPixel;
                return float4(pixelColor, 1);
            }

            ENDCG
        }
    }
}
