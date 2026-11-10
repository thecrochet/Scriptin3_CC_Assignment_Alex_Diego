using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace YetAnotherCharacterSystem.Deprecated
{
    // This is necessary to interface between cinemachine inputs and the input system
    // This is mostly taken from the documentation here: https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/InputSystemComponents.html
    // None of this is part of the module really! But it's still important to know that this is needed in order to work with local multiplayer.
    // This is basically plug and play with any Unity project that uses a PlayerInput component for their source of inputs
    public class CinemachinePlayerInputHandler : InputAxisControllerBase<CinemachinePlayerInputHandler.Reader>
    {
        [Header("Input Source Override")]
        public PlayerInput PlayerInput;

        private void Awake()
        {
            // When the PlayerInput receives an input, send it to all the controllers
            if (PlayerInput == null)
            {
                TryGetComponent(out PlayerInput);
            }

            if (PlayerInput == null)
            {
                Debug.LogError("Cannot find PlayerInput component");
            }
            else
            {
                PlayerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

                PlayerInput.onActionTriggered += (InputAction.CallbackContext value) =>
                {
                    for (int i = 0; i < Controllers.Count; i++)
                        Controllers[i].Input.ProcessInput(value.action);
                };
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                UpdateControllers();
            }
        }

        [Serializable]
        public class Reader : IInputAxisReader
        {
            /// <summary>The input value is multiplied by this amount prior to processing.
            /// Controls the input power.  Set it to a negative value to invert the input</summary>
            [Tooltip("The input value is multiplied by this amount prior to processing.  "
                + "Controls the input power.  Set it to a negative value to invert the input")]
            public float Gain = 1;

            public InputActionReference Input;
            Vector2 _cachedInputValue;

            public void ProcessInput(InputAction action)
            {
                // If it's my action then cache the new value
                if (Input != null && Input.action.id == action.id)
                {
                    if (action.expectedControlType == "Vector2")
                    {
                        _cachedInputValue = action.ReadValue<Vector2>() * Gain;
                    }
                    else
                    {
                        //A short hand to say apply "action.ReadValue" to _cachedInputValue.x and .y
                        _cachedInputValue.x = _cachedInputValue.y = action.ReadValue<float>() * Gain;
                    }
                }
            }

            // IInputAxisReader interface: Called by the framework to read the input value
            public float GetValue(UnityEngine.Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
            {
                return (hint == IInputAxisOwner.AxisDescriptor.Hints.Y ? _cachedInputValue.y : _cachedInputValue.x);
            }
        }
    }
}
