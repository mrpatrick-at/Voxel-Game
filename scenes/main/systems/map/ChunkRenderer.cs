using Godot;
using Godot.Collections;
using System;
namespace VoxelGame.ChunkRenderer;
using VoxelGame.Consts;
[GlobalClass]
// enums
public partial class ChunkRenderer : Node3D {
   // signals
   // exports
   // consts
   // public vars
   // private vars
   private Rid Scenario;
   private readonly Dictionary<Vector3I, ArrayMesh> ChunkMeshes = [];
   public readonly Dictionary<Vector3I, Rid> RenderedChunks = [];

   // built-in override methods
   public override void _Ready() {
      Scenario = GetWorld3D().Scenario;
   }
   public override void _Process(double delta) {

   }
   public override void _ExitTree() {
      Clear();
   }
   // public methods
   public void CreateChunk(Vector3I ChunkCoord, ArrayMesh Mesh) {
      // Try to Remove Chunk, Incase it already exists
      RemoveChunk(ChunkCoord);

      // Make new RenderingServer Instance
      Rid Instance = RenderingServer.InstanceCreate();

      RenderingServer.InstanceSetBase(Instance, Mesh.GetRid()); // Which Mesh to render
      RenderingServer.InstanceSetScenario(Instance, Scenario); // Which 3D World to render in

      // Calc Chunk Location
      Transform3D transform = new(
          Basis.Identity,
          new Vector3(ChunkCoord.X * Consts.Chunk.Size, ChunkCoord.Y * Consts.Chunk.Size, ChunkCoord.Z * Consts.Chunk.Size)
      );

      // Set Chunk Location
      RenderingServer.InstanceSetTransform(Instance, transform);

      // Store Chunk RID
      ChunkMeshes[ChunkCoord] = Mesh;
      RenderedChunks[ChunkCoord] = Instance;
   }
   public void RemoveChunk(Vector3I ChunkCoord) {
      if (RenderedChunks.TryGetValue(ChunkCoord, out Rid Instance)) {
         RenderingServer.FreeRid(Instance);
         RenderedChunks.Remove(ChunkCoord);
      }

      ChunkMeshes.Remove(ChunkCoord);
   }
   public void SetChunkVisible(Vector3I ChunkCoord, bool Visible) {
      if (RenderedChunks.TryGetValue(ChunkCoord, out Rid Instance))
         RenderingServer.InstanceSetVisible(Instance, Visible);
   }
   public void Clear() {
      foreach (Rid Instance in RenderedChunks.Values)
         RenderingServer.FreeRid(Instance);

      RenderedChunks.Clear();
   }
   // private methods
}

