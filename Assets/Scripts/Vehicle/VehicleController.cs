using System;
using Game.Gameplay;
using Game.Vehicle.Wheel;
using Unity.Netcode;
using UnityEngine;

namespace Game.Vehicle
{
    public class VehicleController : TransferableObject, IInteractable
    {
        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] public VehicleSeatManager seatManager;
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

        private Action<bool, int, bool> _pendingSeatCallback;

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
                SetupAsAuthority();
            }
            else
            {
                SetupAsRemote();
            }

            _wheelsState.OnValueChanged += OnWheelsStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            _wheelsState.OnValueChanged -= OnWheelsStateChanged;
            base.OnNetworkDespawn();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);
            if (NetworkManager.Singleton.LocalClientId == current)
                SetupAsAuthority();
            else
                SetupAsRemote();
        }

        protected override bool OnOwnershipRequested(ulong clientRequesting)
        {
            if (!seatManager.IsDriverSeatEmpty())
            {
                return false;
            }
            return base.OnOwnershipRequested(clientRequesting);
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

        public void RequestSeat(ulong clientId, Action<bool, int, bool> callback)
        {
            _pendingSeatCallback = callback;
            ReserveSeatRpc(clientId);
        }

        [Rpc(SendTo.Authority)]
        private void ReserveSeatRpc(ulong clientId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;

            if (!seatManager.TryGetAvailableSeat(out int seatIndex, out bool isDriver))
            {
                SeatResponseRpc(false, -1, false, RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            seatManager.ClaimSeat(seatIndex, clientId);
            SeatResponseRpc(true, seatIndex, isDriver, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SeatResponseRpc(bool success, int seatIndex, bool isDriver, RpcParams rpcParams = default)
        {
            _pendingSeatCallback?.Invoke(success, seatIndex, isDriver);
            _pendingSeatCallback = null;
        }

        // --- GÜNCELLENEN KISIM: Callback kaldırıldı ---
        public void RequestExit(ulong clientId)
        {
            // Sadece bildiriyoruz, cevap beklemiyoruz.
            RequestExitRpc(clientId);
        }

        [Rpc(SendTo.Authority)]
        private void RequestExitRpc(ulong clientId, RpcParams rpcParams = default)
        {
            // İsteği gönderen veya işlem yapılması gereken client
            // (Distributed Authority yapısında bu RPC otoriteye düşer)
            int seatIndex = seatManager.GetSeatIndex(clientId);

            if (seatIndex != -1)
            {
                bool wasDriver = seatManager.IsDriverSeat(seatIndex);
                seatManager.ReleaseSeat(seatIndex);

                if (wasDriver)
                {
                    ResetInputs();
                }
            }
            // ExitResponseRpc kaldırıldı.
        }
        // ---------------------------------------------

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
                wheel.DoPhysicsStep();

            UpdateWheelsNetworkState();
        }

        private void ApplySteer()
        {
            float speed = rb.linearVelocity.magnitude;
            float steerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, speed * 0.04f);

            _smoothSteer = Mathf.SmoothDamp(_smoothSteer, _moveInput.x, ref _steerVelocity, steerSmooth);
            float finalAngle = steerAngle * _smoothSteer;

            foreach (int i in steerWheels)
                wheels[i].SetSteerAngle(finalAngle);
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
                    wheels[id].brakeTorque = maxBrakeTorque;
                return;
            }

            foreach (int id in driveWheels)
                wheels[id].motorTorque = _moveInput.y * maxMotorTorque;
        }

        private void SetWheelsRemoteMode(bool remote)
        {
            foreach (var w in wheels)
                w.SetRemoteMode(remote);
        }

        private void OnWheelsStateChanged(WheelsVisualState prev, WheelsVisualState curr)
        {
            if (!HasAuthority)
                ApplyWheelsState(curr);
        }

        private void ApplyWheelsState(WheelsVisualState state)
        {
            for (int i = 0; i < wheels.Length && i < 4; i++)
            {
                var d = i switch
                {
                    0 => state.Wheel0,
                    1 => state.Wheel1,
                    2 => state.Wheel2,
                    _ => state.Wheel3
                };

                wheels[i].SetNetworkTarget(
                    d.GetAngularVelocity(),
                    d.GetSpringLength(),
                    d.GetSteerAngle());
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
                var d = new WheelVisualData();
                d.SetAngularVelocity(wheels[i].GetAngularVelocity());
                d.SetSpringLength(wheels[i].GetSpringLength());
                d.SetSteerAngle(wheels[i].GetSteerAngle());

                switch (i)
                {
                    case 0: state.Wheel0 = d; break;
                    case 1: state.Wheel1 = d; break;
                    case 2: state.Wheel2 = d; break;
                    case 3: state.Wheel3 = d; break;
                }
            }

            _wheelsState.Value = state;
        }

        public bool CanInteract() => seatManager.HasEmptySeats();
        public void Interact() { }
        public string GetInteractionPrompt() => "Enter the vehicle";
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