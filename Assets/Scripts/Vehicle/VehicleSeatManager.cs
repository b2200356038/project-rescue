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

        [Header("Seat Configuration")] [SerializeField]
        private VehicleSeat[] seats;

        [SerializeField] private Transform exitPoint;
        private NetworkList<ulong> _occupiedSeats;
        private void Awake()
        {
            _occupiedSeats = new NetworkList<ulong>(writePerm: NetworkVariableWritePermission.Server);
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (HasAuthority)
            {
                for (int i = 0; i < seats.Length; i++) _occupiedSeats.Add(ulong.MaxValue);
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
                seats[changeEvent.Index].occupantClientId = changeEvent.Value;
        }

        public void ClaimDriverSeat(ulong clientId)
        {
            int driverSeatIndex = GetDriverSeatIndex();
            if (driverSeatIndex != -1) UpdateDriverSeat(driverSeatIndex, clientId);
        }
        
        private void UpdateDriverSeat(int seatIndex, ulong clientId)
        {
            _occupiedSeats[seatIndex] = clientId;
            seats[seatIndex].occupantClientId = clientId;
        }

        public void RequestEnterPassenger(ulong clientId)
        {
            RequestEnterPassengerServerRpc(clientId);
        }

        [Rpc(SendTo.Authority)]
        private void RequestEnterPassengerServerRpc(ulong clientId)
        {
            for (int i = 0; i < seats.Length; i++)
            {
                if (!seats[i].isDriverSeat && !seats[i].IsOccupied)
                {
                    _occupiedSeats[i] = clientId;
                    seats[i].occupantClientId = clientId;
                    return;
                }
            }
        }
        

        public int GetSeatIndex(ulong clientId)
        {
            for (int i = 0; i < seats.Length; i++)
                if (seats[i].occupantClientId == clientId)
                    return i;
            return -1;
        }

        public int GetDriverSeatIndex()
        {
            for (int i = 0; i < seats.Length; i++)
                if (seats[i].isDriverSeat)
                    return i;
            return -1;
        }

        public int GetFirstEmptyPassengerSeatIndex()
        {
            for (int i = 0; i < seats.Length; i++)
                if (!seats[i].isDriverSeat && !seats[i].IsOccupied)
                    return i;
            return -1;
        }

        public Transform GetSeatTransform(ulong clientId)
        {
            int index = GetSeatIndex(clientId);
            return index != -1 ? seats[index].seatTransform : null;
        }

        public Transform GetSeatTransformByIndex(int index)
        {
            if (index >= 0 && index < seats.Length) return seats[index].seatTransform;
            return null;
        }

        public Transform GetExitPoint() => exitPoint ? exitPoint : transform;

        public bool IsDriverSeatEmpty()
        {
            foreach (var seat in seats)
                if (seat.isDriverSeat && !seat.IsOccupied)
                    return true;
            return false;
        }

        public bool HasEmptySeats()
        {
            foreach (var seat in seats)
                if (!seat.IsOccupied)
                    return true;
            return false;
        }
    }
}