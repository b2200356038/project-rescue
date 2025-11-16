using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Vehicle.Wheel
{
    public class GroundDetection : MonoBehaviour
    {
        [Header("Debug")] public bool showDebug = true;

        private const int MaxHits = 32;
        private RaycastHit[] _spherecastHits;
        private HashSet<Collider> _vehicleColliders;

        private Vector3 _debugCastOrigin;
        private Vector3 _debugCastDirection;
        private float _debugCastDistance;
        private float _debugCastRadius;
        private bool _debugHadHit;
        private WheelHit _debugLastHit;


        private void Awake()
        {
            _spherecastHits = new RaycastHit[MaxHits];
            Rigidbody rb = GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                _vehicleColliders = new HashSet<Collider>(rb.GetComponentsInChildren<Collider>());
            }
        }
        

        public bool WheelCast(
            Vector3 origin,
            Vector3 direction,
            float distance,
            float wheelRadius,
            float wheelWidth,
            ref WheelHit wheelHit,
            LayerMask layerMask
        )
        {
            direction = Vector3.Normalize(direction);
            float castRadius = wheelRadius * 0.5f;

            int hitCount = UnityEngine.Physics.SphereCastNonAlloc(origin, castRadius, direction, _spherecastHits,
                distance, layerMask, QueryTriggerInteraction.Ignore);

            if (hitCount==0)
            {
                _debugHadHit = false;
                return false;
            }
            float minDistance = float.MaxValue;
            RaycastHit closestHit = default;
            bool foundValidHit = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _spherecastHits[i];
                if (_vehicleColliders!=null && _vehicleColliders.Contains(hit.collider))
                {
                    continue;
                }

                if (!IsHitInsideWheel(hit.point, origin, wheelWidth, wheelRadius))
                {
                    continue;
                }
                if (hit.distance<minDistance)
                {
                    minDistance = hit.distance;
                    closestHit = hit;
                    foundValidHit = true;
                }
            }

            if (foundValidHit)
            {
                wheelHit.point = closestHit.point;
                wheelHit.normal = closestHit.normal;
                wheelHit.collider = closestHit.collider;
                _debugHadHit = true;
                _debugLastHit = wheelHit;
                return true;
            }
            return false;
        }

        private bool IsHitInsideWheel(Vector3 hitPoint, Vector3 wheelCenter, float width, float radius)
        {
            Vector3 offset = hitPoint - wheelCenter;
            Vector3 localOffset = transform.InverseTransformVector(offset);
            float halfWidth = width * 0.5f;
            if (localOffset.x < -halfWidth || localOffset.x > halfWidth)
            {
                return false;
            }

            if (localOffset.z > radius || localOffset.z < -radius)
            {
                return false;
            }

            return true;
            
        }
        void OnDrawGizmos()
        {
            if (!showDebug || !Application.isPlaying)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_debugCastOrigin, _debugCastRadius);
            Gizmos.color = _debugHadHit ? Color.green : Color.red;
            Gizmos.DrawRay(_debugCastOrigin, _debugCastDirection * _debugCastDistance);

            if (_debugHadHit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_debugLastHit.point, 0.05f);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(_debugLastHit.point, _debugLastHit.normal * 0.3f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    _debugLastHit.point + Vector3.up * 0.1f,
                    $"Ground Hit\n{_debugLastHit.collider.name}"
                );
#endif
            }
        }
    }
    
}


[Serializable]
public struct WheelHit
{
    public Vector3 point;
    public Vector3 normal;
    public Collider collider;
}