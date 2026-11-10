using KinematicCharacterController;
using Unity.Cinemachine;
using UnityEngine;

namespace YetAnotherCharacterSystem.Deprecated
{
    [RequireComponent(typeof(CharacterMotorControllerBase))]
    public class CharacterManager : MonoBehaviour
    {
        public Transform CameraFollowTarget;
        public Transform CameraLookAtTarget;

        public InputControllerBase InputController;

        public CharacterMotorControllerBase CharacterController { get; private set; }

        public bool IsControlled { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            CharacterController = GetComponent<CharacterMotorControllerBase>();
        }

        // Transfer inputs onto the character controller directly
        public void SetInputs(Vector3 moveInput, Vector3 lookInput, bool jumpInput)
        {
            if (CharacterController == null) return;

            CharacterController.ReceiveInputs(moveInput, lookInput, jumpInput);
        }

        public void AttachInputController(InputControllerBase inputController)
        {
            InputController = inputController;
            IsControlled = true;
        }

        public void DetachInputController()
        {
            InputController = null;
            IsControlled = false;
        }
    }
}