using Game.Input;
using Game.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerVehicleState : PlayerStateBase
    {
        private PlayerController _playerController;
        private PlayerInput _playerInput;
        private VehicleController _vehicleController;

        public void SetVehicle(VehicleController vehicleController)
        {
            _vehicleController = vehicleController;
        }

        public override void OnEnter()
        {
            _playerController = StateMachine.playerController;
            _playerInput = StateMachine.playerInput;
            if (_playerController != null) _playerController.enabled = false;
            if (_playerInput != null) _playerInput.enabled = false;
            InputSystemManager.Instance.EnableVehicleInputs();
            GameInput.Actions.Vehicle.Interact.performed += OnExitRequested;
        }

        public override void OnNetworkUpdate()
        {
            if (_vehicleController == null) return;
            var moveInput = GameInput.Actions.Vehicle.Move.ReadValue<Vector2>();
            _vehicleController.SetMovement(moveInput);
            var handbrakeInput = GameInput.Actions.Vehicle.Jump.ReadValue<float>() > 0f;
            _vehicleController.SetHandbrake(handbrakeInput);
        }

        public override void OnNetworkFixedUpdate()
        {
        }

        void OnExitRequested(InputAction.CallbackContext context)
        {
           // StateMachine.ExitVehicle();
        }

        public override void OnExit()
        { 
            GameInput.Actions.Vehicle.Interact.performed -= OnExitRequested;
            if (_vehicleController != null)
            {
                _vehicleController.ResetInputs();
            }
            if (_playerController != null) _playerController.enabled = true;
            if (_playerInput != null) _playerInput.enabled = true;
            InputSystemManager.Instance.EnableOnFootInputs();
            base.OnExit();
        }
    }
}