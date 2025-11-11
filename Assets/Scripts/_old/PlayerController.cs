using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputReader input;
    private CharacterController _controller;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        input = GetComponent<InputReader>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            input.enabled = true;
            GetComponent<PlayerInput>().enabled = true;
        }
    }

    private void Update()
    {
        if (!IsOwner|| !IsSpawned)
        {
            return;
        }
        Vector3 move = new Vector3(input.move.x, 0, input.move.y);
        _controller.Move(move * moveSpeed * Time.deltaTime);
    }
}