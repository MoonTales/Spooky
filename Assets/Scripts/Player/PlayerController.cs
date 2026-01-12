using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Types = System.Types;

namespace Player
{
    /// <summary>
    /// Class used to handle player input and control the player character
    /// Also will listen to player state changes and adjust controls accordingly
    /// </summary>
    public class PlayerController : EventSubscriberBase
    {
        
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 8.0f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [Space(10)]
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 7.0f;
        [SerializeField] private float gravity = -12.0f;
        [SerializeField] private float initialFallVelocity = -2.0f;
        [Space(10)]
        [Header("Crouching")]
        [SerializeField] private float standHeight = 2.0f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float crouchTransitionSpeed = 10.0f;
        [SerializeField] private float cameraCrouchOffset = 0.4f;
        [Header("Peeking")]
        [SerializeField] private float peekAngle = 15f;
        [SerializeField] private float peekOffset = 0.25f;
        [SerializeField] private float peekSpeed = 10f;
        [SerializeField] private LayerMask WallLayerMask;
        private float _peekAmount;
        private float _peekForwardAmount;
        [Space(10)]
        [Header("Headbob")]
        [SerializeField] private float walkBobSpeed = 14.0f;
        [SerializeField] private float walkBobAmount = 0.05f;
        [SerializeField] private float sprintBobSpeed = 18.0f;
        [SerializeField] private float sprintBobAmount = 0.1f;
        [SerializeField] private float crouchBobSpeed = 8.0f;
        [SerializeField] private float crouchBobAmount = 0.025f;
        [Space(10)]
        [Header("References")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference crouchAction;
        [SerializeField] private InputActionReference sprintAction;
        
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform cameraLeanPivot;
        [SerializeField] private Transform head;
        /* Internal variables */
        
        private CharacterController _characterController;
        private Vector2 _moveInput;
        private bool _isGrounded;
        private bool _isCrouching;
        private bool _isSprinting;
        private bool _cachedSprintState;
        private float _verticalVelocity;
        private float _targetHeight;

        private bool _lockedInput = false;
        private float time;
        
        private float _cameraBaseY;
        private float _headBobOffset;

        
        // Local reference that the controller cares about
        [SerializeField] private Types.PlayerHealthState currentPlayerHealthState;


        private void Awake()
        {
            // set up initial character variables
            _characterController = GetComponent<CharacterController>();
            _targetHeight = standHeight;
            _cameraBaseY = _cameraTransform.localPosition.y;

        }
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnGameStateChanged += OnGameStateChanged,
                () => EventBroadcaster.OnGameStateChanged -= OnGameStateChanged);
        }

        private void Update()
        {
            
            // debug print if input is locked
            if(_lockedInput){

                CinemachineInputAxisController axisController =
                    _cameraTransform.GetComponent<CinemachineInputAxisController>();

                if (axisController != null)
                {
                    axisController.enabled = false;
                }

            }
            else
            {
                CinemachineInputAxisController axisController =
                    _cameraTransform.GetComponent<CinemachineInputAxisController>();
                axisController.enabled = true;

            }
            
            _isGrounded = _characterController.isGrounded;
            // check the cached sprint state when we land, incase it changed mid-air
            if (_isGrounded && !_isSprinting)
            {
                _isSprinting = _cachedSprintState;
            }
            else if (!_isGrounded)
            {
                _isSprinting = false; // cannot sprint in mid-air
            }
            
            HandleGravity();

            HandleMovement();
            HandleCrouchTransition();
            HandleHeadBob();
            HandlePeeking();
            
        }
        
        private void HandlePeeking()
        {
            
            
            
            float targetLean = 0f;

            Vector3 forward = _cameraTransform.forward;
            Debug.DrawRay(_cameraTransform.position, forward * 2f, Color.green);

            if (Keyboard.current.qKey.isPressed)
            {
                targetLean = -1f; // World left
                Vector3 left = -_cameraTransform.right;
                Debug.DrawRay(_cameraTransform.position, left * 2f, Color.blue);
            }
            else if (Keyboard.current.eKey.isPressed)
            {
                targetLean = 1f; // World right
                Vector3 right = _cameraTransform.right;
                Debug.DrawRay(_cameraTransform.position, right * 2f, Color.red);
            }
            // attempt to raycast to a wall in the direction of the peek
            Vector3 peekDirection = targetLean < 0 ? -_cameraTransform.right : _cameraTransform.right;
            RaycastHit hitInfo;
            if (targetLean != 0f)
            {
                if (Physics.Raycast(_cameraTransform.position, peekDirection, out hitInfo, peekOffset + 0.1f, WallLayerMask))
                {
                    // we hit a wall, so we cannot peek fully
                    float distanceToWall = hitInfo.distance;
                    float allowedPeek = Mathf.Max(0f, distanceToWall - 0.1f); // leave a small buffer
                    float peekRatio = allowedPeek / peekOffset;
                    targetLean *= peekRatio;
                }
            }

            // Get the camera's forward vector components in world space
            float forwardX = forward.x; // A component (east/west)
            float forwardZ = forward.z; // B component (north/south)

            // E/Q contributes to both roll and pitch based on camera orientation
            // Roll: controlled by how much camera faces north/south
            // Pitch: controlled by how much camera faces east/west
            float rollContribution = forwardZ * targetLean;
            float pitchContribution = -forwardX * targetLean; // Negative for correct direction

            _peekAmount = Mathf.Lerp(_peekAmount, rollContribution, Time.deltaTime * peekSpeed);
            _peekForwardAmount = Mathf.Lerp(_peekForwardAmount, pitchContribution, Time.deltaTime * peekSpeed);

            // Apply roll and pitch
            float roll = _peekAmount * peekAngle;
            float pitch = _peekForwardAmount * peekAngle;
    
            float offsetX = _peekAmount * peekOffset;
            float offsetZ = _peekForwardAmount * peekOffset;

            cameraLeanPivot.localRotation = Quaternion.Euler(pitch, 0f, -roll);
            cameraLeanPivot.localPosition = new Vector3(offsetX, cameraLeanPivot.localPosition.y, offsetZ);
        }








        
        private void HandleHeadBob()
        {
            if (!_isGrounded)
            {
                _headBobOffset = 0f;
                return;
            }

            if (_moveInput.magnitude > 0.1f)
            {
                float bobSpeed = _isSprinting ? sprintBobSpeed : (_isCrouching ? crouchBobSpeed : walkBobSpeed);

                float bobAmount = _isSprinting
                    ? sprintBobAmount
                    : (_isCrouching ? crouchBobAmount : walkBobAmount);

                time += Time.deltaTime * bobSpeed;
                _headBobOffset = Mathf.Sin(time) * bobAmount;
            }
            else
            {
                time = 0f;
                _headBobOffset = Mathf.Lerp(_headBobOffset, 0f, Time.deltaTime * 5f);
            }

            Vector3 cameraPosition = _cameraTransform.localPosition;
            cameraPosition.y = _cameraBaseY + _headBobOffset;
            _cameraTransform.localPosition = cameraPosition;
        }


        protected override void OnEnable()
        {
            base.OnEnable();
            
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMovePerformed;
            jumpAction.action.performed += OnJump;
            crouchAction.action.performed += OnCrouch;
            sprintAction.action.performed += OnSprint;
            sprintAction.action.canceled += OnSprint;
            
            
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMovePerformed;
            jumpAction.action.performed -= OnJump;
            crouchAction.action.performed -= OnCrouch;
            sprintAction.action.performed -= OnSprint;
            sprintAction.action.canceled -= OnSprint;
        }

        private void OnSprint(InputAction.CallbackContext obj)
        {
            if(_lockedInput){ return; }
            if (!_isGrounded) { return; }
            if (_isCrouching) { return; } // cannot sprint while crouching
            // only allow sprinting to change if we are grounded
            // regardless of whether we can sprint, we want to cache the sprint state for when we land
            _cachedSprintState = obj.performed;

            _isSprinting = obj.performed;
        }

        private void OnCrouch(InputAction.CallbackContext obj)
        {
            if(_lockedInput){ return; }
            if (_isCrouching)
            {
                if (!CanStandUp()) { return;}
                _targetHeight = standHeight;
            } else
            {
                _targetHeight = crouchHeight;
            }
            _isCrouching = !_isCrouching;
            time = 0f;

        }

        private bool CanStandUp()
        {
            float currentHeight = _characterController.height;
            float radius = _characterController.radius;

            // No need to check if we're already tall enough
            float growAmount = standHeight - currentHeight;
            if (growAmount <= 0f)
                return true;

            // World-space bottom of capsule
            Vector3 bottom = transform.position +
                             _characterController.center -
                             Vector3.up * (currentHeight / 2f - radius);

            // Current top of capsule
            Vector3 top = bottom + Vector3.up * (currentHeight - radius * 2f);

            // Cast upward only the missing height
            bool hit = Physics.CapsuleCast(
                bottom,
                top,
                radius,
                Vector3.up,
                growAmount,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            return !hit;
        }


        private void OnMovePerformed(InputAction.CallbackContext obj)
        {
            if(_lockedInput){ return; }
            _moveInput = obj.ReadValue<Vector2>();
        }
        
        private void OnJump(InputAction.CallbackContext obj)
        {
            
            if(_lockedInput){ return; }
            if (_isCrouching) { return;}
            if(_isGrounded)
            {
                // Apply jump force
                _verticalVelocity = jumpForce;
            }
        }

        private void HandleMovement()
        {
            if(_lockedInput){ return; }
            
            Vector3 moveDirection = _cameraTransform.TransformDirection(new Vector3(_moveInput.x, 0, _moveInput.y)).normalized;
            float currentSpeed = _isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);
            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = _verticalVelocity;
            CollisionFlags collisions = _characterController.Move(velocity * Time.deltaTime);
            if ((collisions & CollisionFlags.Above) != 0)
            {
                _verticalVelocity = initialFallVelocity;
            }
        }

        private void HandleGravity()
        {
            if (_isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = initialFallVelocity;
            }
            _verticalVelocity += gravity * Time.deltaTime;
        }

        protected void OnGameStateChanged(Types.GameState newState)
        {
            DebugUtils.Log("PlayerController: Game state changed to: " + newState.ToString());
            switch (newState)
            {
                case Types.GameState.Gameplay:
                    HandleGameplayState();
                    break;
                case Types.GameState.Cutscene:
                    HandleCutsceneState();
                    break;
                // handle other game states as needed
            }
        }

        private void HandleGameplayState()
        {
            // Return to basic player controls
            _lockedInput = false;
        }
        private void HandleCutsceneState()
        {
            // Disable player controls for cutscene
            _lockedInput = true;
            DebugUtils.LogError("PlayerController: Input locked due to Cutscene state!!!!");
        }

        private void HandleCrouchTransition()
        {
            float currentHeight = _characterController.height;
            if (Mathf.Approximately(currentHeight, _targetHeight))
            {
                _characterController.height = _targetHeight;
                return;
            }
            // perform the transition
            float newHeight = Mathf.Lerp(currentHeight, _targetHeight, crouchTransitionSpeed * Time.deltaTime);
            _characterController.height = newHeight;
            _characterController.center = Vector3.up * (newHeight / 2); // we crouch to half the height
            
            float targetCameraBaseY = _targetHeight - cameraCrouchOffset;

            _cameraBaseY = Mathf.Lerp(
                _cameraBaseY,
                targetCameraBaseY,
                crouchTransitionSpeed * Time.deltaTime
            );

        }
    }
}
