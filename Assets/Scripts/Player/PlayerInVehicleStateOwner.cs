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
            StateMachine.rb.isKinematic = true;
            StateMachine.playerCollider.enabled = false;
            StateMachine.playerController.enabled = false;

            if (_vehicle != null && _seatTransform != null)
            {
                StateMachine.transform.SetParent(_vehicle.transform);
                Vector3 seatLocalPos = _vehicle.transform.InverseTransformPoint(_seatTransform.position);
                Quaternion seatLocalRot = Quaternion.Inverse(_vehicle.transform.rotation) * _seatTransform.rotation;
                StateMachine.transform.localPosition = seatLocalPos;
                StateMachine.transform.localRotation = seatLocalRot;
            }

            InputSystemManager.Instance.EnableVehicleInputs();
            GameInput.Actions.Vehicle.Interact.performed += OnInteractPerformed;
        }

        public override void OnNetworkUpdate()
        {
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
            
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            _vehicleHandler.ExitVehicle();
        }

        public override void OnExit()
        {
            base.OnExit();

            GameInput.Actions.Vehicle.Interact.performed -= OnInteractPerformed;
            StateMachine.transform.SetParent(null);

            if (_vehicle != null)
            {
                Transform exitPoint = _vehicle.SeatManager.GetExitPoint();
                StateMachine.transform.position = exitPoint.position;
                StateMachine.transform.rotation = Quaternion.identity;
            }
            
            StateMachine.rb.isKinematic = false;
            StateMachine.playerCollider.enabled = true;
            _vehicle = null;
            _seatTransform = null;
        }
    }
}