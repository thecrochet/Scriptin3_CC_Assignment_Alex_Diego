using System;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace YetAnotherCharacterSystem.Deprecated
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public abstract class CharacterMotorControllerBase : MonoBehaviour, ICharacterController
    {
        //Action list of character events that can be used to register feedback through other character scripts
        public event Action OnCharacterMove;
        public event Action OnCharacterStopMove;
        public event Action OnCharacterJump;
        public event Action OnCharacterGroundHit;

        public KinematicCharacterMotor CharacterMotor { get; private set; }
        public List<Collider> IgnoredColliders = new List<Collider>();

        protected Vector3 _internalVelocity = Vector3.zero;

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            CharacterMotor = GetComponent<KinematicCharacterMotor>();

            // Assign the characterController to the motor
            CharacterMotor.CharacterController = this;
        }

        public abstract void ReceiveInputs(Vector3 moveInput, Vector3 lookInput, bool jumpInput);

        public virtual void AddVelocity(Vector3 velocity)
        {
            _internalVelocity += velocity;
        }

        public abstract void BeforeCharacterUpdate(float deltaTime);

        public abstract void UpdateRotation(ref Quaternion currentRotation, float deltaTime);

        public abstract void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime);

        public abstract void AfterCharacterUpdate(float deltaTime);

        // Here is a good example of why we are using abstraction
        // I don't think that always exposing PostGroundingUpdate is a good idea for the character controller. Its access could be hidden.
        // Virtual CAN be overriden by a child class, but doesn't have to. Abstract HAS TO be overriden by the child class.
        // As such, the abstracted methods accessible here are OnStableGroundEnter and OnStableGroundExit only.
        public virtual void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (CharacterMotor.GroundingStatus.IsStableOnGround && !CharacterMotor.LastGroundingStatus.IsStableOnGround)
            {
                OnStableGroundEnter();
            }
            else if (!CharacterMotor.GroundingStatus.IsStableOnGround && CharacterMotor.LastGroundingStatus.IsStableOnGround)
            {
                OnStableGroundExit();
            }
        }

        protected abstract void OnStableGroundEnter();
        protected abstract void OnStableGroundExit();

        public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        public virtual bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }

            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }

            return true;
        }

        public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// Invoker for the OnCharacterMove event action
        /// Call this to start the chain of feedback OnCharacterMove
        /// </summary>
        protected virtual void InvokeCharacterMoveEvent()
        {
            // ? is used in this context to do a null check
            // This is the same as if(OnCharacterGroundHit != null) OnCharacterGroundHit.Invoke()
            OnCharacterMove?.Invoke();
        }

        /// <summary>
        /// Invoker for the OnCharacterStopMove event action
        /// Call this to start the chain of feedback OnCharacterMove
        /// </summary>
        protected virtual void InvokeCharacterStopMoveEvent()
        {
            // ? is used in this context to do a null check
            // This is the same as if(OnCharacterGroundHit != null) OnCharacterGroundHit.Invoke()
            OnCharacterStopMove?.Invoke();
        }

        /// <summary>
        /// Invoker for the OnCharacterMove event action
        /// Call this to start the chain of feedback OnCharacterJump
        /// </summary>
        protected virtual void InvokeCharacterJumpEvent()
        {
            OnCharacterJump?.Invoke();
        }

        /// <summary>
        /// Invoker for the OnCharacterMove event action
        /// Call this to start the chain of feedback OnCharacterGroundHit
        /// </summary>
        protected virtual void InvokeCharacterGroundHitEvent()
        {
            OnCharacterGroundHit?.Invoke();
        }
    }
}