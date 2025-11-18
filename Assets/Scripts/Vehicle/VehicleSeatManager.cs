using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Game.Vehicle
{
    public class VehicleSeatManager : NetworkBehaviour
    {
        [Header("Vehicle Seats")]
        [SerializeField] private VehicleSeat[] seats;
        
        [Header("Exit")]
        [SerializeField] private Transform exitPoint;

        private NetworkVariable<bool> _isDriverSeatOccupied = new NetworkVariable<bool>(false);
        private Dictionary<ulong, VehicleSeat> _clientSeatMap = new Dictionary<ulong, VehicleSeat>();
        public bool IsDriverSeatOccupied => _isDriverSeatOccupied.Value;

        void Awake()
        {
            InitializeSeats();
        }

        private void InitializeSeats()
        {
            foreach (var seat in seats)
            {
                seat.Initialize();
            }
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

        public bool TryEnterVehicle(ulong clientId)
        {
            VehicleSeat emptySeat = FindEmptySeat();
            
            if (emptySeat == null)
                return false;
            emptySeat.SetOccupant(clientId);
            _clientSeatMap[clientId] = emptySeat;
            if (emptySeat.IsDriverSeat)
            {
                if (IsServer)
                    _isDriverSeatOccupied.Value = true;
            }

            return true;
        }

        public void ExitVehicle(ulong clientId)
        {
            if (!_clientSeatMap.ContainsKey(clientId))
                return;

            VehicleSeat seat = _clientSeatMap[clientId];
            if (seat.IsDriverSeat && IsServer)
                _isDriverSeatOccupied.Value = false;
            seat.ClearOccupant();
            _clientSeatMap.Remove(clientId);
        }

        private VehicleSeat FindEmptySeat()
        {
            foreach (var seat in seats)
            {
                if (seat.IsDriverSeat && !seat.IsOccupied)
                    return seat;
            }
            foreach (var seat in seats)
            {
                if (!seat.IsDriverSeat && !seat.IsOccupied)
                    return seat;
            }

            return null;
        }

        public VehicleSeat GetPlayerSeat(ulong clientId)
        {
            return _clientSeatMap.ContainsKey(clientId) ? _clientSeatMap[clientId] : null;
        }

        public Transform GetSeatTransform(ulong clientId)
        {
            VehicleSeat seat = GetPlayerSeat(clientId);
            return seat != null ? seat.SeatTransform : null;
        }

        public Transform GetExitPoint()
        {
            return exitPoint;
        }
    }
}