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
   private readonly Dictionary<Vector3I, Rid> RenderedChunks = [];
   private readonly ShaderMaterial ChunkMaterial = new();

   // built-in override methods
   public override void _Ready() {
      Scenario = GetWorld3D().Scenario;

      Shader Shader = GD.Load<Shader>("res://scenes/main/systems/map/shader/VoxelChunk.gdshader");

      ChunkMaterial.Shader = Shader;

      Texture2D Atlas = GD.Load<Texture2D>("res://assets/textures/TextureAtlas.png");
      ChunkMaterial.SetShaderParameter("TextureAtlas", Atlas);
   }
   public override void _Process(double delta) {

   }
   public override void _ExitTree() {
      Clear();
   }
   // public methods
   public void CreateChunk(Vector3I ChunkCoord, Godot.Collections.Array MeshArray) {
      // Try to Remove Chunk, Incase it already exists
      RemoveChunk(ChunkCoord);

      // Make the Mesh
      ArrayMesh Mesh = MakeCubeMesh(MeshArray);

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
      foreach (Rid Instance in RenderedChunks.Values) {
         RenderingServer.FreeRid(Instance);
      }

      RenderedChunks.Clear();
      ChunkMeshes.Clear();
   }
   // private methods
   private ArrayMesh MakeCubeMesh(Godot.Collections.Array MeshArray) {
      ArrayMesh CubeMesh = new();

      Mesh.ArrayFormat FormatFlags = Mesh.ArrayFormat.FormatVertex
                                  | Mesh.ArrayFormat.FormatNormal
                                  | Mesh.ArrayFormat.FormatTexUV
                                  | Mesh.ArrayFormat.FormatIndex
                                  // | Mesh.ArrayFormat.FormatColor
                                  | Mesh.ArrayFormat.FormatCustom0;

      int Custom0FormatShift = (int)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift;
      FormatFlags |= (Mesh.ArrayFormat)Custom0FormatShift;

      CubeMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshArray, flags: FormatFlags);

      CubeMesh.SurfaceSetMaterial(0, ChunkMaterial);

      return CubeMesh;
   }
}

