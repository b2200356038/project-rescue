using System;
using Game.Gameplay;
using Game.Vehicle.Wheel;
using Unity.Netcode;
using UnityEngine;

namespace Game.Vehicle
{
    public class VehicleController : NetworkBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private VehicleSeatManager seatManager;
        [SerializeField] private WheelController[] wheels;

        [Header("Drive Settings")]
        [SerializeField] private float maxMotorTorque = 900f;
        [SerializeField] private float maxBrakeTorque = 3000f;
        [SerializeField] private float maxSteerAngle = 28f;
        [SerializeField] private float minSteerAngle = 12f;
        [SerializeField] private float steerSmooth = 0.10f;

        [Header("Wheel Assignment")]
        [SerializeField] private int[] steerWheels = { 0, 1 };
        [SerializeField] private int[] driveWheels = { 0, 1, 2, 3 };
        [SerializeField] private int[] handbrakeWheels = { 2, 3 };

        [Header("Network")]
        [SerializeField] private float wheelSyncRate = 30f;

        private Vector2 _moveInput;
        private bool _handbrakeInput;
        private float _smoothSteer;
        private float _steerVelocity;
        private float _wheelSyncTimer;

        private readonly NetworkVariable<WheelsVisualState> _wheelsState = new(
            new WheelsVisualState(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private Action<bool, int, bool> _pendingEnterCallback;
        private Action<bool> _pendingExitCallback;
        private int _pendingSeatIndex = -1;
        private bool _pendingIsDriver;

        public VehicleSeatManager SeatManager => seatManager;
        public Rigidbody Rigidbody => rb;

        private void Awake()
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
                SetupAsAuthority();
            }
            else
            {
                SetupAsRemote();
            }

            NetworkObject.OnOwnershipRequested += OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse += OnOwnershipRequestResponse;
            _wheelsState.OnValueChanged += OnWheelsStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            NetworkObject.OnOwnershipRequested -= OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
            _wheelsState.OnValueChanged -= OnWheelsStateChanged;
            base.OnNetworkDespawn();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);

            bool isNowAuthority = NetworkManager.Singleton.LocalClientId == current;

            if (isNowAuthority)
            {
                SetupAsAuthority();
            }
            else
            {
                SetupAsRemote();
            }
        }

        private void SetupAsAuthority()
        {
            SetWheelsRemoteMode(false);
            ResetInputs();
        }

        private void SetupAsRemote()
        {
            SetWheelsRemoteMode(true);
            ApplyWheelsState(_wheelsState.Value);
        }

        private bool OnOwnershipRequested(ulong clientRequesting)
        {
            int driverSeatIndex = seatManager.GetDriverSeatIndex();
            if (driverSeatIndex == -1) return false;

            int clientSeatIndex = seatManager.GetSeatIndex(clientRequesting);
            return clientSeatIndex == driverSeatIndex;
        }

        private void OnOwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus status)
        {
            if (_pendingEnterCallback == null) return;

            if (status == NetworkObject.OwnershipRequestResponseStatus.Approved)
            {
                _pendingEnterCallback.Invoke(true, _pendingSeatIndex, _pendingIsDriver);
            }
            else
            {
                ReleaseSeatRpc(_pendingSeatIndex);
                _pendingEnterCallback.Invoke(false, -1, false);
            }

            _pendingEnterCallback = null;
            _pendingSeatIndex = -1;
            _pendingIsDriver = false;
        }

        public void RequestEnter(ulong clientId, Action<bool, int, bool> callback)
        {
            _pendingEnterCallback = callback;
            ReserveSeatRpc(clientId);
        }

        public void RequestExit(ulong clientId, Action<bool> callback)
        {
            _pendingExitCallback = callback;
            RequestExitRpc(clientId);
        }
        [Rpc(SendTo.Authority)]
        private void ReserveSeatRpc(ulong clientId, RpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            if (!seatManager.TryGetAvailableSeat(out int seatIndex, out bool isDriverSeat))
            {
                SeatReservedResponseRpc(false, -1, false, RpcTarget.Single(senderId, RpcTargetUse.Temp));
                return;
            }
            seatManager.ClaimSeat(seatIndex, clientId);
            SeatReservedResponseRpc(true, seatIndex, isDriverSeat, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SeatReservedResponseRpc(bool success, int seatIndex, bool isDriver, RpcParams rpcParams = default)
        {
            if (!success)
            {
                _pendingEnterCallback?.Invoke(false, -1, false);
                _pendingEnterCallback = null;
                return;
            }

            _pendingSeatIndex = seatIndex;
            _pendingIsDriver = isDriver;

            if (isDriver)
            {
                NetworkObject.RequestOwnership();
            }
            else
            {
                _pendingEnterCallback?.Invoke(true, seatIndex, false);
                _pendingEnterCallback = null;
                _pendingSeatIndex = -1;
                _pendingIsDriver = false;
            }
        }

        [Rpc(SendTo.Authority)]
        private void ReleaseSeatRpc(int seatIndex)
        {
            seatManager.ReleaseSeat(seatIndex);
        }

        [Rpc(SendTo.Authority)]
        private void RequestExitRpc(ulong clientId, RpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            int seatIndex = seatManager.GetSeatIndex(clientId);

            if (seatIndex == -1)
            {
                ExitResponseRpc(false, RpcTarget.Single(senderId, RpcTargetUse.Temp));
                return;
            }

            bool wasDriver = seatManager.IsDriverSeat(seatIndex);
            seatManager.ReleaseSeat(seatIndex);

            if (wasDriver)
            {
                ResetInputs();
            }

            ExitResponseRpc(true, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ExitResponseRpc(bool success, RpcParams rpcParams = default)
        {
            _pendingExitCallback?.Invoke(success);
            _pendingExitCallback = null;
        }

        public void SetMovement(Vector2 input)
        {
            if (!HasAuthority) return;
            _moveInput = input;
        }

        public void SetHandbrake(bool input)
        {
            if (!HasAuthority) return;
            _handbrakeInput = input;
        }

        public void ResetInputs()
        {
            _moveInput = Vector2.zero;
            _handbrakeInput = false;
            _smoothSteer = 0f;
            _steerVelocity = 0f;
        }

        private void FixedUpdate()
        {
            if (!HasAuthority) return;

            ApplySteer();
            ApplyMotorAndBrake();

            foreach (var wheel in wheels)
            {
                wheel.DoPhysicsStep();
            }

            UpdateWheelsNetworkState();
        }

        private void ApplySteer()
        {
            float speed = rb.linearVelocity.magnitude;
            float steerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, speed * 0.04f);
            _smoothSteer = Mathf.SmoothDamp(_smoothSteer, _moveInput.x, ref _steerVelocity, steerSmooth);

            float finalAngle = steerAngle * _smoothSteer;
            foreach (int i in steerWheels)
            {
                wheels[i].SetSteerAngle(finalAngle);
            }
        }

        private void ApplyMotorAndBrake()
        {
            foreach (var w in wheels)
            {
                w.motorTorque = 0f;
                w.brakeTorque = 0f;
            }

            if (_handbrakeInput)
            {
                foreach (int id in handbrakeWheels)
                {
                    wheels[id].brakeTorque = maxBrakeTorque;
                }
                return;
            }

            foreach (int id in driveWheels)
            {
                wheels[id].motorTorque = _moveInput.y * maxMotorTorque;
            }
        }

        private void SetWheelsRemoteMode(bool remote)
        {
            foreach (var w in wheels)
            {
                w.SetRemoteMode(remote);
            }
        }

        private void OnWheelsStateChanged(WheelsVisualState previous, WheelsVisualState current)
        {
            if (!HasAuthority)
            {
                ApplyWheelsState(current);
            }
        }

        private void ApplyWheelsState(WheelsVisualState state)
        {
            for (int i = 0; i < wheels.Length && i < 4; i++)
            {
                var data = i switch
                {
                    0 => state.Wheel0,
                    1 => state.Wheel1,
                    2 => state.Wheel2,
                    _ => state.Wheel3
                };
                wheels[i].SetNetworkTarget(data.GetAngularVelocity(), data.GetSpringLength(), data.GetSteerAngle());
            }
        }

        private void UpdateWheelsNetworkState()
        {
            _wheelSyncTimer += Time.fixedDeltaTime;
            if (_wheelSyncTimer < 1f / wheelSyncRate) return;

            _wheelSyncTimer = 0f;

            var state = new WheelsVisualState();
            for (int i = 0; i < wheels.Length && i < 4; i++)
            {
                var data = new WheelVisualData();
                data.SetAngularVelocity(wheels[i].GetAngularVelocity());
                data.SetSpringLength(wheels[i].GetSpringLength());
                data.SetSteerAngle(wheels[i].GetSteerAngle());

                switch (i)
                {
                    case 0: state.Wheel0 = data; break;
                    case 1: state.Wheel1 = data; break;
                    case 2: state.Wheel2 = data; break;
                    case 3: state.Wheel3 = data; break;
                }
            }

            _wheelsState.Value = state;
        }

        public bool CanInteract()
        {
            return seatManager.HasEmptySeats();
        }

        public void Interact()
        {
        }

        public string GetInteractionPrompt()
        {
            return "Enter the vehicle";
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