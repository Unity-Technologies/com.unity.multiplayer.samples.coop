using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundDiscManager : MonoBehaviour
{
    public GameObject normalDisc;
    public GameObject seeThroughDisc;

    // Update is called once per frame
    void Update()
    {
        int layer = LayerMask.NameToLayer("Wall");
        int mask = 1 << layer;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), 40, mask))
        {
            normalDisc.SetActive(false);
            seeThroughDisc.SetActive(true);
        }
        else
        {
            normalDisc.SetActive(true);
            seeThroughDisc.SetActive(false);

        }
    }
}
