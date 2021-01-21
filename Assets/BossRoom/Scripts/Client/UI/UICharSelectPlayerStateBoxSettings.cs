using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Client
{
    /// <summary>
    /// Describes one of the many possible configurations of a char-select screen player state box.
    /// (This is separated out allow for more specialization in the future, e.g. providing a root node
    /// for particles or a placement point for a 3d character representation.)
    /// </summary>
    public class UICharSelectPlayerStateBoxSettings : MonoBehaviour
    {
        public GameObject GraphicsRoot;
        public Text PlayerIndexText;
    }
}
