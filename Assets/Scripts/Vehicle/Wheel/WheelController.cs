using UnityEngine;

namespace Game.Vehicle.Wheel
{
    public class WheelController : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Rigidbody targetRigidbody;

        [Header("Wheel Properties")] [SerializeField]
        public float radius = 0.35f;

        [SerializeField] public float width = 0.25f;

        [Header("Spring")] [SerializeField] private float springMaxLength = 0.3f;
        [SerializeField] private float springMaxForce = 8000f;
        [SerializeField] private AnimationCurve springForceCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Damper")] [SerializeField] private float damperBumpRate = 2000f;
        [SerializeField] private float damperReboundRate = 2000f;

        [Header("Ground Detection")] [SerializeField]
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
        private Vector3 _frictionForce;

        
        [Header("Torque")] [SerializeField] public float motorTorque = 0f;
        [SerializeField] public float brakeTorque;
        [SerializeField] public float wheelMass = 20f;
        [SerializeField] private float rollingResistanceTorque = 30f;

        private float _angularVelocity;
        private float _inertia;

        [Header("Steering")] [SerializeField] private float steerAngle;

        private Vector3 _suspensionUp;
        private Vector3 _suspensionForward;
        private Vector3 _suspensionRight;

        private Vector3 _wheelUp;
        private Vector3 _wheelForward;
        private Vector3 _wheelRight;

        [Header("Friction Settings")] [SerializeField]
        private FrictionPreset activeFrictionPreset;

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

        private float[] _frictionLookupTable;
        private bool _lowSpeedReferenceIsSet;
        private Vector3 _lowSpeedReferencePosition;
        private bool _wakeOneFrame;

        [Header("Visual")] [SerializeField] private Transform visualTransform;
        private float _visualRotationAngle;

        private Quaternion _steerRotation;


        private void Awake()
        {
            if (targetRigidbody == null)
                targetRigidbody = GetComponentInParent<Rigidbody>();
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

            if (useLookupTable && activeFrictionPreset != null)
            {
                BuildFrictionLookupTable();
            }

            _initialized = true;
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

        private float EvaluateFrictionFast(float slip)
        {
            float t = Mathf.Clamp01(slip) * frictionLookupResolution;
            int index = (int)t;
            float fraction = t - index;

            if (index >= frictionLookupResolution)
                return _frictionLookupTable[frictionLookupResolution];

            return Mathf.Lerp(_frictionLookupTable[index],
                _frictionLookupTable[index + 1],
                fraction);
        }

        private int GetAdaptiveSubsteps(float speed)
        {
            if (speed > 6f)
                return minSubsteps;
            else
                return maxSubsteps;
        }

        private void FixedUpdate()
        {
            _dt = Time.fixedDeltaTime;
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
                forwardFriction.force = 0;
                forwardFriction.slip = 0;
                sideFriction.force = 0f;
                sideFriction.slip = 0f;
                _frictionForce = Vector3.zero;
                _angularVelocity = 0f;
                return;
            }

            float lngLoad = _load * forwardFriction.loadFactor;
            float latLoad = _load * sideFriction.loadFactor;

            float lngLoadClamped = Mathf.Pow(Mathf.Clamp01(lngLoad / loadRating), 0.5f)
                                   * loadRating * forwardFriction.loadFactor;
            float latLoadClamped = Mathf.Pow(Mathf.Clamp01(latLoad / loadRating), 0.5f)
                                   * loadRating * sideFriction.loadFactor;

            Vector3 contactVelocity = targetRigidbody.GetPointVelocity(_wheelHit.point);
            if (_wheelHit.collider.attachedRigidbody != null)
            {
                contactVelocity -= _wheelHit.collider.attachedRigidbody.GetPointVelocity(_wheelHit.point);
            }

            forwardFriction.speed = Vector3.Dot(contactVelocity, _wheelForward);
            sideFriction.speed = Vector3.Dot(contactVelocity, _wheelRight);

            float absForwardSpeed = Mathf.Abs(forwardFriction.speed);
            float absSideSpeed = Mathf.Abs(sideFriction.speed);
            float forwardSpeedClamp = 1.5f * (_dt / 0.005f);
            forwardSpeedClamp = Mathf.Clamp(forwardSpeedClamp, 1.5f, 10f);
            float clampedAbsForwardSpeed = Mathf.Max(absForwardSpeed, forwardSpeedClamp);
            float invClampedAbsForwardSpeed = 1.0f / clampedAbsForwardSpeed;

            // LONGITUDINAL FRICTION
            float peakForwardFrictionForce = activeFrictionPreset.BCDE.z * lngLoadClamped;
            float combinedBrakeTorque = brakeTorque + rollingResistanceTorque;
            int dynamicSubsteps = useAdaptiveSubsteps ? GetAdaptiveSubsteps(clampedAbsForwardSpeed) : frictionSubsteps;
            float invSubsteps = 1.0f / dynamicSubsteps;
            float sdt = _dt * invSubsteps;
            float invInertia = 1.0f / _inertia;
            float lngFrictionForceSum = 0.0f;
            float totalWheelTorque = 0.0f;

            for (int substep = 0; substep < dynamicSubsteps; substep++)
            {
                float stepMotorTorque = motorTorque * sdt;
                _angularVelocity += stepMotorTorque * invInertia;
                totalWheelTorque += stepMotorTorque;

                if (_isGrounded)
                {
                    float slipValue = -(_angularVelocity * radius - forwardFriction.speed)
                                      * invClampedAbsForwardSpeed * forwardFriction.stiffness;

                    float absSlip = slipValue < 0 ? -slipValue : slipValue;
                    float slipSign = slipValue >= 0.0f ? 1.0f : -1.0f;
                    float frictionCoeff = useLookupTable && _frictionLookupTable != null
                        ? EvaluateFrictionFast(absSlip)
                        : activeFrictionPreset.Curve.Evaluate(absSlip);

                    float frictionForce = -slipSign * frictionCoeff * lngLoadClamped;
                    if (frictionForce > peakForwardFrictionForce)
                        frictionForce = peakForwardFrictionForce;
                    else if (frictionForce < -peakForwardFrictionForce)
                        frictionForce = -peakForwardFrictionForce;

                    float stepFrictionTorque = frictionForce * radius * sdt;
                    _angularVelocity -= stepFrictionTorque * invInertia;
                    totalWheelTorque += stepFrictionTorque;
                    lngFrictionForceSum += frictionForce * invSubsteps;
                }

                if (combinedBrakeTorque > 0.0f)
                {
                    float angVelSign = _angularVelocity >= 0.0f ? 1.0f : -1.0f;
                    float stepBrakeTorque = angVelSign * combinedBrakeTorque * sdt;
                    float newAngVel = _angularVelocity - stepBrakeTorque * invInertia;
                    _angularVelocity = ((newAngVel >= 0.0f) != (angVelSign >= 0.0f)) ? 0.0f : newAngVel;
                    totalWheelTorque -= stepBrakeTorque;
                }
            }

            forwardFriction.force = lngFrictionForceSum;
            forwardFriction.slip = -(_angularVelocity * radius - forwardFriction.speed)
                                   * invClampedAbsForwardSpeed * forwardFriction.stiffness;
            forwardFriction.slip = Mathf.Clamp(forwardFriction.slip, -1.0f, 1.0f);

            //LATERAL FRICTION
            sideFriction.slip = (Mathf.Atan2(sideFriction.speed, clampedAbsForwardSpeed) * Mathf.Rad2Deg)
                                * 0.01111f * sideFriction.stiffness;

            float sideSlipSign = sideFriction.slip < 0f ? -1f : 1f;
            float absSideSlip = Mathf.Abs(sideFriction.slip);
            float peakSideFrictionForce = activeFrictionPreset.BCDE.z * latLoadClamped;

            float sideMu = useLookupTable && _frictionLookupTable != null
                ? EvaluateFrictionFast(absSideSlip)
                : activeFrictionPreset.Curve.Evaluate(absSideSlip);

            sideFriction.force = -sideSlipSign * sideMu * latLoadClamped;
            bool isLowSpeed = absForwardSpeed < 0.12f && absSideSpeed < 0.12f;
            bool noInputTorque = Mathf.Abs(motorTorque) < 0.1f && Mathf.Abs(brakeTorque) < 0.1f;

            if (_isGrounded && !_wakeOneFrame && isLowSpeed && noInputTorque)
            {
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
                    float invDt = 1f / _dt;
                    Vector3 correctiveForce = invDt * _load * referenceError;
                    if (Mathf.Abs(_angularVelocity) < 0.5f)
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

            if (forwardFriction.force > peakForwardFrictionForce)
                forwardFriction.force = peakForwardFrictionForce;
            else if (forwardFriction.force < -peakForwardFrictionForce)
                forwardFriction.force = -peakForwardFrictionForce;

            if (sideFriction.force > peakSideFrictionForce)
                sideFriction.force = peakSideFrictionForce;
            else if (sideFriction.force < -peakSideFrictionForce)
                sideFriction.force = -peakSideFrictionForce;

            forwardFriction.force *= forwardFriction.grip;
            sideFriction.force *= sideFriction.grip;
            if (frictionCircleStrength > 0)
            {
                float frictionCircleEffect = 1f - (Mathf.Pow(Mathf.Clamp01(Mathf.Abs(forwardFriction.slip)),
                    frictionCirclePower) * frictionCircleStrength);
                sideFriction.force *= frictionCircleEffect;
            }

            _frictionForce = _wheelRight * sideFriction.force + _wheelForward * forwardFriction.force;
            if (_wakeOneFrame) _wakeOneFrame = false;
        }

        private void ApplyFrictionForces()
        {
            if (!_isGrounded) return;

            Vector3 forcePosition = _wheelHit.point + _suspensionUp * (0.8f * springMaxLength);
            targetRigidbody.AddForceAtPosition(_frictionForce, forcePosition);
        }

        private void UpdateVisual()
        {
            if (visualTransform == null) return;

            Vector3 wheelPosition = transform.position - _suspensionUp * _springLength;

            _visualRotationAngle += _angularVelocity * Mathf.Rad2Deg * Time.deltaTime;
            _visualRotationAngle %= 360f;

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
            _lowSpeedReferenceIsSet = false;
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