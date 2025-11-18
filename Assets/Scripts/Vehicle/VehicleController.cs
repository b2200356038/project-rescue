using UnityEngine;
using Game.Vehicle.Wheel;
using Unity.Netcode;

namespace Game.Vehicle
{
    public class VehicleController : NetworkBehaviour
    {
        [Header("References")]
        public Rigidbody rb;
        public WheelController[] wheels;
        
        [Header("Components")]
        [SerializeField] private VehicleSeatManager seatManager;

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

        void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
                
            if (seatManager == null)
                seatManager = GetComponent<VehicleSeatManager>();
        }

        public void OnFixedUpdate()
        {
            if (!enabled) return;
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
                wheels[steerWheels[i]].SetSteerAngle(finalAngle);
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

            foreach (int id in driveWheels)
                wheels[id].motorTorque = forwardInput * maxMotorTorque;
        }

        public void SetMovement(Vector2 input)
        {
            _moveInput = input;
        }

        public void SetHandbrake(bool input)
        {
            _handbrakeInput = input;
        }

        public void ResetInputs()
        {
            _moveInput = Vector2.zero;
            _handbrakeInput = false;
        }
        
        public bool HasEmptySeats() => seatManager.HasEmptySeats();
        public bool TryEnterVehicle(ulong clientId) => seatManager.TryEnterVehicle(clientId);
        public void ExitVehicle(ulong clientId) => seatManager.ExitVehicle(clientId);
        public Transform GetSeatTransform(ulong clientId) => seatManager.GetSeatTransform(clientId);
        public Transform GetExitPoint() => seatManager.GetExitPoint();
    }
}