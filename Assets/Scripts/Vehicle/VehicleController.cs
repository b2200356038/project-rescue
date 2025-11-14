using Game.Input;
using UnityEngine;

namespace Vehicle
{
    public class VehicleController : MonoBehaviour
    {
        [Header("Movement Settings")] public float motorForce = 1500f;
        public float maxSteerAngle = 30f;
        public float brakeForce = 3000f;
        public bool enable4X4 = false;

        [Header("Wheel Transforms")] public Transform frontLeftWheelTransform;
        public Transform frontRightWheelTransform;
        public Transform rearLeftWheelTransform;
        public Transform rearRightWheelTransform;

        [Header("Wheel Settings")] public float wheelRadius = 0.4f;
        public float suspensionHeight = 0.3f;
        public float suspensionSpring = 35000f;
        public float suspensionDamper = 4500f;
        public LayerMask groundLayer;

        [Header("Physics")] public GameObject centerOfMassObject;
        public float downForce = 100f;
        public float gripMultiplier = 10f;
        public float lateralGripStrength = 5000f;
        private Rigidbody _rb;

        [Header("Gear System")] public float[] gearRatios = new float[] { 2.66f, 1.78f, 1.30f, 1.00f, 0.74f };
        public float shiftThreshold = 5000f;
        private int _currentGear = 1;
        private float _stopSpeedThreshold = 1f;

        private WheelData _frontLeft, _frontRight, _rearLeft, _rearRight;
        private float _currentRpm = 0f;
        private float _wheelRotation = 0f;

        private Vector2 _moveInput;
        private bool _brakeInput;
        private float _currentSteerAngle = 0f;

        private class WheelData
        {
            public Transform WheelTransform;
            public bool IsGrounded;
            public float SuspensionLength;
            public Vector3 GroundNormal;
            public Vector3 GroundPoint;
            public float SlipAngle;
            public float ForwardSlip;
            public Vector3 Velocity;
            public bool IsDriveWheel;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            // Rigidbody ayarları
            _rb.mass = 1500f;
            _rb.linearDamping = 0.1f;
            _rb.angularDamping = 0.5f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void Start()
        {
            _frontLeft = new WheelData { WheelTransform = frontLeftWheelTransform, IsDriveWheel = true };
            _frontRight = new WheelData { WheelTransform = frontRightWheelTransform, IsDriveWheel = true };
            _rearLeft = new WheelData { WheelTransform = rearLeftWheelTransform, IsDriveWheel = enable4X4 };
            _rearRight = new WheelData { WheelTransform = rearRightWheelTransform, IsDriveWheel = enable4X4 };
        }

        void Update()
        {
            _moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            _brakeInput = GameInput.Actions.Player.Jump.IsPressed();

            if (centerOfMassObject)
            {
                _rb.centerOfMass = transform.InverseTransformPoint(centerOfMassObject.transform.position);
            }

            // Update current steer angle smoothly
            float targetSteerAngle = _moveInput.x * maxSteerAngle;
            _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetSteerAngle, Time.deltaTime * 5f);

            float speed = _rb.linearVelocity.magnitude;
            _wheelRotation += speed * Time.deltaTime * 360f / (2f * Mathf.PI * wheelRadius);

            UpdateWheelVisuals();
        }

        void FixedUpdate()
        {
            float vertical = _moveInput.y;
            float horizontal = _moveInput.x;

            // Wheel grounding check
            UpdateWheelGrounding(_frontLeft);
            UpdateWheelGrounding(_frontRight);
            UpdateWheelGrounding(_rearLeft);
            UpdateWheelGrounding(_rearRight);

            // Current speed calculation
            float currentSpeedKmph = _rb.linearVelocity.magnitude * 3.6f;

            // Gear shifting
            _currentRpm = Mathf.Abs(currentSpeedKmph * 100f);
            if (_currentRpm > shiftThreshold && _currentGear < gearRatios.Length)
            {
                _currentGear++;
            }
            else if (currentSpeedKmph < _stopSpeedThreshold && _currentGear > 1)
            {
                _currentGear--;
            }

            // Apply suspension forces
            ApplySuspension(_frontLeft);
            ApplySuspension(_frontRight);
            ApplySuspension(_rearLeft);
            ApplySuspension(_rearRight);

            // Apply motor force
            if (Mathf.Abs(vertical) > 0.01f && !_brakeInput)
            {
                ApplyMotorForce(vertical);
            }

            // Apply steering
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                ApplySteering(horizontal);
            }

            // Apply braking
            if (_brakeInput)
            {
                ApplyBraking();
            }

            // Grip force for lateral stability
            ApplyGripForce();

            // Downforce at high speeds
            _rb.AddForce(-transform.up * (downForce * _rb.linearVelocity.magnitude));
        }

        void UpdateWheelGrounding(WheelData wheel)
        {
            if (wheel.WheelTransform == null) return;

            Vector3 rayStart = wheel.WheelTransform.position;
            float rayLength = suspensionHeight + wheelRadius;

            if (UnityEngine.Physics.Raycast(rayStart, -transform.up, out RaycastHit hit, rayLength, groundLayer))
            {
                wheel.IsGrounded = true;
                wheel.SuspensionLength = hit.distance - wheelRadius;
                wheel.GroundNormal = hit.normal;
                wheel.GroundPoint = hit.point;

                // Calculate wheel velocity
                wheel.Velocity = _rb.GetPointVelocity(wheel.WheelTransform.position);

                // Calculate slip angles in local space
                Vector3 localVel = transform.InverseTransformDirection(wheel.Velocity);
                wheel.SlipAngle = Mathf.Atan2(localVel.x, Mathf.Abs(localVel.z)) * Mathf.Rad2Deg;
                wheel.ForwardSlip = localVel.z;
            }
            else
            {
                wheel.IsGrounded = false;
                wheel.SuspensionLength = suspensionHeight;
            }
        }

        void ApplySuspension(WheelData wheel)
        {
            if (!wheel.IsGrounded) return;

            // Spring compression calculation
            float compressionRatio = Mathf.Clamp01(1f - (wheel.SuspensionLength / suspensionHeight));
            float springForce = compressionRatio * suspensionSpring;

            // Damper calculation
            Vector3 suspensionVelocity = _rb.GetPointVelocity(wheel.WheelTransform.position);
            float damperForce = Vector3.Dot(suspensionVelocity, transform.up) * suspensionDamper;

            // Total suspension force
            float totalForce = springForce - damperForce;

            Vector3 force = transform.up * totalForce;
            _rb.AddForceAtPosition(force, wheel.WheelTransform.position);
        }

        void ApplyMotorForce(float input)
        {
            float gearRatio = gearRatios[Mathf.Clamp(_currentGear - 1, 0, gearRatios.Length - 1)];
            float adjustedTorque = input * motorForce * gearRatio;

            // Apply to front wheels
            ApplyWheelForce(_frontLeft, adjustedTorque);
            ApplyWheelForce(_frontRight, adjustedTorque);

            // Apply to rear wheels if 4x4
            if (enable4X4)
            {
                ApplyWheelForce(_rearLeft, adjustedTorque);
                ApplyWheelForce(_rearRight, adjustedTorque);
            }
        }

        void ApplyWheelForce(WheelData wheel, float torque)
        {
            if (!wheel.IsGrounded || !wheel.IsDriveWheel) return;

            Vector3 force = transform.forward * torque;
            _rb.AddForceAtPosition(force, wheel.WheelTransform.position);
        }

        void ApplySteering(float steerInput)
        {
            // Check if front wheels are grounded
            if (!_frontLeft.IsGrounded || !_frontRight.IsGrounded) return;

            float steerAngle = steerInput * maxSteerAngle;
            float speed = _rb.linearVelocity.magnitude;

            // Only steer if moving
            if (speed > 0.5f)
            {
                // Calculate desired direction
                Vector3 steerDirection = Quaternion.Euler(0, steerAngle, 0) * transform.forward;

                // Calculate torque based on speed (more torque at higher speeds)
                float steerTorque = steerInput * speed * 500f;

                // Apply torque
                _rb.AddTorque(transform.up * steerTorque);
            }
        }

        void ApplyGripForce()
        {
            // Her tekerlek için ayrı grip hesapla
            ApplyWheelGrip(_frontLeft);
            ApplyWheelGrip(_frontRight);
            ApplyWheelGrip(_rearLeft);
            ApplyWheelGrip(_rearRight);
        }

        void ApplyWheelGrip(WheelData wheel)
        {
            if (!wheel.IsGrounded) return;

            // Tekerlek pozisyonundaki hız
            Vector3 wheelVelocity = _rb.GetPointVelocity(wheel.WheelTransform.position);

            // Yanal (sideways) hız
            Vector3 lateralVelocity = transform.right * Vector3.Dot(wheelVelocity, transform.right);

            // Grip kuvveti - yanal kaymayı önle
            Vector3 gripForce = -lateralVelocity * lateralGripStrength * Time.fixedDeltaTime;

            _rb.AddForceAtPosition(gripForce, wheel.WheelTransform.position, ForceMode.Acceleration);
        }

        void ApplyBraking()
        {
            // Simple brake force opposite to velocity
            Vector3 brakeForceVec = -_rb.linearVelocity.normalized * brakeForce;
            _rb.AddForce(brakeForceVec);

            // Additional rotational brake - daha agresif
            _rb.angularVelocity *= 0.9f;

            // Düşük hızda tamamen durdur
            if (_rb.linearVelocity.magnitude < 0.5f)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        void UpdateWheelVisuals()
        {
            Quaternion rotation = Quaternion.Euler(_wheelRotation, 0, 0);

            // Front wheels - with steering
            if (frontLeftWheelTransform)
            {
                Quaternion steerRotation = Quaternion.Euler(0, _currentSteerAngle, 0);
                frontLeftWheelTransform.localRotation = steerRotation * rotation;
            }

            if (frontRightWheelTransform)
            {
                Quaternion steerRotation = Quaternion.Euler(0, _currentSteerAngle, 0);
                Quaternion flipRotation = Quaternion.Euler(0, 180, 0);
                frontRightWheelTransform.localRotation = steerRotation * rotation * flipRotation;
            }

            // Rear wheels - no steering
            if (rearLeftWheelTransform)
                rearLeftWheelTransform.localRotation = rotation;
            if (rearRightWheelTransform)
                rearRightWheelTransform.localRotation = rotation * Quaternion.Euler(0, 180, 0);
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            DrawWheelDebug(_frontLeft, Color.red);
            DrawWheelDebug(_frontRight, Color.green);
            DrawWheelDebug(_rearLeft, Color.blue);
            DrawWheelDebug(_rearRight, Color.yellow);
        }

        void DrawWheelDebug(WheelData wheel, Color color)
        {
            if (wheel == null || wheel.WheelTransform == null) return;

            Gizmos.color = color;
            Vector3 pos = wheel.WheelTransform.position;
            float rayLength = suspensionHeight + wheelRadius;

            // Draw suspension ray
            Gizmos.DrawLine(pos, pos - transform.up * rayLength);

            // Draw ground contact point if grounded
            if (wheel.IsGrounded)
            {
                Gizmos.DrawWireSphere(wheel.GroundPoint, 0.1f);

                // Draw compression amount
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, wheel.GroundPoint);
            }
        }
    }

}