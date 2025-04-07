Shader "Custom/TestShader"
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
            };
            
            struct Sphere
            {
                float3 position;
                float radius;  
                RayTracingMaterial material;
            };

            StructuredBuffer<Sphere> Spheres;
            int numSpheres;

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

            //Generate a random value between 0 and 1
            float RandomValue(inout uint state)
            {
                state = state * 747796405 + 2891336453;
                uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
                result = (result >> 22) ^ result;
                return result / 4294967295.0;
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

                return closestHit;
            }

            int MaxBounceCount;

            float3 Trace(Ray ray, inout int rngState)
            {
                float3 incomingLight = 0;
                float3 rayColor = 1;


                for(int i = 0; i <= MaxBounceCount; i++)
                {
                    HitInfo hitInfo = CalculateRayCollision(ray);
                    if(hitInfo.didHit)
                    {
                        ray.origin = hitInfo.hitPoint;
                        ray.direction = RandomHemisphereDirection(hitInfo.normal, rngState);    

                        RayTracingMaterial material = hitInfo.material;
                        float3 emittedLight = material.emissionColor * material.emissionStrength;
                        incomingLight += emittedLight * rayColor;
                        rayColor *= material.color;
                    }
                    else
                    {
                        break;
                    }
                }

                return incomingLight;
            }

            float3 ViewParams;
            float4x4 CamLocalToWorldMatrix;

            float4 frag (v2f i) : SV_Target
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;    
                uint pixelIndex = pixelCoord.y * numPixels.x * pixelCoord.x;
                uint rngState = pixelIndex;

                float3 viewPointLocal = float3(i.uv - 0.5, 1) * ViewParams;
                float3 viewPoint = mul(CamLocalToWorldMatrix, float4(viewPointLocal, 1));

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.direction = normalize(viewPoint - ray.origin);

                float3 pixelColor = Trace(ray, rngState);
                return float4(pixelColor, 1);
            }

            ENDCG
        }
    }
}
