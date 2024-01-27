using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    struct SomeEvent
    {
        public string message;
    }

    public class EventListenerExample : MonoBehaviour
    {
        private EventListener<SomeEvent> _someEventListener = new();

        // Start is called before the first frame update
        IEnumerator Start()
        {
            _someEventListener.StartListen((e) =>
            {
                Debug.Log(e.message);
            });

            yield return new WaitForSeconds(3);

            SomeEvent someEvent = new();
            someEvent.message = "이벤트!!!";

            Event.Emit(someEvent);
        }
    }
}
