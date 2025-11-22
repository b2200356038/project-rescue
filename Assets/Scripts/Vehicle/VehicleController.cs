using System;
using Game.Gameplay;
using Game.Vehicle.Wheel;
using Unity.Netcode;
using UnityEngine;

namespace Game.Vehicle
{
    public class VehicleController : NetworkBehaviour, IInteractable, IOwnershipRequestable
    {
        [Header("References")] public Rigidbody rb;
        public WheelController[] wheels;
        [SerializeField] public VehicleSeatManager seatManager;
        [Header("Drive Settings")] public float maxMotorTorque = 900f;
        public float maxBrakeTorque = 3000f;
        public float maxSteerAngle = 28f;
        public float minSteerAngle = 12f;
        public float steerSmooth = 0.10f;

        [Header("Wheel Assignment")] public int[] steerWheels = new int[] { 0, 1 };
        public int[] driveWheels = new int[] { 0, 1, 2, 3 };
        public int[] handbrakeWheels = new int[] { 2, 3 };

        [Header("Network Settings")] [SerializeField]
        private float wheelSyncRate = 30f;

        private Vector2 _moveInput;
        private bool _handbrakeInput;
        private float _smoothSteer;
        private float _steerVelocity;

        private NetworkVariable<WheelsVisualState> _wheelsState = new NetworkVariable<WheelsVisualState>(
            new WheelsVisualState(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private float _wheelSyncTimer;

        public event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus>
            OnNetworkObjectOwnershipRequestResponse;

        void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (seatManager == null) seatManager = GetComponent<VehicleSeatManager>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (HasAuthority)
            {
                NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
                NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired);
            }
            NetworkObject.OnOwnershipRequested += OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse += OnOwnershipRequestResponseCallback;

            if (!HasAuthority)
            {
                foreach (var w in wheels) w.SetRemoteMode(true);
                _wheelsState.OnValueChanged += OnWheelsStateChanged;
                ApplyWheelsState(_wheelsState.Value);
            }
            else
            {
                ResetInputs();
            }
        }

        public override void OnNetworkDespawn()
        {
            NetworkObject.OnOwnershipRequested -= OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponseCallback;

            if (!HasAuthority)
            {
                _wheelsState.OnValueChanged -= OnWheelsStateChanged;
            }

            base.OnNetworkDespawn();
        }

        private bool OnOwnershipRequested(ulong clientRequesting)
        {
            bool canTransfer = seatManager.IsDriverSeatEmpty();
            Debug.Log($"Ownership request from client {clientRequesting}: {(canTransfer ? "APPROVED" : "DENIED")}");
            return canTransfer;
        }

        private void OnOwnershipRequestResponseCallback(NetworkObject.OwnershipRequestResponseStatus status)
        {
            Debug.Log($"Ownership request response: {status}");
            OnNetworkObjectOwnershipRequestResponse?.Invoke(this, status);
        }

        public bool TryGetAvailableSeat(out int seatIndex, out bool isDriverSeat)
        {
            seatIndex = -1;
            isDriverSeat = false;

            if (seatManager.IsDriverSeatEmpty())
            {
                seatIndex = seatManager.GetDriverSeatIndex();
                isDriverSeat = true;
                return true;
            }

            int emptyPassengerSeat = seatManager.GetFirstEmptyPassengerSeatIndex();
            if (emptyPassengerSeat != -1)
            {
                seatIndex = emptyPassengerSeat;
                isDriverSeat = false;
                return true;
            }

            return false;
        }

        public void RequestDriverOwnership(Action<bool> onOwnershipResult)
        {
            if (HasAuthority)
            {
                Debug.Log("Already has authority, granting immediately");
                onOwnershipResult?.Invoke(true);
                return;
            }

            Debug.Log("Requesting vehicle ownership...");
            var status = NetworkObject.RequestOwnership();
            
            if (status == NetworkObject.OwnershipRequestStatus.RequestSent)
            {
                Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus> handler = null;
                handler = (behaviour, responseStatus) =>
                {
                    OnNetworkObjectOwnershipRequestResponse -= handler;
                    
                    bool approved = responseStatus == NetworkObject.OwnershipRequestResponseStatus.Approved;
                    Debug.Log($"Ownership result: {(approved ? "APPROVED" : "DENIED")}");
                    
                    onOwnershipResult?.Invoke(approved);
                };

                OnNetworkObjectOwnershipRequestResponse += handler;
            }
            else
            {
                Debug.LogWarning($"Request ownership failed with status: {status}");
                onOwnershipResult?.Invoke(false);
            }
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);
            Debug.Log($"Vehicle ownership changed from {previous} to {current}");
            if (HasAuthority)
            {
                foreach (var w in wheels) w.SetRemoteMode(false);
                ResetInputs();
            }
            else
            {
                foreach (var w in wheels) w.SetRemoteMode(true);
            }
        }

        public void ClaimDriverSeat(ulong clientId)
        {
            seatManager.ClaimDriverSeat(clientId);
        }

        public void EnterPassenger(ulong clientId)
        {
            seatManager.RequestEnterPassenger(clientId);
        }

        public void Interact()
        {
        }

        public bool CanInteract() => seatManager.HasEmptySeats();
        public string GetInteractionPrompt() => seatManager.IsDriverSeatEmpty() ? "Drive" : "Enter";

        private void OnWheelsStateChanged(WheelsVisualState previous, WheelsVisualState current)
        {
            if (!HasAuthority) ApplyWheelsState(current);
        }

        private void ApplyWheelsState(WheelsVisualState state)
        {
            for (int i = 0; i < 4 && i < wheels.Length; i++)
            {
                var wheelData = i == 0 ? state.Wheel0 : i == 1 ? state.Wheel1 : i == 2 ? state.Wheel2 : state.Wheel3;
                wheels[i].SetNetworkTarget(wheelData.GetAngularVelocity(), wheelData.GetSpringLength(),
                    wheelData.GetSteerAngle());
            }
        }

        public void FixedUpdate()
        {
            if (HasAuthority)
            {
                ApplySteer();
                ApplyMotorAndBrake();
                foreach (var wheel in wheels) wheel.DoPhysicsStep();
                UpdateWheelsNetworkState();
            }
        }

        private void UpdateWheelsNetworkState()
        {
            _wheelSyncTimer += Time.fixedDeltaTime;
            if (_wheelSyncTimer >= 1f / wheelSyncRate)
            {
                _wheelSyncTimer = 0f;
                var state = new WheelsVisualState();
                for (int i = 0; i < 4 && i < wheels.Length; i++)
                {
                    var wheelData = new WheelVisualData();
                    wheelData.SetAngularVelocity(wheels[i].GetAngularVelocity());
                    wheelData.SetSpringLength(wheels[i].GetSpringLength());
                    wheelData.SetSteerAngle(wheels[i].GetSteerAngle());
                    if (i == 0) state.Wheel0 = wheelData;
                    else if (i == 1) state.Wheel1 = wheelData;
                    else if (i == 2) state.Wheel2 = wheelData;
                    else if (i == 3) state.Wheel3 = wheelData;
                }

                _wheelsState.Value = state;
            }
        }

        void ApplySteer()
        {
            float speed = rb.linearVelocity.magnitude;
            float steerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, speed * 0.04f);
            _smoothSteer = Mathf.SmoothDamp(_smoothSteer, _moveInput.x, ref _steerVelocity, steerSmooth);
            float finalAngle = steerAngle * _smoothSteer;
            for (int i = 0; i < steerWheels.Length; i++) wheels[steerWheels[i]].SetSteerAngle(finalAngle);
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
                foreach (int id in handbrakeWheels) wheels[id].brakeTorque = maxBrakeTorque;
                return;
            }

            foreach (int id in driveWheels) wheels[id].motorTorque = forwardInput * maxMotorTorque;
        }

        public void SetMovement(Vector2 input)
        {
            if (!IsOwner) return;
            _moveInput = input;
        }

        public void SetHandbrake(bool input)
        {
            if (!IsOwner) return;
            _handbrakeInput = input;
        }

        public void ResetInputs()
        {
            _moveInput = Vector2.zero;
            _handbrakeInput = false;
            _smoothSteer = 0f;
            _steerVelocity = 0f;
            if (HasAuthority) ApplyMotorAndBrake();
        }
    }

    public struct WheelsVisualState : INetworkSerializable
    {
        public WheelVisualData Wheel0;
        public WheelVisualData Wheel1;
        public WheelVisualData Wheel2;
        public WheelVisualData Wheel3;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            Wheel0.NetworkSerialize(serializer);
            Wheel1.NetworkSerialize(serializer);
            Wheel2.NetworkSerialize(serializer);
            Wheel3.NetworkSerialize(serializer);
        }
    }
}