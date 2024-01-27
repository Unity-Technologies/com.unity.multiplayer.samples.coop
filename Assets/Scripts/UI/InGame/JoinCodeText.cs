using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PanicBuying
{
    public class JoinCodeText : MonoBehaviour
    {
        [SerializeField]
        private NetworkState networkState;

        [SerializeField]
        private TextMeshProUGUI text;

        private void Awake()
        {
            text.text = "Code:" + networkState.JoinCode;
        }
    }
}
