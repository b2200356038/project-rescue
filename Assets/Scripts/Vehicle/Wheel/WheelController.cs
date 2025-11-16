using System;
using UnityEngine;

namespace Game.Vehicle.Wheel
{
    public class WheelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody targetRigidbody;
        
        [Header("Wheel Properties")]
        public float radius = 0.35f;
        public float width = 0.25f;

        [Header("Spring")]
        [SerializeField] private float springMaxLength = 0.3f;
        [SerializeField] private float springMaxForce = 8000f;
        [SerializeField] private AnimationCurve springForceCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Damper")]
        [SerializeField] private float damperBumpRate = 2000f; 
        [SerializeField] private float damperReboundRate = 2000f;
        
        [Header("Ground Detection")]
        public LayerMask groundLayer = ~0;
        private GroundDetection _groundDetection;
        private WheelHit _wheelHit;
        private bool _isGrounded;
        private bool _initialized;
        
        private float _springLength;
        private float _prevSpringLength;
        private float _springCompression;     
        private float _springCompressionVelocity;
        private float _springForce;
        private float _damperForce;
        private float _load;
        
        private float _dt;
        private float _invDt;

        public bool IsGrounded => _isGrounded;
        public Vector3 HitPoint => _wheelHit.point;
        public Vector3 HitNormal => _wheelHit.normal;
        public Collider HitCollider => _wheelHit.collider;
        
        public float SpringLength => _springLength;
        public float SpringCompression => _springCompression;
        public float SpringForce => _springForce;
        public float DamperForce => _damperForce;
        public float Load => _load;
        
        [Header("Friction Settings")]
        [SerializeField] private AnimationCurve forwardFrictionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve sideFrictionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Range(0.5f, 3f)]
        public float forwardGrip = 1.5f;
        
        [Range(0.5f, 5f)]
        public float sideGrip = 2.0f;
        
        [Range(0.1f, 2f)]
        public float forwardStiffness = 1.0f;
        
        [Range(0.1f, 5f)]
        public float sideStiffness = 1.0f;
        
        [Range(0f, 100f)]
        public float rollingResistance = 30f;
        
        [Range(0f, 1f)]
        public float frictionCircleStrength = 0.8f;

        [Header("Wheel Torque")]
        public float motorTorque = 0f;       
        public float brakeTorque = 0f;       
        public float wheelMass = 20f;

        private float _angularVelocity = 0f;
        private float _inertia;
        
        [Header("Steering")]
        public float steerAngle = 0f;
        
        private Vector3 _suspensionUp;
        private Vector3 _suspensionForward;
        private Vector3 _suspensionRight;
        
        private Vector3 _wheelUp;
        private Vector3 _wheelForward;
        private Vector3 _wheelRight;
        
        [Header("Visual")]
        [SerializeField] private Transform visualTransform;
        private float _visualRotationAngle = 0f;

        private Vector3 _frictionForce;
        private float _forwardSpeed;
        private float _sideSpeed;
        private Quaternion _steerRotation;

        private bool _lowSpeedReferenceIsSet;
        private Vector3 _lowSpeedReferencePosition;
        private bool _wakeOneFrame;

        [Header("Debug Info")]
        [SerializeField] private float debugForwardSlip;
        [SerializeField] private float debugSideSlip;
        [SerializeField] private float debugForwardForce;
        [SerializeField] private float debugSideForce;
        [SerializeField] private float debugCombinedSlip;
        [SerializeField] private bool debugAntiCreepActive;
        [SerializeField] private float debugReferenceError;
        [SerializeField] private float debugCorrectiveForce;
        
        private void Awake()
        {
            if (targetRigidbody == null)
                targetRigidbody = GetComponentInParent<Rigidbody>();
                
            if (forwardFrictionCurve == null || forwardFrictionCurve.keys.Length == 0)
            {
                forwardFrictionCurve = new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.1f, 1f),
                    new Keyframe(1f, 0.8f)
                );
            }
            
            if (sideFrictionCurve == null || sideFrictionCurve.keys.Length == 0)
            {
                sideFrictionCurve = new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.15f, 1f),
                    new Keyframe(1f, 0.9f)
                );
            }
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            _groundDetection = GetComponent<GroundDetection>();
            if (_groundDetection == null)
                _groundDetection = gameObject.AddComponent<GroundDetection>();

            _inertia = wheelMass * radius * radius;

            _initialized = true;
        }

        private void FixedUpdate()
        {
            _dt = Time.fixedDeltaTime;
            _invDt = 1f / _dt;

            if (!_initialized)
                return;

            UpdateSteeringTransform();
            UpdateGroundDetection();
            UpdateSuspension();
            ApplySuspensionForce();
            UpdateWheelSpaceVectors();
            UpdateFriction();
            ApplyFrictionForces();
        }

        private void LateUpdate()
        {
            UpdateVisual();
        }

        private void UpdateSteeringTransform()
        {
            _steerRotation = Quaternion.AngleAxis(steerAngle, transform.up);
            
            _suspensionUp = transform.up;
            _suspensionForward = _steerRotation * transform.forward;
            _suspensionRight = _steerRotation * transform.right;
        }
        
        private void UpdateSuspension()
        {
            _prevSpringLength = _springLength;

            if (_isGrounded)
            {
                Vector3 localHitPoint = Quaternion.Inverse(_steerRotation) * 
                    transform.InverseTransformPoint(_wheelHit.point);
                
                float hitAngle = Mathf.Asin(Mathf.Clamp(localHitPoint.z / radius, -1f, 1f));
                float localGroundY = localHitPoint.y + radius * Mathf.Cos(hitAngle);
                
                float targetLength = Mathf.Clamp(-localGroundY, 0f, springMaxLength);
                _springLength = targetLength;
            }
            else
            {
                float extensionSpeed = 6f * _dt;
                _springLength = Mathf.MoveTowards(_springLength, springMaxLength, extensionSpeed);
            }

            _springCompressionVelocity = (_prevSpringLength - _springLength) / _dt;
            _springCompression = springMaxLength > 0
                ? (springMaxLength - _springLength) / springMaxLength : 0f;

            if (_isGrounded)
            {
                _springForce = springMaxForce * springForceCurve.Evaluate(_springCompression);
                _damperForce = CalculateDamperForce(_springCompressionVelocity);
                _load = Mathf.Max(0f, _springForce + _damperForce);
            }
            else
            {
                _springForce = 0f;
                _damperForce = 0f;
                _load = 0f;
            }
        }

        private float CalculateDamperForce(float velocity)
        {
            return velocity > 0 
                ? damperBumpRate * velocity
                : damperReboundRate * velocity;
        }

        private void ApplySuspensionForce()
        {
            if (!_isGrounded) return;

            Vector3 suspensionForce = _wheelHit.normal * _load;
            Vector3 suspensionPosition = transform.position;
            targetRigidbody.AddForceAtPosition(suspensionForce, suspensionPosition);

            Debug.DrawRay(suspensionPosition, suspensionForce * 0.001f, Color.yellow);
        }
        
        private void UpdateGroundDetection()
        {
            Vector3 castOrigin = transform.position + _suspensionUp * (radius * 1.1f);
            Vector3 castDirection = -_suspensionUp;
            float castDistance = radius * 2.2f + springMaxLength;
            
            _isGrounded = _groundDetection.WheelCast(
                castOrigin,
                castDirection,
                castDistance,
                radius,
                width,
                ref _wheelHit,
                groundLayer
            );
        }
        
        private void UpdateWheelSpaceVectors()
        {
            _wheelUp = _suspensionUp;
            _wheelForward = _suspensionForward;
            _wheelRight = _suspensionRight;
        }

        private void UpdateFriction()
        {
            if (!_isGrounded)
            {
                _angularVelocity += (motorTorque / _inertia) * _dt;
                _angularVelocity = Mathf.Clamp(_angularVelocity, -200f, 200f);
                
                _lowSpeedReferenceIsSet = false;
                debugAntiCreepActive = false;
                
                return;
            }
            
            Vector3 contactVelocity = targetRigidbody.GetPointVelocity(_wheelHit.point);
            _forwardSpeed = Vector3.Dot(contactVelocity, _wheelForward);
            _sideSpeed = Vector3.Dot(contactVelocity, _wheelRight);
            
            float wheelSpeed = _angularVelocity * radius;
            
            float absForwardSpeed = _forwardSpeed < 0 ? -_forwardSpeed : _forwardSpeed;
            float absSideSpeed = _sideSpeed < 0 ? -_sideSpeed : _sideSpeed;
            
            float forwardSpeedClamp = 1.5f * (_dt / 0.005f);
            forwardSpeedClamp = forwardSpeedClamp < 1.5f ? 1.5f : forwardSpeedClamp > 10f ? 10f : forwardSpeedClamp;
            float clampedAbsForwardSpeed = absForwardSpeed < forwardSpeedClamp ? forwardSpeedClamp : absForwardSpeed;
            float invClampedAbsForwardSpeed = 1.0f / clampedAbsForwardSpeed;
            
            float forwardSlip = -(wheelSpeed - _forwardSpeed) * invClampedAbsForwardSpeed * forwardStiffness;
            forwardSlip = Mathf.Clamp(forwardSlip, -1f, 1f);
            
            float absForwardSlip = forwardSlip < 0 ? -forwardSlip : forwardSlip;
            float forwardFrictionCoeff = forwardFrictionCurve.Evaluate(absForwardSlip);
            float maxForwardForce = _load * forwardGrip;
            float forwardForce = -Mathf.Sign(forwardSlip) * forwardFrictionCoeff * maxForwardForce;
            
            if (absForwardSpeed > 0.1f)
            {
                forwardForce -= rollingResistance * Mathf.Sign(_forwardSpeed);
            }
            
            float sideSlip = (Mathf.Atan2(_sideSpeed, clampedAbsForwardSpeed) * Mathf.Rad2Deg) * 0.01111f * sideStiffness;
            sideSlip = Mathf.Clamp(sideSlip, -1f, 1f);
            
            float absSideSlip = sideSlip < 0 ? -sideSlip : sideSlip;
            float sideFrictionCoeff = sideFrictionCurve.Evaluate(absSideSlip);
            float maxSideForce = _load * sideGrip;
            float sideForce = -Mathf.Sign(sideSlip) * sideFrictionCoeff * maxSideForce;
            
            if (frictionCircleStrength > 0f)
            {
                sideForce *= 1f - (Mathf.Pow(Mathf.Clamp01(absForwardSlip), 3f) * frictionCircleStrength);
            }
            
            bool isLowSpeed = absForwardSpeed < 0.12f && absSideSpeed < 0.12f;
            bool isWheelStopped = Mathf.Abs(_angularVelocity) < 0.5f;
            bool hasNoInput = Mathf.Abs(motorTorque) < 1f && brakeTorque < 1f;
            
            if (_isGrounded && !_wakeOneFrame && isLowSpeed)
            {
                debugAntiCreepActive = true;
                
                float verticalOffset = _springLength + radius;
                Vector3 currentPosition = transform.position - _suspensionUp * verticalOffset;

                if (!_lowSpeedReferenceIsSet)
                {
                    _lowSpeedReferenceIsSet = true;
                    _lowSpeedReferencePosition = currentPosition;
                }
                else
                {
                    Vector3 referenceError = _lowSpeedReferencePosition - currentPosition;
                    float errorMagnitude = referenceError.magnitude;
                    
                    const float maxCorrectionDistance = 0.05f;
                    
                    if (errorMagnitude > maxCorrectionDistance)
                    {
                        _lowSpeedReferencePosition = currentPosition;
                        referenceError = Vector3.zero;
                    }
                    
                    float correctionStrength = 1f;
                    
                    if (!isWheelStopped || !hasNoInput)
                    {
                        correctionStrength = 0.2f;
                    }
                    
                    Vector3 correctiveForce = referenceError * (_invDt * _load * correctionStrength);
                    
                    float maxCorrectiveForce = _load * 0.5f;
                    float correctiveForceMag = correctiveForce.magnitude;
                    if (correctiveForceMag > maxCorrectiveForce)
                    {
                        correctiveForce = correctiveForce.normalized * maxCorrectiveForce;
                    }

                    if (isWheelStopped)
                    {
                        forwardForce += Vector3.Dot(correctiveForce, _suspensionForward);
                    }

                    sideForce += Vector3.Dot(correctiveForce, _suspensionRight);
                    
                    debugReferenceError = errorMagnitude;
                    debugCorrectiveForce = correctiveForceMag;
                }
            }
            else
            {
                _lowSpeedReferenceIsSet = false;
                debugAntiCreepActive = false;
                debugReferenceError = 0f;
                debugCorrectiveForce = 0f;
            }
            
            forwardForce = forwardForce > maxForwardForce ? maxForwardForce
                : forwardForce < -maxForwardForce ? -maxForwardForce : forwardForce;
            sideForce = sideForce > maxSideForce ? maxSideForce
                : sideForce < -maxSideForce ? -maxSideForce : sideForce;
            
            _frictionForce = _suspensionForward * forwardForce + _suspensionRight * sideForce;
            
            float frictionTorque = forwardForce * radius;
            float brakeTorqueApplied = 0f;
            
            if (brakeTorque > 0f)
            {
                float prevAngVel = _angularVelocity;
                float angVelSign = _angularVelocity >= 0.0f ? 1.0f : -1.0f;
                brakeTorqueApplied = angVelSign * brakeTorque;
                
                _angularVelocity += (motorTorque - frictionTorque - brakeTorqueApplied) * _dt / _inertia;
                
                if (_angularVelocity >= 0.0f && prevAngVel < 0.0f ||
                    _angularVelocity < 0.0f && prevAngVel >= 0.0f)
                {
                    _angularVelocity = 0.0f;
                }
            }
            else
            {
                _angularVelocity += (motorTorque - frictionTorque) * _dt / _inertia;
            }
            
            if (isLowSpeed && hasNoInput && isWheelStopped)
            {
                _angularVelocity *= 0.9f;
                
                if (Mathf.Abs(_angularVelocity) < 0.05f)
                {
                    _angularVelocity = 0f;
                }
            }
            
            _angularVelocity = Mathf.Clamp(_angularVelocity, -200f, 200f);
            
            if (_wakeOneFrame) _wakeOneFrame = false;
            
            debugForwardSlip = forwardSlip;
            debugSideSlip = sideSlip;
            debugForwardForce = forwardForce;
            debugSideForce = sideForce;
            debugCombinedSlip = Mathf.Sqrt(forwardSlip * forwardSlip + sideSlip * sideSlip);
        }

        private void ApplyFrictionForces()
        {
            if (!_isGrounded) return;
            
            targetRigidbody.AddForceAtPosition(_frictionForce, _wheelHit.point);
            
            Debug.DrawRay(_wheelHit.point, _frictionForce * 0.01f, Color.red);
            Debug.DrawRay(_wheelHit.point, _wheelForward * 0.5f, Color.blue);
            Debug.DrawRay(_wheelHit.point, _wheelRight * 0.5f, Color.green);
            
            if (debugAntiCreepActive && _lowSpeedReferenceIsSet)
            {
                Debug.DrawLine(_lowSpeedReferencePosition, 
                              _lowSpeedReferencePosition + Vector3.up * 0.2f, 
                              Color.cyan);
            }
        }

        private void UpdateVisual()
        {
            if (visualTransform == null) return;

            Vector3 wheelPosition = transform.position - _suspensionUp * _springLength;
            
            _visualRotationAngle += _angularVelocity * Mathf.Rad2Deg * Time.deltaTime;
            _visualRotationAngle = _visualRotationAngle % 360f;
            
            Quaternion steerYaw = Quaternion.AngleAxis(steerAngle, transform.up);
            Quaternion rollRotation = Quaternion.AngleAxis(_visualRotationAngle, Vector3.right);
            Quaternion finalRotation = transform.rotation * steerYaw * rollRotation;
            
            visualTransform.SetPositionAndRotation(wheelPosition, finalRotation);
        }

        public void SetSteerAngle(float angle)
        {
            steerAngle = Mathf.Clamp(angle, -45f, 45f);
        }

        public void WakeUp()
        {
            _wakeOneFrame = true;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector3 wheelPos = transform.position - _suspensionUp * _springLength;
            Gizmos.DrawWireSphere(wheelPos, radius);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _suspensionForward * 0.5f);
            
            if (_isGrounded)
            {
                Gizmos.color = Color.yellow;
                float scale = 0.001f;
                Gizmos.DrawRay(_wheelHit.point, _wheelForward * debugForwardForce * scale);
                Gizmos.DrawRay(_wheelHit.point, _wheelRight * debugSideForce * scale);
                
                if (debugAntiCreepActive && _lowSpeedReferenceIsSet)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(_lowSpeedReferencePosition, 0.05f);
                    
                    Vector3 currentPos = transform.position - _suspensionUp * (_springLength + radius);
                    Gizmos.DrawLine(_lowSpeedReferencePosition, currentPos);
                }
            }
        }
    }
}