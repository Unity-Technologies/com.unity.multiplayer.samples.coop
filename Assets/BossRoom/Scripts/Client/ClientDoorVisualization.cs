using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a door in the client. The visuals of the door animate as
/// "opening" and "closing", but for physics purposes this is an illusion:
/// whenever the door is open on the server, the door's physics are disabled,
/// and vice versa.
///
/// (The door will eventually have real graphics and its own Animator,
/// which will be used instead of the manual angle-rotation logic used here.)
/// </summary>
[RequireComponent(typeof(NetworkDoorState))]
public class ClientDoorVisualization : NetworkedBehaviour
{
    [SerializeField]
    [Tooltip("This physics and navmesh obstacle is enabled when the door is closed.")]
    private GameObject m_PhysicsObject;

    [SerializeField]
    private GameObject m_DoorVisual;

    [SerializeField]
    [Tooltip("The y angle of the DoorVisual when the door is fully opened")]
    private float m_OpenAngle = 0;

    [SerializeField]
    [Tooltip("The y angle of the DoorVisual when the door is fully closed")]
    private float m_ClosedAngle = 120;

    [SerializeField]
    [Tooltip("How long it takes for the door to open")]
    private float m_AnimationDuration = 1;

    private NetworkDoorState m_DoorState;
    private bool m_WasLastOpen;
    private Coroutine m_CoroAnimateDoor;

    private void Awake()
    {
        m_DoorState = GetComponent<NetworkDoorState>();

        // don't call Update() until NetworkStart() decides if we're IsClient or not!
        enabled = false;
    }

    public override void NetworkStart()
    {
        if (IsClient)
        {
            enabled = true;
            m_WasLastOpen = m_DoorState.IsOpen.Value;
            ForceDoorPos(m_WasLastOpen);
        }
    }

    private void Update()
    {
        bool isOpen = m_DoorState.IsOpen.Value;
        if (isOpen != m_WasLastOpen)
        {
            if (m_CoroAnimateDoor != null)
            {
                StopCoroutine(m_CoroAnimateDoor);
            }

            if (isOpen)
            {
                m_PhysicsObject.SetActive(false);
                m_CoroAnimateDoor = StartCoroutine(CoroRotateDoor(m_ClosedAngle, m_OpenAngle));
            }
            else
            {
                m_PhysicsObject.SetActive(true);
                m_CoroAnimateDoor = StartCoroutine(CoroRotateDoor(m_OpenAngle, m_ClosedAngle));
            }
        }
        m_WasLastOpen = isOpen;
    }

    // during initial startup, sets the visuals and physics appropriately with no animation
    private void ForceDoorPos(bool open)
    {
        if (open)
        {
            m_PhysicsObject.SetActive(false);
            m_DoorVisual.transform.localRotation = Quaternion.Euler(0, m_OpenAngle, 0);
        }
        else
        {
            m_PhysicsObject.SetActive(true);
            m_DoorVisual.transform.localRotation = Quaternion.Euler(0, m_ClosedAngle, 0);
        }
    }

    // coroutine to animate the door opening
    // (Replace with actual Animator door animation?!)
    IEnumerator CoroRotateDoor(float startY, float stopY)
    {
        float timeRemaining = m_AnimationDuration;
        while (timeRemaining > 0)
        {
            yield return new WaitForFixedUpdate();
            float t = 1-(timeRemaining / m_AnimationDuration);
            float angle = Mathf.LerpAngle(startY, stopY, t);
            m_DoorVisual.transform.localRotation = Quaternion.Euler(0, angle, 0);
            timeRemaining -= Time.fixedDeltaTime;
        }
        m_CoroAnimateDoor = null;
    }

}
