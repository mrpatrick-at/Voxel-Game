extends Node3D
## enums
## consts
const player_max_speed:int = 32
## exports
## public vars
static var grid_info:Array = []

static var cam_speed_mod:float = 1
static var player_speed:Vector3 = Vector3.ZERO
static var player_rotation:float = 0
## private vars
## onready vars
@onready var esc_menu: CenterContainer = $"../EscMenu"
@onready var float_cam: Camera3D = $FloatCam
@onready var body_cam: Camera3D = $"../PlayerBody/BodyCam"

@onready var player_body: RigidBody3D = $"../PlayerBody"
# obj_ for node refrences
## built-in override methods

func _ready() -> void:
	pass 

func _process(_delta: float) -> void:
	if Input.is_action_just_released(&"_input_menu_esc"):
		_toggle_esc_menu()
	
	if esc_menu.is_visible_in_tree():
		return
	
	_update_cam_pos(_delta)
	
	var collision_ray:Dictionary = get_mouse_collision_pos()
	if !collision_ray.is_empty():
		if collision_ray.collider == MeshInstance3D:
			grid_info = get_grid_info(collision_ray)
			return
	
	grid_info = []

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.is_pressed():
		
		if Input.is_action_pressed(&"_input_cam_mod_speed"):
			cam_speed_mod = 10
		
		_movement_keys(event)
	
	if event is InputEventMouse:
		_mouse_input()

## public methods

func get_mouse_collision_pos(custom_mouse_position:Vector2 = get_viewport().get_mouse_position()) -> Dictionary:
	var mouse_position:Vector2 = custom_mouse_position
	var camera:Camera3D = get_viewport().get_camera_3d()
	var ray_normal:Vector3 = camera.project_ray_normal(mouse_position)
	
	# Query Camera
	var query: PhysicsRayQueryParameters3D = PhysicsRayQueryParameters3D.new()
	query.from = camera.global_position
	query.to = camera.global_position + (ray_normal * camera.far)
	
	# If ray is colliding return position
	var collision_ray:Dictionary = camera.get_world_3d().direct_space_state.intersect_ray(query)
	return collision_ray

func get_grid_info(collision_ray:Dictionary) -> Array:
	var collision_pos:= Vector3(snappedf(collision_ray.position.x,0.01),snappedf(collision_ray.position.y,0.01),snappedf(collision_ray.position.z,0.01))
	var adjusted_pos:Vector3 = Vector3(collision_pos.x,collision_pos.y - 0.01,collision_pos.z)
	
	var mesh_instance:MeshInstance3D = collision_ray.collider
	
	var map_pos:Vector3 = mesh_instance.to_local(collision_pos)
	
	#var map_pos:Vector3i = gridmap.local_to_map(gridmap.to_local(adjusted_pos))
	#
	#var cell_item:int = gridmap.get_cell_item(map_pos)
	
	return [collision_pos,map_pos]

## private methods

func _toggle_esc_menu() -> void:
	if esc_menu.is_visible_in_tree():
		esc_menu.hide()
	else:
		esc_menu.show()

func _movement_keys(event:InputEvent) -> void:
	if Input.is_action_pressed(&"_input_cam_move_right"):
		if player_speed.x < player_max_speed:
			player_speed.x += 2
		print("Right", player_speed)
	
	if Input.is_action_pressed(&"_input_cam_move_left"):
		if player_speed.x > -player_max_speed:
			player_speed.x += -2
		print("Left", player_speed)
	
	if Input.is_action_just_pressed(&"_input_cam_move_up"):
		if player_speed.y < player_max_speed:
			player_speed.y += 32
		print("Up", player_speed)
	
	if Input.is_action_just_pressed(&"_input_cam_move_down"):
		if player_speed.y > -player_max_speed:
			player_speed.y -= 32
		print("Down", player_speed)
	
	if Input.is_action_pressed(&"_input_cam_move_backward"):
		if player_speed.z < player_max_speed:
			player_speed.z += 2
		print("Backward", player_speed)
	
	if Input.is_action_pressed(&"_input_cam_move_forward"):
		if player_speed.z > -player_max_speed:
			player_speed.z += -2
		print("Forward", player_speed)
	
	if Input.is_action_pressed(&"_input_camera_rotate_left"):
		if player_rotation < 1.0:
			player_rotation += 0.2
		
		print("Rotate Left", player_rotation)
	
	if Input.is_action_pressed(&"_input_camera_rotate_right"):
		if player_rotation > -1.0:
			player_rotation += -0.2
		
		print("Rotate Riight", player_rotation)

static func _mouse_input() -> void:
	if Input.is_action_just_released(&"_input_mouse_left"):
		if grid_info.is_empty():
			print_rich("[color=gold]PLAYER- [color=red]Selection not inside Gridmap")
		else:
			print_rich("[color=gold]PLAYER-[color=/] Mouse_pos: [color=gold]%s[color=/], Map_pos: [color=gold]%s[color=/], cell_item: [color=gold]%s"%grid_info)
	
	if Input.is_action_just_released(&"_input_mouse_middle"):
		print("MMB pressed")
	
	if Input.is_action_just_released(&"_input_mouse_right"):
		print("RMB pressed")
	

func _update_cam_pos(_delta:float) -> void:
	player_body.translate(player_speed/ 2 * _delta)
	if player_speed.x > 0:
		player_speed.x += -0.1
	
	if player_speed.x < 0:
		player_speed.x += 0.1
	
	if player_speed.y > 0:
		player_speed.y += -0.1
	
	if player_speed.y < 0:
		player_speed.y += 0.1
	
	if player_speed.z > 0:
		player_speed.z += -0.1
	
	if player_speed.z < 0:
		player_speed.z += 0.1
	
	player_body.rotate_y(player_rotation * _delta)
	#if player_rotation > 0:
			#player_rotation += -0.1
	#if player_rotation < 0:
		#player_rotation += 0.1
