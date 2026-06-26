using Godot;
using Godot.Collections;
using System;
// enums

public partial class Player : Node3D {
	// signals
	// exports
	// consts
	// public vars
	public float CamSpeedMod = 1;
	public Vector3 Speed = Vector3.Zero;
	public Vector3 BodyRotateSpeed = Vector3.Zero;
	public Vector3 CamRotateSpeed = Vector3.Zero;
	// private vars
	private CenterContainer EscMenu;
	private Camera3D Cam;
	// built-in override methods
		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {
			EscMenu = GetNode<CenterContainer>("../EscMenu");
			Cam = GetNode<Camera3D>("Camera3D");
		}
		
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta) {
			if (Input.IsActionJustReleased("_input_esc")) {
				ToggleEscMenu();
			}
			if (EscMenu.IsVisibleInTree()) {
				return;
			}

			UpdatePos(delta);
			
		}
	// public methods
	// private methods
	private void UpdatePos(double delta) {
		Vector3 NewPos = new(
			this.GlobalPosition.X + Speed.X * (float)delta,
			this.GlobalPosition.Y + Speed.Y * (float)delta,
			this.GlobalPosition.Z + Speed.Z * (float)delta
			);
		this.GlobalPosition = NewPos;

		Vector3 BodyRotate = new(
			this.Rotation.X + BodyRotateSpeed.X * (float)delta,
			this.Rotation.Y + BodyRotateSpeed.Y * (float)delta,
			this.Rotation.Z + BodyRotateSpeed.Z * (float)delta
			);
		this.GlobalRotation = BodyRotate;

		Vector3 CamRotate = new(
			this.Rotation.X + CamRotateSpeed.X * (float)delta,
			this.Rotation.Y + CamRotateSpeed.Y * (float)delta,
			this.Rotation.Z + CamRotateSpeed.Z * (float)delta
		);
		Cam.GlobalRotation = CamRotate;
	}
	private void ToggleEscMenu() {
		if (EscMenu.IsVisibleInTree()) {
			EscMenu.Hide();
		} else {
			EscMenu.Show();
		}
	}
}

