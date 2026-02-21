using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = Unity.Mathematics.Random;
using Types = System.Types;

namespace Player
{
    /// <summary>
    /// Class used to handle player input and control the player character
    /// Also will listen to player state changes and adjust controls accordingly
    /// </summary>
    public class PlayerController : Singleton<PlayerController>
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
        
        [Space(10)]
        [Header("Audio Guards")]
        [SerializeField] private float landingMinAirborneTimeForSfx = 0.5f; // Minimum airborne time before a grounded edge is treated as a real landing.
        [SerializeField] private float landingMinDropDistanceForSfx = 0.5f; // Minimum vertical drop to suppress rapid stair-step landing retriggers.

        [Space(10)]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform head;
        [Header("Camera Effects")]
        [SerializeField] private CameraEffectsSystems cameraEffects;
        
        /* Internal variables */
        private CharacterController _characterController;
        private Vector2 _moveInput;
        private bool _isGrounded; public bool IsGrounded() { return _isGrounded; }
        private bool _wasGrounded;
        private bool _isCrouching;
        private bool _isSprinting;
        private bool _cachedSprintState;
        private float _verticalVelocity;
        private float _targetHeight;
        private bool _lockedInput = false;
        private float _cameraBaseY;
        private float _currentSpeed;
        private bool _isInspecting = false;

        // Audio internals
        private string _surfaceType;
        private float _audioEffectSpeed = 0.5f; // time between footstep sounds
        private bool _jumpSfxArmed;
        private bool _jumpRequested;
        private bool _landingAudioArmed;
        private bool _hasGameplayAirborneToken;
        private float _gameplayAirborneTime;
        private float _gameplayAirbornePeakY;
        private bool _suppressNextLandingAfterPause;

        // Local reference that the controller cares about
        private Types.PlayerMentalState _currentPlayerMentalState;
        private Types.PlayerMovementState _playerMovementState;
        private void FixedUpdate()
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
            Types.GameState currentGameState = GetCurrentGameState();
            HandleLandingSfx(currentGameState);
            SyncJumpAudioTracking(currentGameState);
            HandleJumpRequest(currentGameState);
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
            
            _wasGrounded = _isGrounded;
            
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
                    stepSoundsAI.intensity = 0;
                    break;
                case Types.PlayerMovementState.Walking:
                    // logic for entering walking state
                    stepSoundsAI.intensity = 5;
                    _audioEffectSpeed = 0.5f;
                    break;
                case Types.PlayerMovementState.Sprinting:
                    // logic for entering sprinting state
                    stepSoundsAI.intensity = 7;
                    _audioEffectSpeed = 0.3f;
                    break;
                case Types.PlayerMovementState.CrouchIdle:
                    // logic for entering crouch idle state
                    stepSoundsAI.intensity = 0;
                    break;
                case Types.PlayerMovementState.CrouchWalking:
                    // logic for entering crouch walking state
                    stepSoundsAI.intensity = 3;
                    _audioEffectSpeed = 0.7f;
                    break;
                default:
                    // handle other states if any
                    break;
            }
        }

        #region Initialization

        protected override void Awake()
        {
            base.Awake();
            // set up initial character variables
            _characterController = GetComponent<CharacterController>();
            _targetHeight = standHeight;
            _cameraBaseY = _cameraTransform.localPosition.y;

        }

        private void Start()
        {
            StartCoroutine(PlayFootstepSounds());
            _currentSpeed = walkSpeed;
            _isGrounded = _characterController.isGrounded;
            _wasGrounded = _isGrounded;
            _landingAudioArmed = GetCurrentGameState() == Types.GameState.Gameplay && _isGrounded;
            _hasGameplayAirborneToken = false;
            _gameplayAirborneTime = 0f;
            _suppressNextLandingAfterPause = false;
            _jumpSfxArmed = GetCurrentGameState() == Types.GameState.Gameplay && !IsJumpActionPressed();
            _jumpRequested = false;

        }
        protected override void OnEnable()
        {
            base.OnEnable();
            
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMovePerformed;
            jumpAction.action.performed += OnJump;
            jumpAction.action.canceled += OnJumpCanceled;
            crouchAction.action.performed += OnCrouch;
            sprintAction.action.performed += OnSprint;
            sprintAction.action.canceled += OnSprint;
            flashlightToggleAction.action.performed += OnFlashlightToggle;
        }

        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
            TrackSubscription(() => SceneManager.sceneLoaded += OnSceneLoaded,
                () => SceneManager.sceneLoaded -= OnSceneLoaded);
        }

        private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
        {
            if (_isCrouching)
            {
                ForceCrouch();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetLandingAudioTracking(disarmUntilGrounded: true);
            _suppressNextLandingAfterPause = false;
            ResetJumpAudioTracking(disarmUntilRelease: true);
        }
        

        private void OnFlashlightToggle(InputAction.CallbackContext obj)
        {
            if(_lockedInput){ return; }

            if (GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Nightmare && GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Tutorial) { return;}
            // Logic to toggle flashlight
            // we can just do a check here, to make sure we are not in the pause meny gamestate
            // there is other places this can go, but this works and its easy
            if (GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Paused) { return; }
            Flashlight.Instance.ToggleFlashlight();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMovePerformed;
            jumpAction.action.performed -= OnJump;
            jumpAction.action.canceled -= OnJumpCanceled;
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
        }

        public void ForceCrouch()
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
        }
        private void OnJump(InputAction.CallbackContext obj)
        {
            if(_lockedInput){ return; }
            _jumpRequested = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext obj)
        {
            _jumpSfxArmed = GetCurrentGameState() == Types.GameState.Gameplay;
        }
        private void HandleGravity()
        {
            // Freeze vertical integration outside gameplay so pause/cutscene transitions do not accumulate fake fall velocity.
            if (GetCurrentGameState() != Types.GameState.Gameplay)
            {
                if (_isGrounded && _verticalVelocity < 0f)
                {
                    _verticalVelocity = initialFallVelocity;
                }
                return;
            }

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
            float newHeight = Mathf.Lerp(currentHeight, _targetHeight, crouchTransitionSpeed * Time.fixedDeltaTime);
            _characterController.height = newHeight;
            _characterController.center = Vector3.up * (newHeight / 2); // we crouch to half the height
            
            float targetCameraBaseY = _targetHeight - cameraCrouchOffset;

            _cameraBaseY = Mathf.Lerp(
                _cameraBaseY,
                targetCameraBaseY,
                crouchTransitionSpeed * Time.fixedDeltaTime
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
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, speedChangeRate * Time.fixedDeltaTime);

            Vector3 velocity = moveDirection * _currentSpeed; velocity.y = _verticalVelocity;

            CollisionFlags collisions = _characterController.Move(velocity * Time.fixedDeltaTime);

            if ((collisions & CollisionFlags.Above) != 0 && _verticalVelocity > 0)
            {
                _verticalVelocity = 0;
            }
        }

        #endregion
        
        #region Sound Management
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
                bool isGameplayState = GameStateManager.Instance != null
                                       && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay;
                if (!isGameplayState)
                {
                    yield return null;
                    continue;
                }

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

                if (_surfaceType == "Unknown")
                {
                    yield return null;
                    continue;
                }

                AudioManager.Instance.PlayFootstep(_surfaceType, transform);
                
                yield return new WaitForSeconds(_audioEffectSpeed);

            }
        }

        private bool CanPlayPlayerMovementSfx(Types.GameState currentGameState)
        {
            return currentGameState == Types.GameState.Gameplay;
        }

        private void HandleLandingSfx(Types.GameState currentGameState)
        {
            // Landing SFX is gameplay-only; menu/cutscene/pause state updates should never emit landings.
            if (currentGameState != Types.GameState.Gameplay)
            {
                return;
            }

            // After transitions we disarm landing until we are safely grounded again.
            if (!_landingAudioArmed)
            {
                if (_isGrounded)
                {
                    _landingAudioArmed = true;
                }
                return;
            }

            if (!_isGrounded)
            {
                // If the player intentionally moved off-ground after unpausing, clear one-shot pause suppression.
                if (_suppressNextLandingAfterPause && IsPlayerMoving())
                {
                    _suppressNextLandingAfterPause = false;
                }

                // Cache airborne state to validate real landings.
                if (!_hasGameplayAirborneToken)
                {
                    _gameplayAirbornePeakY = transform.position.y;
                }
                _hasGameplayAirborneToken = true;
                _gameplayAirborneTime += Time.deltaTime;
                _gameplayAirbornePeakY = Mathf.Max(_gameplayAirbornePeakY, transform.position.y);
                return;
            }

            if (_isGrounded && !_wasGrounded)
            {
                // Hard gate: skip the first landing edge after unpausing to avoid pause/resume false positives.
                if (_suppressNextLandingAfterPause)
                {
                    _suppressNextLandingAfterPause = false;
                    _hasGameplayAirborneToken = false;
                    _gameplayAirborneTime = 0f;
                    return;
                }

                bool hadMeaningfulAirborneTime = _gameplayAirborneTime >= Mathf.Max(0f, landingMinAirborneTimeForSfx);
                float dropDistance = _gameplayAirbornePeakY - transform.position.y;
                bool hadMeaningfulDropDistance = dropDistance >= Mathf.Max(0f, landingMinDropDistanceForSfx);
                
                if (_hasGameplayAirborneToken && hadMeaningfulAirborneTime && hadMeaningfulDropDistance)
                {
                    Debug.Log("PlayerAudio: Landing SFX");
                    AudioManager.Instance.PlaySfx(AudioManager.SfxId.Landing, transform);
                }

                // Landing edge consumed; reset airborne tracking for the next jump/fall cycle.
                _hasGameplayAirborneToken = false;
                _gameplayAirborneTime = 0f;
                _gameplayAirbornePeakY = transform.position.y;
            }
        }

        private void BeginGameplayAirborneToken()
        {
            // A new real jump should not inherit pause-resume landing suppression.
            _suppressNextLandingAfterPause = false;
            _landingAudioArmed = true;
            _hasGameplayAirborneToken = true;
            _gameplayAirborneTime = 0f;
            _gameplayAirbornePeakY = transform.position.y;
        }

        private void SyncJumpAudioTracking(Types.GameState currentGameState)
        {
            if (currentGameState != Types.GameState.Gameplay)
            {
                _jumpRequested = false;
                return;
            }

            // Require a clean release before jump SFX can fire again after transitions.
            if (!_jumpSfxArmed && !IsJumpActionPressed())
            {
                _jumpSfxArmed = true;
            }
        }

        private void HandleJumpRequest(Types.GameState currentGameState)
        {
            // Consume one queued jump input request per frame.
            if (!_jumpRequested)
            {
                return;
            }

            _jumpRequested = false;

            // Ignore jump requests outside gameplay.
            if (!CanPlayPlayerMovementSfx(currentGameState))
            {
                return;
            }

            // Only allow a real jump when input is armed, player is grounded, and not crouching.
            if (!_jumpSfxArmed || _isCrouching || !_isGrounded)
            {
                return;
            }

            // Valid gameplay jump: apply velocity, mark airborne token for landing validation, and emit jump SFX once.
            BeginGameplayAirborneToken();
            _verticalVelocity = jumpForce;
            Debug.Log("PlayerAudio: Jump SFX");
            AudioManager.Instance.PlayPlayerJumping(fromTransform: transform);
            _jumpSfxArmed = false;
        }

        private void HandleGameplayJumpStateEntry(Types.GameState previousGameState)
        {
            bool enteredGameplayFromOtherState = previousGameState != Types.GameState.Gameplay;
            if (enteredGameplayFromOtherState)
            {
                ResetJumpAudioTracking(disarmUntilRelease: true);
                return;
            }

            _jumpRequested = false;
            _jumpSfxArmed = !IsJumpActionPressed();
        }

        private void ResetJumpAudioTracking(bool disarmUntilRelease)
        {
            _jumpRequested = false;
            _jumpSfxArmed = !disarmUntilRelease && !IsJumpActionPressed();
        }

        private void ResetLandingAudioTracking(bool disarmUntilGrounded)
        {
            _landingAudioArmed = !disarmUntilGrounded && _isGrounded;
            _hasGameplayAirborneToken = false;
            _gameplayAirborneTime = 0f;
            _gameplayAirbornePeakY = transform.position.y;
            _wasGrounded = _characterController != null && _characterController.isGrounded;
        }

        private void HandleGameplayAudioStateEntry(Types.GameState previousGameState)
        {
            // Pause resume should preserve true airborne jumps/falls so they can still produce a landing.
            if (previousGameState == Types.GameState.Paused)
            {
                bool groundedNow = _characterController != null && _characterController.isGrounded;

                _landingAudioArmed = true;
                _wasGrounded = groundedNow;
                // When resuming grounded, suppress the next grounded edge once to block pause/unpause false landings.
                _suppressNextLandingAfterPause = groundedNow;

                if (groundedNow)
                {
                    _hasGameplayAirborneToken = false;
                    _gameplayAirborneTime = 0f;
                    _gameplayAirbornePeakY = transform.position.y;
                }
                else if (!_hasGameplayAirborneToken)
                {
                    // Resume while airborne still needs a token so the eventual grounded edge can be treated as a real landing.
                    _hasGameplayAirborneToken = true;
                    _gameplayAirbornePeakY = transform.position.y;
                }

                return;
            }

            // Non-pause transitions should require a fresh grounded gameplay frame before landing can trigger.
            ResetLandingAudioTracking(disarmUntilGrounded: true);
            _suppressNextLandingAfterPause = false;
        }

        #endregion
        
        
        protected override void OnGameStateChanged(Types.GameState newState)
        {
            Types.GameState previousGameState = ResolvePreviousGameStateForTransition(newState);
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
                case Types.GameState.Paused:
                    HandlePausedState();
                    break;
                // handle other game states as needed
            }

            if (newState == Types.GameState.Gameplay)
            {
                HandleGameplayAudioStateEntry(previousGameState);
                HandleGameplayJumpStateEntry(previousGameState);
            }
            else if (newState == Types.GameState.MainMenu)
            {
                ResetLandingAudioTracking(disarmUntilGrounded: true);
                _suppressNextLandingAfterPause = false;
                ResetJumpAudioTracking(disarmUntilRelease: true);
            }
            else
            {
                _suppressNextLandingAfterPause = false;
                ResetJumpAudioTracking(disarmUntilRelease: true);
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
            _isInspecting = false;
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
            SyncLandingTrackingForStateTransition();
        }

        private void HandleInspectionState()
        {
            _isInspecting = true;
            // Disable player controls for cutscene
            _lockedInput = true;
            // disable the head so its hidden
            for (int i = 0; i < ObjectsToDisableOnCutscene.Length; i++)
            {
                ObjectsToDisableOnCutscene[i].SetActive(false);
            }
            // if the player is currently moving (or has any input at all, we want to disable it)
            StopAllPlayerMovement();
            SyncLandingTrackingForStateTransition();
        }

        private void HandlePausedState()
        {
            _lockedInput = true;
            StopAllPlayerMovement();
            SyncLandingTrackingForStateTransition();
            AudioManager.Instance?.StopFootstepsImmediate();
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
        public bool IsPlayerInspecting()
        {
            return _isInspecting;
        }

        private void StopAllPlayerMovement()
        {
            _cachedSprintState = false;
            _isSprinting = false;
            _moveInput = Vector2.zero;
        }

        private void SyncLandingTrackingForStateTransition()
        {
            bool groundedNow = _characterController != null && _characterController.isGrounded;
            _wasGrounded = groundedNow;
            if (groundedNow)
            {
                _hasGameplayAirborneToken = false;
                _gameplayAirborneTime = 0f;
            }
        }

        private Types.GameState GetCurrentGameState()
        {
            return GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentGameState()
                : Types.GameState.MainMenu;
        }

        private Types.GameState GetPreviousGameState()
        {
            return GameStateManager.Instance != null
                ? GameStateManager.Instance.GetPreviousGameState()
                : Types.GameState.MainMenu;
        }

        private Types.GameState ResolvePreviousGameStateForTransition(Types.GameState newState)
        {
            Types.GameState currentGameState = GetCurrentGameState();
            if (currentGameState == newState)
            {
                return GetPreviousGameState();
            }

            return currentGameState;
        }

        private bool IsJumpActionPressed()
        {
            return jumpAction != null
                && jumpAction.action != null
                && jumpAction.action.IsPressed();
        }

        
        #endregion
        
    }
}

