using System;

namespace VoxelGame.scenes.main.systems.threading;

public sealed class WorkItem(Action action) {
   public Action Action { get; } = action ?? throw new ArgumentNullException(nameof(action));

   public void Execute() {
      Action();
   }
}


