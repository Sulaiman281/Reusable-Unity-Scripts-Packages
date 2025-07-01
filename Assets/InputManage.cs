using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputManage : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SprintInput { get; private set; }

    [Header("Input Events")]
    public UnityEvent<Vector2> OnMoveInput;
    public UnityEvent<Vector2> OnLookInput;
    public UnityEvent<bool> OnJumpInput;
    public UnityEvent<bool> OnSprintInput;

    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        // Move Input
        Vector2 move = _playerInput.actions["Move"].ReadValue<Vector2>();
        if (move != MoveInput)
        {
            MoveInput = move;
            OnMoveInput?.Invoke(MoveInput);
        }

        // Look Input
        Vector2 look = _playerInput.actions["Look"].ReadValue<Vector2>();
        if (look != LookInput)
        {
            LookInput = look;
            OnLookInput?.Invoke(LookInput);
        }
    }

    // These methods must be assigned in PlayerInput's "Events" tab for Jump and Sprint actions
    public void OnJump(InputValue value)
    {
        Debug.Log("Jump Input: " + value.isPressed);
        JumpInput = value.isPressed;
        OnJumpInput?.Invoke(JumpInput);
    }

    public void OnSprint(InputValue value)
    {
        Debug.Log("Sprint Input: " + value.isPressed);
        SprintInput = value.isPressed;
        OnSprintInput?.Invoke(SprintInput);
    }
}
