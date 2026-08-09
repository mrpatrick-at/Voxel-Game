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
	public Vector3I Coord {get; set;}
	public ArrayMesh CubeMesh {get; set;}
	public Godot.Vector3[] Triangles {get; set;}
	public bool HasFaces {get; set;}
	// private vars
	// built-in override methods
	public override void _Ready() {
		ulong StartTime = Time.GetTicksUsec();
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] VoxelChunk [color=gold]{Coord}[/color] Starting Creation");

		ShaderMaterial ChunkMaterial = new(){
            Shader = GD.Load<Shader>("res://scenes/main/systems/map/shader/VoxelChunk.gdshader")
        };
        Texture2D TextureAtlas = GD.Load<Texture2D>("res://assets/textures/TextureAtlas.png");
		(ChunkMaterial as ShaderMaterial).SetShaderParameter("TextureAtlas", TextureAtlas);

		this.MaterialOverride = ChunkMaterial;

		this.GlobalPosition = new Godot.Vector3(Coord.X << 4, Coord.Y << 4, Coord.Z << 4);
		if (HasFaces) {
			this.Mesh = CubeMesh;
			StaticBody3D StaticBody = MakeStaticBodyFromTriangles(Triangles);
			this.AddChild(StaticBody);
		}

		float EndTime = (Godot.Time.GetTicksUsec() - StartTime) / 1000f;
		GD.PrintRich($"[color=Springgreen]VoxelChunk-[/color] Created VoxelChunk in [color=gold]{EndTime}ms[/color]");
	}
		
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

	}
	// public methods
	// private methods
	private static StaticBody3D MakeStaticBodyFromTriangles(Godot.Vector3[] Triangles) {
		ConcavePolygonShape3D Shape = new();
		Shape.SetFaces(Triangles); // Expects an array of vertices where every 3 vertices make a triangle

		CollisionShape3D CollisionShape = new() {
			Shape = Shape
		};

		StaticBody3D StaticBody = new() {
			CollisionLayer = 1,
			CollisionMask = 1
		};

		StaticBody.AddChild(CollisionShape);
		return StaticBody;
	}
}