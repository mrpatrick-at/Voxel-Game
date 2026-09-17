using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace VoxelGame.scenes.main.systems.threading;

public sealed class WorkerPool : IDisposable {
   // Queue
   private readonly ConcurrentQueue<WorkItem> WorkQueue = new();

   // Synchronization
   private readonly SemaphoreSlim WorkSignal = new(0);
   private readonly CancellationTokenSource Cancellation = new();

   // Workers
   private readonly List<Worker> Workers = [];

   public int WorkerCount => Workers.Count;

   public WorkerPool(int workerCount) {
      if (workerCount <= 0)
         throw new ArgumentOutOfRangeException(nameof(workerCount));

      for (int i = 0; i < workerCount; i++) {
         Worker Worker = new(
            i,
            WorkQueue,
            WorkSignal,
            Cancellation.Token
         );

         Workers.Add(Worker);
      }
   }

   // Queue
   public void Enqueue(Action action) {
      if (action == null)
         throw new ArgumentNullException(nameof(action));

      if (Cancellation.IsCancellationRequested)
         throw new ObjectDisposedException(nameof(WorkerPool));

      WorkQueue.Enqueue(new WorkItem(action));
      WorkSignal.Release();
   }

   // Shutdown
   public void Dispose() {
      if (Cancellation.IsCancellationRequested)
         return;

      Cancellation.Cancel();

      // Wake every worker so they can see cancellation.
      for (int i = 0; i < Workers.Count; i++)
         WorkSignal.Release();

      foreach (Worker Worker in Workers)
         Worker.Join();

      Workers.Clear();

      while (WorkQueue.TryDequeue(out _)) {
      }

      WorkSignal.Dispose();
      Cancellation.Dispose();
   }
}

