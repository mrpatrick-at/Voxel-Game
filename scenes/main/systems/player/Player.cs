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
	const float MouseSensitivity = 0.2F;
	// public vars
	public float CamSpeedMod = 1;
	public Vector3 Speed = Vector3.Zero;
	public Vector2 RotationSpeed = Vector2.Zero;
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
		if (EscMenu.IsVisibleInTree()) {
				return;
			}
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
		if (MouseEvent is InputEventMouseMotion MouseMotion) {
			RotationSpeed = new(
				-MouseMotion.Relative.Y * MouseSensitivity,
				-MouseMotion.Relative.X * MouseSensitivity
			);
		}
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
		Vector3 MoveDirection = (Transform.Basis.X * Speed.X) + (Transform.Basis.Z * Speed.Z);
		Vector3 NewPos = new(
			this.GlobalPosition.X + MoveDirection.X * (float)delta,
			this.GlobalPosition.Y + Speed.Y * (float)delta,
			this.GlobalPosition.Z + MoveDirection.Z * (float)delta
			);
		this.GlobalPosition = NewPos;

		Cam.Rotation = new Vector3(
			Math.Clamp(Cam.GlobalRotation.X + RotationSpeed.X * (float)delta,-89f,89f),
			0,
			0
			);
		this.GlobalRotation = new Vector3(
			0,
			this.GlobalRotation.Y + RotationSpeed.Y * (float)delta,
			0
		);
		RotationSpeed = Vector2.Zero;
	}
	private void ToggleEscMenu() {
		if (EscMenu.IsVisibleInTree()) {
			EscMenu.Hide();
			Input.MouseMode = Input.MouseModeEnum.Captured;
		} else {
			EscMenu.Show();
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}
}

