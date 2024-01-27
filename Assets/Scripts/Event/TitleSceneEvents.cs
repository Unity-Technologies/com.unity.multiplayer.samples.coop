using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PanicBuying
{
    public struct CreateRoomButtonClicked { };

    public struct JoinRoomButtonClicked { };

    public struct JoinRoomSubmited {

        public string code;

        public JoinRoomSubmited(string code)
        {
            this.code = code;
        }
    };

    public struct OptionButtonClicked { };
}
