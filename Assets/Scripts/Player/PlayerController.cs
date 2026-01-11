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
        [Space(10)]
        [Header("References")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference crouchAction;
        [SerializeField] private InputActionReference sprintAction;
        /* Internal variables */
        private Transform _cameraTransform;
        private CharacterController _characterController;
        private Vector2 _moveInput;
        private bool _isGrounded;
        private bool _isCrouching;
        private bool _isSprinting;
        private bool _cachedSprintState;
        private float _verticalVelocity;
        private float _targetHeight;
        
        // Local reference that the controller cares about
        [SerializeField] private Types.PlayerHealthState currentPlayerHealthState;


        private void Awake()
        {
            // set up initial character variables
            _characterController = GetComponent<CharacterController>();
            // look through all of the children of this player, to find an object with the cinemachine camera component
            foreach (Transform child in transform)
            {
                if (child.GetComponentInChildren<CinemachineCamera>() != null)
                {
                    _cameraTransform = child;
                    DebugUtils.LogSuccess("PlayerController: Found Cinemachine Camera in child object: " + child.name);
                    break;
                }
            }
            _targetHeight = standHeight;
        }

        private void Update()
        {
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
            // only allow sprinting to change if we are grounded
            
            // regardless of whether we can sprint, we want to cache the sprint state for when we land
            _cachedSprintState = obj.performed;
            
            if (!_isGrounded) { return; }
            _isSprinting = obj.performed;
        }

        private void OnCrouch(InputAction.CallbackContext obj)
        {
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
            _moveInput = obj.ReadValue<Vector2>();
        }
        
        private void OnJump(InputAction.CallbackContext obj)
        {
            if(_isGrounded)
            {
                // Apply jump force
                _verticalVelocity = jumpForce;
            }
        }

        private void HandleMovement()
        {
            
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

        protected override void OnGameStateChanged(Types.GameState newState)
        {
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
        }
        private void HandleCutsceneState()
        {
            // Disable player controls for cutscene
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
            
            Vector3 cameraTargetPosition = _cameraTransform.localPosition;
            cameraTargetPosition.y = _targetHeight - cameraCrouchOffset;
            _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, cameraTargetPosition, crouchTransitionSpeed * Time.deltaTime);
        }
    }
}
