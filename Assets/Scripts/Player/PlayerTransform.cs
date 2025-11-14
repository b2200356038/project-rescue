using System;
using Input;
using Physics;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(Rigidbody))]
    class PlayerTransform : PhysicsObjectMotion, INetworkUpdateSystem
    {
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PhysicsPlayerController physicsPlayerController;
        private NetworkVariable<FixedString32Bytes> _playerId = new NetworkVariable<FixedString32Bytes>();
        Camera _mainCamera;
        public override void OnNetworkSpawn()
        {
            if (!HasAuthority)
            {
                base.OnNetworkSpawn();
                return;
            }
            var camFollow = Camera.main?.GetComponent<CameraFollow>();
            if (camFollow != null)
                camFollow.SetTarget(transform);
            _playerId.Value= new FixedString32Bytes(AuthenticationService.Instance.PlayerId);
            playerInput.enabled = true;
            physicsPlayerController.enabled = true;
            GameInput.Actions.Player.Jump.performed += OnJumped;
            Rigidbody.isKinematic = false;
            Rigidbody.freezeRotation = true;
            Rigidbody.linearVelocity = Vector3.zero;
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {  
            base.OnNetworkDespawn();
            GameInput.Actions.Player.Jump.performed -= OnJumped;
            this.UnregisterAllNetworkUpdates();
        }

        void OnJumped(InputAction.CallbackContext _)
        {
            physicsPlayerController.SetJump(true);
        }
        
        void OnTransformUpdate()
        {
            var forward = transform.forward;
            var right = transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            var movement = forward * moveInput.y + right * moveInput.x;
            physicsPlayerController.SetMovement(movement);
            var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
            physicsPlayerController.SetSprint(isSprinting);
        }
        
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    OnTransformUpdate();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    physicsPlayerController.OnFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }
        
    }
}
