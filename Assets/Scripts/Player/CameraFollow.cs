using UnityEngine;

namespace Game.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target to follow")]
        public Transform target;

        [Header("Offsets")]
        public Vector3 positionOffset = new Vector3(0, 5f, -8f);
        public float positionSmoothTime = 0.1f;
        
        public float rotationSpeed = 10f;
        private Vector3 _velocity = Vector3.zero;

        private void Awake()
        {
            if (target != null)
            {
                positionOffset = transform.position - target.position;
            }
        }

        public void SetTarget(Transform t)
        {
            target = t;
            if (target != null)
            {
                positionOffset = transform.position - target.position;
            }
        }

        private void FixedUpdate()
        {
            if (target == null) return;
            
            Vector3 desiredPosition = target.position + positionOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, positionSmoothTime);
            
            Vector3 directionToTarget = target.position - transform.position;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }
    }
}