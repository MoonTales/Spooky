using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using XtremeFPS.FPSController;
using Random = Unity.Mathematics.Random;
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
        [SerializeField] private float walkBobZAmount = 0.02f;
        [SerializeField] private float sprintBobZAmount = 0.04f;
        [SerializeField] private float crouchBobZAmount = 0.01f;
        [SerializeField] private float walkBobXAmount = 0.02f;
        [SerializeField] private float sprintBobXAmount = 0.035f;
        [SerializeField] private float crouchBobXAmount = 0.01f;

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
        [SerializeField] private Types.PlayerMovementState PlayerMovementState;
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
            
            HandleStateDetection();
            DetectSurfaceAndMovement();
            
            HandleGravity();
            HandleMovement();
            HandleCrouchTransition();
            HandleHeadBob();
            HandlePeeking();
            
        }

        private void HandleStateDetection()
        {
            
            // each one we will check in order of precedence, as each later one can override the earlier ones
            Types.PlayerMovementState checkState = Types.PlayerMovementState.Idle; // default state
            // First check if we are IDLE
            if (_moveInput.magnitude < 0.1f && _isGrounded)
            {
                checkState = Types.PlayerMovementState.Idle;
            }
            // Next check if we are CROUCHING_IDLE
            if (_isCrouching && _moveInput.magnitude < 0.1f && _isGrounded)
            {
                checkState = Types.PlayerMovementState.CrouchIdle;
            }
            // next check if we are CROUCH WALKING
            if (_isCrouching && _moveInput.magnitude >= 0.1f && _isGrounded)
            {
                checkState = Types.PlayerMovementState.CrouchWalking;
            }
            // next check if we are we walking (not sprinting or crouching)
            if (!_isCrouching && !_isSprinting && _moveInput.magnitude >= 0.1f && _isGrounded)
            {
                checkState = Types.PlayerMovementState.Walking;
            }
            // finally check if we are SPRINTING
            if (!_isCrouching && _isSprinting && _moveInput.magnitude >= 0.1f && _isGrounded)
            {
                checkState = Types.PlayerMovementState.Sprinting;
            }
            
            SwitchMovementState(checkState);
        }

        private void SwitchMovementState(Types.PlayerMovementState movementState)
        {
            // if the current state is the same as the new state, do nothing
            if (PlayerMovementState == movementState) { return; }
            
            // set our current state to the new state
            PlayerMovementState = movementState;
            
            // Now we can handle the state switch logic (if there is any)
            switch (movementState)
            {
                case Types.PlayerMovementState.Idle:
                    // logic for entering idle state
                    DebugUtils.Log("PlayerController: Entered Idle State");
                    break;
                case Types.PlayerMovementState.Walking:
                    // logic for entering walking state
                    DebugUtils.Log("PlayerController: Entered Walking State");
                    break;
                case Types.PlayerMovementState.Sprinting:
                    // logic for entering sprinting state
                    DebugUtils.Log("PlayerController: Entered Sprinting State");
                    break;
                case Types.PlayerMovementState.CrouchIdle:
                    // logic for entering crouch idle state
                    DebugUtils.Log("PlayerController: Entered Crouch Idle State");
                    break;
                case Types.PlayerMovementState.CrouchWalking:
                    // logic for entering crouch walking state
                    DebugUtils.Log("PlayerController: Entered Crouch Walking State");
                    break;
                default:
                    // handle other states if any
                    break;
            }
        }

        #region Initialization
        private void Awake()
        {
            // set up initial character variables
            _characterController = GetComponent<CharacterController>();
            _targetHeight = standHeight;
            _cameraBaseY = _cameraTransform.localPosition.y;

        }

        private void Start()
        {
            StartCoroutine(PlayFootstepSounds());
        }
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnGameStateChanged += OnGameStateChanged,
                () => EventBroadcaster.OnGameStateChanged -= OnGameStateChanged);
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
        #endregion

        
        #region Movement Methods
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
        private void HandleGravity()
        {
            if (_isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = initialFallVelocity;
            }
            _verticalVelocity += gravity * Time.deltaTime;
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
        #endregion
        
        #region Headbob and Peaking (refactor to new script soon)
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
                // boxcast, which will be the size of the "head"
                if (Physics.BoxCast(
                        head.position,
                        new Vector3(0.2f, 0.2f, 0.2f),
                        peekDirection,
                        out hitInfo,
                        head.rotation,
                        peekOffset,
                        WallLayerMask,
                        QueryTriggerInteraction.Ignore
                    )){
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
            
            // edge case
            // if we happen to be crouching and sprinting at the same time, prioritize crouch headbob
            if(_isCrouching && _isSprinting)
            {
                _isSprinting = false;
            }
            
            if (_moveInput.magnitude > 0.1f)
            {
                float bobSpeed = _isSprinting
                    ? sprintBobSpeed
                    : (_isCrouching ? crouchBobSpeed : walkBobSpeed);

                float yBobAmount = _isSprinting
                    ? sprintBobAmount
                    : (_isCrouching ? crouchBobAmount : walkBobAmount);

                float xBobAmount = _isSprinting
                    ? sprintBobXAmount
                    : (_isCrouching ? crouchBobXAmount : walkBobXAmount);

                float zBobAmount = _isSprinting
                    ? sprintBobZAmount
                    : (_isCrouching ? crouchBobZAmount : walkBobZAmount);

                time += Time.deltaTime * bobSpeed;

                // Vertical bob
                float yOffset = Mathf.Sin(time) * yBobAmount;

                // Left / Right sway (alternates per step)
                float xOffset = Mathf.Sin(time * 0.5f) * xBobAmount;

                // Forward / Back bob
                float zOffset = Mathf.Cos(time * 0.5f) * zBobAmount;

                Vector3 cameraPosition = _cameraTransform.localPosition;
                cameraPosition.y = _cameraBaseY + yOffset;
                cameraPosition.x = xOffset;
                cameraPosition.z = zOffset;
                _cameraTransform.localPosition = cameraPosition;
            }
            else
            {
                time = 0f;

                Vector3 cameraPosition = _cameraTransform.localPosition;
                cameraPosition.y = Mathf.Lerp(cameraPosition.y, _cameraBaseY, Time.deltaTime * 5f);
                cameraPosition.x = Mathf.Lerp(cameraPosition.x, 0f, Time.deltaTime * 5f);
                cameraPosition.z = Mathf.Lerp(cameraPosition.z, 0f, Time.deltaTime * 5f);
                _cameraTransform.localPosition = cameraPosition;
            }
        }
        #endregion

        
        #region Sound Management
        public string SurfaceType { get; private set; }
        private AudioSource audioSource;
        public AudioClip[] soundConcrete;
        /// <summary>
        ///  Function used to detect the surface type the player is currently on
        ///
        /// Works by shooting a raycast downwards and checking the tag of the hit collider
        /// </summary>
        private void DetectSurfaceAndMovement()
        {
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f)) return;
            SurfaceType = hit.collider.tag.ToLower() switch
            {
                "grass" => "grass",
                "metals" => "metal",
                "gravel" => "gravel",
                "water" => "water",
                "concrete" => "concrete",
                "wood" => "wood",
                _ => "Unknown",
            };
        }
        private IEnumerator PlayFootstepSounds()
        {
            while (true)
            {
                if (!_isGrounded )
                {
                    yield return null;
                    continue;
                }

                switch (SurfaceType)
                {
                    case "grass":
                        //audioSource.clip = soundGrass[Random.Range(0, soundGrass.Length)];
                        Debug.Log("Playing grass sound");
                        break;
                    case "gravel":
                        //audioSource.clip = soundGravel[Random.Range(0, soundGravel.Length)];
                        Debug.Log("Playing gravel sound");
                        break;
                    case "water":
                        //audioSource.clip = soundWater[Random.Range(0, soundWater.Length)];
                        Debug.Log("Playing water sound");
                        break;
                    case "metal":
                        //audioSource.clip = soundMetal[Random.Range(0, soundMetal.Length)];
                        Debug.Log("Playing metal sound");
                        break;
                    case "concrete":
                        //audioSource.clip = soundConcrete[Random.Range(0, soundConcrete.Length)];
                        Debug.Log("Playing concrete sound");
                        break;
                    case "wood":
                        //audioSource.clip = soundWood[Random.Range(0, soundWood.Length)];
                        Debug.Log("Playing wood sound");
                        break;
                    default:
                        yield return null;
                        break;
                }

                //if (audioSource.clip != null)
                //{
                    //audioSource.PlayOneShot(audioSource.clip);
                    //yield return new WaitForSeconds(0.5f/*AudioEffectSpeed*/);
                //}
                //else yield return null;
                yield return null;
            }
        }
        
        #endregion
        
        
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

        
    }
}
