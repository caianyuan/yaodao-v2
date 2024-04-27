using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineFreeLook))]
public class CameraLook : MonoBehaviour
{
    private CinemachineFreeLook cinemachine;

    private CursorControls playerInput;

    private bool _isRMBPressed = false;
    private bool _cameraMovementLock = false;


    private void Awake()
    {
        playerInput = new CursorControls();
        cinemachine = GetComponent<CinemachineFreeLook>();
    }

    private void OnEnable()
    {
        playerInput.PlayerMain.RotateCamera.performed += OnCameraMove;
        playerInput.PlayerMain.MouseControlCamera.performed += OnEnableMouseControlCamera;
        playerInput.PlayerMain.MouseControlCamera.canceled += OnDisableMouseControlCamera;
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.PlayerMain.RotateCamera.performed -= OnCameraMove;
        playerInput.PlayerMain.MouseControlCamera.performed -= OnEnableMouseControlCamera;
        playerInput.PlayerMain.MouseControlCamera.canceled -= OnDisableMouseControlCamera;
        playerInput.Disable();
    }

    private void OnEnableMouseControlCamera(InputAction.CallbackContext context)
    {
        _isRMBPressed = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(DisableMouseControlForFrame());
    }

    IEnumerator DisableMouseControlForFrame()
    {
        _cameraMovementLock = true;
        yield return new WaitForEndOfFrame();
        _cameraMovementLock = false;
    }
    private void OnDisableMouseControlCamera(InputAction.CallbackContext context)
    {
        _isRMBPressed = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        //when mouse control is disabled, the input need to be cleared
        //or the last frame's input will 'stick' until the action is invoked again
        cinemachine.m_XAxis.m_InputAxisValue = 0;
        cinemachine.m_YAxis.m_InputAxisValue = 0;
    }

    private void OnCameraMove(InputAction.CallbackContext context)
    {
        if (_cameraMovementLock)
            return;
        if (!_isRMBPressed)
            return;
        //Vector2 delta = playerInput.PlayerMain.RotateCamera.ReadValue<Vector2>();
        Vector2 delta = context.ReadValue<Vector2>();
        // cinemachine.m_XAxis.Value = delta.x  * Time.deltaTime * cinemachine.m_XAxis.m_MaxSpeed;
        // cinemachine.m_YAxis.Value = delta.y * Time.deltaTime * cinemachine.m_YAxis.m_MaxSpeed;
        cinemachine.m_XAxis.m_InputAxisValue = delta.x * 1 * Time.deltaTime;
        cinemachine.m_YAxis.m_InputAxisValue = delta.y * 1 * Time.deltaTime;
    }
}