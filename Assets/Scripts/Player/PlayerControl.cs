using System;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.PanicBuying.Character;

namespace Unity.PanicBuying.Character
{

    public class PlayerControl : NetworkBehaviour
    {
        public enum AnimationState
        {
            Idle,
            Walk,
            Run,
            Jump,
            SneakIdle,
            SneakWalk,
            InsertCoin,
            Attack,
            Throw
        }

        [Header("Movement Speed")]
        public float walkSpeed;
        public float runSpeed;
        public float sneakSpeed;
        public float cameraSensX;
        public float cameraSensY;

        public float weight;
        bool isSneak = false;


        //stamina
        public float maxStamina;
        public float staminaChangeRate;
        public float staminaCooldown;
        public float curStamina;

        private bool runnable = true;
        bool isRun = false;

        [Header("Jump & Ground Check")]
        public float jumpForce;
        public float playerHeight;

        bool jumpable = true;

        public NetworkAnimator networkAnimator;
        public Transform orientation;
        public Transform body;
        public GameObject fpsCamera;

        float horizontalInput;
        float verticalInput;

        Rigidbody rigidbody;

        public float defaultMass;

        AnimationState animationState = AnimationState.Idle;

        private bool isGrounded;

        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            rigidbody.mass = defaultMass;
            Transform spawnPoint = GameObject.Find("PlayerSpawnPoint").transform;
            transform.position = spawnPoint.position;

            if (IsOwner)
            {
                GameObject cam = Instantiate(fpsCamera);
                FPSCameraController camController = cam.GetComponent<FPSCameraController>();
                camController.orientation = orientation;
                camController.body = body;
                camController.sensX = cameraSensX;
                camController.sensY = cameraSensY;
            }
        }


        private void Update()
        {
            if (!IsOwner) return;
            //Physics.BoxCast();
        }

        private void Animate()
        {
            switch (animationState)
            {
                case AnimationState.Idle:
                    networkAnimator.Animator.SetBool("Walk", false);
                    networkAnimator.Animator.SetBool("Run", false);
                    networkAnimator.Animator.SetBool("Sneak", false);
                    networkAnimator.Animator.SetBool("Jump", false);
                    break;
                case AnimationState.Walk:
                    networkAnimator.Animator.SetBool("Walk", true);
                    networkAnimator.Animator.SetBool("Run", false);
                    networkAnimator.Animator.SetBool("Sneak", false);
                    networkAnimator.Animator.SetBool("Jump", false);
                    break;
                case AnimationState.Run:
                    networkAnimator.Animator.SetBool("Run", true);
                    networkAnimator.Animator.SetBool("Sneak", false);
                    break;
                case AnimationState.SneakIdle:
                    networkAnimator.Animator.SetBool("Walk", false);
                    networkAnimator.Animator.SetBool("Run", false);
                    networkAnimator.Animator.SetBool("Sneak", true);
                    networkAnimator.Animator.SetBool("Jump", false);
                    break;
                case AnimationState.SneakWalk:
                    networkAnimator.Animator.SetBool("Walk", true);
                    networkAnimator.Animator.SetBool("Run", false);
                    networkAnimator.Animator.SetBool("Sneak", true);
                    networkAnimator.Animator.SetBool("Jump", false);
                    break;
                case AnimationState.Jump:
                    networkAnimator.Animator.SetBool("Walk", false);
                    networkAnimator.Animator.SetBool("Run", false);
                    networkAnimator.Animator.SetBool("Sneak", false);
                    networkAnimator.Animator.SetBool("Jump", true);
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                GetInput();
                MovePlayer();
                Animate();
            }
        }



        private void GetInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            if (runnable)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    isRun = true;
                    isSneak = false;
                }
                else
                {
                    isRun = false;
                }
            }

            isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight);

            if (isGrounded)
            {
                if (isSneak)
                {
                    animationState = AnimationState.SneakIdle;
                }
                else
                {
                    animationState = AnimationState.Idle;
                }
            }
            else
            {
                animationState = AnimationState.Jump;
            }

            Debug.DrawLine(transform.position, transform.position + Vector3.down * playerHeight, Color.red);

            if (isGrounded && !isRun && Input.GetKeyDown(KeyCode.C))
            {
                isSneak = !isSneak;
            }

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isSneak = false;
                animationState = AnimationState.Jump;
            }
            Debug.Log(isGrounded);
            Debug.Log(animationState);
        }

        private void MovePlayer()
        {
            var direction = Vector3.Normalize(orientation.forward * verticalInput + orientation.right * horizontalInput);
            if (direction.magnitude > 0.0f)
            {
                float moveSpeed;
                if (isSneak)
                {
                    if(isGrounded)
                    {
                        animationState = AnimationState.SneakWalk;
                    }
                   
                    moveSpeed = sneakSpeed;
                    IncreaseStamina();
                }
                else if (isRun)
                {
                    if (isGrounded)
                    {
                        animationState = AnimationState.Run;
                    }
                    moveSpeed = runSpeed;
                    DecreaseStamina();
                }
                else
                {
                    if (isGrounded)
                    {
                        animationState = AnimationState.Walk;
                    }
                    moveSpeed = walkSpeed;
                    IncreaseStamina();
                }
                rigidbody.AddForce(direction * moveSpeed * Time.deltaTime, ForceMode.VelocityChange);
            }
            else
            {
                IncreaseStamina();
                IncreaseStamina();
            }


        }
        public void SetWeight(float weigth)
        {
            rigidbody.mass = defaultMass + weigth;
        }

        private void DecreaseStamina()
        {
            curStamina -= staminaChangeRate;
            if (curStamina < 0)
            {
                runnable = false;
                Invoke("CooldownSlow", staminaCooldown);
            }
        }

        private void IncreaseStamina()
        {
            curStamina += staminaChangeRate;
            if (curStamina > maxStamina)
            {
                curStamina = maxStamina;
            }
        }

        private void CooldownSlow()
        {
            runnable = true;
        }
    }
}
