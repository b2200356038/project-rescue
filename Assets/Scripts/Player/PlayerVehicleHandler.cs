using Game.Gameplay;
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

        private VehicleController _currentVehicle;
        private VehicleController _pendingVehicle;
        private VehicleController _targetVehicle;
        private PlayerStateMachine _stateMachine;

        public VehicleController CurrentVehicle => _currentVehicle;
        
        private void Awake()
        {
            _stateMachine = GetComponent<PlayerStateMachine>();
        }

        public override void OnNetworkSpawn()
        {
            _vehicleRef.OnValueChanged += OnVehicleRefChanged;
            if (!IsOwner && _vehicleRef.Value.TryGet(out NetworkObject obj))
            {
                _currentVehicle = obj.GetComponent<VehicleController>();
            }
        }

        public override void OnNetworkDespawn()
        {
            _vehicleRef.OnValueChanged -= OnVehicleRefChanged;

            if (_targetVehicle != null && _targetVehicle.NetworkObject != null)
            {
                 if (_targetVehicle is IOwnershipRequestable requestable)
                 {
                     requestable.OnNetworkObjectOwnershipRequestResponse -= OnOwnershipRequestResponse;
                 }
            }
        }

        private void OnVehicleRefChanged(NetworkObjectReference prev, NetworkObjectReference current)
        {
            if (IsOwner)
            {
                if (!current.TryGet(out _))
                {
                    _currentVehicle = null;
                    if (_stateMachine.CurrentState == PlayerState.Vehicle)
                    {
                        _stateMachine.ChangeState(PlayerState.OnFoot);
                    }
                }
                _stateMachine.ChangeState(PlayerState.Vehicle);
                return;
            }
            if (current.TryGet(out NetworkObject obj))
            {
                _currentVehicle = obj.GetComponent<VehicleController>();
            }
            else
            {
                _currentVehicle = null;
            }
        }
        

        public void TryEnterVehicle(VehicleController vehicle)
        {
            if (!IsOwner) return;
            if (_pendingVehicle != null) return;
            _targetVehicle = vehicle;
            bool needsOwnership = _targetVehicle.SeatManager.IsDriverSeatEmpty();

            if (needsOwnership)
            {
                HandleOwnershipTransferAndEnter(_targetVehicle);
            }
            else
            {
                RequestSeatEntry(_targetVehicle);
            }
        }

        private void HandleOwnershipTransferAndEnter(VehicleController vehicle)
        {
            NetworkObject targetNetObj = vehicle.NetworkObject;

            if (targetNetObj.HasAuthority)
            {
                RequestSeatEntry(vehicle);
                return;
            }

            if (targetNetObj.IsOwnershipTransferable)
            {
                 targetNetObj.ChangeOwnership(OwnerClientId);
                 RequestSeatEntry(vehicle);
            }
            else 
            {
                if (vehicle is IOwnershipRequestable requestable)
                {
                    requestable.OnNetworkObjectOwnershipRequestResponse += OnOwnershipRequestResponse;
                }
                
                var status = targetNetObj.RequestOwnership();
                
                if (status != NetworkObject.OwnershipRequestStatus.RequestSent)
                {
                    if (vehicle is IOwnershipRequestable r) r.OnNetworkObjectOwnershipRequestResponse -= OnOwnershipRequestResponse;
                    _targetVehicle = null;
                }
            }
        }

        private void OnOwnershipRequestResponse(NetworkBehaviour behaviour, NetworkObject.OwnershipRequestResponseStatus status)
        {
            if (behaviour is IOwnershipRequestable requestable)
            {
                requestable.OnNetworkObjectOwnershipRequestResponse -= OnOwnershipRequestResponse;
            }

            if (status == NetworkObject.OwnershipRequestResponseStatus.Approved)
            {
                if (behaviour is VehicleController vehicle)
                {
                    RequestSeatEntry(vehicle);
                }
            }
            else
            {
                _targetVehicle = null;
            }
        }

        private void RequestSeatEntry(VehicleController vehicle)
        {
            _pendingVehicle = vehicle;
            vehicle.RequestSeat(OwnerClientId, OnSeatResponse);
        }

        private void OnSeatResponse(bool success, int seatIndex, bool isDriver)
        {
            if (!success)
            {
                _pendingVehicle = null;
                _targetVehicle = null;
                return;
            }
            FinalizeEnterVehicle();
        }

        private void FinalizeEnterVehicle()
        {
            if (_pendingVehicle == null) return;
            
            _currentVehicle = _pendingVehicle;
            _pendingVehicle = null;
            _targetVehicle = null;
            if (_currentVehicle.NetworkObject == null)
            {
                return;
            }
            _vehicleRef.Value = new NetworkObjectReference(_currentVehicle.NetworkObject);
        }

        public void ExitVehicle()
        {   
            if (!IsOwner) return;
            _currentVehicle.RequestExit(OwnerClientId);
            _vehicleRef.Value = default;
        }
    }
}