using UnityEngine;

namespace Game.Vehicle.Test
{
    public class Oscillator : MonoBehaviour
    {
        public Vector3 travel;
        public float speed = 1f;

        private Vector3 initPos;
        private float time;

        private Rigidbody _rb;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            initPos = transform.position;
        }

        void FixedUpdate()
        {
            time += Time.fixedDeltaTime * speed;
            _rb.MovePosition(initPos + travel * Mathf.Sin(time));
        }
    }
}
