using Game.Input;
using Game.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerOnFootState : PlayerStateBase
    {
        private PlayerController _playerController;
        private PlayerInput _playerInput;
        private Rigidbody _rb;
        
        public override void OnEnter()
        {
            _playerController = StateMachine.playerController;
            _playerInput = StateMachine.playerInput;
            _rb  = StateMachine.rb;
            _playerController.enabled = true;
            _playerInput.enabled=true;
            _rb.isKinematic = false;
            InputSystemManager.Instance.EnableOnFootInputs();
            GameInput.Actions.Player.Jump.performed +=OnJumped;
        }

        public override void OnNetworkUpdate()
        {
            var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            _playerController.SetMovement(moveInput);
            var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
            _playerController.SetSprint(isSprinting);
        }
        
        void OnJumped(InputAction.CallbackContext _)
        {
            _playerController.SetJump(true);
        }

        public override void OnNetworkFixedUpdate()
        {
            _playerController.OnFixedUpdate();
        }

        public override void OnExit()
        {
            GameInput.Actions.Player.Jump.performed -=OnJumped;
            base.OnExit();
        }
    }
}
