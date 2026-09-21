using System;
using System.Collections.Concurrent;
using System.Threading;

namespace VoxelGame.scenes.main.systems.threading;

public sealed class Worker {
   private readonly Thread Thread;
   private readonly ConcurrentQueue<WorkItem> WorkQueue;
   private readonly SemaphoreSlim WorkSignal;
   private readonly CancellationToken Cancellation;

   public Worker(
      int id,
      ConcurrentQueue<WorkItem> workQueue,
      SemaphoreSlim workSignal,
      CancellationToken cancellation) {
      WorkQueue = workQueue;
      WorkSignal = workSignal;
      Cancellation = cancellation;

      Thread = new Thread(Run) {
         IsBackground = true,
         Name = $"Worker-{id}"
      };

      Thread.Start();
   }

   private void Run() {
      Console.WriteLine($"Started {Thread.CurrentThread.Name}");

      try {
         while (!Cancellation.IsCancellationRequested) {
            WorkSignal.Wait(Cancellation);

            if (WorkQueue.TryDequeue(out WorkItem Work)) {
               // Console.WriteLine(
               //    $"{Thread.CurrentThread.Name} executing work"
               // );

               try {
                  Work.Execute();
               }
               catch (Exception Error) {
                  Console.Error.WriteLine(
                     $"Worker exception: {Error}"
                  );
               }
            }
         }
      }
      catch (OperationCanceledException) {
         Console.WriteLine($"{Thread.CurrentThread.Name} stopped");
      }
   }

   public void Join() {
      Thread.Join();
   }
}


