using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pot_break : MonoBehaviour
{
    public GameObject broken;

    void OnMouseDown()
    {
        Instantiate(broken, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
