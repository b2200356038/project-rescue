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
        private int _pendingSeatIndex = -1;

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
            _vehicleRef.OnValueChanged += OnVehicleRefChanged;
            _seatIndex.OnValueChanged += OnSeatIndexChanged;
            SyncState();
        }

        public override void OnNetworkDespawn()
        {
            _vehicleRef.OnValueChanged -= OnVehicleRefChanged;
            _seatIndex.OnValueChanged -= OnSeatIndexChanged;

            if (_pendingVehicle != null && _pendingVehicle.NetworkObject != null)
            {
                _pendingVehicle.NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
            }
        }

        private void OnVehicleRefChanged(NetworkObjectReference prev, NetworkObjectReference current)
        {
            SyncState();
        }

        private void OnSeatIndexChanged(int prev, int current)
        {
            SyncState();
        }

        private void SyncState()
        {
            if (_vehicleRef.Value.TryGet(out NetworkObject obj))
                _currentVehicle = obj.GetComponent<VehicleController>();
            else
                _currentVehicle = null;

            if (_currentVehicle != null && _seatIndex.Value >= 0)
            {
                if (_stateMachine.CurrentState != PlayerState.Vehicle)
                    _stateMachine.ChangeState(PlayerState.Vehicle);
            }
            else
            {
                if (_stateMachine.CurrentState == PlayerState.Vehicle)
                    _stateMachine.ChangeState(PlayerState.OnFoot);
            }
        }

        public void TryEnterVehicle(VehicleController vehicle)
        {
            if (!IsOwner) return;
            if (_pendingVehicle != null) return;
            if (IsInVehicle) return;

            _pendingVehicle = vehicle;
            vehicle.RequestSeat(OwnerClientId, OnSeatResponse);
        }

        private void OnSeatResponse(bool success, int seatIndex, bool isDriver)
        {
            if (!success)
            {
                _pendingVehicle = null;
                return;
            }

            _pendingSeatIndex = seatIndex;

            if (isDriver)
            {
                if (_pendingVehicle.NetworkObject.HasAuthority)
                {
                    FinalizeEnterVehicle(seatIndex, true);
                }
                else
                {
                    _pendingVehicle.NetworkObject.OnOwnershipRequestResponse += OnOwnershipRequestResponse;
                    var status = _pendingVehicle.NetworkObject.RequestOwnership();

                    if (status != NetworkObject.OwnershipRequestStatus.RequestSent)
                    {
                        _pendingVehicle.NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
                        _pendingVehicle = null;
                        _pendingSeatIndex = -1;
                    }
                }
            }
            else
            {
                FinalizeEnterVehicle(seatIndex, false);
            }
        }

        private void OnOwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus status)
        {
            if (_pendingVehicle != null)
            {
                _pendingVehicle.NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
            }

            if (status == NetworkObject.OwnershipRequestResponseStatus.Approved)
            {
                FinalizeEnterVehicle(_pendingSeatIndex, true);
            }
            else
            {
                _pendingVehicle = null;
                _pendingSeatIndex = -1;
            }
        }

        private void FinalizeEnterVehicle(int seatIndex, bool isDriver)
        {
            if (_pendingVehicle == null) return;
            VehicleController vehicleToEnter = _pendingVehicle;
            _pendingVehicle = null;

            if (vehicleToEnter.NetworkObject == null)
            {
                Debug.LogError("Vehicle NetworkObject yok!");
                return;
            }

            var networkObjRef = new NetworkObjectReference(vehicleToEnter.NetworkObject);
            _vehicleRef.Value = networkObjRef;
            _isDriver.Value = isDriver;
            _seatIndex.Value = seatIndex;
            _currentVehicle = vehicleToEnter;
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