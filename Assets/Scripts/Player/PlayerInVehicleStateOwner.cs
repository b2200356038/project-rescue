using Game.Input;
using Game.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerInVehicleStateOwner : PlayerStateBase
    {
        private PlayerVehicleHandler _vehicleHandler;
        private VehicleController _vehicle;
        private Transform _seatTransform;
        private bool _isDriver;

        internal override void Initialize(PlayerStateMachine playerStateMachine)
        {
            base.Initialize(playerStateMachine);
            _vehicleHandler = playerStateMachine.GetComponent<PlayerVehicleHandler>();
        }

        public override void OnEnter()
        {
            _vehicle = _vehicleHandler.CurrentVehicle;
            _isDriver = _vehicleHandler.IsDriver;
            _seatTransform = _vehicle.seatManager.GetSeatTransformByIndex(_vehicleHandler.SeatIndex);
            StateMachine.rb.isKinematic = true;
            StateMachine.playerCollider.enabled = false;
            StateMachine.playerController.enabled = false;
            SnapToSeat();
            if (_isDriver)
            {
                InputSystemManager.Instance.EnableVehicleInputs();
            }
            else
            {
                InputSystemManager.Instance.EnableUIInputs();
            }

            GameInput.Actions.Vehicle.Interact.performed += OnInteractPerformed;
        }

        public override void OnNetworkUpdate()
        {
            Debug.Log(_isDriver);
            Debug.Log(_vehicle==null);
            if (!_isDriver || _vehicle == null) return;
            var moveInput = GameInput.Actions.Vehicle.Move.ReadValue<Vector2>();
            _vehicle.SetMovement(moveInput);
            var handbrake = GameInput.Actions.Vehicle.Jump.IsPressed();
            _vehicle.SetHandbrake(handbrake);
        }

        public override void OnNetworkFixedUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            SnapToSeat();
        }

        private void SnapToSeat()
        {
            if (_seatTransform == null) return;
            StateMachine.transform.position = _seatTransform.position;
            StateMachine.transform.rotation = _seatTransform.rotation;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            _vehicleHandler.TryExitVehicle();
        }

        public override void OnExit()
        {
            base.OnExit();

            GameInput.Actions.Vehicle.Interact.performed -= OnInteractPerformed;
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