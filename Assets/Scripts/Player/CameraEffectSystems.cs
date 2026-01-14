using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Handles camera effects including headbob and peeking/leaning mechanics
    /// </summary>
    public class CameraEffectsSystems : MonoBehaviour
    {
        [Header("Headbob Settings")]
        [Header("Walking")]
        [SerializeField] private float walkBobSpeed = 14.0f;
        [SerializeField] private float walkBobYAmount = 0.05f;
        [SerializeField] private float walkBobZAmount = 0.02f;
        [SerializeField] private float walkBobXAmount = 0.02f;
        [Header("Sprinting")]
        [SerializeField] private float sprintBobSpeed = 18.0f;
        [SerializeField] private float sprintBobYAmount = 0.1f;
        [SerializeField] private float sprintBobZAmount = 0.04f;
        [SerializeField] private float sprintBobXAmount = 0.035f;
        [Header("Crouching")]
        [SerializeField] private float crouchBobSpeed = 8.0f;
        [SerializeField] private float crouchBobYAmount = 0.025f;
        [SerializeField] private float crouchBobZAmount = 0.01f;
        [SerializeField] private float crouchBobXAmount = 0.01f;
        [Space(10)]
        [Header("Peeking Settings")]
        [SerializeField] private float peekAngle = 15f;
        [SerializeField] private float peekOffset = 0.25f;
        [SerializeField] private float peekSpeed = 10f;
        [SerializeField] private LayerMask wallLayerMask;
        
        [Space(10)]
        [Header("References")]
        [SerializeField] private Transform cameraLeanPivot;
        [SerializeField] private Transform headTransform;
        
        

        // Internal state
        private float _bobTimer;
        private float _cameraBaseY;
        private float _peekAmount;
        private float _peekForwardAmount;
        private float _peekVerticalAmount;
        [SerializeField] private Transform _cameraTransform; // this is gonna be assigned, as just.. us lol.

        private void Awake()
        {
            if (_cameraTransform != null)
            {
                _cameraBaseY = _cameraTransform.localPosition.y;
            }
            
        }

        /// <summary>
        /// Updates camera base Y position (called when crouching/standing from a player controller)
        /// </summary>
        public void UpdateCameraBaseY(float newBaseY)
        {
            _cameraBaseY = newBaseY;
        }

        /// <summary>
        /// Main update method - This is called via the main player scripts Update() function
        /// </summary>
        public void UpdateEffects(bool isGrounded, bool isMoving, bool isSprinting, bool isCrouching)
        {
            HandleHeadBob(isGrounded, isMoving, isSprinting, isCrouching);
            HandlePeeking();
        }

        #region Headbob
        private void HandleHeadBob(bool isGrounded, bool isMoving, bool isSprinting, bool isCrouching)
        {
            // if the player is not grounded, reset headbob and exit the function early
            if (!isGrounded) { ResetHeadBob(); return; }

            // Edge case: prioritize crouch headbob if both crouching and sprinting
            if (isCrouching && isSprinting)
            {
                isSprinting = false;
            }

            // if we are moving, apply headbob effect, otherwise reset it
            if (isMoving)
            {
                ApplyHeadBob(isSprinting, isCrouching);
            }
            else
            {
                ResetHeadBob();
            }
        }

        private void ApplyHeadBob(bool isSprinting, bool isCrouching)
        {
            float bobSpeed = GetBobSpeed(isSprinting, isCrouching);
            float yBobAmount = GetYBobAmount(isSprinting, isCrouching);
            float xBobAmount = GetXBobAmount(isSprinting, isCrouching);
            float zBobAmount = GetZBobAmount(isSprinting, isCrouching);

            _bobTimer += Time.deltaTime * bobSpeed;

            // Calculate offsets
            float yOffset = Mathf.Sin(_bobTimer) * yBobAmount;
            float xOffset = Mathf.Sin(_bobTimer * 0.5f) * xBobAmount; // currently, x bob is half speed
            float zOffset = Mathf.Cos(_bobTimer * 0.5f) * zBobAmount; // currently, z bob is half speed

            // Apply to camera
            // get the position of the camera
            Vector3 cameraPosition = _cameraTransform.localPosition;
            // update the position with offsets
            cameraPosition.y = _cameraBaseY + yOffset;
            cameraPosition.x = xOffset;
            cameraPosition.z = zOffset;
            // set the new local position
            _cameraTransform.localPosition = cameraPosition;
        }

        private void ResetHeadBob()
        {
            // Reset everything back to defaults
            _bobTimer = 0f;
            Vector3 cameraPosition = _cameraTransform.localPosition;
            cameraPosition.y = Mathf.Lerp(cameraPosition.y, _cameraBaseY, Time.deltaTime * 5f);
            cameraPosition.x = Mathf.Lerp(cameraPosition.x, 0f, Time.deltaTime * 5f);
            cameraPosition.z = Mathf.Lerp(cameraPosition.z, 0f, Time.deltaTime * 5f);
            _cameraTransform.localPosition = cameraPosition;
        }
        #endregion

        #region Peeking

        private void HandlePeeking()
        {
            float targetLean = 0f;
            float targetVerticalLean = 0f;

            Vector3 forward = _cameraTransform.forward;
            Debug.DrawRay(_cameraTransform.position, forward * 2f, Color.green);

            // Get input
            if (Keyboard.current.qKey.isPressed)
            {
                targetLean = -1f; // Left
                Debug.DrawRay(_cameraTransform.position, -_cameraTransform.right * 2f, Color.blue);
            }
            else if (Keyboard.current.eKey.isPressed)
            {
                targetLean = 1f; // Right
                Debug.DrawRay(_cameraTransform.position, _cameraTransform.right * 2f, Color.red);
            }

            if (Keyboard.current.rKey.isPressed)
            {
                targetVerticalLean = 1f; // Upward
                Debug.DrawRay(_cameraTransform.position, Vector3.up * 2f, Color.yellow);
            }

            // Apply collision detection for horizontal peeking
            if (targetLean != 0f)
            {
                targetLean = ApplyHorizontalPeekCollision(targetLean);
            }

            // Apply collision detection for vertical peeking
            if (targetVerticalLean != 0f)
            {
                targetVerticalLean = ApplyVerticalPeekCollision(targetVerticalLean);
            }

            // Calculate contributions based on camera orientation
            float forwardX = forward.x;
            float forwardZ = forward.z;

            float rollContribution = forwardZ * targetLean;
            float pitchContribution = -forwardX * targetLean;

            // Smooth interpolation
            _peekAmount = Mathf.Lerp(_peekAmount, rollContribution, Time.deltaTime * peekSpeed);
            _peekForwardAmount = Mathf.Lerp(_peekForwardAmount, pitchContribution, Time.deltaTime * peekSpeed);
            _peekVerticalAmount = Mathf.Lerp(_peekVerticalAmount, targetVerticalLean, Time.deltaTime * peekSpeed);

            // Apply rotation and position
            float roll = _peekAmount * peekAngle;
            float pitch = _peekForwardAmount * peekAngle;

            float offsetX = _peekAmount * peekOffset;
            float offsetZ = _peekForwardAmount * peekOffset;
            float offsetY = _peekVerticalAmount * peekOffset;

            cameraLeanPivot.localRotation = Quaternion.Euler(pitch, 0f, -roll);
            cameraLeanPivot.localPosition = new Vector3(offsetX, offsetY, offsetZ);
        }

        private float ApplyHorizontalPeekCollision(float targetLean)
        {
            Vector3 peekDirection = targetLean < 0 ? -_cameraTransform.right : _cameraTransform.right;
            
            if (Physics.BoxCast(
                headTransform.position,
                new Vector3(0.2f, 0.2f, 0.2f),
                peekDirection,
                out RaycastHit hitInfo,
                headTransform.rotation,
                peekOffset,
                wallLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                float distanceToWall = hitInfo.distance;
                float allowedPeek = Mathf.Max(0f, distanceToWall - 0.1f);
                float peekRatio = allowedPeek / peekOffset;
                return targetLean * peekRatio;
            }

            return targetLean;
        }

        private float ApplyVerticalPeekCollision(float targetVerticalLean)
        {
            if (Physics.BoxCast(
                headTransform.position,
                new Vector3(0.2f, 0.2f, 0.2f),
                Vector3.up,
                out RaycastHit hitInfo,
                headTransform.rotation,
                peekOffset,
                wallLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                float distanceToWall = hitInfo.distance;
                float allowedPeek = Mathf.Max(0f, distanceToWall - 0.1f);
                float peekRatio = allowedPeek / peekOffset;
                return targetVerticalLean * peekRatio;
            }

            return targetVerticalLean;
        }

        #endregion
        
        
        #region Headbob Helper Functions
        private float GetZBobAmount(bool isSprinting, bool isCrouching)
        {
            if (isSprinting) return sprintBobZAmount;
            if (isCrouching) return crouchBobZAmount;
            return walkBobZAmount;
        }
        private float GetBobSpeed(bool isSprinting, bool isCrouching)
        {
            if (isSprinting) return sprintBobSpeed;
            if (isCrouching) return crouchBobSpeed;
            return walkBobSpeed;
        }
        private float GetYBobAmount(bool isSprinting, bool isCrouching)
        {
            if (isSprinting) return sprintBobYAmount;
            if (isCrouching) return crouchBobYAmount;
            return walkBobYAmount;
        }
        private float GetXBobAmount(bool isSprinting, bool isCrouching)
        {
            if (isSprinting) return sprintBobXAmount;
            if (isCrouching) return crouchBobXAmount;
            return walkBobXAmount;
        }
        #endregion
    }


}