using Game.Vehicle;
using UnityEngine;

namespace Game.Player
{
    public class PlayerInVehicleStateNotOwner : PlayerStateBase
    {
        private PlayerVehicleHandler _vehicleHandler;
        private VehicleController _vehicle;
        private Transform _seatTransform;

        internal override void Initialize(PlayerStateMachine playerStateMachine)
        {
            base.Initialize(playerStateMachine);
            _vehicleHandler = playerStateMachine.GetComponent<PlayerVehicleHandler>();
        }

        public override void OnEnter()
        {
            _vehicle = _vehicleHandler.CurrentVehicle;
            _seatTransform = _vehicle?.SeatManager.GetSeatTransformByIndex(_vehicleHandler.SeatIndex);
            StateMachine.rb.isKinematic = true;
            StateMachine.playerCollider.enabled = false;
            SnapToSeat();
        }
        

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();
            SnapToSeat();
        }

        private void SnapToSeat()
        {
            if (_seatTransform == null) return;
            StateMachine.transform.position = _seatTransform.position;
            StateMachine.transform.rotation = _seatTransform.rotation;
        }

        public override void OnExit()
        {
            base.OnExit();

            if (_vehicle != null)
            {
                Transform exitPoint = _vehicle.SeatManager.GetExitPoint();
                StateMachine.transform.position = exitPoint.position;
                StateMachine.transform.rotation = exitPoint.rotation;
            }
            StateMachine.rb.isKinematic = false;
            StateMachine.playerCollider.enabled = true;
            _vehicle = null;
            _seatTransform = null;
        }
    }
}