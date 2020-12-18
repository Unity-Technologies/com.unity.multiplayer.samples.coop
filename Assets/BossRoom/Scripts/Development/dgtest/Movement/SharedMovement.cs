using MLAPI;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Emit;
using UnityEngine;

public class SharedMovement
{
    protected MovementHost m_parent;
    protected Animator m_animator;

    protected float m_speed; //how fast we are moving in m/s. 
    protected float m_angleSpeed; //how fast we are rotating in radians/s.

    protected MovementHost.MovementMessage m_currPos;
    protected MovementHost.MovementMessage m_targetPos;

    protected static float FULL_RUN_SPEED_M_S = 6f;
    protected static float FULL_ROTATE_R_S = 1.5f;

    public SharedMovement(MovementHost i_parent )
    {
        m_parent = i_parent;
    }

    public virtual void Start()
    {
        m_animator = m_parent.GetComponent<Animator>();
    }

    public virtual void Update()
    {
        float now = ProgramState.Instance.NetTime;
        
        if( (m_currPos.m_pos != m_targetPos.m_pos) || (m_currPos.m_face != m_targetPos.m_face) && now <= m_targetPos.m_timeorspeed )
        {
            //time's up! advance m_currPos to match m_targetPos. 
            m_currPos.m_pos = m_targetPos.m_pos;
            m_currPos.m_face = m_targetPos.m_face;
            m_speed = 0;
        }
        else
        {
            float t = (now - m_currPos.m_timeorspeed) / (m_targetPos.m_timeorspeed - m_currPos.m_timeorspeed);
            m_parent.transform.position = Vector3.Lerp(m_currPos.m_pos, m_targetPos.m_pos, t);

            float curr_facing = Mathf.Lerp(m_currPos.m_face, m_targetPos.m_face, t);
            Vector2 offset = Angle2Vec(curr_facing);
            Vector3 look_at = m_parent.transform.position;
            look_at.x += offset.x;
            look_at.z += offset.y;
            m_parent.transform.LookAt(look_at);
        }

        UpdateAnims();
    }

    private void UpdateAnims()
    {
        float speed_val = Mathf.Clamp(m_speed / FULL_RUN_SPEED_M_S, -1, 1);

        m_animator.SetFloat("Speed", speed_val);
        //m_animator.SetFloat("Direction", 0);  TODO
    }

    /// <summary>
    /// Returns the Vec2 associated with this Facing Angle (in radians). Angle 0 is the +Z axis, and angle proceeds counter-clockwise. 
    /// </summary>
    /// <param name="i_angle"></param>
    /// <returns></returns>
    protected static Vector2 Angle2Vec(float i_angle )
    {
        float a = i_angle + (Mathf.PI / 2);
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    /// <summary>
    /// Converts a facing vector to a facing angle (in radians). Angle 0 is the +Z axis, and angle proceeds counter-clockwise. 
    /// </summary>
    /// <param name="i_vec"></param>
    /// <returns></returns>
    protected static float Vec2Angle(ref Vector2 i_vec )
    {
        return Vec2Angle(i_vec.y, i_vec.x);
    }

    /// <summary>
    /// Converts a facing vector to a facing angle (in radians). Angle 0 is the +Z axis, and angle proceeds counter-clockwise. 
    /// </summary>
    /// <param name="i_x"></param>
    /// <param name="i_z"></param>
    /// <returns></returns>
    protected static float Vec2Angle(float i_x, float i_z )
    {
        return Mathf.Atan2(i_z, i_x) - (Mathf.PI / 2f);
    }


    /// <summary>
    /// Sets the position and facing of this entity at a given time (which must be in the future). No validation is done, 
    /// although if the time is in the past, then it is snapped to "now". 
    /// </summary>
    /// <param name="i_newPos">the world-space position of the entity to move to.</param>
    /// <param name="i_newFace">facing angle in XZ plane, in radians. 0 faces along the +Z axis. </param>
    /// <param name="i_moveTime">networked time in seconds when the entity is at this position/facing. </param>
    public virtual void DoMove( ref Vector3 i_newPos, float i_newFace, float i_moveTime )
    {
        var t = m_parent.transform;
        float now = ProgramState.Instance.NetTime;
        m_currPos.m_pos = t.position;
        m_currPos.m_face = Vec2Angle(t.forward.x, t.forward.z);
        m_currPos.m_timeorspeed = now;

        m_targetPos.m_pos = i_newPos;
        m_targetPos.m_face = i_newFace;
        m_targetPos.m_timeorspeed = Mathf.Max(now+0.001f, i_moveTime);

        Vector3 movediff = i_newPos - m_currPos.m_pos;
        float timediff = Mathf.Max(m_targetPos.m_timeorspeed - m_currPos.m_timeorspeed, 0.001f); //pretend at least 1ms elapsed. 
        m_speed = movediff.magnitude / timediff;

        Vector2 movediffXZ = new Vector2(movediff.x, movediff.z);
        Vector2 facing = Angle2Vec(m_targetPos.m_face);
        m_speed *= Mathf.Sign(Vector2.Dot(movediffXZ, facing)); //if movement vector is opposite of face, negate the speed.
    }

    /// <summary>
    /// The server should validate this is a sensible move, and also modify the speedortime member to be a time.
    /// </summary>
    /// <param name="i_moveMessage"></param>
    protected virtual void ValidateMovement(ref MovementHost.MovementMessage i_moveMessage) {}

    public virtual void ProcessMovement(ref MovementHost.MovementMessage i_moveMessage )
    {
        ValidateMovement(ref i_moveMessage);
        DoMove(ref i_moveMessage.m_pos, i_moveMessage.m_face, i_moveMessage.m_timeorspeed);
        
    }

}
