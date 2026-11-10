using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace YetAnotherCharacterSystem.Deprecated
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputController : InputControllerBase
    {
        public override int ControllerID
        {
            get
            {
                //If SplitScreenIndex is -1 that means we're alone. So return 1 as Player ID 1. Otherwise, send splitScreenIndex + 1
                return (_playerInput.splitScreenIndex == -1) ? 1 : _playerInput.splitScreenIndex + 1;
            }
        }

        public bool OrientCharacterTowardsCamera = false;

        private PlayerInput _playerInput;
        private CinemachineBrain _cinemachineBrain;
        private CinemachineCamera _cinemachineCamera;

        //Store separately the absolute move input before transforming to relative direction with the camera
        private Vector3 _absoluteMoveInput;

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            BindInputActions();
        }

        private void OnDisable()
        {
            UnbindInputActions();
        }

        protected override void Start()
        {
            //Allows to run the overriden method first, then run our code
            base.Start();

            InitializePlayerInputs();
        }

        protected override void TransferInputsToCharacter()
        {
            //Process the input direction change based on camera before sending the inputs
            ProcessMoveDirectionWithCamera();

            base.TransferInputsToCharacter();
        }

        protected override void TakeControl(CharacterManager character)
        {
            base.TakeControl(character);

            //Since the cinemachine camera is within the character at the moment, we need to wait for controlling a character before being able to setup the camera
            SetupCinemachine();
        }

        private void InitializeComponents()
        {
            _playerInput = GetComponent<PlayerInput>();
            _cinemachineBrain = GetComponentInChildren<CinemachineBrain>();
            _cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
        }

        private void InitializePlayerInputs()
        {
            SwitchActionMap("Gameplay", true);
        }

        public void SwitchActionMap(string actionMapName, bool disableCursor)
        {
            if (disableCursor)
            {
                DisableCursor();
            }
            else
            {
                EnableCursor();
            }

            _playerInput.SwitchCurrentActionMap(actionMapName);
        }

        private void DisableCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void EnableCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetupCinemachine()
        {
            int splitScreenID = _playerInput.splitScreenIndex;

            OutputChannels cameraOutputChannel = OutputChannels.Default;

            switch (splitScreenID)
            {
                //Player 1 is ID -1 during singleplayer. Player 1 is ID 0 during splitscreen
                case -1:
                    cameraOutputChannel = OutputChannels.Channel01;
                    break;

                case 0:
                    cameraOutputChannel = OutputChannels.Channel01;
                    break;

                case 1: cameraOutputChannel = OutputChannels.Channel02;
                    break;

                case 2: cameraOutputChannel = OutputChannels.Channel03;
                    break;

                case 3:
                    cameraOutputChannel = OutputChannels.Channel04;
                    break;

                default:
                    Debug.Log("Split screen camera index out of range");
                    break;
            }

            _cinemachineBrain.ChannelMask = cameraOutputChannel;

            //Make sure the camera align to the character
            _cinemachineBrain.WorldUpOverride = ControllingCharacter.transform;

            _cinemachineCamera.OutputChannel = cameraOutputChannel;
            _cinemachineCamera.Follow = ControllingCharacter.CameraFollowTarget;
            _cinemachineCamera.LookAt = ControllingCharacter.CameraLookAtTarget;
        }

        private void ProcessMoveDirectionWithCamera()
        {
            if (ControllingCharacter == null) return;

            //Get rotation of the cinemachine camera
            Quaternion cameraRotation = _cinemachineBrain.OutputCamera.transform.rotation;
            Vector3 characterUp = ControllingCharacter.CharacterController.CharacterMotor.CharacterUp;

            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, characterUp).normalized;

            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.up, characterUp).normalized;
            }

            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, characterUp);

            _characterMoveInput = cameraPlanarRotation * _absoluteMoveInput;

            if (OrientCharacterTowardsCamera)
            {
                _characterLookInput = cameraPlanarDirection;
            }
            else
            {
                _characterLookInput = _characterMoveInput.normalized;
            }

            /*
             * The above implementation is the original example of the Kinematic Character Controller
             * I personnaly find it to be quite heavy vs the following code block that I would normally use.
             * Both are doing the same operations I believe, but there may be more that needs to be taken into account here.
             * 
            _characterMoveInput = _cinemachineBrain.OutputCamera.transform.TransformDirection(_absoluteMoveInput);
            _characterMoveInput.y = 0;
            _characterMoveInput = _characterMoveInput.normalized * _absoluteMoveInput.magnitude;
            _characterLookInput = _characterMoveInput.normalized;
            */
        }

        public void BindEventToInputAction(string inputActionName, InputActionPhase inputPhase, Action<InputAction.CallbackContext> callbackEvent)
        {
            switch (inputPhase)
            {
                case InputActionPhase.Started:
                    _playerInput.actions.FindAction(inputActionName).started += callbackEvent;
                    break;
                case InputActionPhase.Performed:
                    _playerInput.actions.FindAction(inputActionName).performed += callbackEvent;
                    break;
                case InputActionPhase.Canceled:
                    _playerInput.actions.FindAction(inputActionName).canceled += callbackEvent;
                    break;
            }
        }

        public void UnbindEventToInputAction(string inputActionName, InputActionPhase inputPhase, Action<InputAction.CallbackContext> callbackEvent)
        {
            switch (inputPhase)
            {
                case InputActionPhase.Started:
                    _playerInput.actions.FindAction(inputActionName).started -= callbackEvent;
                    break;
                case InputActionPhase.Performed:
                    _playerInput.actions.FindAction(inputActionName).performed -= callbackEvent;
                    break;
                case InputActionPhase.Canceled:
                    _playerInput.actions.FindAction(inputActionName).canceled -= callbackEvent;
                    break;
            }
        }

        private void BindInputActions()
        {
            BindEventToInputAction("Move", InputActionPhase.Performed, OnMove);
            BindEventToInputAction("Jump", InputActionPhase.Performed, OnJump);
        }

        private void UnbindInputActions()
        {
            UnbindEventToInputAction("Move", InputActionPhase.Performed, OnMove);
            UnbindEventToInputAction("Jump", InputActionPhase.Performed, OnJump);
        }

        // Make sure that your input actions in the InputActionAsset are set to passthrough and not value!
        // That way, we get an update when the input is pressed AND released. Otherwise we would only get one update upon press and not release.
        // This also applies to joysticks getting released to neutral position.
        private void OnMove(InputAction.CallbackContext inputContext)
        {
            Vector2 inputValue = inputContext.ReadValue<Vector2>();

            //Clamp value so that the vector is never longer than 1
            _absoluteMoveInput = Vector3.ClampMagnitude(new Vector3(inputValue.x, 0, inputValue.y), 1f);
        }

        private void OnJump(InputAction.CallbackContext inputContext)
        {
            _characterJumpInput = inputContext.ReadValueAsButton();
        }
    }
}