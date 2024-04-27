using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GengController : MonoBehaviour
{
    [SerializeField] private float gengSpeed = 2.0f;
    [SerializeField] private float gengJumpHeight = 1.0f;
    [SerializeField] private float gengRotationSpeed = 4.0f;


    private float gravityValue = -9.81f;

    private CharacterInput gengInput;
    private CharacterController gengZhaoController;

    //Varialbles to store player input values
    private Vector2 currentMovementInput;
    private Vector3 currentMovement;

    private Transform cameraMainTransform;

    //Jumping Variables
    private float initialJumpVelocity;
    private float maxJumpHeight = 1.0f;
    private float maxJumpTime = 0.5f;

    private float velocityY;
    private float tempCurrentMovementY;

    //Animator
    private Animator animator;
    private bool isWalking = false;

    private void Awake()
    {
        //初始化设置引用变量
        gengInput = new CharacterInput();
        gengZhaoController = GetComponent<CharacterController>();

        animator = GetComponent<Animator>();

        cameraMainTransform = Camera.main.transform;


        setupJumpVariables();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        handleGravity();
        CalculateMovement();
    }

    //gangZhao Move


    private void OnEnable()
    {
        gengInput.GamePlay.Move.started += OnMove;
        gengInput.GamePlay.Move.performed += OnMove;
        gengInput.GamePlay.Move.canceled += OnMove;

        gengInput.GamePlay.Jump.started += OnJump;
        gengInput.GamePlay.Jump.performed += OnJump;
        gengInput.GamePlay.Jump.canceled += OnJump;

        gengInput.GamePlay.Interact.performed += OnInteract;

        gengInput.GamePlay.Enable();
    }

    private void OnDisable()
    {
        gengInput.GamePlay.Move.started -= OnMove;
        gengInput.GamePlay.Move.performed -= OnMove;
        gengInput.GamePlay.Move.canceled -= OnMove;

        gengInput.GamePlay.Jump.started -= OnJump;
        gengInput.GamePlay.Jump.performed -= OnJump;
        gengInput.GamePlay.Jump.canceled -= OnJump;


        gengInput.GamePlay.Interact.performed -= OnInteract;


        gengInput.GamePlay.Disable();
    }


    private void CalculateMovement()
    {
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;

        tempCurrentMovementY = currentMovement.y;

        currentMovement = cameraMainTransform.forward * currentMovement.z +
                          cameraMainTransform.right * currentMovement.x;

        if (currentMovementInput != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(currentMovementInput.x, currentMovementInput.y) * Mathf.Rad2Deg +
                                cameraMainTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation =
                Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * gengRotationSpeed);
        }

        currentMovement.y = tempCurrentMovementY;

        gengZhaoController.Move(currentMovement * Time.deltaTime);
        
        //animation
        AnimateWalk(currentMovement);
    }

    void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = gengInput.GamePlay.Move.ReadValue<Vector2>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;
        if (!gengZhaoController.isGrounded)
            return;
        velocityY += initialJumpVelocity;
    }
    // interact 
    void OnInteract(InputAction.CallbackContext context)
    {
        //Debug.Log("发生交互");
        float interactRange = 1f;
        Collider[] colliderArray = Physics.OverlapSphere(transform.position, interactRange);
        foreach (Collider collider in colliderArray)
        {
            //Debug.Log(collider);
            if (collider.TryGetComponent(out NPCInteractable npcInteractable))
            {
                npcInteractable.Interact();
            }
            
        }
    }

    void setupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravityValue = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }


    private void handleGravity()
    {
        if (gengZhaoController.isGrounded && velocityY < 0.0f)
        {
            velocityY = -1.0f;
        }
        else
        {
            velocityY += gravityValue * 1 * Time.deltaTime;
        }

        currentMovement.y = velocityY;
    }

    void AnimateWalk(Vector3 movement)
    {
        isWalking = (movement.x > 0.1f || movement.x < -0.1f) || (movement.z > 0.1f || movement.z < -0.1f)
            ? true
            : false;
        animator.SetBool("IsWalking", isWalking);
    }
}