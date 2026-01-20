using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
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

        // I'm so sorry that the changes I'm about to make are so sloppy :(
        [SerializeField] private Attractor stepSoundsAI;

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 8.0f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [SerializeField] private float speedChangeRate = 10f;
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

        [Space(10)]
        [Header("References")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference crouchAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference flashlightToggleAction;
        [SerializeField] private GameObject[] ObjectsToDisableOnCutscene;
        
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform head;
        [Header("Camera Effects")]
        [SerializeField] private CameraEffectsSystems cameraEffects;
        
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
        private float _currentSpeed;

        
        // Local reference that the controller cares about
        private Types.PlayerHealthState currentPlayerHealthState;
        private Types.PlayerMovementState _playerMovementState;
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
            cameraEffects.UpdateEffects(_isGrounded, IsPlayerMoving(), _isSprinting, _isCrouching);
            

            
        }

        private void HandleStateDetection()
        {
            
            // each one we will check in order of precedence, as each later one can override the earlier ones
            Types.PlayerMovementState checkState = Types.PlayerMovementState.Idle; // default state
            // First check if we are IDLE
            if (!IsPlayerMoving() && _isGrounded)
            {
                checkState = Types.PlayerMovementState.Idle;_currentSpeed = walkSpeed;

            }
            // Next check if we are CROUCHING_IDLE
            if (_isCrouching && !IsPlayerMoving() && _isGrounded)
            {
                checkState = Types.PlayerMovementState.CrouchIdle;
            }
            // next check if we are CROUCH WALKING
            if (_isCrouching && IsPlayerMoving() && _isGrounded)
            {
                checkState = Types.PlayerMovementState.CrouchWalking;
            }
            // next check if we are we walking (not sprinting or crouching)
            if (!_isCrouching && !_isSprinting && IsPlayerMoving() && _isGrounded)
            {
                checkState = Types.PlayerMovementState.Walking;
            }
            // finally check if we are SPRINTING
            if (!_isCrouching && _isSprinting && IsPlayerMoving() && _isGrounded)
            {
                checkState = Types.PlayerMovementState.Sprinting;
            }
            
            SwitchMovementState(checkState);
        }

        private void SwitchMovementState(Types.PlayerMovementState movementState)
        {
            // if the current state is the same as the new state, do nothing
            if (_playerMovementState == movementState) { return; }
            
            // set our current state to the new state
            _playerMovementState = movementState;
            
            // Now we can handle the state switch logic (if there is any)
            switch (movementState)
            {
                case Types.PlayerMovementState.Idle:
                    // logic for entering idle state
                    DebugUtils.Log("PlayerController: Entered Idle State");
                    stepSoundsAI.intensity = 0;
                    break;
                case Types.PlayerMovementState.Walking:
                    // logic for entering walking state
                    DebugUtils.Log("PlayerController: Entered Walking State");
                    stepSoundsAI.intensity = 5;
                    _audioEffectSpeed = 0.5f;
                    break;
                case Types.PlayerMovementState.Sprinting:
                    // logic for entering sprinting state
                    DebugUtils.Log("PlayerController: Entered Sprinting State");
                    stepSoundsAI.intensity = 7;
                    _audioEffectSpeed = 0.3f;
                    break;
                case Types.PlayerMovementState.CrouchIdle:
                    // logic for entering crouch idle state
                    DebugUtils.Log("PlayerController: Entered Crouch Idle State");
                    stepSoundsAI.intensity = 0;
                    break;
                case Types.PlayerMovementState.CrouchWalking:
                    // logic for entering crouch walking state
                    DebugUtils.Log("PlayerController: Entered Crouch Walking State");
                    stepSoundsAI.intensity = 3;
                    _audioEffectSpeed = 0.7f;
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
            _currentSpeed = walkSpeed;

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
            flashlightToggleAction.action.performed += OnFlashlightToggle;
            
            
        }

        private void OnFlashlightToggle(InputAction.CallbackContext obj)
        {
            if(_lockedInput){ return; }
            // Logic to toggle flashlight
            Flashlight.Instance.ToggleFlashlight();
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
            
            // edge case, we can STOP sprinting mid-air, but cannot START sprinting mid-air
            if (!_isGrounded)
            {
                // if we are trying to start sprinting mid-air, cache it as true
                if (obj.performed)
                {
                    _cachedSprintState = true;
                }
                else
                {
                    _cachedSprintState = false;
                }
                _isSprinting = false;
                return;
            }
            
            if(_lockedInput){ return; }
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
            
            cameraEffects.UpdateCameraBaseY(_cameraBaseY);

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
            if (_lockedInput) { return; }

            Vector3 moveDirection = _cameraTransform.TransformDirection(new Vector3(_moveInput.x, 0, _moveInput.y)).normalized;

            float targetSpeed = _isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);

            // Smoothly interpolate speed
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

            Vector3 velocity = moveDirection * _currentSpeed; velocity.y = _verticalVelocity;

            CollisionFlags collisions = _characterController.Move(velocity * Time.deltaTime);

            if ((collisions & CollisionFlags.Above) != 0)
            {
                _verticalVelocity = initialFallVelocity;
            }
        }

        #endregion
        
        #region Sound Management
        private string _surfaceType;
        private float _audioEffectSpeed = 0.5f; // time between footstep sounds
        /// <summary>
        /// Function used to detect the surface type the player is currently on
        ///
        /// Works by shooting a raycast downwards and checking the tag of the hit collider
        /// </summary>
        private void DetectSurfaceAndMovement()
        {
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f)) return;
            _surfaceType = hit.collider.tag.ToLower() switch
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
                
                // if we are not moving, do not play footstep sounds
                if (!IsPlayerMoving())
                {
                    yield return null;
                    continue;
                }

                switch (_surfaceType)
                {
                    case "grass":
                        Debug.Log("Playing grass sound");
                        AudioManager.Instance.PlayPlayerWalkingGrass();
                        break;
                    case "gravel":
                        Debug.Log("Playing gravel sound");
                        AudioManager.Instance.PlayPlayerWalkingGravel();
                        break;
                    case "water":
                        Debug.Log("Playing water sound");
                        AudioManager.Instance.PlayPlayerWalkingWater();
                        break;
                    case "metal":
                        Debug.Log("Playing metal sound");
                        AudioManager.Instance.PlayPlayerWalkingMetal();
                        break;
                    case "concrete":
                        Debug.Log("Playing concrete sound");
                        AudioManager.Instance.PlayPlayerWalkingConcrete();
                        break;
                    case "wood":
                        Debug.Log("Playing wood sound");
                        AudioManager.Instance.PlayPlayerWalkingWood();
                        break;
                    default:
                        yield return null;
                        break;
                }
                
                yield return new WaitForSeconds(_audioEffectSpeed);

            }
        }
        
        #endregion
        
        
        private void OnGameStateChanged(Types.GameState newState)
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
                case Types.GameState.MainMenu:
                    HandleMainMenuState();
                    break;
                case Types.GameState.Inspecting:
                    HandleInspectionState();
                    break;
                // handle other game states as needed
            }
        }

        private void HandleMainMenuState()
        {
            _lockedInput = true;
            for (int i = 0; i < ObjectsToDisableOnCutscene.Length; i++)
            {
                ObjectsToDisableOnCutscene[i].SetActive(false);
            }
            
            // check if the flashlight is on
            if (Flashlight.Instance.IsFlashlightOn())
            {
                Flashlight.Instance.ToggleFlashlight();
            }
        }
        private void HandleGameplayState()
        {
            // Return to basic player controls
            _lockedInput = false;
            for (int i = 0; i < ObjectsToDisableOnCutscene.Length; i++)
            {
                ObjectsToDisableOnCutscene[i].SetActive(true);
            }
        }
        private void HandleCutsceneState()
        {
            // Disable player controls for cutscene
            _lockedInput = true;
            DebugUtils.LogError("PlayerController: Input locked due to Cutscene state!!!!");
            // disable the head so its hidden
            for (int i = 0; i < ObjectsToDisableOnCutscene.Length; i++)
            {
                ObjectsToDisableOnCutscene[i].SetActive(false);
            }
            
            // check if the flashlight is on
            if (Flashlight.Instance.IsFlashlightOn())
            {
                Flashlight.Instance.ToggleFlashlight();
            }
            StopAllPlayerMovement();
        }

        private void HandleInspectionState()
        {
            // Disable player controls for cutscene
            _lockedInput = true;
            // disable the head so its hidden
            for (int i = 0; i < ObjectsToDisableOnCutscene.Length; i++)
            {
                ObjectsToDisableOnCutscene[i].SetActive(false);
            }
        }

        #region Helper Function
        /// <summary>
        /// A series of various helpers to determine possible things about the player, mostly generic movement
        /// </summary>
        
        // Determine if the player is moving
        public bool IsPlayerMoving()
        {
            // Check if the player is moving based on input magnitude (anything higher than 0.05 is considered moving)
            return _moveInput.magnitude > 0.05f;
        }
        
        public void LockInput()
        {
            _lockedInput = true;
        }
        public void UnlockInput()
        {
            _lockedInput = false;
        }
        public float GetDistanceToPlayer(Vector3 position)
        {
            return Vector3.Distance(position, transform.position);
        }

        private void StopAllPlayerMovement()
        {
            _isCrouching = false;
            _cachedSprintState = false;
            _isSprinting = false;
            _moveInput = Vector2.zero;
        }
        
        
        #endregion
        
    }
}
