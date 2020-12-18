using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuFX : MonoBehaviour
{
    private GameObject m_backdrop;
    private float m_xbase;
    private float m_start;


    // Start is called before the first frame update
    void Start()
    {
        m_backdrop = GameObject.Find("MainMenuBackdrop");
        m_xbase = ((RectTransform)m_backdrop.transform).anchoredPosition.x;
        m_start = UnityEngine.Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        //do a little sway on the backdrop.
        float elapsed = UnityEngine.Time.time - m_start;
        float offset = 15*Mathf.Sin(0.4f * elapsed);

        RectTransform t = (RectTransform)m_backdrop.transform;

        var pos = t.anchoredPosition;
        pos.x = m_xbase + offset;

        t.anchoredPosition = pos;

        //t.anchoredPosition.Set(m_xbase + offset, t.anchoredPosition.y);

        //t.ForceUpdateRectTransforms();

        //m_backdrop.transform.position.Set(m_start + offset, m_backdrop.transform.position.y, m_backdrop.transform.position.z);
    }
}
