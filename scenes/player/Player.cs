using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
[GlobalClass]
// enums
public partial class Player : CharacterBody3D {
    // signals
    // Ground Movement Vars
    [Export] public int WalkSpeed = 5;
    [Export] public int SprintSpeed = 7;
    [Export] public int JumpSpeed = 5;
    [Export] public int GroundAcceleration = 14;
    [Export] public int MinGroundDeceleration = 10;
    [Export] public int GroundFriction = 2;
    // Air Movement Vars
    [Export] public float AirCap = 0.85F;
    [Export] public int AirAccelaration = 800;
    [Export] public int AirMoveSpeed = 500;
    // Misc Movement Vars
    public Vector3 WishDirection = Vector3.Zero;
    public Vector3 NoclipWishDirection = Vector3.Zero;
    public Vector3 CamAlignedWishDirection = Vector3.Zero;
    public float WishSpeed = 0;
    public Vector2 RotationSpeed = Vector2.Zero;
    public float NoclipSpeedMult = 3F;
    public bool NoclipEnabled = false;
    // Mouse Vars
    [Export] public float MouseSensitivity = 4F;
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
    private CollisionShape3D Collision;
    // built-in override methods
    public override void _Ready() {
        // External Nodes
        EscMenu = GetNode<CenterContainer>("../EscMenu");
        DebugNode = GetNode<Node3D>("../DebugNode");
        // Internal Nodes
        WorldModel = GetNode<Node3D>("WorldModel");
        Head = GetNode<Node3D>("Head");
        Cam = GetNode<Camera3D>("Head/Camera3D");
        Collision = GetNode<CollisionShape3D>("CollisionShape3D");

        foreach (VisualInstance3D Child in WorldModel.FindChildren("*", "VisualInstance3D").Cast<VisualInstance3D>()) {
            Child.SetLayerMaskValue(1, false);
            Child.SetLayerMaskValue(2, true);
        }
    }
    public override void _Input(InputEvent @event) {
        base._Input(@event);

        if (@event is InputEventMouse MouseEvent) {
            HandleMouseInput(MouseEvent);
        } else if (@event is InputEventKey KeyEvent) {
            HandleKeyInput(KeyEvent);
        }

        // Movement
        Vector3 InputDirection = new(
            Input.GetAxis("_input_move_left", "_input_move_right"),
            Input.GetAxis("_input_move_down", "_input_move_up"),
            Input.GetAxis("_input_move_forward", "_input_move_backward")
        );

        WishSpeed = InputDirection.Length() * AirMoveSpeed;

        WishDirection = this.GlobalTransform.Basis * new Vector3(InputDirection.X, 0, InputDirection.Z).Normalized();
        NoclipWishDirection = this.GlobalTransform.Basis * new Vector3(InputDirection.X, InputDirection.Y, InputDirection.Z).Normalized();
        // CamAlignedWishDirection = Cam.GlobalTransform.Basis * new Vector3(InputDirection.X, 0, InputDirection.Y).Normalized();
    }
    public override void _Process(double delta) { // Called for Every Frame
        if (EscMenu.IsVisibleInTree()) {
            return;
        }
        UpdateRoation((float)delta);
    }
    public override void _PhysicsProcess(double delta) { // Called 60 times a sec
        if (EscMenu.IsVisibleInTree()) {
            return;
        }

        float Delta = (float)delta;

        if (NoclipEnabled) {
            HandeNoclip(Delta);
        } else {
            if (this.IsOnFloor()) {
                HandleGroundPhysics(Delta);
            } else {
                HandleAirPhysics(Delta);
            }

            Vector3 HorizontalVelocity = new(this.Velocity.X, 0, this.Velocity.Z);

            MoveAndSlide();

            if (!IsOnFloor() && IsOnWall()) {
                Vector3 Normal = GetWallNormal();
                if (IsSurfaceTooSteep(Normal)) {
                    if (!TryStepUp(Delta, HorizontalVelocity)) {
                        this.MotionMode = CharacterBody3D.MotionModeEnum.Floating;
                        this.Velocity = ClipVelocity(Normal, 1, this.Velocity, Delta);
                    }
                } else {
                    this.MotionMode = CharacterBody3D.MotionModeEnum.Grounded;
                    this.Velocity = ClipVelocity(Normal, 1, this.Velocity, Delta);
                }
            }
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
    // Input Handling
    private void HandleMouseInput(InputEventMouse MouseEvent) {
        // Mouse Motion
        if (MouseEvent is InputEventMouseMotion MouseMotion) {

            RotationSpeed = new(
                -MouseMotion.Relative.Y,
                -MouseMotion.Relative.X
            );
        }
        // Mouse Buttons
        if (MouseEvent is InputEventMouseButton MouseButtonEvent) {

            if (MouseButtonEvent.ButtonIndex == MouseButton.Left) {
                GD.PrintRich("[color=lightblue]Player-[/color] LMB Pressed");
            }
            if (MouseButtonEvent.ButtonIndex == MouseButton.Right) {
                GD.PrintRich("[color=lightblue]Player-[/color] RMB Pressed");
            }
            if (MouseButtonEvent.ButtonIndex == MouseButton.Middle) {
                GD.PrintRich("[color=lightblue]Player-[/color] MMB Pressed");
            }
            // Mouse Scroll
            if (MouseButtonEvent.ButtonIndex == MouseButton.WheelUp) {
                NoclipSpeedMult = Mathf.Min(30F, NoclipSpeedMult * 1.1F);
            } else if (MouseButtonEvent.ButtonIndex == MouseButton.WheelDown) {
                NoclipSpeedMult = Mathf.Max(1F, NoclipSpeedMult * 0.9F);
            }
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

        if (Input.IsActionPressed("_input_move_noclip")) {
            NoclipEnabled = !NoclipEnabled;
            Collision.Disabled = NoclipEnabled;
            NoclipSpeedMult = 3F;
            GD.PrintRich($"[color=lightblue]Player-[/color] Noclip Enabled: [color=gold]{NoclipEnabled}");
        }
    }
    // Movement Handling
    private int GetMoveSpeed() {
        return Input.IsActionPressed("_input_move_sprint") ? SprintSpeed : WalkSpeed;
    }
    private void HandeNoclip(float delta) {
        float Speed = GetMoveSpeed() * NoclipSpeedMult;

        this.Velocity = NoclipWishDirection * Speed;
        this.GlobalPosition += this.Velocity * delta;
    }
    private void HandleGroundPhysics(float delta) {
        Vector3 NewVelocity = this.Velocity;

        // Apply Ground Friction
        float VelocityLength = new Vector3(NewVelocity.X, 0, NewVelocity.Z).Length();

        float Control = Mathf.Max(VelocityLength, MinGroundDeceleration);

        float Drop = Control * GroundFriction * delta;

        float SpeedScale = Mathf.Max(VelocityLength - Drop, 0);
        if (VelocityLength > 0) {
            SpeedScale /= VelocityLength;
        }

        NewVelocity.X *= SpeedScale;
        NewVelocity.Z *= SpeedScale;

        // Accelaration
        int MoveSpeed = GetMoveSpeed();

        float SpeedInWishDirection = NewVelocity.Dot(WishDirection);
        float SpeedLeftTillCap = MoveSpeed - SpeedInWishDirection;

        if (SpeedLeftTillCap > 0) {
            float AccelarationSpeed = Mathf.Min(GroundAcceleration * MoveSpeed * delta, SpeedLeftTillCap);
            // GD.Print($"AccelarationSpeed: {AccelarationSpeed} Is same as Cap? {AccelarationSpeed == SpeedLeftTillCap}");
            NewVelocity += AccelarationSpeed * WishDirection;
        }

        // Jumping
        if (Input.IsActionPressed("_input_move_up")) {
            NewVelocity.Y += JumpSpeed;
        }

        HeadbobEffect(delta);

        this.Velocity = NewVelocity;
    }
    private void HandleAirPhysics(float delta) {
        Vector3 NewVelocity = this.Velocity;

        // Apply Gravity
        float Gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
        NewVelocity.Y -= Gravity * delta;

        // Accelaration
        float SpeedInWishDirection = NewVelocity.Dot(WishDirection);
        float SpeedCap = Mathf.Min(WishSpeed, AirCap);
        float SpeedLeftTillCap = SpeedCap - SpeedInWishDirection;

        if (SpeedLeftTillCap > 0) {
            float AccelarationSpeed = Mathf.Min(AirAccelaration * AirMoveSpeed * delta, SpeedLeftTillCap);
            // GD.Print($"AccelarationSpeed: {AccelarationSpeed} Is same as Cap? {AccelarationSpeed == SpeedLeftTillCap}");

            NewVelocity += WishDirection * AccelarationSpeed;
        }
        this.Velocity = NewVelocity;
    }
    private bool TryStepUp(float delta, Vector3 HorizontalVelocity) {
        if (HorizontalVelocity.LengthSquared() < 0.001f) {
            return false;
        }

        float StepHeight = 0.5F;
        Vector3 StartingPosition = this.GlobalPosition;

        // Move Up
        Vector3 UpMotion = Vector3.Up * StepHeight;

        if (TestMove(GlobalTransform, UpMotion)) {
            GlobalPosition = StartingPosition;
            GD.Print("Couldn't move up");
            return false;
        }

        this.GlobalPosition += UpMotion;

        // Move Horizontally
        Vector3 HorizontalMotion = HorizontalVelocity * StepHeight;

        if (TestMove(GlobalTransform, HorizontalMotion)) {
            GlobalPosition = StartingPosition;
            GD.Print("Couldn't move forward");
            return false;
        }

        Vector3 HorizontalMove = HorizontalVelocity * delta;

        GlobalPosition += HorizontalMove;

        // Move Down
        float DownStep = 0.05F;
        bool FoundGround = false;
        for (float DownDistance = 0F; DownDistance < StepHeight; DownDistance += DownStep) {
            float MoveAmount = Mathf.Min(DownStep, StepHeight - DownDistance);

            if (TestMove(GlobalTransform, Vector3.Down * MoveAmount)) {
                FoundGround = true;
                break;
            }

            GlobalPosition += Vector3.Down * MoveAmount;
        }

        if (!FoundGround) {
            GlobalPosition = StartingPosition;
            GD.Print("Couldn't find ground");
            return false;
        }

        // Succesfull Step
        Vector3 NewVelocity = new(HorizontalVelocity.X, this.Velocity.Y, HorizontalVelocity.Z);

        this.Velocity = NewVelocity;

        return true;
    }
    private Vector3 ClipVelocity(Vector3 Normal, float Overbounce, Vector3 NewVelocity, float delta) {
        float Backoff = NewVelocity.Dot(Normal) * Overbounce;

        if (Backoff >= 0) return Vector3.Zero;

        NewVelocity -= Normal * Backoff;

        // Second Iteration to make sure not clipping thru Plane. Not Sure why this is Necesarry, but it Was in the Original
        float Adjust = NewVelocity.Dot(Normal);
        if (Adjust < 0) {
            NewVelocity -= Normal * Adjust;
        }
        return NewVelocity;
    }
    private bool IsSurfaceTooSteep(Vector3 Normal) {
        if (Normal.Dot(Vector3.Up) < Mathf.Cos(FloorMaxAngle)) return true;
        return false;
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
        float SmoothSpeed = MouseSensitivity * delta;

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

