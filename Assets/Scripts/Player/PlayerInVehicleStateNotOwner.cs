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
            StateMachine.rb.isKinematic = true;
            StateMachine.playerCollider.enabled = false;
            if (_vehicle != null && _seatTransform != null)
            {
                StateMachine.transform.SetParent(_vehicle.transform);
                Vector3 seatLocalPos = _vehicle.transform.InverseTransformPoint(_seatTransform.position);
                Quaternion seatLocalRot = Quaternion.Inverse(_vehicle.transform.rotation) * _seatTransform.rotation;
                StateMachine.transform.localPosition = seatLocalPos;
                StateMachine.transform.localRotation = seatLocalRot;
            }
        }


        public override void OnNetworkLateUpdate()
        {
            base.OnNetworkLateUpdate();
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