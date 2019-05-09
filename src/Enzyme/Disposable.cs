using System;

namespace Enzyme
{
    class Disposable : IDisposable
    {
        readonly Action dispose;

        public Disposable(Action dispose) => this.dispose = dispose;

        public void Dispose() => dispose();
    }
}