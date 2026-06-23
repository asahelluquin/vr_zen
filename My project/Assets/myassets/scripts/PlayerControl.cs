using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    CharacterController controller;
    Camera cam;
    float rotX = 0f;
    Vector3 velocity;
    bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // -- Detectar si esta en el suelo --
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // pequeno valor negativo para mantenerse pegado al suelo

        // -- Movimiento --
        Vector2 move = Keyboard.current != null ? new Vector2(
            (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0),
            (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0)
        ) : Vector2.zero;

        Vector3 direction = transform.right * move.x + transform.forward * move.y;
        controller.Move(direction * speed * Time.deltaTime);

        // -- Salto --
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // -- Gravedad --
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // -- Camara --
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.1f;
            rotX -= mouseDelta.y;
            rotX = Mathf.Clamp(rotX, -80f, 80f);
            cam.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
            transform.Rotate(Vector3.up * mouseDelta.x);
        }

        // -- Soltar cursor --
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
    }
}