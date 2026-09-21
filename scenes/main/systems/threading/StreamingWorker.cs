using System;
using System.Threading;

public sealed class StreamingWorker<T> : IDisposable {
   private readonly Thread _thread;
   private readonly AutoResetEvent _wakeSignal = new(false);
   private readonly object _lock = new();

   private readonly Action<T> _action;

   private T _value;
   private bool _hasValue;
   private bool _stopping;
   private bool _disposed;

   public StreamingWorker(
       string name,
       Action<T> action,
       T initialValue = default!) {
      ArgumentException.ThrowIfNullOrWhiteSpace(name);
      ArgumentNullException.ThrowIfNull(action);

      _action = action;
      _value = initialValue;

      _thread = new Thread(Run) {
         IsBackground = true,
         Name = name
      };

      _thread.Start();
   }

   /// <summary>
   /// Replaces the pending value and wakes the worker.
   /// Multiple calls while the worker is busy are coalesced.
   /// </summary>
   public void Signal(T value) {
      lock (_lock) {
         ObjectDisposedException.ThrowIf(_disposed, this);

         _value = value;
         _hasValue = true;
      }

      _wakeSignal.Set();
   }

   private void Run() {
      try {
         while (true) {
            _wakeSignal.WaitOne();

            T value;

            lock (_lock) {
               if (_stopping)
                  return;

               if (!_hasValue)
                  continue;

               value = _value;
               _hasValue = false;
            }

            try {
               _action(value);
            }
            catch (Exception error) {
               Console.Error.WriteLine(
                   $"StreamingWorker '{_thread.Name}' failed:\n{error}");
            }
         }
      }
      catch (ObjectDisposedException) {
         // The synchronization primitive was disposed while
         // the worker was shutting down.
      }
   }

   public void Dispose() {
      lock (_lock) {
         if (_disposed)
            return;

         _disposed = true;
         _stopping = true;
      }

      _wakeSignal.Set();

      if (Thread.CurrentThread != _thread)
         _thread.Join();

      _wakeSignal.Dispose();
   }
}
