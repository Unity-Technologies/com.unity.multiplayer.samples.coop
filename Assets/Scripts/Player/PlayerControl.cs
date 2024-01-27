using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.PanicBuying.Character;

namespace Unity.PanicBuying.Character
{
    public enum moveState
    {
        Walking,
        Running,
        Slow
    }

    public class PlayerControl : NetworkBehaviour
    {
        [Header("Movement Speed")]
        public float walkingSpeed;
        public float runningSpeed;
        public float slowSpeed;
        public float moveMultiplier;
        public float maxStamina;
        public float staminaChangeRate;
        public float staminaCooldown;

        public float curStamina;
        public float moveSpeed;
        public moveState moveType;

        [Header("Jump & Ground Check")]
        public float jumpForce;
        public float jumpCooldownTime;
        public float gravityScale;
        public float playerHeight;
        bool isGrounded;

        public Transform orientation;
        public Transform playerObj;
        public GameObject tpsCamera;
        public GameObject fpsCamera;

        float horizontalInput;
        float verticalInput;

        bool jumpAvailable = true;

        Vector3 moveDir;

        Rigidbody rbody;

        // Start is called before the first frame update
        void Start()
        {
            rbody = GetComponent<Rigidbody>();
            Transform spawnPoint = GameObject.Find("SpawnPoint").transform;
            transform.position = spawnPoint.position;

            if (IsOwner)
            {
                //// setting TPS cam
                //GameObject cam = Instantiate(tpsCamera);
                //CinemachineFreeLook cineCam = cam.GetComponent<CinemachineFreeLook>();
                //cineCam.Follow = transform;
                //cineCam.LookAt = transform;
                //TPSCameraController camController = cam.GetComponent<TPSCameraController>();
                //camController.orientation = orientation;
                //camController.player = transform;
                //camController.playerObj = playerObj;

                // setting FPS cam
                GameObject cam = Instantiate(fpsCamera);
                FPSCameraController camController = cam.GetComponent<FPSCameraController>();
                camController.orientation = orientation;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f);

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded && jumpAvailable)
            {
                rbody.velocity = new Vector3(rbody.velocity.x, 0f, rbody.velocity.z);
                rbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                jumpAvailable = false;
                Invoke("CooldownJump", jumpCooldownTime);
            }
            GetInput();

        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                GiveGravity();
                MovePlayer();
                SpeedControl();
                
            }
        }

        private void GiveGravity()
        {
            rbody.AddForce(Vector3.down * gravityScale, ForceMode.Force);
        }

        private void GetInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            if (moveType != moveState.Slow)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    moveType = moveState.Running;
                else
                    moveType = moveState.Walking;
            }
        }

        private void MovePlayer()
        {
            moveDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

            switch (moveType)
            {
                case moveState.Walking:
                    moveSpeed = walkingSpeed;
                    curStamina += staminaChangeRate;
                    break;
                case moveState.Running:
                    moveSpeed = runningSpeed;
                    curStamina -= staminaChangeRate;
                    break;
                case moveState.Slow:
                    moveSpeed = slowSpeed;
                    curStamina += staminaChangeRate;
                    break;
            }
            rbody.AddForce(moveDir.normalized * moveSpeed * moveMultiplier, ForceMode.Force);

            if (curStamina < 0)
            {
                moveType = moveState.Slow;
                Invoke("CooldownSlow", staminaCooldown);
            }
            else if (curStamina > maxStamina)
                curStamina = maxStamina;
        }

        private void SpeedControl()
        {
            Vector3 moveVelocity = new Vector3(rbody.velocity.x, 0f, rbody.velocity.z);

            if (moveVelocity.magnitude > moveSpeed)
            {
                Vector3 controlledVelocity = moveVelocity.normalized * moveSpeed;
                rbody.velocity = new Vector3(controlledVelocity.x, rbody.velocity.y, controlledVelocity.z);
            }
        }

        private void CheckStamina()
        {
            if (curStamina < 0)
            {
                moveType = moveState.Slow;
                Invoke("CooldownSlow", staminaCooldown);
            }
        }

        private void CooldownSlow()
        {
            moveType = moveState.Walking;
        }

        private void CooldownJump()
        {
            jumpAvailable = true;
        }
    }
}
