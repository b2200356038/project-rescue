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
        [SerializeField] private Transform exitPoint;

        private NetworkList<ulong> _occupiedSeats;

        private void Awake()
        {
            _occupiedSeats = new NetworkList<ulong>(writePerm: NetworkVariableWritePermission.Owner);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (HasAuthority)
            {
                for (int i = 0; i < seats.Length; i++)
                {
                    _occupiedSeats.Add(ulong.MaxValue);
                }
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

        #region Seat Queries

        public bool TryGetAvailableSeat(out int seatIndex, out bool isDriverSeat)
        {
            seatIndex = -1;
            isDriverSeat = false;

            if (IsDriverSeatEmpty())
            {
                seatIndex = GetDriverSeatIndex();
                isDriverSeat = true;
                return true;
            }

            int emptySeat = GetFirstEmptyPassengerSeatIndex();
            if (emptySeat != -1)
            {
                seatIndex = emptySeat;
                return true;
            }

            return false;
        }

        public int GetSeatIndex(ulong clientId)
        {
            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i].occupantClientId == clientId)
                    return i;
            }
            return -1;
        }

        public int GetDriverSeatIndex()
        {
            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i].isDriverSeat)
                    return i;
            }
            return -1;
        }

        public int GetFirstEmptyPassengerSeatIndex()
        {
            for (int i = 0; i < seats.Length; i++)
            {
                if (!seats[i].isDriverSeat && !seats[i].IsOccupied)
                    return i;
            }
            return -1;
        }

        public bool IsDriverSeatEmpty()
        {
            foreach (var seat in seats)
            {
                if (seat.isDriverSeat && !seat.IsOccupied)
                    return true;
            }
            return false;
        }

        public bool HasEmptySeats()
        {
            foreach (var seat in seats)
            {
                if (!seat.IsOccupied)
                    return true;
            }
            return false;
        }

        public bool IsDriverSeat(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= seats.Length) return false;
            return seats[seatIndex].isDriverSeat;
        }

        public bool IsSeatEmpty(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= seats.Length) return false;
            return !seats[seatIndex].IsOccupied;
        }

        #endregion

        #region Seat Operations

        public void ClaimSeat(int seatIndex, ulong clientId)
        {
            if (seatIndex < 0 || seatIndex >= seats.Length) return;

            _occupiedSeats[seatIndex] = clientId;
            seats[seatIndex].occupantClientId = clientId;
        }

        public void ReleaseSeat(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= seats.Length) return;

            _occupiedSeats[seatIndex] = ulong.MaxValue;
            seats[seatIndex].occupantClientId = ulong.MaxValue;
        }

        #endregion

        #region Transform Getters

        public Transform GetSeatTransform(ulong clientId)
        {
            int index = GetSeatIndex(clientId);
            return index != -1 ? seats[index].seatTransform : null;
        }

        public Transform GetSeatTransformByIndex(int index)
        {
            if (index >= 0 && index < seats.Length)
                return seats[index].seatTransform;
            return null;
        }

        public Transform GetExitPoint()
        {
            return exitPoint != null ? exitPoint : transform;
        }

        #endregion
    }
}