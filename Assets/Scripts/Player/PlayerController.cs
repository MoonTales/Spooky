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
        [Header("References")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        
        /* Internal variables */
        private Transform _cameraTransform;
        private CharacterController _characterController;
        private Vector2 _moveInput;
        private bool _isGrounded;
        private float _verticalVelocity;
        
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
        }

        private void Update()
        {
            _isGrounded = _characterController.isGrounded;
            HandleGravity();
            HandleMovement();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMovePerformed;
            jumpAction.action.performed += Jump;
            
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMovePerformed;
            jumpAction.action.performed -= Jump;
        }

        private void OnMovePerformed(InputAction.CallbackContext obj)
        {
            _moveInput = obj.ReadValue<Vector2>();
        }
        
        private void Jump(InputAction.CallbackContext obj)
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
            float currentSpeed = walkSpeed;
            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
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
    }
}
