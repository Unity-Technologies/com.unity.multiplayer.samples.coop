using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
 public class ScrollingUVs : MonoBehaviour 
 {
     public float ScrollX =.01f;
     public float ScrollY =.01f;
    void Update () 
    {
        float OffsetX = Time.time * ScrollX;
        float OffsetY = Time.time * ScrollY;
        GetComponent<Renderer>().material.mainTextureOffset = new Vector2(OffsetX, OffsetY);
    }
 }