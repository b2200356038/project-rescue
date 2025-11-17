using Game.Input;
using Game.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerOnFootState : PlayerStateBase
    {
        private PhysicsPlayerController _physicsPlayerController;
        private PlayerInput _playerInput;
        private Rigidbody _rb;
        
        public override void OnEnter()
        {
            _physicsPlayerController = StateMachine.physicsPlayerController;
            _playerInput = StateMachine.playerInput;
            _rb  = StateMachine.rb;
            _physicsPlayerController.enabled = true;
            _playerInput.enabled=true;
            _rb.isKinematic = false;
            InputSystemManager.Instance.EnableOnFootInputs();
            GameInput.Actions.Player.Jump.performed +=OnJumped;
        }

        public override void OnNetworkUpdate()
        {
            var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            _physicsPlayerController.SetMovement(moveInput);
            var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
            _physicsPlayerController.SetSprint(isSprinting);
        }
        
        void OnJumped(InputAction.CallbackContext _)
        {
            _physicsPlayerController.SetJump(true);
        }

        public override void OnNetworkFixedUpdate()
        {
            _physicsPlayerController.OnFixedUpdate();
        }

        public override void OnExit()
        {
            GameInput.Actions.Player.Jump.performed -=OnJumped;
            base.OnExit();
        }
    }
}
