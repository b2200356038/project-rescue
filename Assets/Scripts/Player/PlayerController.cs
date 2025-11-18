using System;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private PlayerControllerSettings playerControllerSettings;
        public bool Grounded { get; private set; }
        private RaycastHit[] _raycastHits = new RaycastHit[1];
        private Ray _ray;

        private Vector3 _movement;
        private bool _jump;
        private bool _sprint;
        
        internal event Action PlayerJumped;

        internal void OnFixedUpdate()
        {
            if (rb != null && rb.isKinematic)
            {
                return;
            }
            UpdateGroundedStatus();
            ApplyMovement();
            ApplyJump();
            ApplyDrag();
            ApplyCustomGravity();
        }


        private void UpdateGroundedStatus()
        {
            Grounded = IsGrounded();
        }

        private bool IsGrounded()
        { 
            _ray.origin = rb.worldCenterOfMass;
            _ray.direction = Vector3.down;
            return UnityEngine.Physics.RaycastNonAlloc(_ray, _raycastHits,
                playerControllerSettings.GroundCheckDistance) > 0;
        }

        private void ApplyMovement()
        {
            if (Mathf.Approximately(_movement.magnitude, 0f))
            {
                return;
            }

            var velocity = rb.linearVelocity;
            var desiredVelocity = _movement * (_sprint
                ? playerControllerSettings.SprintSpeed
                : playerControllerSettings.WalkSpeed);

            var targetVelocity = new Vector3(desiredVelocity.x, velocity.y, desiredVelocity.z);
            var velocityChange = targetVelocity - velocity;

            if (Grounded)
            {
                var force = velocityChange * playerControllerSettings.Acceleration;
                rb.AddForce(force, ForceMode.Force);
            }
            else
            {
                var force = velocityChange * playerControllerSettings.Acceleration *
                            playerControllerSettings.AirControlFactor;
                rb.AddForce(force, ForceMode.Force);
            }
            

            _movement = Vector3.zero;
        }



        private void ApplyJump()
        {
            if (_jump && Grounded)
            {          
                rb.AddForce(Vector3.up *playerControllerSettings.JumpImpusle, ForceMode.Impulse);
                PlayerJumped?.Invoke();
            }
            _jump = false;
        }

        private void ApplyDrag()
        {
            var groundVelocity = rb.linearVelocity;
            groundVelocity.y = 0f;
            if (groundVelocity.magnitude > 0f)
            {
                var dragForce = -playerControllerSettings.DragCoefficient * groundVelocity.magnitude * groundVelocity;
                rb.AddForce(dragForce, ForceMode.Acceleration);
            }
        }

        private void ApplyCustomGravity()
        {
            var customGravity = UnityEngine.Physics.gravity * (playerControllerSettings.CustomGravityMultiplier - 1);
            rb.AddForce(customGravity, ForceMode.Acceleration);
        }

        public void SetMovement(Vector2 moveInput)
        {
            _movement = transform.forward * moveInput.y + transform.right * moveInput.x;
        }

        public void SetJump(bool jump)
        {
            if (!_jump)
            {
                _jump = jump;
            }
        }

        public void SetSprint(bool sprint)
        {
            _sprint = sprint;
        }
    }
}