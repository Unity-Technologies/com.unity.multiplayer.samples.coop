using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PanicBuying
{
    public struct NetworkStateWorkEvent {
        public bool working;

        public NetworkStateWorkEvent(bool working)
        {
            this.working = working;
        }
    };

    public struct CreateRoomButtonClicked { };

    public struct JoinRoomSubmited {

        public string code;

        public JoinRoomSubmited(string code)
        {
            this.code = code;
        }
    };

    public struct OptionButtonClicked { };
}
