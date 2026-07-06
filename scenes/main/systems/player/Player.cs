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
	const float MouseSensitivity = 0.5F;
	// public vars
	public float CamSpeedMod = 1;
	public Vector3I Direction = Vector3I.Zero;
	public Vector3 Speed = Vector3.Zero;
	public Vector2 RotationSpeed = Vector2.Zero;
	// private vars
	private CenterContainer EscMenu;
	private Camera3D Cam;
	private Node3D World;
	PackedScene DebugCubeScene = GD.Load<PackedScene>("res://scenes/debug_cube.tscn");
	// built-in override methods
	public override void _Ready() {
		EscMenu = GetNode<CenterContainer>("../EscMenu");
		Cam = GetNode<Camera3D>("Camera3D");
		World = GetNode<Node3D>("..");
	}
	public override void _Process(double delta) { // Called for Every Frame
		if (EscMenu.IsVisibleInTree()) {
			return;
		}
		UpdatePos(delta);
	}

    public override void _PhysicsProcess(double delta) { // Called 60 times a sec
		if (EscMenu.IsVisibleInTree()) {
				return;
			}
		Speed = CalcMovement();
    }
    public override void _Input(InputEvent @event) {
        base._Input(@event);
		if (@event is InputEventMouse MouseEvent) {
			HandleMouseInput(MouseEvent);
		} else if (@event is InputEventKey KeyEvent) {
			HandleKeyInput(KeyEvent);
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
	private void HandleKeyInput(InputEventKey KeyEvent) {
		if (KeyEvent.IsActionReleased("_input_menu_esc")) {
			ToggleEscMenu();
		}
		// Movement
		int XDir = 0;
		int YDir = 0;
		int ZDir = 0;

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

		if (Input.IsActionPressed("_input_move_backward")) {
			ZDir = 1;
		}
		if (Input.IsActionPressed("_input_move_forward")) {
			ZDir -= 1;
		}

		int SpeedMod = Input.IsActionPressed("_input_mod_speed") ? 2 : 1;

		Direction = new(XDir * SpeedMod, YDir * SpeedMod, ZDir * SpeedMod); // TODO: Adjust Max Speed to Increase when Speed Mod is active

		// Misc Keys
		if (Input.IsActionPressed("_input_spawn_debug")) {
			RigidBody3D DebugCube = (RigidBody3D)DebugCubeScene.Instantiate();
			World.AddChild(DebugCube);
			DebugCube.GlobalPosition = this.GlobalPosition;
		}
	}
	private Vector3 CalcMovement() {
		Vector3 NewSpeed = new(
			Math.Clamp(Speed.X + (Direction.X * MoveSpeed) - Math.Sign(Speed.X) * SpeedDecay, -MaxSpeed, MaxSpeed),
			Math.Clamp(Speed.Y + (Direction.Y * MoveSpeed) - Math.Sign(Speed.Y) * SpeedDecay, -MaxSpeed, MaxSpeed),
			Math.Clamp(Speed.Z + (Direction.Z * MoveSpeed) - Math.Sign(Speed.Z) * SpeedDecay, -MaxSpeed, MaxSpeed)
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
		
		float SmoothSpeed = 32f * (float)delta;

		float FrameRotationSpeedX = RotationSpeed.X * SmoothSpeed;
		float FrameRotationSpeedY = RotationSpeed.Y * SmoothSpeed;

		float TargetRotationX = Mathf.Clamp(Cam.Rotation.X + FrameRotationSpeedX, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
		float TargetRotationY = this.Rotation.Y + FrameRotationSpeedY;

		Cam.Rotation = new Vector3(
			TargetRotationX,
			0,
			0
			);
		
		this.Rotation = new Vector3(
			0,
			TargetRotationY,
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

