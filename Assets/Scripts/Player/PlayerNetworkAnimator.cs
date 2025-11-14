using System;
using Game.Input;
using Game.Physics;
using Unity.Netcode.Components;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(PhysicsPlayerController))]
    public class PlayerNetworkAnimator : NetworkAnimator
    {
        [SerializeField] private PhysicsPlayerController physicsPlayerController;

        static readonly int GroundedId = Animator.StringToHash("Grounded");
        static readonly int MoveId = Animator.StringToHash("Move");
        static readonly int JumpId = Animator.StringToHash("Jump");
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            physicsPlayerController.PlayerJumped += OnPlayerJumped;
        }

        private void OnPlayerJumped()
        {
            SetTrigger(JumpId);
        }
        
        void LateUpdate()
        {
            if (!HasAuthority)
            {
                return;
            }
            Animator.SetBool(GroundedId, physicsPlayerController.Grounded);
            var moveInput = GameInput.Actions.Player.Move.ReadValue<Vector2>();
            var isSprinting = GameInput.Actions.Player.Sprint.ReadValue<float>() > 0f;
            Animator.SetFloat(MoveId, moveInput.magnitude * (isSprinting ? 2f : 1f));
        }
    }
    
}
