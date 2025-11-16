using UnityEngine;
using Game.Input;
using Game.Vehicle.Wheel; // SENİN GameInput wrapper’ın

namespace Game.Vehicle
{
    public class CarController : MonoBehaviour
    {
        [Header("References")]
        public Rigidbody rb;
        public WheelController[] wheels;

        
        [Header("Drive Settings")]
        public float maxMotorTorque = 900f;
        public float maxBrakeTorque = 3000f;

        [Header("Steering")]
        public float maxSteerAngle = 28f;
        public float minSteerAngle = 12f;
        public float steerSmooth = 0.10f;

        [Header("Wheel Assignment")]
        public int[] steerWheels = new int[] { 0, 1 };
        public int[] driveWheels = new int[] { 0, 1, 2, 3 };
        public int[] handbrakeWheels = new int[] { 2, 3 };

        private Vector2 _moveInput;
        private bool _handbrakeInput;

        private float _smoothSteer;
        private float _steerVelocity;
        private float _dt;

        void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            _moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            _handbrakeInput = GameInput.Actions.Player.Jump.ReadValue<float>() > 0.1f;
        }

        void FixedUpdate()
        {
            _dt = Time.fixedDeltaTime;

            ApplySteer();
            ApplyMotorAndBrake();
        }
        
        void ApplySteer()
        {
            float speed = rb.linearVelocity.magnitude;
            float steerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, speed * 0.04f);

            _smoothSteer = Mathf.SmoothDamp(
                _smoothSteer,
                _moveInput.x,
                ref _steerVelocity,
                steerSmooth
            );

            float finalAngle = steerAngle * _smoothSteer;

            for (int i = 0; i < steerWheels.Length; i++)
                wheels[steerWheels[i]].steerAngle = finalAngle;
        }
        
        void ApplyMotorAndBrake()
        {
            foreach (var w in wheels)
            {
                w.motorTorque = 0f;
                w.brakeTorque = 0f;
            }

            float forwardInput = _moveInput.y;

            if (_handbrakeInput)
            {
                foreach (int id in handbrakeWheels)
                    wheels[id].brakeTorque = maxBrakeTorque;

                return;
            }
            
            if (forwardInput > 0f)
            {
                foreach (int id in driveWheels)
                    wheels[id].motorTorque = forwardInput * maxMotorTorque;
            }
        }
    }
}
