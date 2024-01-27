using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PanicBuying
{
    public class JoinButton : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField inputField;

        public void Join()
        {
            JoinRoomSubmited e = new(inputField.text);

            Event.Emit(e);
        }
    } 
}
