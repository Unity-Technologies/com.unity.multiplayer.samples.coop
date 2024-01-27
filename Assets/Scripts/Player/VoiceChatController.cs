using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Vivox;
using Unity.Services.Vivox.AudioTaps;

namespace PanicBuying
{
    public class VoiceChatController : MonoBehaviour
    {
        [SerializeField]
        private NetworkState networkState;

        private float _remainTimeToUpdate = 0.0f;

        private void Update()
        {
            _remainTimeToUpdate -= Time.deltaTime;

            if(_remainTimeToUpdate <= 0.0f)
            {
                VivoxService.Instance.Set3DPosition(gameObject, networkState.JoinCode);
                _remainTimeToUpdate = 0.5f; // Only update after 0.3 or more seconds
            }
        }
    }
}
