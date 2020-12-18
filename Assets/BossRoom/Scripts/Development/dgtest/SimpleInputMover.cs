using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInputMover : MonoBehaviour
{
    public float TurnSpeed = 80; // degrees per second
    public float WalkSpeed = 2.0f; // meters per second
    public float RunSpeed = 4.0f; // meters per second
    public float StrafeSpeed = 1.0f; // meters per second

    public float MinZoomDistance = 3;
    public float MaxZoomDistance = 10;
    public float ZoomSpeed = 3;
    public CinemachineVirtualCamera VirtualCam;

    private CharacterController controller;
    private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    private void Update()
    {
        bool isRunning = !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);

        float horizontalMove = 0;
        float verticalMove = 0;
        float rotateSpeed = 0;
        float animateMovement = 0;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            if (isRunning)
            {
                verticalMove = -RunSpeed;
                animateMovement = 1;
            }
            else
            {
                verticalMove = -WalkSpeed;
                animateMovement = 0.5f;
            }
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            if (isRunning)
            {
                verticalMove = RunSpeed;
                animateMovement = 1f;
            }
            else
            {
                verticalMove = WalkSpeed;
                animateMovement = 0.5f;
            }
        }

        if (Input.GetKey(KeyCode.Q))
        {
            horizontalMove = StrafeSpeed;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            horizontalMove = -StrafeSpeed;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            rotateSpeed = -TurnSpeed;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            rotateSpeed = TurnSpeed;
        }

        controller.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
        controller.SimpleMove(transform.TransformDirection(new Vector3(horizontalMove, 0, verticalMove)));
        UpdateMoveAnimation(animateMovement);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            BeginAttackAnimation(1);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && VirtualCam)
            ZoomCamera(scroll);
    }


    private void UpdateMoveAnimation(float movementSpeed)
    {
        animator.SetFloat("Speed", movementSpeed);
    }

    private void BeginAttackAnimation(int attackAnimationID)
    {
        animator.SetInteger("AttackID", attackAnimationID);
        animator.SetTrigger("BeginAttack");
    }

    private void ZoomCamera(float scroll)
    {
        CinemachineComponentBase[] components = VirtualCam.GetComponentPipeline();
        foreach (CinemachineComponentBase component in components)
        {
            if (component is CinemachineFramingTransposer)
            {
                CinemachineFramingTransposer c = (CinemachineFramingTransposer)component;
                c.m_CameraDistance += -scroll * ZoomSpeed;
                if (c.m_CameraDistance < MinZoomDistance)
                    c.m_CameraDistance = MinZoomDistance;
                if (c.m_CameraDistance > MaxZoomDistance)
                    c.m_CameraDistance = MaxZoomDistance;
            }
        }
    }
}
