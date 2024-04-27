using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    // Start is called before the first frame update
    private CustomInput controls;
    private bool isRMBPressed;

    private void Awake()
    {
        controls = new CustomInput();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        //controls.CameraControl.XYAxis.canceled += _ => EndedClick();
        //controls.CameraControl.XYAxis.Disable();
        controls.Disable();
        //controls.CameraControl.XYAxis.performed -= _ => StartedClick();
        
    }

    private void Start()
    {
        // Mouse mouse = Mouse.current;
        // mouse.rightButton
        //controls.CameraControl.XYAxis.started += _ => StartedClick();
        
        controls.CameraControl.XYAxis.performed += _ => StartedClick();
        
        
    }

    private void StartedClick()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("执行进行中");
        isRMBPressed = true;
    }

    private void EndedClick()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("执行完毕");
    }
}
