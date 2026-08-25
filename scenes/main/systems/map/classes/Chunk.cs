using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
namespace VoxelGame.Chunk;

using System.Security.Cryptography.X509Certificates;
using VoxelGame.Consts;
[GlobalClass]
// enums
public partial class VoxelChunk : MeshInstance3D {
    // signals
    // exports
    // consts
    // public vars
    public Vector3I Coord { get; set; }
    public ArrayMesh CubeMesh { get; set; }
    public Godot.Vector3[] Triangles { get; set; }
    public bool HasFaces { get; set; }
    public ChunkCollision Collision = new();
    // private vars
    // built-in override methods
    public override void _Ready() {
        ulong StartTime = Time.GetTicksUsec();
        GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] VoxelChunk [color=gold]{Coord}[/color] Starting Creation");

        // Get Block Textures
        Texture2D TextureAtlas = GD.Load<Texture2D>("res://assets/textures/TextureAtlas.png");

        // Apply Shader
        ShaderMaterial ChunkMaterial = new() {
            Shader = GD.Load<Shader>("res://scenes/main/systems/map/shader/VoxelChunk.gdshader")
        };
        (ChunkMaterial as ShaderMaterial).SetShaderParameter("TextureAtlas", TextureAtlas);
        this.MaterialOverride = ChunkMaterial;

        this.AddChild(Collision);

        if (HasFaces) {
            Reload();
        }

        float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
        GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Created VoxelChunk in [color=gold]{EndTime}ms[/color]");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {

    }
    // public methods
    public void SetChunkData(Vector3I NewCoord, ArrayMesh NewCubeMesh, Godot.Vector3[] NewTriangles, bool NewHasFaces) {
        Coord = NewCoord;
        CubeMesh = NewCubeMesh;
        Triangles = NewTriangles;
        HasFaces = NewHasFaces;

        Reload();
    }
    public void Reload() {
        this.GlobalPosition = new Godot.Vector3(Coord.X << 4, Coord.Y << 4, Coord.Z << 4);

        if (HasFaces) {
            this.Mesh = CubeMesh;
        } else {
            this.Mesh = null;
        }

        Collision.SetCollision(Triangles);

    }
    // private methods
}
