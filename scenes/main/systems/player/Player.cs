using Godot;
using Godot.Collections;
using System;
using System.Runtime.CompilerServices;
// enums
public partial class Player : Node3D {
	// signals
	// exports
	// consts
	const int MaxSpeed = 32;
	const int MoveSpeed = 4;
	const int SpeedDecay = 2;
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
			if (Input.IsActionJustReleased("_input_menu_esc")) {
				ToggleEscMenu();
			}
			if (EscMenu.IsVisibleInTree()) {
				return;
			}

			UpdatePos(delta);
		}

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
		Speed = CalcMovement();
    }

    public override void _Input(InputEvent @event) {
        base._Input(@event);
		if (@event is InputEventMouse MouseEvent) {
				HandleMouseInput(MouseEvent);
		}
    }
	// public methods
	// private methods
	private void HandleMouseInput(InputEventMouse MouseEvent) {
		bool PressedLeft = (MouseEvent.ButtonMask & MouseButtonMask.Left) != 0;
		bool PressedRight = (MouseEvent.ButtonMask & MouseButtonMask.Right) != 0;
		bool PressedMiddle = (MouseEvent.ButtonMask & MouseButtonMask.Middle) != 0;

		if (PressedLeft) {
			GD.Print("PLAYER- LMB Pressed");
		}
		if (PressedRight) {
			GD.Print("PLAYER- RMB Pressed");
		}
		if (PressedMiddle) {
			GD.Print("PLAYER- MMB Pressed");
		}

	}
	private Vector3 CalcMovement() {
		float XDir = 0;
		float YDir = 0;
		float ZDir = 0;
		if (Input.IsActionPressed("_input_move_backward")) {
			ZDir = 1;
		}
		if (Input.IsActionPressed("_input_move_forward")) {
			ZDir -= 1;
		}

		if (Input.IsActionPressed("_input_move_right")) {
			XDir = 1;
		}
		if (Input.IsActionPressed("_input_move_left")) {
			XDir -= 1;
		}

		if (Input.IsActionPressed("_input_move_up")) {
			YDir = 1;
		}
		if (Input.IsActionPressed("_input_move_down")) {
			YDir -= 1;
		}

		int SpeedMod = Input.IsActionPressed("_input_mod_speed") ? 2 : 1;

		Vector3 NewSpeed = new(
			Math.Clamp(Speed.X + (XDir * MoveSpeed * SpeedMod) - Math.Sign(Speed.X) * SpeedDecay, -MaxSpeed, MaxSpeed),
			Math.Clamp(Speed.Y + (YDir * MoveSpeed * SpeedMod) - Math.Sign(Speed.Y) * SpeedDecay, -MaxSpeed, MaxSpeed),
			Math.Clamp(Speed.Z + (ZDir * MoveSpeed * SpeedMod) - Math.Sign(Speed.Z) * SpeedDecay, -MaxSpeed, MaxSpeed)
		);
		return NewSpeed;
	}
	private void UpdatePos(double delta) {
		Vector3 NewPos = new(
			this.GlobalPosition.X + Speed.X * (float)delta,
			this.GlobalPosition.Y + Speed.Y * (float)delta,
			this.GlobalPosition.Z + Speed.Z * (float)delta
			);
		this.GlobalPosition = NewPos;

		Vector3 NewBodyRotate = new(
			this.Rotation.X + BodyRotateSpeed.X * (float)delta,
			this.Rotation.Y + BodyRotateSpeed.Y * (float)delta,
			this.Rotation.Z + BodyRotateSpeed.Z * (float)delta
			);
		// this.GlobalRotation = NewBodyRotate;

		Vector3 NewCamRotate = new(
			this.Rotation.X + CamRotateSpeed.X * (float)delta,
			this.Rotation.Y + CamRotateSpeed.Y * (float)delta,
			this.Rotation.Z + CamRotateSpeed.Z * (float)delta
		);
		// Cam.GlobalRotation = NewCamRotate;
	}
	private void ToggleEscMenu() {
		if (EscMenu.IsVisibleInTree()) {
			EscMenu.Hide();
		} else {
			EscMenu.Show();
		}
	}
}

