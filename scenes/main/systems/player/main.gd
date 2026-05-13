extends Node3D
## enums
## consts
const cam_move_speed:float = 40
const cam_zoom_speed:float = 200
const cam_rotate_speed:float = 2
const cam_jump_speed:float = 1000
## exports
## public vars
static var cam_movement:Vector3 = Vector3.ZERO
static var cam_zoom:float = 0
static var cam_rotation:float = 0
static var grid_info:Array = []

static var cam_speed_mod:float = 1
## private vars
## onready vars
@onready var esc_menu: CenterContainer = $"../esc_menu"
@onready var float_cam: Camera3D = $FloatCam
@onready var body_cam: Camera3D = $"../PlayerBody/BodyCam"
@onready var player_body: RigidBody3D = $"../PlayerBody"
# obj_ for node refrences
## built-in override methods

func _ready() -> void:
	pass 

func _process(_delta: float) -> void:
	if Input.is_action_just_released(&"_input_menu_esc"):
		toggle_esc_menu()
	
	if esc_menu.is_visible_in_tree():
		return
	
	if Input.is_action_pressed(&"_input_cam_mod_speed"):
		cam_speed_mod = 10
	_input_cam_move(_delta)
	_input_cam_zoom(_delta)
	_input_cam_rotate(_delta)
	_update_cam_pos()
	
	_mouse_input(_delta)
	
	var collision_ray:Dictionary = get_mouse_collision_pos()
	if !collision_ray.is_empty():
		if collision_ray.collider == GridMap:
			grid_info = get_grid_info(collision_ray)
			return
	
	grid_info = []

func _on_button_close_menu_pressed() -> void:
	esc_menu.hide()

func _on_button_switch_cam_pressed() -> void:
	esc_menu.hide()

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
	
	var gridmap:GridMap = collision_ray.collider
	
	var map_pos:Vector3i = gridmap.local_to_map(gridmap.to_local(adjusted_pos))
	
	var cell_item:int = gridmap.get_cell_item(map_pos)
	
	return [collision_pos,map_pos,cell_item]

## private methods

func toggle_esc_menu() -> void:
	if esc_menu.is_visible_in_tree():
		esc_menu.hide()
	else:
		esc_menu.show()

static func _mouse_input(_delta:float) -> void:
	if Input.is_action_just_released(&"_input_mouse_left"):
		if grid_info.is_empty():
			print_rich("[color=gold]PLAYER- [color=red]Selection not inside Gridmap")
		else:
			print_rich("[color=gold]PLAYER-[color=/] Mouse_pos: [color=gold]%s[color=/], Map_pos: [color=gold]%s[color=/], cell_item: [color=gold]%s"%grid_info)
	
	if Input.is_action_just_released(&"_input_mouse_middle"):
		print("MMB pressed")
	
	if Input.is_action_just_released(&"_input_mouse_right"):
		print("RMB pressed")
	

# Camera Movement
static func _input_cam_move(_delta:float) -> void:
	var direction:Vector3 = Vector3.ZERO
	if Input.is_action_pressed(&"_input_cam_move_forward"):
		direction.z += -1
	
	if Input.is_action_pressed(&"_input_cam_move_backward"):
		direction.z += 1
	
	if Input.is_action_pressed(&"_input_cam_move_left"):
		direction.x += -1
	
	if Input.is_action_pressed(&"_input_cam_move_right"):
		direction.x += 1
	
	if Input.is_action_just_pressed(&"_input_cam_move_jump"):
		direction.y += 1
	
	if direction != Vector3.ZERO:
		cam_movement.x = direction.x * _delta * cam_move_speed
		cam_movement.y = direction.y * _delta * cam_jump_speed
		cam_movement.z = direction.z * _delta * cam_move_speed

# Camera Zoom
static func _input_cam_zoom(_delta:float) -> void:
	var zoom:float = 0
	if Input.is_action_pressed(&"_input_cam_zoom_in") or Input.is_action_just_released(&"_input_mouse_middle_up"):
		zoom += -1
	
	if Input.is_action_pressed(&"_input_cam_zoom_out") or Input.is_action_just_released(&"_input_mouse_middle_down"):
		zoom += 1
	
	if zoom != 0:
		cam_zoom = zoom * _delta * cam_zoom_speed

# Camera Rotation
static func _input_cam_rotate(_delta:float) -> void:
	var direction:float = 0
	if Input.is_action_pressed(&"_input_camera_rotate_left"):
		direction += -1
	
	if Input.is_action_pressed(&"_input_camera_rotate_right"):
		direction += 1
	
	if direction != 0:
		cam_rotation = _delta * cam_rotate_speed * direction

func _update_cam_pos() -> void:
	var cam:Camera3D = get_viewport().get_camera_3d()
	
	if cam == float_cam:
		translate_object_local(Vector3(
			cam_movement.x * cam_speed_mod,
			cam_movement.y * cam_jump_speed,
			cam_movement.z * cam_speed_mod
			)
		)
		float_cam.translate_object_local(Vector3(
			0,
			0,
			cam_zoom * cam_speed_mod
			)
		)
		global_rotation.y += cam_rotation
	
	elif cam == body_cam:
		var gravity = 0
		player_body.contact_monitor = true
		if !player_body.get_colliding_bodies().is_empty():
			gravity = player_body.get_gravity().y
		
		player_body.translate_object_local(Vector3(
			cam_movement.x * cam_speed_mod,
			cam_movement.y + gravity,
			cam_movement.z * cam_speed_mod,
		))
		player_body.global_rotation.y -= cam_rotation
	
	
	cam_movement = Vector3.ZERO
	cam_zoom = 0
	cam_rotation = 0
	cam_speed_mod = 1
