using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

namespace YetAnotherCharacterSystem.Deprecated
{
    public abstract class InputControllerBase : MonoBehaviour
    {
        // The character we are currently controlling
        public CharacterManager ControllingCharacter;

        // Set to false if you want to disable inputs transfer to the character
        public bool CanControl = true;

        // The controller ID, which should reflect the Player ID or maybe something else for AIs, like -1?
        public abstract int ControllerID { get; }

        // Handling of the inputs to transfer could much better
        // This is resulting from the different architecture of the Input System and the Kinematic Character Controller kind of clashing together in regard to input management
        protected Vector3 _characterMoveInput = Vector3.zero;
        protected Vector3 _characterLookInput = Vector3.zero;
        protected bool _characterJumpInput = false;

        // This is done purposefully on start and not awake
        // That allows the charcter controllers to properly setup on awake before controlling them
        protected virtual void Start()
        {
            if (ControllingCharacter != null)
            {
                TakeControl(ControllingCharacter);
            }
            else
            {
                //FindAndControlAvailableCharacter();
            }
        }

        protected virtual void Update()
        {
            TransferInputsToCharacter();
        }

        protected virtual void TransferInputsToCharacter()
        {
            //Do not continue if there is no character to control
            if (ControllingCharacter == null) return;

            if (CanControl)
            {
                ControllingCharacter.SetInputs(_characterMoveInput, _characterLookInput, _characterJumpInput);
            }
            else
            {
                ControllingCharacter.SetInputs(Vector3.zero, Vector3.zero, false);
            }
        }

        protected void FindAndControlAvailableCharacter()
        {
            //Generally not something we would like to do, but this is fine for the example sake
            CharacterManager[] allCharacters = FindObjectsByType<CharacterManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);

            foreach (CharacterManager aCharacter in allCharacters)
            {
                if (!aCharacter.IsControlled)
                {
                    TakeControl(aCharacter);

                    //End the function once a character is found and controlled.
                    return;
                }
            }

            if (ControllingCharacter == null)
            {
                Debug.LogError("No available character to control");
            }
        }

        protected virtual void TakeControl(CharacterManager character)
        {
            if (ControllingCharacter != null)
            {
                ControllingCharacter.DetachInputController();
            }

            character.AttachInputController(this);
            ControllingCharacter = character;
        }
    }
}