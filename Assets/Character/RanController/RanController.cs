using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RanController : MonoBehaviour
{
    [SerializeField] private float ranSpeed = 2.0f;
    [SerializeField] private float ranJumpHeight = 1.0f;
    [SerializeField] private float ranRotationSpeed = 4.0f;

    private float gravityValue = -9.81f;
    // private bool groundedPlayer;
    // private Vector3 playerVelocity;

    private CharacterInput ranInput;
    private CharacterController ranHongXiaController;

    //variables to store player input values
    private Vector2 currentMovementInput;
    private Vector3 currentMovement;
    //private bool isMovementPressed;

    private Transform cameraMainTransform;

    //Jumping varibales
    //private bool isJumpPressed = false;
    private float initialJumpVelocity;
    private float maxJumpHeight = 1.0f;
    private float maxJumpTime = 0.5f;

    //private bool isJumping = false;

    private float velocityY;
    private float tempCurrentMovementY;


    private void Awake()
    {
        //初始化设置引用变量
        ranInput = new CharacterInput();
        ranHongXiaController = GetComponent<CharacterController>();
        cameraMainTransform = Camera.main.transform;


        setupJumpVariables();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        handleGravity();
        CalculateMovement();
    }

    private void OnEnable()
    {
        // ranInput.GamePlay.Move.started += OnMovementInput;
        // ranInput.GamePlay.Move.performed += OnMovementInput;
        // ranInput.GamePlay.Move.canceled += OnMovementInput;
        ranInput.GamePlay.Move.started += OnMove;
        ranInput.GamePlay.Move.performed += OnMove;
        ranInput.GamePlay.Move.canceled += OnMove;

        ranInput.GamePlay.Jump.started += OnJump;
        ranInput.GamePlay.Jump.performed += OnJump;
        ranInput.GamePlay.Jump.canceled += OnJump;

        ranInput.GamePlay.Enable();
    }

    private void OnDisable()
    {
        ranInput.GamePlay.Move.started -= OnMove;
        ranInput.GamePlay.Move.performed -= OnMove;
        ranInput.GamePlay.Move.canceled -= OnMove;

        ranInput.GamePlay.Jump.started -= OnJump;
        ranInput.GamePlay.Jump.performed -= OnJump;
        ranInput.GamePlay.Jump.canceled -= OnJump;
        ranInput.GamePlay.Disable();
    }


    private void CalculateMovement()
    {
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;

        tempCurrentMovementY = currentMovement.y;

        currentMovement = cameraMainTransform.forward * currentMovement.z +
                          cameraMainTransform.right * currentMovement.x;
        //currentMovement = cameraForward * currentMovement.z + cameraRight * currentMovement.x;
        if (currentMovementInput != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(currentMovementInput.x, currentMovementInput.y) * Mathf.Rad2Deg +
                                cameraMainTransform.eulerAngles.y;
            //float targetAngle = Mathf.Atan2(currentMovementInput.x, currentMovementInput.y) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * ranRotationSpeed);
        }

        currentMovement.y = tempCurrentMovementY;


        ranHongXiaController.Move(currentMovement * Time.deltaTime);
    }

    void OnJump(InputAction.CallbackContext context)
    {
        // isJumpPressed = context.ReadValueAsButton();
        // Debug.Log(isJumpPressed);
        if (!context.started)
            return;
        if (!ranHongXiaController.isGrounded)
            return;
        velocityY += initialJumpVelocity;
    }

    void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = ranInput.GamePlay.Move.ReadValue<Vector2>();
    }

    void setupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravityValue = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }

    private void handleGravity()
    {
        if (ranHongXiaController.isGrounded && velocityY < 0.0f)
        {
            velocityY = -1.0f;
        }
        else
        {
            //float gravity = -9.81f;
            velocityY += gravityValue * 1 * Time.deltaTime;
        }

        currentMovement.y = velocityY;
    }

    // void handleJump()
    // {
    //     if (ranHongXiaController.isGrounded && !isJumping && isJumpPressed)
    //     {
    //         //Debug.Log("is Grounded");
    //         isJumping = true;
    //         playerVelocity.y += Mathf.Sqrt(ranJumpHeight * -3.0f * gravityValue);
    //         ranHongXiaController.Move(playerVelocity * Time.deltaTime);
    //         // playerVelocity.y += gravityValue * Time.deltaTime;
    //         //currentMovement.y = playerVelocity.y;
    //     }
    //     else if (ranHongXiaController.isGrounded && isJumping && !isJumpPressed)
    //     {
    //         //Debug.Log("is not Grounded");
    //         isJumping = false;
    //     }
    // }
}