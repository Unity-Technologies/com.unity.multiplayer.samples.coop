using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PanicBuying
{
    public interface IUsable
    {
        enum Type
        {
            LMB,
            RMB,
            F
        }
        // Start is called before the first frame update
        void OnUse(Type type);
    }
}
