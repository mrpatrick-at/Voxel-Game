using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
// enums
public partial class Player : CharacterBody3D {
	// signals
	// Ground Movement Vars
	[Export] public int WalkSpeed = 6;
	[Export] public int SprintSpeed = 8;
	[Export] public int JumpSpeed = 8;
	[Export] public int SpeedDecay = 2;
	[Export] public int GroundAcceleration = 14;
	[Export] public int GroundDeceleration = 10;
	[Export] public int GroundFriction = 6;
	// Air Movement Vars
	[Export] public float AirCap = 0.85F;
	[Export] public int AirAccelaration = 800;
	[Export] public int AirMoveSpeed = 500;
	// Misc Movement Vars
	public Vector3 WishDirection = Vector3.Zero;
	public Vector2 RotationSpeed = Vector2.Zero;
	// Mouse Vars
	[Export] public float MouseSensitivity = 0.5F;
	// Animation Vars
	[Export] public float HeadbobMoveAmount = 0.06F;
	[Export] public float HeadbobFrequency = 2.4F;
	public float HeadbobTime = 0F;
	// private vars
	private PackedScene DebugCubeScene = GD.Load<PackedScene>("res://scenes/debug/debug_cube.tscn");
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
	// Input Handling
	public override void _Input(InputEvent @event) {
        base._Input(@event);

		if (@event is InputEventMouse MouseEvent) {
			HandleMouseInput(MouseEvent);
		} else if (@event is InputEventKey KeyEvent) {
			HandleKeyInput(KeyEvent);
		}

		// Movement
		Vector2 InputDirection = Input.GetVector("_input_move_left","_input_move_right","_input_move_up","_input_move_down").Normalized();
		WishDirection = this.GlobalTransform.Basis * new Vector3(InputDirection.X, 0, InputDirection.Y);
    }
	public void _OnDeleteDebugPressed() {
		RigidBody3D[] Children = [.. DebugNode.GetChildren().OfType<RigidBody3D>()];
		foreach (RigidBody3D Child in Children) {
			DebugNode.RemoveChild(Child);
			Child.QueueFree();
		}
		GD.PrintRich($"[color=lightblue]Player-[/color] Deleted [color=gold]{Children.Length}[/color] Debug Objects");
		}
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

		if (Input.IsActionPressed("_input_spawn_debug")) {
			RigidBody3D DebugCube = (RigidBody3D)DebugCubeScene.Instantiate();
			DebugNode.AddChild(DebugCube);
			DebugCube.GlobalPosition = this.GlobalPosition;
			GD.PrintRich($"[color=lightblue]Player-[/color] Created Debug Cube");
		}
	}
	private int GetMoveSpeed() {
		return Input.IsActionPressed("_input_move_sprint") ? SprintSpeed : WalkSpeed;
	}
	// Process
	public override void _Process(double delta) { // Called for Every Frame
		if (EscMenu.IsVisibleInTree()) {
			return;
		}
		UpdateRoation((float)delta);
	}
	// Physics
    public override void _PhysicsProcess(double delta) { // Called 60 times a sec
		if (EscMenu.IsVisibleInTree()) {
				return;
			}
		if (this.IsOnFloor()) {
			HandleGroundPhysics((float)delta);
		} else {
			HandleAirPhysics((float)delta);
		}
		GD.Print($"Velocity: {this.Velocity}");
		MoveAndSlide();
    }
	private void HandleGroundPhysics(float delta) {
		Vector3 NewVelocity = this.Velocity;
		int MoveSpeed = GetMoveSpeed();

		float SpeedInWishDirection = NewVelocity.Dot(WishDirection);
		float SpeedLeftTillCap = MoveSpeed - SpeedInWishDirection;

		if (SpeedLeftTillCap > 0) {
			float AccelarationSpeed = Mathf.Min(GroundAcceleration * MoveSpeed * delta, SpeedLeftTillCap);
			// GD.Print($"AccelarationSpeed: {AccelarationSpeed} Is same as Cap? {AccelarationSpeed == SpeedLeftTillCap}");
			NewVelocity += AccelarationSpeed * WishDirection;
		}
		
		// Deceleration
		float Control = Mathf.Max(NewVelocity.Length(), GroundDeceleration);
		float Drop = Control * GroundFriction * delta;
		float NewSpeed = Mathf.Max(NewVelocity.Length() - Drop, 0);
		if (NewVelocity.Length() > 0) {
			NewSpeed /= NewVelocity.Length();
		}
		NewVelocity *= NewSpeed;

		// Jumping
		if (Input.IsActionPressed("_input_move_jump")) {
			NewVelocity.Y += JumpSpeed;
		}

		HeadbobEffect(delta);

		this.Velocity = NewVelocity;
	}
	private void HandleAirPhysics(float delta) {
		Vector3 NewVelocity = this.Velocity;

		float Gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
       	NewVelocity.Y -= Gravity * (float)delta;

		float SpeedInWishDirection = NewVelocity.Dot(WishDirection);
		float CappedSpeed = Mathf.Min(WishDirection.Length(), AirCap); // CappedSpeed = AirCap, Unless WishDir is 0 / cancels out.
		float SpeedLeftTillCap = CappedSpeed - SpeedInWishDirection;
		// GD.Print($"CurrentSpeedInWIshDirection: {SpeedInWishDirection}, CappedSpeed: {CappedSpeed}, SpeedLeftTillCap: {SpeedLeftTillCap}");

		if (SpeedLeftTillCap > 0) {
			float AccelarationSpeed = Mathf.Min(AirAccelaration * AirMoveSpeed * delta, SpeedLeftTillCap); // Note: Adjust Later
			GD.Print($"AccelarationSpeed: {AccelarationSpeed} Is same as Cap? {AccelarationSpeed == SpeedLeftTillCap}");
			NewVelocity += AccelarationSpeed * WishDirection;
		}

		this.Velocity = NewVelocity;
	}
	// Animations
	private void HeadbobEffect(float delta) {
		HeadbobTime += delta * this.Velocity.Length();

		Transform3D NewTransform = Cam.Transform;
		NewTransform.Origin = new Vector3(
			Mathf.Cos(HeadbobTime * HeadbobFrequency * 0.5F) * HeadbobMoveAmount,
			Mathf.Cos(HeadbobTime * HeadbobFrequency) * HeadbobMoveAmount,
			0
		);

		Cam.Transform = NewTransform;
	}
	// public methods
	// private methods
	private void UpdateRoation(float delta) {
		float SmoothSpeed = 32f * delta;

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

