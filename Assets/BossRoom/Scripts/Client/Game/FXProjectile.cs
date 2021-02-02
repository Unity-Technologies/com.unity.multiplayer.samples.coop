using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logic that handles an FX-based pretend-missile.
/// There are three stages to this missile:
///     - wind-up (while the caster is animating)
///     - flying through the air
///     - impacted the target/impacted the ground (in the case of a miss)
/// 
/// This script should be attached to a projectile prefab. The prefab's initial state should be set up for the "windup"
/// stage -- that is, any game objects needed for display in this state should be enabled, and game objects needed for
/// the other stages should be disabled.
/// </summary>
public class FXProjectile : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> m_ShowTheseWhenFiring;

    [SerializeField]
    private List<GameObject> m_HideTheseWhenFiring;

    [SerializeField]
    private List<GameObject> m_ShowTheseOnTargetImpact;

    [SerializeField]
    private List<GameObject> m_HideTheseOnTargetImpact;

    [SerializeField]
    private List<GameObject> m_ShowTheseOnFloorImpact;

    [SerializeField]
    private List<GameObject> m_HideTheseOnFloorImpact;

    [SerializeField]
    [Tooltip("If this projectile plays an impact particle, how long should we stay alive for it to keep playing?")]
    private float m_PostImpactDurationSeconds = 1;

    private Vector3 m_StartPoint;
    private Transform m_TargetDestination; // null if we're a "miss" projectile (i.e. we hit nothing)
    private Vector3 m_MissDestination; // only used if m_TargetDestination is null
    private float m_WindupDuration;
    private float m_FlightDuration;
    private float m_Age;

    private enum State
    {
        WINDUP,
        FLYING,
        IMPACT,
    }
    private State m_State;

    public void Initialize(Vector3 startPoint, Transform target, Vector3 missPos, float windupTime, float flightTime)
    {
        m_StartPoint = startPoint;
        m_TargetDestination = target;
        m_MissDestination = missPos;
        m_WindupDuration = windupTime;
        m_FlightDuration = flightTime;
        m_State = State.WINDUP;
    }

    public void Cancel()
    {
        // we could play a "poof" particle... but for now we just instantly disappear
        Object.Destroy(gameObject);
    }

    private void Update()
    {
        m_Age += Time.deltaTime;
        switch (m_State)
        {
            case State.WINDUP:
                if (m_Age >= m_WindupDuration)
                {
                    SwitchState(State.FLYING);
                }
                break;
            case State.FLYING:
                if (m_Age >= m_WindupDuration + m_FlightDuration)
                {
                    SwitchState(State.IMPACT);
                }
                else
                {
                    // we're flying through the air. Reposition ourselves to be closer to the destination
                    float progress = (m_Age - m_WindupDuration) / m_FlightDuration;
                    transform.position = Vector3.Lerp(m_StartPoint, m_TargetDestination?m_TargetDestination.position:m_MissDestination, progress);
                }
                break;
            case State.IMPACT:
                if (m_Age >= m_WindupDuration + m_FlightDuration + m_PostImpactDurationSeconds)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }


    private void SwitchState(State newState)
    {
        if (newState == State.FLYING)
        {
            foreach (var gameObject in m_HideTheseWhenFiring)
            {
                gameObject.SetActive(false);
            }
            foreach (var gameObject in m_ShowTheseWhenFiring)
            {
                gameObject.SetActive(true);
            }
        }
        else if (newState == State.IMPACT)
        {
            // is it impacting an actual enemy? We allow different graphics for the "miss" case
            if (m_TargetDestination)
            {
                foreach (var gameObject in m_HideTheseOnTargetImpact)
                {
                    gameObject.SetActive(false);
                }
                foreach (var gameObject in m_ShowTheseOnTargetImpact)
                {
                    gameObject.SetActive(true);
                }
            }
            else
            {
                foreach (var gameObject in m_HideTheseOnFloorImpact)
                {
                    gameObject.SetActive(false);
                }
                foreach (var gameObject in m_ShowTheseOnFloorImpact)
                {
                    gameObject.SetActive(true);
                }
            }

        }
        m_State = newState;
    }
}
