using UnityEngine;

namespace Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset;

        private void Awake()
        {
            if (target != null)
            {
                offset = transform.position - target.position;
            }
        }

        public void SetTarget(Transform t)
        {
            target = t;
            offset = transform.position - target.position;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            transform.position = target.position + offset;
        }
    }
}