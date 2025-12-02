using Game.Vehicle;
using Unity.Netcode;
using UnityEngine;

namespace Game.Player
{
    public class PlayerVehicleHandler : NetworkBehaviour
    {
        private readonly NetworkVariable<NetworkObjectReference> _vehicleRef = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<int> _seatIndex = new(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<bool> _isDriver = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private VehicleController _currentVehicle;
        private VehicleController _pendingVehicle;
        private PlayerStateMachine _stateMachine;

        public VehicleController CurrentVehicle => _currentVehicle;
        public int SeatIndex => _seatIndex.Value;
        public bool IsDriver => _isDriver.Value;
        public bool IsInVehicle => _currentVehicle != null && _seatIndex.Value >= 0;

        private void Awake()
        {
            _stateMachine = GetComponent<PlayerStateMachine>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _vehicleRef.OnValueChanged += OnVehicleRefChanged;
            _seatIndex.OnValueChanged += OnSeatIndexChanged;
            SyncVehicleState();
        }

        public override void OnNetworkDespawn()
        {
            _vehicleRef.OnValueChanged -= OnVehicleRefChanged;
            _seatIndex.OnValueChanged -= OnSeatIndexChanged;
            base.OnNetworkDespawn();
        }

        private void OnVehicleRefChanged(NetworkObjectReference prev, NetworkObjectReference current)
        {
            SyncVehicleState();
        }

        private void OnSeatIndexChanged(int prev, int current)
        {
            SyncVehicleState();
        }

        private void SyncVehicleState()
        {
            if (_vehicleRef.Value.TryGet(out NetworkObject netObj))
            {
                _currentVehicle = netObj.GetComponent<VehicleController>();
            }
            else
            {
                _currentVehicle = null;
            }
            if (_currentVehicle != null && _seatIndex.Value >= 0)
            {
                if (_stateMachine.CurrentState != PlayerState.Vehicle)
                {
                    _stateMachine.ChangeState(PlayerState.Vehicle);
                }
            }
            else
            {
                if (_stateMachine.CurrentState == PlayerState.Vehicle)
                {
                    _stateMachine.ChangeState(PlayerState.OnFoot);
                }
            }
        }

        public void TryEnterVehicle(VehicleController vehicle)
        {
            if (!IsOwner) return;
            if (IsInVehicle) return;
            _pendingVehicle = vehicle;
            vehicle.RequestEnter(OwnerClientId, OnEnterResponse);
        }
        

        private void OnEnterResponse(bool success, int seatIndex, bool isDriver)
        {
            if (!success)
            {
                _pendingVehicle = null;
                return;
            }

            _currentVehicle = _pendingVehicle;
            _pendingVehicle = null;
            _vehicleRef.Value = new NetworkObjectReference(_currentVehicle.NetworkObject);
            _seatIndex.Value = seatIndex;
            _isDriver.Value = isDriver;
        }

        public void TryExitVehicle()
        {
            if (!IsOwner) return;
            if (!IsInVehicle) return;

            _currentVehicle.RequestExit(OwnerClientId, OnExitResponse);
        }

        private void OnExitResponse(bool success)
        {
            if (!success) return;

            _vehicleRef.Value = default;
            _seatIndex.Value = -1;
            _isDriver.Value = false;
            _currentVehicle = null;
        }
    }
}