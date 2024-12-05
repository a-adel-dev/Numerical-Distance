using UnityEngine.InputSystem;

public class PlayerInput
{
    public class KeyboardActions
    {
        public InputAction AnyKey { get; private set; }

        public void Enable()
        {
            AnyKey = new InputAction(binding: "<Keyboard>/*");
            AnyKey.Enable();
        }
    }

    public KeyboardActions keyboardActions { get; private set; }

    public PlayerInput()
    {
        keyboardActions = new KeyboardActions();
    }
}