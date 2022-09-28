using System;
using System.Collections.Generic;

namespace Unity.BossRoom.Infrastructure
{
    public class DisposableGroup : IDisposable
    {
        readonly List<IDisposable> m_Disposables = new List<IDisposable>();

        public void Dispose()
        {
            foreach (var disposable in m_Disposables)
            {
                disposable.Dispose();
            }

            m_Disposables.Clear();
        }

        public void Add(IDisposable disposable)
        {
            m_Disposables.Add(disposable);
        }
    }
}
