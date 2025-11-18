using UnityEngine;

namespace Game.Vehicle
{
    public class VehicleSeat : MonoBehaviour
    {
        [SerializeField] private Transform seatTransform;
        [SerializeField] private bool isDriverSeat;
        [SerializeField] private int seatIndex;

        private bool _isOccupied;
        private ulong _occupantClientId;

        public Transform SeatTransform => seatTransform;
        public bool IsDriverSeat => isDriverSeat;
        public int SeatIndex => seatIndex;
        public bool IsOccupied => _isOccupied;
        public ulong OccupantClientId => _occupantClientId;

        public void Initialize()
        {
            _isOccupied = false;
        }

        public void SetOccupant(ulong clientId)
        {
            _isOccupied = true;
            _occupantClientId = clientId;
        }

        public void ClearOccupant()
        {
            _isOccupied = false;
            _occupantClientId = 0;
        }
    }
}
