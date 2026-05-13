extends Node3D
class_name Chunk
## enums
## consts
## exports
## public vars
static var gridmap:GridMap
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods
func setup(chunk_pos:Vector2i, mesh_library:MeshLibrary) -> void:
	gridmap = GridMap.new()
	gridmap.mesh_library = mesh_library
	gridmap.cell_size = Vector3(2,2,2)
	add_child(gridmap)
	
	global_position = Vector3(chunk_pos.x << 7, 0, chunk_pos.y << 7)
	print(global_position)

func set_tile(local_x: int,local_y:int, local_z: int, item_id: int) -> void:
	gridmap.set_cell_item(Vector3i(local_x, local_y, local_z), item_id)

func delete_tile(local_x: int, height: int, local_z: int) -> void:
	set_tile(local_x, local_z, height, -1)

## private methods
