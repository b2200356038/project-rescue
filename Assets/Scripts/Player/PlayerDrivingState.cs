using Game.Input;
using Game.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerDrivingState : PlayerStateBase
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
            _playerController.enabled = false;
            _playerInput.enabled = false;
            if (_vehicleController != null)
            {
                _vehicleController.enabled = true;
            }
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
            if (_vehicleController == null) return;
            _vehicleController.OnFixedUpdate();
        }

        void OnExitRequested(InputAction.CallbackContext _)
        {
            if (_vehicleController == null) return;
        }

        public override void OnExit()
        { 
            GameInput.Actions.Vehicle.Interact.performed -= OnExitRequested;
            if (_vehicleController != null)
            {
                _vehicleController.ResetInputs();
                _vehicleController.enabled = false;
            }
            _playerController.enabled = true;
            _playerInput.enabled = true;
            InputSystemManager.Instance.EnableOnFootInputs();
            base.OnExit();
        }
    }
}