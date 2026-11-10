using KinematicCharacterController;
using Unity.Cinemachine;
using UnityEngine;

namespace YetAnotherCharacterSystem
{
    public struct CharacterControllerInputs
    {
        public Vector2 MoveAxis;
        public Quaternion CameraRotation;
        public bool JumpPressed;
    }

    public class YetAnotherCharacterController : CharacterControllerBase<CharacterControllerInputs>
    {
        public Unity.Cinemachine.CinemachineCamera DebugCamera;

        [Header("Ground Movement")]
        public float MaxGroundMoveSpeed = 5;
        public float GroundMoveRate = 5;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 2;
        public float AirMoveRate = 2;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpSpeed = 10f;
        public float JumpPreGroundCoyoteTime = 0f;
        public float JumpPostGroundCoyoteTime = 0f;

        [Header("Gravity")]
        public Vector3 Gravity = new Vector3(0, -30f, 0);

        [Header("Rotation")]
        public float RotationRate = 2;
        public bool OrientCharacterTowardsCamera = false;

        private Vector3 _moveInput;
        private Vector3 _lookInput;

        private bool _hasJumpedThisFrame;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceGrounded = Mathf.Infinity;

        private void Update()
        {
            CharacterControllerInputs inputs = new CharacterControllerInputs();

            inputs.MoveAxis = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            inputs.CameraRotation = DebugCamera.State.GetFinalOrientation();
            inputs.JumpPressed = Input.GetButtonDown("Jump");

            SetInputs(inputs);
        }

        public override void SetInputs(in CharacterControllerInputs inputs)
        {
            // Clamp move input from 0 to 1 in length. This makes it normalized when clamped.
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxis.x, 0f, inputs.MoveAxis.y), 1f);

            // Extracts the relative plane of the camera from its rotation, 
            Vector3 cameraPlanarDirection = GetCameraPlanarDirection(inputs.CameraRotation);
            Quaternion cameraPlanarRotation = GetCameraPlanarRotation(cameraPlanarDirection);

            // Set move input to the camera relative direction and character plane
            _moveInput = cameraPlanarRotation * moveInputVector;

            if (OrientCharacterTowardsCamera)
            {
                _lookInput = cameraPlanarDirection;
            }
            else
            {
                _lookInput = _moveInput.normalized;
            }

            // Jumping input
            if (inputs.JumpPressed)
            {
                _timeSinceJumpRequested = 0f;
            }
        }

        /// <summary>
        /// Get the camera relative forward direction projected onto the character plane
        /// </summary>
        /// <param name="cameraRotation"></param>
        /// <returns></returns>
        private Vector3 GetCameraPlanarDirection(in Quaternion cameraRotation)
        {
            // Calculate camera direction and rotation on the character plane
            // cameraRotation * Vector3.forward is the camera relative transform.forward
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, CharacterMotor.CharacterUp).normalized;

            // Handles the edge case where the player is looking directly up or down
            // cameraRotation * Vector3.up gives us the relative Camera transform.up
            // In this case, transform.up would point forward if the player is looking upward or downward
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.up, CharacterMotor.CharacterUp).normalized;
            }

            return cameraPlanarDirection;
        }

        /// <summary>
        /// Get the camera relative rotation projected onto the character plane
        /// </summary>
        /// <param name="cameraPlanarDirection"></param>
        /// <returns></returns>
        private Quaternion GetCameraPlanarRotation(in Vector3 cameraPlanarDirection)
        {
            return Quaternion.LookRotation(cameraPlanarDirection, CharacterMotor.CharacterUp);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInput != Vector3.zero && RotationRate > 0f)
            {
                // Smoothly interpolate from current to target look direction
                Vector3 smoothedLookInputDirection = Vector3.Slerp(CharacterMotor.CharacterForward, _lookInput, GetSmoothTimeFactor(RotationRate, deltaTime)).normalized;

                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, CharacterMotor.CharacterUp);
            }
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (CharacterMotor.GroundingStatus.IsStableOnGround)
            {
                GroundMovement(ref currentVelocity, deltaTime);
            }
            else
            {
                AirMovement(ref currentVelocity, deltaTime);
            }

            ProcessJump(ref currentVelocity, deltaTime);
        }

        /// <summary>
        /// Handles the ground movement logic
        /// </summary>
        /// <param name="currentVelocity">The current velocity reference passed by the Character Motor. Sent back to the Character Motor afterwards.</param>
        /// <param name="deltaTime">The deltaTime sharpness received from the Character Motor</param>
        protected void GroundMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            // Reorient current velocity to the ground surface
            currentVelocity = CharacterMotor.GetDirectionTangentToSurface(currentVelocity, CharacterMotor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            // Calculate the maximum target velocity while aligning it to the ground normal
            Vector3 inputRight = Vector3.Cross(_moveInput, CharacterMotor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(CharacterMotor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInput.magnitude;
            Vector3 targetVelocity = reorientedInput * MaxGroundMoveSpeed;

            // Smooth movement Velocity using an exponential decay curve
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, GetSmoothTimeFactor(GroundMoveRate, deltaTime));
        }

        /// <summary>
        /// Handles the air movement logic
        /// </summary>
        /// <param name="currentVelocity">The current velocity reference passed by the Character Motor. Sent back to the Character Motor afterwards.</param>
        /// <param name="deltaTime">The deltaTime sharpness received from the Character Motor</param>
        protected void AirMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            // Apply air movement but only if the player is actually pushing the move input
            // Checking the sqrMagnitude is a faster way than magnitude for roughly checking that the vector isn't equal to 0.
            // sqrMagnitude is the squared magnitude, so it cannot be used to measure the length of the vector though. Use vector.magnitude in that case.
            if (_moveInput.sqrMagnitude > 0f)
            {
                // Calculate the maximum air movement speed allowed
                Vector3 targetVelocity = _moveInput * MaxAirMoveSpeed;

                // Prevent climbing on un-stable slopes with air movement
                // This stops the Skyrim Horse like experience that allows to climb steep slopes
                if (CharacterMotor.GroundingStatus.FoundAnyGround)
                {
                    Vector3 wallSideDirection = Vector3.Cross(CharacterMotor.CharacterUp, CharacterMotor.GroundingStatus.GroundNormal);
                    Vector3 wallNormal = Vector3.Cross(wallSideDirection, CharacterMotor.CharacterUp).normalized;
                    targetVelocity = Vector3.ProjectOnPlane(targetVelocity, wallNormal);
                }

                Vector3 velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, Gravity);

                currentVelocity += velocityDiff * GetSmoothTimeFactor(AirMoveRate, deltaTime);
            }

            // Apply gravity linearly
            currentVelocity += Gravity * deltaTime;
        }

        protected void ProcessJump(ref Vector3 currentVelocity, float deltaTime)
        {
            _hasJumpedThisFrame = false;

            // Check whether the jump was requested within a certain time frame
            // This allows the player to successfully jump when pressing the button just before landing on the ground. Pre-Ground "Coyote Time" jumping
            bool jumpRequestValid = _timeSinceJumpRequested <= JumpPreGroundCoyoteTime;

            // Check if we are actually allowed to jump. We need to be grounded or have left the ground for less than JumpPostGroundCoyoteTime
            bool isGrounded = CanJumpOnSurface() || _timeSinceGrounded <= JumpPostGroundCoyoteTime;

            if (jumpRequestValid && isGrounded)
            {
                JumpMovement(ref currentVelocity, deltaTime);

                _timeSinceJumpRequested = Mathf.Infinity;
                _timeSinceGrounded = Mathf.Infinity;

                _hasJumpedThisFrame = true;
            }

            _timeSinceJumpRequested += deltaTime;
        }

        protected void JumpMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            // Calculate jump direction before ungrounding
            Vector3 jumpDirection = CharacterMotor.CharacterUp;

            // If the character stands on sliding ground, jump in the direction of the slope normal
            // Otherwise players can "pogo" their way up
            if (CharacterMotor.GroundingStatus.FoundAnyGround && !CharacterMotor.GroundingStatus.IsStableOnGround)
            {
                jumpDirection = CharacterMotor.GroundingStatus.GroundNormal;
            }

            // Makes the character skip ground probing/snapping on its next update. 
            // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
            CharacterMotor.ForceUnground(0.1f);

            // Add to the return velocity and reset jump state
            currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, CharacterMotor.CharacterUp);
        }

        /// <summary>
        /// Handle the timeSinceGrounded timer after the character update. This allows coyote time jump
        /// </summary>
        protected void ProcessJumpPostUpdate(float deltaTime)
        {
            // If we're on a ground surface, we are allowed to jump, so set timeSinceLastAbleToJump to 0
            if (CanJumpOnSurface())
            {
                // We make sure that we haven't jumped this frame though, as we might have triggered a jump but still touching ground. Reseting timeSinceLastAbleToJump in that case wouldn't be a good idea
                if (!_hasJumpedThisFrame)
                {
                    _timeSinceGrounded = 0f;
                }
            }
            else
            {
                // Keep track of time since we were last able to jump (for grace period)
                _timeSinceGrounded += deltaTime;
            }
        }

        // 
        /// <summary>
        /// Checks if we are allowed to jump on the ground surface the character is currently on
        /// </summary>
        /// <returns></returns>
        private bool CanJumpOnSurface()
        {
            return AllowJumpingWhenSliding ?
                    CharacterMotor.GroundingStatus.FoundAnyGround :
                    CharacterMotor.GroundingStatus.IsStableOnGround;
        }

        /// <summary>
        /// Using 1 - Mathf.Exp(-Rate * deltaTime) is generally prefered to Time.deltaTime * Rate
        /// Time.deltaTime * Rate is linear and a rough estimation of the movement (Euler Method). The exponential method is preferred as it is more stable physically (Exponential Decay)
        /// Found an article about this here: https://medium.com/@lemapp09/beginning-game-development-lerp-is-broken-1f92ae0cd2bb
        /// </summary>
        /// <param name="sharpness">Sharpness modifies the curve of the function. A value of 5 approximates a curve that goes from 0 to 1 in a second.</param>
        /// <param name="deltaTime">The current deltaTime</param>
        private float GetSmoothTimeFactor(float sharpness, float deltaTime)
        {
            return (1 - Mathf.Exp(-sharpness * deltaTime));
        }

        public override void BeforeCharacterUpdate(float deltaTime)
        {
        }

        protected override void OnStableGroundEnter()
        {
        }

        protected override void OnStableGroundExit()
        {
        }

        public override void AfterCharacterUpdate(float deltaTime)
        {
            ProcessJumpPostUpdate(deltaTime);
        }

        #region Advanced Collision Handling

        public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public override void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        #endregion
    }
}
