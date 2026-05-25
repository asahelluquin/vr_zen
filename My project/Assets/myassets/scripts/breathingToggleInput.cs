using UnityEngine;
using UnityEngine.InputSystem;

public class BreathingToggleInput : MonoBehaviour
{
    [SerializeField] private InputActionReference toggleAction; // un botón del mando
    [SerializeField] private BreathingGuide guide;

    private void OnEnable()  { toggleAction.action.performed += OnPress; toggleAction.action.Enable(); }
    private void OnDisable() { toggleAction.action.performed -= OnPress; }
    private void OnPress(InputAction.CallbackContext _) => guide.ToggleBreathing();
}