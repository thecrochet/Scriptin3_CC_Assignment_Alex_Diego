using KinematicCharacterController;
using KinematicCharacterController.Examples;
using System.Collections.Generic;
using UnityEngine;

namespace YetAnotherCharacterSystem.Deprecated
{
    public class CharacterMotorController : CharacterMotorControllerBase
    {
        public enum CharacterAdditionalOrientationMethod
        {
            None,
            TowardsGravity,
            TowardsGroundSlopeAndGravity,
        }

        [Header("Movement")]
        public float MaximumMoveSpeed = 10f;
        public float MovementAcceleration = 15f;

        [Header("Rotation")]
        public float OrientationAcceleration = 10f;
        public float AdditionalOrientationAcceleration = 10f;
        public CharacterAdditionalOrientationMethod CharacterAdditionalOrientation = CharacterAdditionalOrientationMethod.None;

        [Header("Air Movement")]
        public float MaximumAirMoveSpeed = 15f;
        public float AirAcceleration = 15f;
        public float Drag = 0.1f;
        public Vector3 Gravity = new Vector3(0, -9.81f, 0);

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpUpSpeed = 10f;
        public float JumpForwardSpeed = 10f;
        public float JumpPreGroundGraceTime = 0.2f;
        public float JumpPostGroundGraceTime = 0.2f;

        protected Vector3 _moveInput = Vector3.zero;
        protected Vector3 _lookInput = Vector3.zero;

        protected bool _jumpRequested = false;
        protected bool _canJump = true;
        protected bool _jumpedThisFrame = false;

        protected float _timeSinceJumpRequested = Mathf.Infinity;
        protected float _timeSinceLastAbleToJump = 0f;

        // Not so proud of the implementation of the inputs here as they are hard-coded.
        // Further abstraction would help, ideally storing each character mechanic individually
        public override void ReceiveInputs(Vector3 moveInput, Vector3 lookInput, bool jumpInput)
        {
            _moveInput = moveInput;
            _lookInput = lookInput;

            if (jumpInput)
            {
                _jumpRequested = jumpInput;
                _timeSinceJumpRequested = 0f;
            }
        }

        public bool IsGroundedForJump()
        {
            return AllowJumpingWhenSliding ?
                    CharacterMotor.GroundingStatus.FoundAnyGround :
                    CharacterMotor.GroundingStatus.IsStableOnGround;
        }

        public override void BeforeCharacterUpdate(float deltaTime)
        {
        }

        // Todo: Not very optimized.
        // Note that writing "Todo:" or "Hack:" will show these comments when you open "View/Task List" in Visual Studio!
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if(_lookInput.sqrMagnitude > 0f && OrientationAcceleration > 0f)
            {
                // Smoothly interpolate from current to target look direction
                Vector3 smoothedLookInputDirection = Vector3.Slerp(CharacterMotor.CharacterForward, _lookInput, 1 - Mathf.Exp(-OrientationAcceleration * deltaTime)).normalized;
                
                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, CharacterMotor.CharacterUp);
            }

            Vector3 currentUp = (currentRotation * Vector3.up);

            if (CharacterAdditionalOrientation == CharacterAdditionalOrientationMethod.TowardsGravity)
            {
                // Rotate from current up to invert gravity
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-AdditionalOrientationAcceleration * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
            else if (CharacterAdditionalOrientation == CharacterAdditionalOrientationMethod.TowardsGroundSlopeAndGravity)
            {
                if (CharacterMotor.GroundingStatus.IsStableOnGround)
                {
                    Vector3 initialCharacterBottomHemiCenter = CharacterMotor.TransientPosition + (currentUp * CharacterMotor.Capsule.radius);

                    Vector3 smoothedGroundNormal = Vector3.Slerp(CharacterMotor.CharacterUp, CharacterMotor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-AdditionalOrientationAcceleration * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                    // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                    CharacterMotor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * CharacterMotor.Capsule.radius));
                }
                else
                {
                    Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-AdditionalOrientationAcceleration * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                }
            }
            else
            {
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-AdditionalOrientationAcceleration * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            bool foundAnyGround = CharacterMotor.GroundingStatus.FoundAnyGround;
            bool onStableGround = CharacterMotor.GroundingStatus.IsStableOnGround;

            // Ground movement
            if (onStableGround)
            {
                GroundMovement(ref currentVelocity, deltaTime);

                //Invoke the OnCharacterMove event when grounded and move input is currently used
                if (_moveInput.sqrMagnitude > 0)
                {
                    InvokeCharacterMoveEvent();
                }
                else
                {
                    InvokeCharacterStopMoveEvent();
                }
            }
            // Air movement
            else
            {
                AirMovement(ref currentVelocity, deltaTime);
            }

            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;
            if (_jumpRequested && _canJump)
            {
                if (IsGroundedForJump() || _timeSinceLastAbleToJump <= JumpPostGroundGraceTime)
                {
                    JumpMovement(ref currentVelocity, deltaTime);

                    _jumpRequested = false;
                    _canJump = false;
                    _jumpedThisFrame = true;

                    //Invoke the OnCharacterJump event
                    InvokeCharacterJumpEvent();
                }
            }

            if (_internalVelocity.sqrMagnitude > 0)
            {
                AdditionalMovement(ref currentVelocity, deltaTime);
            }
        }

        private void GroundMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 groundNormal = CharacterMotor.GroundingStatus.GroundNormal;

            // Reorient velocity to the ground surface tangent
            currentVelocity = CharacterMotor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * currentVelocity.magnitude;

            Vector3 crossInputCharacterUp = Vector3.Cross(_moveInput, CharacterMotor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(groundNormal, crossInputCharacterUp).normalized * _moveInput.magnitude;
            Vector3 targetVelocity = reorientedInput * MaximumMoveSpeed;

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-MovementAcceleration * deltaTime));
        }

        private void AirMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_moveInput.sqrMagnitude > 0f)
            {
                Vector3 airVelocity = _moveInput * AirAcceleration * deltaTime;

                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, CharacterMotor.CharacterUp);

                if (currentVelocityOnInputsPlane.magnitude < MaximumAirMoveSpeed)
                {
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + airVelocity, MaximumAirMoveSpeed);
                    airVelocity = newTotal - currentVelocityOnInputsPlane;
                }
                else
                {
                    if (Vector3.Dot(currentVelocityOnInputsPlane, airVelocity) > 0f)
                    {
                        airVelocity = Vector3.ProjectOnPlane(airVelocity, currentVelocityOnInputsPlane.normalized);
                    }
                }

                // Prevents air-climbing sloped walls
                if (CharacterMotor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(currentVelocity + airVelocity, airVelocity) > 0f)
                    {
                        Vector3 crossCharacterUpSurfaceNormal = Vector3.Cross(CharacterMotor.CharacterUp, CharacterMotor.GroundingStatus.GroundNormal);
                        Vector3 perpendicularObstructionNormal = Vector3.Cross(crossCharacterUpSurfaceNormal, CharacterMotor.CharacterUp).normalized;
                        airVelocity = Vector3.ProjectOnPlane(airVelocity, perpendicularObstructionNormal);
                    }
                }

                //Apply air velocity
                currentVelocity += airVelocity;
            }

            //Apply gravity
            currentVelocity += Gravity * deltaTime;

            //Apply drag, basically air friction
            currentVelocity *= 1f / (1f + (Drag * deltaTime));
        }

        private void JumpMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            //Adjust jump direction
            Vector3 jumpDirection = CharacterMotor.CharacterUp;

            // If the character stands on sliding ground, jump in the direction of the slope normal
            // Otherwise players can "pogo" their way up
            if (CharacterMotor.GroundingStatus.FoundAnyGround && !CharacterMotor.GroundingStatus.IsStableOnGround)
            {
                jumpDirection = CharacterMotor.GroundingStatus.GroundNormal;
            }

            // Makes the character skip ground probing/snapping on its next update. 
            // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
            CharacterMotor.ForceUnground();

            currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, CharacterMotor.CharacterUp);
            currentVelocity += _moveInput * JumpForwardSpeed;
        }

        private void AdditionalMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity += _internalVelocity;
            _internalVelocity = Vector3.zero;
        }

        public override void AfterCharacterUpdate(float deltaTime)
        {
            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundGraceTime)
            {
                _jumpRequested = false;
            }

            if (IsGroundedForJump())
            {
                if (!_jumpedThisFrame)
                {
                    _canJump = true;
                }

                _timeSinceLastAbleToJump = 0f;
            }
            else
            {
                _timeSinceLastAbleToJump += deltaTime;
            }
        }

        protected override void OnStableGroundEnter()
        {
            InvokeCharacterGroundHitEvent();
        }

        protected override void OnStableGroundExit()
        {
        }
    }
}
