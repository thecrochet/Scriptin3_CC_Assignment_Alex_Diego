using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using KinematicCharacterController;
using System;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class CC_ELEMENTO : MonoBehaviour, ICharacterController
{
    // declare variables for the input and cinemachine camera
    public InputActionReference MoveAction;
    public InputActionReference JumpAction;
    public CinemachineCamera PlayerCamera;

    // 2 More actions************************************
    public InputActionReference RunAction;
    public InputActionReference GroundPoundAction;

    // How fast you can move on the ground
    public float MaxGroundSpeed = 10f;
    // how much movement you can do on the air 
    public float MaxAirSpeed = 5f;

    // While the run button is held, the speeds above are multiplied by this ********************************
    public float RunSpeedMultiplier = 1.6f;

    // how fast your velocity changes in the ground
    public float GroundAcceleration = 5f;
    // how fast your velocity changes in the air
    public float AirAcceleration = 2f;

    // how much up velocity the jump ads
    public float JumpStrength = 10f;
    // how quicly the character turns
    public float RotationRate = 5f;

    // how strong the character falls
    public Vector3 Gravity = new Vector3(0, -10, 0);

    // ******************************
    // Coyotoe Time : Extra time after leaving the ground where player can still jump
    public float CoyoteTime = 0.1f;

    // Buffer Time : Jump pressed this many seconds before landing still counts
    public float JumpBufferTime = 0.1f;

    // Checkbox in the Inspector to turn the double jump on or off
    public bool HasDoubleJump = true;

    // How fast the character falls during the ground pound 
    public float GroundPoundSpeed = 30f;
    // ******************************************************

    // declare for the inputs variables
    private Vector3 _moveInput;
    private Vector3 _lookInput;

    // True while the run button is held ******************************
    private bool _isRunning;

    // 2 timers using at Infinity. meaning it happened a long time ago (so nothing triggers by accident)******************
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceGrounded = Mathf.Infinity;


    // Jump settings
    private bool _hasDoubleJumped = true;
    private bool _hasJumpedThisFrame;

    // Ground pound ***********************
    private bool _groundPoundRequested;
    private bool _isGroundPounding;


    //declare the variable for the Motor of the character
    private KinematicCharacterMotor _kinematicMotor;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        //connect the motor from the KCC ASSETS to this game object
        _kinematicMotor = GetComponent<KinematicCharacterMotor>();
        _kinematicMotor.CharacterController = this;
    }

    private void Update()
    {
        // ********************************
        if (JumpAction.action.WasPressedThisFrame())
        {
            // the timer goes to 0 as the player just pressed jump
            _timeSinceJumpRequested = 0f;
        }

        if (GroundPoundAction != null && GroundPoundAction.action.WasPressedThisFrame())
        {
            _groundPoundRequested = true;
        }
    }


    public void BeforeCharacterUpdate(float deltaTime)
    {
        // this read the player input first 
        UpdateInputs();
    }

  
    private void UpdateInputs()
    {
        // connect the move action from the new input system values 2D into the variable
        Vector2 moveActionInput = MoveAction.action.ReadValue<Vector2>();

        //transfor the 2D vector values into 3D vector values and clamp the values for horizontal movements to be the same
        // 0 is becuase we are not trying to move up and down on Z
        _moveInput = new Vector3(moveActionInput.x, 0, moveActionInput.y);
        _moveInput = Vector3.ClampMagnitude(_moveInput, 1);

        // this move the character direction based of the camer direction
        // comment this one if doing a sidescroller plataformer
        _moveInput = GetCameraPlanarRotation(PlayerCamera.State.GetFinalOrientation()) * _moveInput;

        // saves the move new direction into the look direction
        _lookInput = _moveInput;

        // "IsPressed" is true for as long as the run button is held**************************
        _isRunning = RunAction != null && RunAction.action.IsPressed();
    }

    // this is the logic for the rotation of the character
    private Quaternion GetCameraPlanarRotation(Quaternion cameraRotation)
    {
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, _kinematicMotor.CharacterUp).normalized;

        if (cameraPlanarDirection.sqrMagnitude == 0)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.up, _kinematicMotor.CharacterUp).normalized;
        }

        return Quaternion.LookRotation(cameraPlanarDirection, _kinematicMotor.CharacterUp);
    }

    // this rotate the character
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInput == Vector3.zero) return;

        Vector3 targetLookDirection = Vector3.Slerp(_kinematicMotor.CharacterForward, _lookInput, deltaTime * RotationRate);

        currentRotation = Quaternion.LookRotation(targetLookDirection, _kinematicMotor.CharacterUp);
    }

    // this move the charater
    // currentVelocity has the direction and how fast the character move
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {

        //  If we are slamming down we return here *******************
        if (HandleGroundPound(ref currentVelocity))
        {
            return;
        }

        // is the character standing on the ground?
        if (_kinematicMotor.GroundingStatus.IsStableOnGround)
        {
            GroundMovement(ref currentVelocity, deltaTime);
        }
        // is the character on the air?
        else
        {
            AirMovement(ref currentVelocity, deltaTime);
        }

        // this checks if the jump should happend
        // "ref" means we work on the real velocity of the motor, not on a copy of it
        ApplyJumping(ref currentVelocity, deltaTime);
    }





    // this is the logic for the grond movement
    private void GroundMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        // Run button multiplies the speed ******************
        float speed = MaxGroundSpeed;
        if (_isRunning)
        {
            speed = speed * RunSpeedMultiplier;
        }

        Vector3 targetVelocity = _moveInput * speed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, deltaTime * GroundAcceleration);
    }

    // this is the logic for the air movement
    private void AirMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_moveInput.sqrMagnitude > 0)
        {

            // Same idea in the air, so a running jump keeps its speed **********************
            float speed = MaxAirSpeed;
            if (_isRunning)
            {
                speed = speed * RunSpeedMultiplier;
            }

            Vector3 targetVelocity = _moveInput * speed;

            Vector3 velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, Gravity);

            currentVelocity += velocityDiff * deltaTime * AirAcceleration;
        }

        //Gravity make the character fall only aply in air movement not in ground movement
        currentVelocity += Gravity * deltaTime;
    }

    // ****************************************************
    //Handle Jump
    private void ApplyJumping(ref Vector3 currentVelocity, float deltaTime)
    {
        _hasJumpedThisFrame = false;

        // JUMP BUFFER: did the player press jump recently
        bool jumpRequestValid = _timeSinceJumpRequested <= JumpBufferTime;

        if (jumpRequestValid)
        {
            // COYOTE TIME: are we on the ground
           
            if (_timeSinceGrounded <= CoyoteTime)
            {
                Jump(ref currentVelocity);

                // the ground jump is used: we can't use the same ground twice
                _timeSinceGrounded = Mathf.Infinity;
                _timeSinceJumpRequested = Mathf.Infinity;
                _hasJumpedThisFrame = true;
            }
            // DOUBLE JUMP: we are in the air. Is the double jump on, and not used yet?
            else if (HasDoubleJump && !_hasDoubleJumped)
            {
                Jump(ref currentVelocity);

                // No more jumps until we touch the ground again
                _hasDoubleJumped = true;
                _timeSinceJumpRequested = Mathf.Infinity;
                _hasJumpedThisFrame = true;
            }
        }

        // timer keeps running
        _timeSinceJumpRequested += deltaTime;
    }

    // Jump in its own method because the ground jump and the double jump use it
    private void Jump(ref Vector3 currentVelocity)
    {
        
        _kinematicMotor.ForceUnground();

        // this calculate the jump force
        Vector3 jumpForce = _kinematicMotor.CharacterUp * JumpStrength;

        // We take away the current up/down speed, then we add the jump force.
        currentVelocity += jumpForce - Vector3.Project(currentVelocity, _kinematicMotor.CharacterUp);
    }
    // **********************************************

   

    // Ground pound. Returns true while we are slamming **************
    private bool HandleGroundPound(ref Vector3 currentVelocity)
    {
        // start the slam
        if (_groundPoundRequested && !_isGroundPounding && !_kinematicMotor.GroundingStatus.IsStableOnGround)
        {
            _isGroundPounding = true;
        }

        // pressed on the ground : nothing happens
        _groundPoundRequested = false;

        // not slamming
        if (!_isGroundPounding)
        {
            return false;
        }

        // slamming logic: straight down at a fixed speed 
        currentVelocity = -_kinematicMotor.CharacterUp * GroundPoundSpeed;

        // jump presses during the slam are ignored
        _timeSinceJumpRequested = Mathf.Infinity;
        _hasJumpedThisFrame = false;

        return true;
    }

    // this runs after the motor moved the character ************************
    public void AfterCharacterUpdate(float deltaTime)
    {
        // resets the jump
        if (_kinematicMotor.GroundingStatus.IsStableOnGround)
        {
           
            if (!_hasJumpedThisFrame)
            {
                // we touch the ground: coyote stopwatch at 0, and we get our double jump back
                _timeSinceGrounded = 0f;
                _hasDoubleJumped = false;
            }
        }
        else
        {
            // in the air: the coyote time keeps running
            _timeSinceGrounded += deltaTime;
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    // The motor calls this right after it checked the ground *****************************
    public void PostGroundingUpdate(float deltaTime)
    {

        // we were slamming and now we touch stable ground: the slam is over
        if (_isGroundPounding && _kinematicMotor.GroundingStatus.IsStableOnGround)
        {
            _isGroundPounding = false;

            // stop final movement
            _kinematicMotor.BaseVelocity = Vector3.zero;
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }
}

