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
            
            struct Sphere
            {
                float3 position;
                float radius;  
                RayTracingMaterial material;
            };

            StructuredBuffer<Sphere> Spheres;
            int numSpheres;

            struct Plane
            {
                float3 position;

                float3 normal;
                float3 right;
                float3 up;

                float2 halfSize;

                RayTracingMaterial material;
            };

            StructuredBuffer<Plane> Planes;
            int numPlanes;

            struct Ray
            {
                float3 origin;
                float3 direction;
            };

            struct HitInfo
            {
                bool didHit;
                float distance;
                float3 hitPoint;
                float3 normal;

                RayTracingMaterial material;
            };

            // PCG (permuted congruential generator). Thanks to:
			// www.pcg-random.org and www.shadertoy.com/view/XlGcRh
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

            HitInfo HitSphere(Sphere sphere, Ray ray)
            {
                HitInfo hitInfo = (HitInfo)0;
                float3 offsetRayOrigin = ray.origin - sphere.position;

                float a = dot(ray.direction, ray.direction);
                float b = 2.0 * dot(offsetRayOrigin, ray.direction);
                float c = dot(offsetRayOrigin, offsetRayOrigin) - sphere.radius * sphere.radius;

                float discriminant = b * b - 4.0 * a * c;

                if(discriminant >= 0)
                {
                    float dst = (-b - sqrt(discriminant)) / (2.0 * a);

                    if(dst >= 0)
                    {
                        hitInfo.didHit = true;
                        hitInfo.distance = dst;
                        hitInfo.hitPoint = ray.origin + ray.direction * dst;
                        hitInfo.normal = normalize(hitInfo.hitPoint - sphere.position);
                    }
                }

                return hitInfo;
            }

            HitInfo HitPlane(Plane plane, Ray ray)
            {
                HitInfo hitInfo = (HitInfo)0;

                float dotProduct = dot(ray.direction, plane.normal);

                if(dotProduct > 0) return hitInfo;

                float t = dot(plane.position - ray.origin, plane.normal) / dotProduct;
                float3 hitPoint = ray.origin + t * ray.direction;
                float3 toHit = hitPoint - plane.position;

                float uDist = dot(toHit, plane.right);
                float vDist = dot(toHit, plane.up);

                hitInfo.didHit = t >= 0 && abs(uDist) <= plane.halfSize.x && abs(vDist) <= plane.halfSize.y;
                hitInfo.distance = t;
                hitInfo.hitPoint = hitPoint;
                hitInfo.normal = plane.normal;

                return hitInfo;
            }


            HitInfo CalculateRayCollision(Ray ray)
            {
                HitInfo closestHit = (HitInfo)0;

                closestHit.distance = 1.#INF;

                for(int i = 0; i < numSpheres; i++)
                {
                    Sphere sphere = Spheres[i];
                    HitInfo hitInfo = HitSphere(sphere, ray);

                    if(hitInfo.didHit && hitInfo.distance < closestHit.distance)
                    {
                        closestHit = hitInfo;
                        closestHit.material = sphere.material;
                    }
                }

                for(int j = 0; j < numPlanes; j++)
                {
                    Plane plane = Planes[j];
                    HitInfo hitInfo = HitPlane(plane, ray);

                    if(hitInfo.didHit && hitInfo.distance < closestHit.distance)
                    {
                        closestHit = hitInfo;
                        closestHit.material = plane.material;
                    }
                }

                return closestHit;
            }

            int MaxBounceCount;
            int NumRaysPerPixel;

            float3 skyColor;

            float3 Trace(Ray ray, inout int rngState)
            {
                float3 incomingLight = 0;
                float3 rayColor = 1;


                for(int i = 0; i <= MaxBounceCount; i++)
                {
                    HitInfo hitInfo = CalculateRayCollision(ray);

                    if(hitInfo.didHit)
                    {
                        RayTracingMaterial material = hitInfo.material;
                        ray.origin = hitInfo.hitPoint;

                        float3 diffuseDirection = normalize(hitInfo.normal + RandomDirection(rngState));
                        float3 specularDirection = reflect(ray.direction, hitInfo.normal);

                        bool isSpecularBounce = material.specularProbability >= RandomValue(rngState);
                        ray.direction = lerp(diffuseDirection, specularDirection, material.smoothness * isSpecularBounce);

                        float3 emittedLight = material.emissionColor * material.emissionStrength;
                        incomingLight += emittedLight * rayColor;
                        rayColor *= material.color;
                    }
                    else
                    {
                        incomingLight += skyColor * rayColor;
                        break;
                    }
                }

                return incomingLight;
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

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.direction = normalize(viewPoint - ray.origin);

                float3 totalIncomingLight = 0;

                for(int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
                {
                    totalIncomingLight += Trace(ray, rngState);
                }

                float3 pixelColor = totalIncomingLight / NumRaysPerPixel;
                return float4(pixelColor, 1);
            }

            ENDCG
        }
    }
}
