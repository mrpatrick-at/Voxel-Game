using Godot;
using Godot.Collections;
using System;
// enums
public partial class EscMenu : CenterContainer
{
	// signals
	// exports
	// consts
	// public vars
	// private vars
	// onready vars
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {

		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {
			
		}
		public void _OnClosePressed() {
			this.Hide();
		}
		public void _OnQuitPressed() {
			GetTree().Quit();
		}
	// public methods
	// private methods
}

