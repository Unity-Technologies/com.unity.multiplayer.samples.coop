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
    List<GameObject> m_ShowTheseWhenFiring;

    [SerializeField]
    List<GameObject> m_HideTheseWhenFiring;

    [SerializeField]
    List<GameObject> m_ShowTheseOnTargetImpact;

    [SerializeField]
    List<GameObject> m_HideTheseOnTargetImpact;

    [SerializeField]
    List<GameObject> m_ShowTheseOnFloorImpact;

    [SerializeField]
    List<GameObject> m_HideTheseOnFloorImpact;

    [SerializeField]
    [Tooltip("If this projectile plays an impact particle, how long should we stay alive for it to keep playing?")]
    float m_PostImpactDurationSeconds = 1;

    Vector3 m_StartPoint;
    Transform m_TargetDestination; // null if we're a "miss" projectile (i.e. we hit nothing)
    Vector3 m_MissDestination; // only used if m_TargetDestination is null
    float m_WindupDuration;
    float m_FlightDuration;
    float m_Age;

    enum State
    {
        Windup,
        Flying,
        Impact
    }

    State m_State;

    public void Initialize(Vector3 startPoint, Transform target, Vector3 missPos, float windupTime, float flightTime)
    {
        m_StartPoint = startPoint;
        m_TargetDestination = target;
        m_MissDestination = missPos;
        m_WindupDuration = windupTime;
        m_FlightDuration = flightTime;
        m_State = State.Windup;
    }

    public void Cancel()
    {
        // we could play a "poof" particle... but for now we just instantly disappear
        Destroy(gameObject);
    }

    void Update()
    {
        m_Age += Time.deltaTime;
        switch (m_State)
        {
            case State.Windup:
                if (m_Age >= m_WindupDuration)
                {
                    SwitchState(State.Flying);
                }
                break;
            case State.Flying:
                if (m_Age >= m_WindupDuration + m_FlightDuration)
                {
                    SwitchState(State.Impact);
                }
                else
                {
                    // we're flying through the air. Reposition ourselves to be closer to the destination
                    float progress = (m_Age - m_WindupDuration) / m_FlightDuration;
                    transform.position = Vector3.Lerp(m_StartPoint, m_TargetDestination?m_TargetDestination.position:m_MissDestination, progress);
                }
                break;
            case State.Impact:
                if (m_Age >= m_WindupDuration + m_FlightDuration + m_PostImpactDurationSeconds)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    void SwitchState(State newState)
    {
        if (newState == State.Flying)
        {
            foreach (var gameObjectToHide in m_HideTheseWhenFiring)
            {
                gameObjectToHide.SetActive(false);
            }
            foreach (var gameObjectToShow in m_ShowTheseWhenFiring)
            {
                gameObjectToShow.SetActive(true);
            }
        }
        else if (newState == State.Impact)
        {
            // is it impacting an actual enemy? We allow different graphics for the "miss" case
            if (m_TargetDestination)
            {
                foreach (var gameObjectToHide in m_HideTheseOnTargetImpact)
                {
                    gameObjectToHide.SetActive(false);
                }
                foreach (var gameObjectToShow in m_ShowTheseOnTargetImpact)
                {
                    gameObjectToShow.SetActive(true);
                }
            }
            else
            {
                foreach (var gameObjectToHide in m_HideTheseOnFloorImpact)
                {
                    gameObjectToHide.SetActive(false);
                }
                foreach (var gameObjectToShow in m_ShowTheseOnFloorImpact)
                {
                    gameObjectToShow.SetActive(true);
                }
            }

        }
        m_State = newState;
    }
}
