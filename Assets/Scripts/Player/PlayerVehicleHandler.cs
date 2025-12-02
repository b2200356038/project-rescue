using System;
using Game.Vehicle;
using Unity.Netcode;

namespace Game.Player
{
    public class PlayerVehicleHandler : NetworkBehaviour
    {
        private readonly NetworkVariable<NetworkObjectReference> _vehicleRef = new(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<int> _seatIndex = new(-1, 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private VehicleController _currentVehicle;

        public VehicleController CurrentVehicle => _currentVehicle;
        public int SeatIndex => _seatIndex.Value;
        public bool IsInVehicle => _currentVehicle != null;

        public event Action<VehicleController, int, bool> OnEnteredVehicle;
        public event Action OnExitedVehicle;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _vehicleRef.OnValueChanged += OnVehicleRefChanged;

            if (_vehicleRef.Value.TryGet(out NetworkObject netObj))
            {
                _currentVehicle = netObj.GetComponent<VehicleController>();
            }
        }

        private void OnVehicleRefChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
        {
            if (newValue.TryGet(out NetworkObject netObj))
            {
                _currentVehicle = netObj.GetComponent<VehicleController>();
            }
            else
            {
                _currentVehicle = null;
            }
        }

        public void TryEnterVehicle(VehicleController vehicle)
        {
            if (!IsOwner) return;

            vehicle.RequestEnter(OwnerClientId, (success, seatIndex, isDriver) =>
            {
                if (success)
                {
                    _vehicleRef.Value = vehicle.NetworkObject;
                    _seatIndex.Value = seatIndex;
                    _currentVehicle = vehicle;
                    OnEnteredVehicle?.Invoke(vehicle, seatIndex, isDriver);
                }
            });
        }

        public void TryExitVehicle()
        {
            if (!IsOwner || !IsInVehicle) return;

            _currentVehicle.RequestExit(OwnerClientId, (success) =>
            {
                if (success)
                {
                    _vehicleRef.Value = default;
                    _seatIndex.Value = -1;
                    _currentVehicle = null;
                    OnExitedVehicle?.Invoke();
                }
            });
        }

        public override void OnNetworkDespawn()
        {
            _vehicleRef.OnValueChanged -= OnVehicleRefChanged;
            base.OnNetworkDespawn();
        }
    }
}