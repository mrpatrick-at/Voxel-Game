extends Resource
class_name ChunkData
## enums
## consts
## exports
@export var chunk_coord: Vector2i
@export var tile_data: PackedInt32Array
## public vars
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
func _init(_coord:= Vector2i.ZERO) -> void:
	chunk_coord = _coord
	tile_data = PackedInt32Array()
	tile_data.resize(81920)

## public methods
## private methods
