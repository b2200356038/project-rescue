using UnityEngine;

namespace Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float followSpeed = 20f;

        public void SetTarget(Transform t)
        {
            target = t;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            transform.position = Vector3.Lerp(
                transform.position,
                target.position + offset,
                followSpeed * Time.deltaTime);
        }
    }
}
