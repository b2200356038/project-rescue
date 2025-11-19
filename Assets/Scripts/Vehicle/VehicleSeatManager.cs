using Unity.Netcode;
using UnityEngine;

namespace Game.Vehicle
{
    public class VehicleSeatManager : NetworkBehaviour
    {
        [System.Serializable]
        public class VehicleSeat
        {
            public Transform seatTransform;
            public bool isDriverSeat;
            public ulong occupantClientId = ulong.MaxValue; 
            public bool IsOccupied => occupantClientId != ulong.MaxValue;
        }

        [Header("Seat Configuration")]
        [SerializeField] private VehicleSeat[] seats;
        
        [Header("Exit Point")]
        [SerializeField] private Transform exitPoint;

        private NetworkList<ulong> _occupiedSeats;

        private void Awake()
        {
            _occupiedSeats = new NetworkList<ulong>(
                writePerm: NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                for (int i = 0; i < seats.Length; i++)
                    _occupiedSeats.Add(ulong.MaxValue);
            }
            _occupiedSeats.OnListChanged += OnSeatsChanged;
        }

        public override void OnNetworkDespawn()
        {
            _occupiedSeats.OnListChanged -= OnSeatsChanged;
            base.OnNetworkDespawn();
        }

        private void OnSeatsChanged(NetworkListEvent<ulong> changeEvent)
        {
            if (changeEvent.Index >= 0 && changeEvent.Index < seats.Length)
            {
                seats[changeEvent.Index].occupantClientId = changeEvent.Value;
            }
        }
        
        public void RequestEnterVehicle(ulong clientId)
        {
            RequestEnterVehicleServerRpc(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestEnterVehicleServerRpc(ulong clientId)
        {
            if (GetSeatIndex(clientId) != -1) return;
            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i].isDriverSeat && !seats[i].IsOccupied)
                {
                    OccupySeat(i, clientId);
                    return;
                }
            }
            for (int i = 0; i < seats.Length; i++)
            {
                if (!seats[i].isDriverSeat && !seats[i].IsOccupied)
                {
                    OccupySeat(i, clientId);
                    return;
                }
            }
        }

        public void RequestExitVehicle(ulong clientId)
        {
            RequestExitVehicleServerRpc(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestExitVehicleServerRpc(ulong clientId)
        {
            int seatIndex = GetSeatIndex(clientId);
            if (seatIndex != -1)
            {
                bool wasDriver = seats[seatIndex].isDriverSeat;
                _occupiedSeats[seatIndex] = ulong.MaxValue;
                seats[seatIndex].occupantClientId = ulong.MaxValue;
                if (wasDriver)
                {
                    NetworkObject.RemoveOwnership();
                }
            }
        }

        private void OccupySeat(int index, ulong clientId)
        {
            _occupiedSeats[index] = clientId;
            seats[index].occupantClientId = clientId;
            if (seats[index].isDriverSeat)
            {
                NetworkObject.ChangeOwnership(clientId);
            }
        }
        
        public int GetSeatIndex(ulong clientId)
        {
            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i].occupantClientId == clientId) return i;
            }
            return -1;
        }

        public Transform GetSeatTransform(ulong clientId)
        {
            int index = GetSeatIndex(clientId);
            return index != -1 ? seats[index].seatTransform : null;
        }

        public Transform GetExitPoint() => exitPoint ? exitPoint : transform;

        public bool IsDriverSeatEmpty()
        {
            foreach (var seat in seats)
                if (seat.isDriverSeat && !seat.IsOccupied) return true;
            return false;
        }
        
        public bool HasEmptySeats()
        {
            foreach (var seat in seats) if (!seat.IsOccupied) return true;
            return false;
        }
    }
}