using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
// enums
public partial class Player : CharacterBody3D {
	// signals
	// exports
	// consts
	const int MoveSpeed = 4;
	const int JumpSpeed = 8;
	const int SpeedDecay = 2;
	const float MouseSensitivity = 0.5F;
	// public vars
	public Vector3I Input_Direction = Vector3I.Zero;
	public Vector2 RotationSpeed = Vector2.Zero;
	public int SpeedMod = 1;
	// private vars
	PackedScene DebugCubeScene = GD.Load<PackedScene>("res://scenes/debug_cube.tscn");
	// External Nodes
	private CenterContainer EscMenu;
	private Node3D DebugNode;
	// Internal Nodes
	private Node3D WorldModel;
	private Node3D Head;
	private Camera3D Cam;
	// built-in override methods
	public override void _Ready() {
		// External Nodes
		EscMenu = GetNode<CenterContainer>("../EscMenu");
		DebugNode = GetNode<Node3D>("../DebugNode");
		// Internal Nodes
		WorldModel = GetNode<Node3D>("WorldModel");
		Head = GetNode<Node3D>("Head");
		Cam = GetNode<Camera3D>("Head/Camera3D");

		foreach (VisualInstance3D Child in WorldModel.FindChildren("*", "VisualInstance3D").Cast<VisualInstance3D>()) {
			Child.SetLayerMaskValue(1, false);
			Child.SetLayerMaskValue(2, true);
		}
	}
	public override void _Process(double delta) { // Called for Every Frame
		if (EscMenu.IsVisibleInTree()) {
			return;
		}
		UpdateRoation(delta);
	}

    public override void _PhysicsProcess(double delta) { // Called 60 times a sec
		if (EscMenu.IsVisibleInTree()) {
				return;
			}
		UpdatePos(delta);
    }
    public override void _Input(InputEvent @event) {
        base._Input(@event);
		if (@event is InputEventMouse MouseEvent) {
			HandleMouseInput(MouseEvent);
		} else if (@event is InputEventKey KeyEvent) {
			HandleKeyInput(KeyEvent);
		}
    }
	public void _OnDeleteDebugPressed() {
		RigidBody3D[] Children = [.. DebugNode.GetChildren().OfType<RigidBody3D>()];
		foreach (RigidBody3D Child in Children) {
			DebugNode.RemoveChild(Child);
			Child.QueueFree();
		}
		GD.PrintRich($"[color=lightblue]Player-[/color] Deleted [color=gold]{Children.Length}[/color] Debug Objects");
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
			GD.PrintRich("[color=lightblue]Player-[/color] LMB Pressed");
		}
		if (PressedRight) {
			GD.PrintRich("[color=lightblue]Player-[/color] RMB Pressed");
		}
		if (PressedMiddle) {
			GD.PrintRich("[color=lightblue]Player-[/color] MMB Pressed");
		}

	}
	private void HandleKeyInput(InputEventKey KeyEvent) {
		if (KeyEvent.IsActionReleased("_input_menu_esc")) {
			ToggleEscMenu();
		}

		// Movement Keys
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

		SpeedMod = Input.IsActionPressed("_input_mod_speed") ? 2 : 1;

		Input_Direction = new(XDir, YDir, ZDir);

		// Misc Keys
		if (Input.IsActionPressed("_input_spawn_debug")) {
			RigidBody3D DebugCube = (RigidBody3D)DebugCubeScene.Instantiate();
			DebugNode.AddChild(DebugCube);
			DebugCube.GlobalPosition = this.GlobalPosition;
			GD.PrintRich($"[color=lightblue]Player-[/color] Created Debug Cube");
		}
	}
	private void UpdatePos(double delta) {
		Vector3 NewVelocity = this.Velocity;

		Vector3 Direction = (Transform.Basis * Input_Direction);
		Vector3 MovementVelocity = GetMovementVelocity(delta, Direction);

		if (this.IsOnFloor()) {
			NewVelocity = MovementVelocity;
		} else {
			// Gravity
			GD.Print($"Not on Floor. Gravity:{this.GetGravity()}");
			NewVelocity.X = MovementVelocity.X;
			NewVelocity.Y += this.GetGravity().Y * (float)delta;
			NewVelocity.Z = MovementVelocity.Z;
		}

		this.Velocity = NewVelocity;

		GD.Print($"Velocity: {Velocity}");
		MoveAndSlide();
	}
	private Vector3 GetMovementVelocity(double delta ,Vector3 Direction) {
		if (Direction != Vector3.Zero) {
			return new(
				Direction.X * MoveSpeed,
				Direction.Y * JumpSpeed,
				Direction.Z * MoveSpeed
			);
		} else {
			return new(
				Mathf.MoveToward(Velocity.X, 0, 1),
				Mathf.MoveToward(Velocity.Y, 0, 1),
				Mathf.MoveToward(Velocity.Z, 0, 1)
			);
		}
	}
	private void UpdateRoation(double delta) {
		float SmoothSpeed = 32f * (float)delta;

		float FrameRotationSpeedX = RotationSpeed.X * SmoothSpeed;
		float FrameRotationSpeedY = RotationSpeed.Y * SmoothSpeed;

		float TargetRotationX = Mathf.Clamp(Head.Rotation.X + FrameRotationSpeedX, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
		float TargetRotationY = this.Rotation.Y + FrameRotationSpeedY;

		Head.Rotation = new Vector3(
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

