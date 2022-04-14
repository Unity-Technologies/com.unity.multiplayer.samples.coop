using System;
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
