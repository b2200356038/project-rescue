using UnityEngine;

namespace Game.Vehicle.Wheel
{
    public class WheelController : MonoBehaviour
    {
        private const float LOW_SPEED_THRESHOLD = 0.12f;
        private const float MIN_TORQUE_THRESHOLD = 0.1f;
        private const float ANGULAR_VELOCITY_LOCK_THRESHOLD = 0.5f;
        private const float SPRING_EXTENSION_SPEED = 6f;
        private const float FORWARD_SPEED_BASE_CLAMP = 1.5f;
        private const float FORWARD_SPEED_MIN_CLAMP = 1.5f;
        private const float FORWARD_SPEED_MAX_CLAMP = 10f;
        private const float SLIP_ANGLE_CONVERSION = 0.01111f; 
        private const float FORCE_POSITION_OFFSET_FACTOR = 0.8f;

        [Header("References")] 
        [SerializeField] private Rigidbody targetRigidbody;
        [SerializeField] private Transform visualTransform;

        [Header("Wheel Properties")] 
        [SerializeField] public float radius = 0.35f;
        [SerializeField] public float width = 0.25f;
        [SerializeField] public float wheelMass = 20f;

        [Header("Spring")] 
        [SerializeField] private float springMaxLength = 0.3f;
        [SerializeField] private float springMaxForce = 8000f;
        [SerializeField] private AnimationCurve springForceCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Damper")] 
        [SerializeField] private float damperBumpRate = 2000f;
        [SerializeField] private float damperReboundRate = 2000f;

        [Header("Torque")] 
        [SerializeField] public float motorTorque = 0f;
        [SerializeField] public float brakeTorque;
        [SerializeField] private float rollingResistanceTorque = 30f;

        [Header("Steering")] 
        [SerializeField] private float steerAngle;

        [Header("Friction Settings")] 
        [SerializeField] private FrictionPreset activeFrictionPreset;
        [SerializeField] private Friction forwardFriction;
        [SerializeField] private Friction sideFriction;
        [SerializeField] private float loadRating = 5400f;
        [Range(0, 1)] [SerializeField] private float frictionCircleStrength = 1f;
        [Range(1f, 5f)] [SerializeField] private float frictionCirclePower = 3f;
        [SerializeField] private int frictionSubsteps = 3;
        [SerializeField] private bool useLookupTable = true;
        [SerializeField] private int frictionLookupResolution = 100;
        [SerializeField] private bool useAdaptiveSubsteps = false;
        [SerializeField] private int minSubsteps = 1;
        [SerializeField] private int maxSubsteps = 10;

        [Header("Ground Detection")] 
        [SerializeField] public LayerMask groundLayer = ~0;
        
        private GroundDetection _groundDetection;
        private WheelHit _wheelHit;
        private bool _isGrounded;
        private float _springLength;
        private float _prevSpringLength;
        private float _springCompression;
        private float _springCompressionVelocity;
        private float _springForce;
        private float _damperForce;
        private float _load;
        private float _angularVelocity;
        private float _inertia;
        private Quaternion _steerRotation;
        private Vector3 _suspensionUp;
        private Vector3 _suspensionForward;
        private Vector3 _suspensionRight;
        private Vector3 _wheelUp;
        private Vector3 _wheelForward;
        private Vector3 _wheelRight;
        private Vector3 _frictionForce;
        private float[] _frictionLookupTable;
        private bool _lowSpeedReferenceIsSet;
        private Vector3 _lowSpeedReferencePosition;
        private bool _wakeOneFrame;
        
        private float _dt;
        private bool _initialized;

        private bool _isRemoteMode = false;
        private float _visualRotationAngle;

        private float _targetAngularVelocity;
        private float _targetSpringLength;
        private float _targetSteerAngle;
        
        private float _displayAngularVelocity;
        private float _displaySpringLength;
        private float _displaySteerAngle;

        private void Awake()
        {
            if (targetRigidbody == null)
                targetRigidbody = GetComponentInParent<Rigidbody>();
        }

        private void Start()
        {
            Initialize();
        }

        internal void DoPhysicsStep()
        {
            if (!_initialized || _isRemoteMode) return;

            _dt = Time.fixedDeltaTime;
            
            UpdateSteeringTransform();
            UpdateGroundDetection();
            UpdateSuspension();
            ApplySuspensionForce();
            UpdateWheelSpaceVectors();
            UpdateFriction();
            ApplyFrictionForces();
            
            _displayAngularVelocity = _angularVelocity;
            _displaySpringLength = _springLength;
            _displaySteerAngle = steerAngle;
        }

        private void LateUpdate()
        {
            if (_isRemoteMode)
            {
                float t = Time.deltaTime * 15f; 
                
                _displayAngularVelocity = Mathf.Lerp(_displayAngularVelocity, _targetAngularVelocity, t);
                _displaySpringLength = Mathf.Lerp(_displaySpringLength, _targetSpringLength, t);
                _displaySteerAngle = Mathf.Lerp(_displaySteerAngle, _targetSteerAngle, t);
            }
            
            ApplyVisualTransform(_displayAngularVelocity, _displaySpringLength, _displaySteerAngle);
        }

        private void ApplyVisualTransform(float angularVel, float springLen, float steerAng)
        {
            if (visualTransform == null) return;
            
            Vector3 wheelPosition = transform.position - transform.up * springLen;

            _visualRotationAngle += angularVel * Mathf.Rad2Deg * Time.deltaTime;
            _visualRotationAngle %= 360f;

            Quaternion steerYaw = Quaternion.AngleAxis(steerAng, transform.up);
            Quaternion rollRotation = Quaternion.AngleAxis(_visualRotationAngle, Vector3.right);
            Quaternion finalRotation = transform.rotation * steerYaw * rollRotation;

            visualTransform.SetPositionAndRotation(wheelPosition, finalRotation);
        }

        public void SetRemoteMode(bool isRemote)
        {
            _isRemoteMode = isRemote;
            
            if (isRemote)
            {
                _targetAngularVelocity = _displayAngularVelocity;
                _targetSpringLength = _displaySpringLength;
                _targetSteerAngle = _displaySteerAngle;
            }
        }

        public void SetNetworkTarget(float angVel, float springLen, float steer)
        {
            _targetAngularVelocity = angVel;
            _targetSpringLength = springLen;
            _targetSteerAngle = steer;
        }

        public float GetAngularVelocity() => _angularVelocity;
        public float GetSpringLength() => _springLength;
        public float GetSteerAngle() => steerAngle;

        public void SetSteerAngle(float angle)
        {
            steerAngle = Mathf.Clamp(angle, -45f, 45f);
        }

        public void WakeUp()
        {
            _wakeOneFrame = true;
            _lowSpeedReferenceIsSet = false;
        }

        public void Initialize()
        {
            InitializeGroundDetection();
            InitializeInertia();
            InitializeFrictionLookupTable();
            _initialized = true;
        }

        private void InitializeGroundDetection()
        {
            _groundDetection = GetComponent<GroundDetection>();
            if (_groundDetection == null)
                _groundDetection = gameObject.AddComponent<GroundDetection>();
        }

        private void InitializeInertia()
        {
            _inertia = wheelMass * radius * radius;
        }

        private void InitializeFrictionLookupTable()
        {
            if (useLookupTable && activeFrictionPreset != null)
                BuildFrictionLookupTable();
        }

        private void BuildFrictionLookupTable()
        {
            _frictionLookupTable = new float[frictionLookupResolution + 1];
            for (int i = 0; i <= frictionLookupResolution; i++)
            {
                float t = (float)i / frictionLookupResolution;
                _frictionLookupTable[i] = activeFrictionPreset.Curve.Evaluate(t);
            }
        }

        private void UpdateSteeringTransform()
        {
            _steerRotation = Quaternion.AngleAxis(steerAngle, transform.up);
            _suspensionUp = transform.up;
            _suspensionForward = _steerRotation * transform.forward;
            _suspensionRight = _steerRotation * transform.right;
        }

        private void UpdateWheelSpaceVectors()
        {
            _wheelUp = _suspensionUp;
            _wheelForward = _suspensionForward;
            _wheelRight = _suspensionRight;
        }

        private void UpdateGroundDetection()
        {
            Vector3 castOrigin = transform.position + _suspensionUp * (radius * 1.1f);
            Vector3 castDirection = -_suspensionUp;
            float castDistance = radius * 2.2f + springMaxLength;

            _isGrounded = _groundDetection.WheelCast(
                castOrigin, castDirection, castDistance, radius, width, ref _wheelHit, groundLayer
            );
        }

        private void UpdateSuspension()
        {
            _prevSpringLength = _springLength;

            if (_isGrounded)
                UpdateGroundedSpringLength();
            else
                UpdateAirborneSpringLength();

            CalculateSuspensionForces();
        }

        private void UpdateGroundedSpringLength()
        {
            Vector3 localHitPoint = Quaternion.Inverse(_steerRotation) *
                                    transform.InverseTransformPoint(_wheelHit.point);

            float hitAngle = Mathf.Asin(Mathf.Clamp(localHitPoint.z / radius, -1f, 1f));
            float localGroundY = localHitPoint.y + radius * Mathf.Cos(hitAngle);
            _springLength = Mathf.Clamp(-localGroundY, 0f, springMaxLength);
        }

        private void UpdateAirborneSpringLength()
        {
            float extensionSpeed = SPRING_EXTENSION_SPEED * _dt;
            _springLength = Mathf.MoveTowards(_springLength, springMaxLength, extensionSpeed);
        }

        private void CalculateSuspensionForces()
        {
            _springCompressionVelocity = (_prevSpringLength - _springLength) / _dt;
            _springCompression = springMaxLength > 0
                ? (springMaxLength - _springLength) / springMaxLength
                : 0f;

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
            return velocity > 0 ? damperBumpRate * velocity : damperReboundRate * velocity;
        }

        private void ApplySuspensionForce()
        {
            if (!_isGrounded) return;
            Vector3 suspensionForce = _wheelHit.normal * _load;
            targetRigidbody.AddForceAtPosition(suspensionForce, transform.position);
        }

        private void UpdateFriction()
        {
            if (!_isGrounded)
            {
                forwardFriction.force = 0;
                forwardFriction.slip = 0;
                sideFriction.force = 0f;
                sideFriction.slip = 0f;
                _frictionForce = Vector3.zero;
                _angularVelocity = 0f;
                return;
            }

            CalculateLoadDistribution(out float lngLoadClamped, out float latLoadClamped);
            Vector3 contactVelocity = CalculateContactVelocity();
            CalculateFrictionSpeeds(contactVelocity);

            float clampedAbsForwardSpeed = GetClampedForwardSpeed();
            float peakForwardFrictionForce = activeFrictionPreset.BCDE.z * lngLoadClamped;
            float peakSideFrictionForce = activeFrictionPreset.BCDE.z * latLoadClamped;

            UpdateLongitudinalFriction(clampedAbsForwardSpeed, lngLoadClamped, peakForwardFrictionForce);
            UpdateLateralFriction(clampedAbsForwardSpeed, latLoadClamped, peakSideFrictionForce);
            ApplyLowSpeedStabilization(clampedAbsForwardSpeed, peakForwardFrictionForce, peakSideFrictionForce);
            ApplyFrictionCircle();
            
            _frictionForce = _wheelRight * sideFriction.force + _wheelForward * forwardFriction.force;

            if (_wakeOneFrame) _wakeOneFrame = false;
        }

        private void CalculateLoadDistribution(out float lngLoadClamped, out float latLoadClamped)
        {
            float lngLoad = _load * forwardFriction.loadFactor;
            float latLoad = _load * sideFriction.loadFactor;

            lngLoadClamped = Mathf.Pow(Mathf.Clamp01(lngLoad / loadRating), 0.5f) * loadRating * forwardFriction.loadFactor;
            latLoadClamped = Mathf.Pow(Mathf.Clamp01(latLoad / loadRating), 0.5f) * loadRating * sideFriction.loadFactor;
        }

        private Vector3 CalculateContactVelocity()
        {
            Vector3 contactVelocity = targetRigidbody.GetPointVelocity(_wheelHit.point);
            if (_wheelHit.collider.attachedRigidbody != null)
                contactVelocity -= _wheelHit.collider.attachedRigidbody.GetPointVelocity(_wheelHit.point);
            return contactVelocity;
        }

        private void CalculateFrictionSpeeds(Vector3 contactVelocity)
        {
            forwardFriction.speed = Vector3.Dot(contactVelocity, _wheelForward);
            sideFriction.speed = Vector3.Dot(contactVelocity, _wheelRight);
        }

        private float GetClampedForwardSpeed()
        {
            float absForwardSpeed = Mathf.Abs(forwardFriction.speed);
            float forwardSpeedClamp = FORWARD_SPEED_BASE_CLAMP * (_dt / 0.005f);
            forwardSpeedClamp = Mathf.Clamp(forwardSpeedClamp, FORWARD_SPEED_MIN_CLAMP, FORWARD_SPEED_MAX_CLAMP);
            return Mathf.Max(absForwardSpeed, forwardSpeedClamp);
        }

        private void UpdateLongitudinalFriction(float clampedAbsForwardSpeed, float lngLoadClamped, float peakForce)
        {
            int dynamicSubsteps = useAdaptiveSubsteps ? GetAdaptiveSubsteps(clampedAbsForwardSpeed) : frictionSubsteps;

            float invSubsteps = 1.0f / dynamicSubsteps;
            float sdt = _dt * invSubsteps;
            float invInertia = 1.0f / _inertia;
            float invClampedAbsForwardSpeed = 1.0f / clampedAbsForwardSpeed;
            float lngFrictionForceSum = 0.0f;
            float combinedBrakeTorque = brakeTorque + rollingResistanceTorque;

            for (int substep = 0; substep < dynamicSubsteps; substep++)
            {
                float stepMotorTorque = motorTorque * sdt;
                _angularVelocity += stepMotorTorque * invInertia;

                float slipValue = -(_angularVelocity * radius - forwardFriction.speed) * invClampedAbsForwardSpeed * forwardFriction.stiffness;
                float absSlip = Mathf.Abs(slipValue);
                float slipSign = slipValue >= 0.0f ? 1.0f : -1.0f;

                float frictionCoeff = EvaluateFrictionCoefficient(absSlip);
                float frictionForce = Mathf.Clamp(-slipSign * frictionCoeff * lngLoadClamped, -peakForce, peakForce);

                float stepFrictionTorque = frictionForce * radius * sdt;
                _angularVelocity -= stepFrictionTorque * invInertia;
                lngFrictionForceSum += frictionForce * invSubsteps;

                if (combinedBrakeTorque > 0.0f) ApplyBrakeTorque(combinedBrakeTorque, sdt, invInertia);
            }

            forwardFriction.force = lngFrictionForceSum;
            forwardFriction.slip = Mathf.Clamp(-(_angularVelocity * radius - forwardFriction.speed) * invClampedAbsForwardSpeed * forwardFriction.stiffness, -1.0f, 1.0f);
        }

        private float EvaluateFrictionCoefficient(float absSlip)
        {
            if (useLookupTable && _frictionLookupTable != null)
                return EvaluateFrictionFast(absSlip);
            return activeFrictionPreset.Curve.Evaluate(absSlip);
        }

        private float EvaluateFrictionFast(float slip)
        {
            float t = Mathf.Clamp01(slip) * frictionLookupResolution;
            int index = (int)t;
            float fraction = t - index;

            if (index >= frictionLookupResolution)
                return _frictionLookupTable[frictionLookupResolution];

            return Mathf.Lerp(_frictionLookupTable[index], _frictionLookupTable[index + 1], fraction);
        }

        private void ApplyBrakeTorque(float combinedBrakeTorque, float sdt, float invInertia)
        {
            float angVelSign = _angularVelocity >= 0.0f ? 1.0f : -1.0f;
            float stepBrakeTorque = angVelSign * combinedBrakeTorque * sdt;
            float newAngVel = _angularVelocity - stepBrakeTorque * invInertia;
            _angularVelocity = ((newAngVel >= 0.0f) != (angVelSign >= 0.0f)) ? 0.0f : newAngVel;
        }

        private void UpdateLateralFriction(float clampedAbsForwardSpeed, float latLoadClamped, float peakForce)
        {
            sideFriction.slip = (Mathf.Atan2(sideFriction.speed, clampedAbsForwardSpeed) * Mathf.Rad2Deg) * SLIP_ANGLE_CONVERSION * sideFriction.stiffness;
            float sideSlipSign = sideFriction.slip < 0f ? -1f : 1f;
            float absSideSlip = Mathf.Abs(sideFriction.slip);
            float sideMu = EvaluateFrictionCoefficient(absSideSlip);
            sideFriction.force = Mathf.Clamp(-sideSlipSign * sideMu * latLoadClamped, -peakForce, peakForce);
        }

        private void ApplyLowSpeedStabilization(float clampedAbsForwardSpeed, float peakForwardForce, float peakSideForce)
        {
            float absForwardSpeed = Mathf.Abs(forwardFriction.speed);
            float absSideSpeed = Mathf.Abs(sideFriction.speed);
            bool isLowSpeed = absForwardSpeed < LOW_SPEED_THRESHOLD && absSideSpeed < LOW_SPEED_THRESHOLD;
            bool noInputTorque = Mathf.Abs(motorTorque) < MIN_TORQUE_THRESHOLD && Mathf.Abs(brakeTorque) < MIN_TORQUE_THRESHOLD;

            if (_isGrounded && !_wakeOneFrame && isLowSpeed && noInputTorque)
            {
                Vector3 currentPosition = transform.position - _suspensionUp * (_springLength + radius);

                if (!_lowSpeedReferenceIsSet)
                {
                    _lowSpeedReferenceIsSet = true;
                    _lowSpeedReferencePosition = currentPosition;
                }
                else
                {
                    Vector3 referenceError = _lowSpeedReferencePosition - currentPosition;
                    Vector3 correctiveForce = (1f / _dt) * _load * referenceError;

                    if (Mathf.Abs(_angularVelocity) < ANGULAR_VELOCITY_LOCK_THRESHOLD)
                    {
                        float forwardCorrection = Vector3.Dot(correctiveForce, _wheelForward);
                        forwardFriction.force += forwardCorrection;
                    }
                    float sideCorrection = Vector3.Dot(correctiveForce, _wheelRight);
                    sideFriction.force += sideCorrection;
                }
            }
            else
            {
                _lowSpeedReferenceIsSet = false;
            }

            forwardFriction.force = Mathf.Clamp(forwardFriction.force, -peakForwardForce, peakForwardForce);
            sideFriction.force = Mathf.Clamp(sideFriction.force, -peakSideForce, peakSideForce);
        }

        private void ApplyFrictionCircle()
        {
            forwardFriction.force *= forwardFriction.grip;
            sideFriction.force *= sideFriction.grip;

            if (frictionCircleStrength > 0)
            {
                float frictionCircleEffect = 1f - (Mathf.Pow(Mathf.Clamp01(Mathf.Abs(forwardFriction.slip)), frictionCirclePower) * frictionCircleStrength);
                sideFriction.force *= frictionCircleEffect;
            }
        }

        private void ApplyFrictionForces()
        {
            if (!_isGrounded) return;
            Vector3 forcePosition = _wheelHit.point + _suspensionUp * (FORCE_POSITION_OFFSET_FACTOR * springMaxLength);
            targetRigidbody.AddForceAtPosition(_frictionForce, forcePosition);
        }

        private int GetAdaptiveSubsteps(float speed)
        {
            return speed > 6f ? minSubsteps : maxSubsteps;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector3 wheelPos = transform.position - _suspensionUp * _springLength;
            Gizmos.DrawWireSphere(wheelPos, radius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _suspensionForward * 0.5f);
        }
    }
}