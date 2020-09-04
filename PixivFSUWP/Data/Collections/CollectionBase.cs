using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Data.Collections
{
    public abstract class CollectionBase<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        protected bool _busy = false;
        protected string nexturl = "begin";
        protected bool _emergencyStop = false;
        protected EventWaitHandle pause = new ManualResetEvent(true);

        public virtual IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            if (_busy)
                throw new InvalidOperationException("Only one operation in flight at a time");
            _busy = true;
            return AsyncInfo.Run((c) => LoadMoreItemsAsync(c, count));
        }
        public virtual bool HasMoreItems => !string.IsNullOrEmpty(nexturl);
        protected abstract Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count);

        protected virtual void LoadMoreItemsAsync_Finally()
        {
            _busy = false;
            if (!_emergencyStop) return;
            nexturl = string.Empty;
            Clear();
            GC.Collect();
        }
        public virtual void StopLoading()
        {
            _emergencyStop = true;
            if (_busy)
            {
                ResumeLoading();
            }
            else
            {
                Clear();
                GC.Collect();
            }
        }

        public virtual void PauseLoading()
        {
            pause.Reset();
        }

        public virtual void ResumeLoading()
        {
            pause.Set();
        }
    }
}
