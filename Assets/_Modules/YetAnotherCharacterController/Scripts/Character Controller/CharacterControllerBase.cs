using KinematicCharacterController;
using System.Collections.Generic;
using UnityEngine;

namespace YetAnotherCharacterSystem
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public abstract class CharacterControllerBase<T> : MonoBehaviour, KinematicCharacterController.ICharacterController 
        where T : struct
    {
        public KinematicCharacterMotor CharacterMotor { get; private set; }

        public List<Collider> IgnoredColliders = new List<Collider>();

        /// <summary>
        /// Bind the Controller to the Motor.
        /// </summary>
        private void Start()
        {
            BindToCharacterMotor();
        }

        /// <summary>
        /// Binds the Character Motor Controller to the Character Motor
        /// </summary>
        protected virtual void BindToCharacterMotor()
        {
            CharacterMotor = GetComponent<KinematicCharacterMotor>();

            // Assign the characterController to the motor
            CharacterMotor.CharacterController = this;
        }

        /// <summary>
        /// Set the inputs for this Character Controller, using the supplied inputStruct
        /// The inputs read by the character controller are contained in a struct
        /// We are using Generics in this class "CharacterControllerBase<T>" where T: struct as a way to specify that we want to enforce the use of a struct as the input source
        /// Then, we use that T reference below here to specify that we want a struct of type T when setting inputs
        /// This is a bit advanced, but it allows us to pass a struct to this class, bind it and get no boxing issue
        /// </summary>
        /// <param name="inputStruct">The input struct used for this controller</param>
        public abstract void SetInputs(in T inputStruct);

        public abstract void UpdateRotation(ref Quaternion currentRotation, float deltaTime);

        public abstract void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime);

        public abstract void BeforeCharacterUpdate(float deltaTime);

        // Using the PostGroundingUpdate method to check whether we are on stable or unstable ground, then calling the appropriate method
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

        public abstract void AfterCharacterUpdate(float deltaTime);

        public virtual bool IsColliderValidForCollisions(Collider col)
        {
            // If there are no ignored colliders, then the hit collider is valid
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }
            
            // If the hit collider is included in IgnoredColliders list, then the hit is not valid
            if (IgnoredColliders.Contains(col))
            {
                return false;
            }

            // Return a valid hit otherwise
            return true;
        }

        public abstract void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);

        public abstract void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);

        public abstract void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport);

        public abstract void OnDiscreteCollisionDetected(Collider hitCollider);
    }
}