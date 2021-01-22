using System;
using Unity.MultiPie;

namespace BossRoom.Scripts.Editor.SliceBehaviors
{
    public class EmptySliceBehavior : SliceBehavior
    {
        public override void OnAllSlicesStarted()
        {
        }

        public override void OnSliceMessageReceived(int senderId, string message)
        {
        }

    }
}
