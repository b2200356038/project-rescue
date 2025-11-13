using System;
using UnityEngine;

namespace Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsPlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private PhysicsPlayerControllerSettings physicsPlayerControllerSettings;
        public bool Grounded { get; private set; }
        private RaycastHit[] _raycastHits = new RaycastHit[1];
        private Ray _ray;

        private Vector3 _movement;
        private bool _jump;
        private bool _sprint;

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
                physicsPlayerControllerSettings.GroundCheckDistance) > 0;
        }

        private void ApplyMovement()
        {
            if (Mathf.Approximately(_movement.magnitude, 0f))
            {
                return;
            }

            var velocity = rb.linearVelocity;
            var desiredVelocity = _movement * (_sprint
                ? physicsPlayerControllerSettings.SprintSpeed
                : physicsPlayerControllerSettings.WalkSpeed);

            var targetVelocity = new Vector3(desiredVelocity.x, velocity.y, desiredVelocity.z);
            var velocityChange = targetVelocity - velocity;

            if (Grounded)
            {
                var force = velocityChange * physicsPlayerControllerSettings.Acceleration;
                rb.AddForce(force, ForceMode.Force);
            }
            else
            {
                var force = velocityChange * physicsPlayerControllerSettings.Acceleration *
                            physicsPlayerControllerSettings.AirControlFactor;
                rb.AddForce(force, ForceMode.Force);
            }
            

            _movement = Vector3.zero;
        }



        private void ApplyJump()
        {
            if (_jump && Grounded)
            {          
                rb.AddForce(Vector3.up *physicsPlayerControllerSettings.JumpImpusle, ForceMode.Impulse);
                _jump = false;
            }
        }

        private void ApplyDrag()
        {
            var groundVelocity = rb.linearVelocity;
            groundVelocity.y = 0f;
            if (groundVelocity.magnitude > 0f)
            {
                var dragForce = -physicsPlayerControllerSettings.DragCoefficient * groundVelocity.magnitude * groundVelocity;
                rb.AddForce(dragForce, ForceMode.Acceleration);
            }
        }

        private void ApplyCustomGravity()
        {
            var customGravity = UnityEngine.Physics.gravity * (physicsPlayerControllerSettings.CustomGravityMultiplier - 1);
            rb.AddForce(customGravity, ForceMode.Acceleration);
        }

        public void SetMovement(Vector3 movement)
        {
            _movement = movement;
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