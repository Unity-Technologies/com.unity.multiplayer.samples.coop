using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BossRoom.Client
{
  public class CharacterSwap : MonoBehaviour
  {
    [System.SerializableAttribute]
    public class CharacterModelSet
    {
      public GameObject ears;
      public GameObject head;
      public GameObject mouth;
      public GameObject hair;
      public GameObject eyes;
      public GameObject torso;
      public GameObject gear_left_hand;
      public GameObject gear_right_hand;
      public GameObject hand_right;
      public GameObject hand_left;
      public GameObject shoulder_right;
      public GameObject shoulder_left;


      public void setFullActive(bool isActive)
      {
        ears?.SetActive(isActive);
        head?.SetActive(isActive);
        mouth?.SetActive(isActive);
        hair?.SetActive(isActive);
        eyes?.SetActive(isActive);
        torso?.SetActive(isActive);
        gear_left_hand?.SetActive(isActive);
        hand_right?.SetActive(isActive);
        hand_left?.SetActive(isActive);
        shoulder_right?.SetActive(isActive);
        shoulder_left?.SetActive(isActive);
        gear_left_hand?.SetActive(isActive);
        gear_right_hand?.SetActive(isActive);
      }
    }
    [SerializeField]
    KeyCode hotswapKey;

    [SerializeField]
    public int modelIndex = 0;
    public CharacterModelSet[] characterModels;

    public void cycleModel()
    {
      if (modelIndex == characterModels.Length)
      {
        modelIndex = 0;
      }
      else
      {
        modelIndex += 1;
      }
      this.swapToModel(modelIndex);
    }


    public void swapToModel(int idx)
    {
      for (int x = 0; x < characterModels.Length; x++)
      {
        characterModels[x].setFullActive(x == idx);
      }
    }

    // Update is called once per frame
    void Update()
    {
      if (Input.GetKeyDown(hotswapKey))
      {
        cycleModel();
      }
    }
  }

}
