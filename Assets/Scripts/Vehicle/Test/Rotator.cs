using UnityEngine;

namespace Game.Vehicle.Test
{
    public class Rotator : MonoBehaviour
    {public Vector3 rotation;
        private Rigidbody _rb;


        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }


        private void FixedUpdate()
        {
            _rb.MoveRotation(transform.rotation * Quaternion.Euler(rotation * Time.fixedDeltaTime));
        }
    }
}
