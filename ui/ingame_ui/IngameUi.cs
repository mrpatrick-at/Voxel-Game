using Godot;
using Godot.Collections;
using System;
namespace VoxelGame.IngameUi;
using VoxelGame.Consts;
public partial class IngameUi : Control
{
	// enums
	// consts
	// exports
	// public vars
	public TextureRect NoiseViewer;
	public Label SeedLabel;
	// private vars
	// onready vars
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {
			NoiseViewer = GetNode<TextureRect>("PanelContainer/VBoxContainer/NoiseViewer");
			SeedLabel = GetNode<Label>("PanelContainer/VBoxContainer/PanelContainer/HBoxContainer/SeedLabel");

		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {
			
		}
	// public methods
	public void _NoiseUpdate(int Seed, FastNoiseLite Noise) {
		NoiseTexture2D NoiseTex = new();
		NoiseTex.Height = Consts.World.Width;
		NoiseTex.Width = Consts.World.Length;
		NoiseTex.GenerateMipmaps = false;
		NoiseTex.Noise = Noise;
		NoiseViewer.Texture = NoiseTex;
		SeedLabel.Text = Seed.ToString();
	}

    // private methods
}

