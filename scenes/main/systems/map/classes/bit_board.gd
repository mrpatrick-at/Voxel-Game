extends Resource
class_name Bitboard

@export var data: PackedInt64Array

func _init(size:int) -> void:
	data.resize(size)

func set_voxel(x: int, y: int, z: int, solid: bool) -> void:
	var bit_index:int = x + (z << 4) + (y << 8)
	var array_index:int = bit_index >> 6
	var bit_pos:int = bit_index & 63
	
	if solid:
		data[array_index] |= (1 << bit_pos)
	else:
		data[array_index] &= ~(1 << bit_pos)

func set_voxel_v(coord:Vector3i, solid:bool) -> void:
	set_voxel(coord.x, coord.y, coord.z, solid)

func get_voxel(x: int, y: int, z: int) -> bool:
	var bit_index:int = x + (z << 4) + (y << 8)
	return bool(data[bit_index >> 6] & (1 << (bit_index & 63)))

func get_voxel_v(coord:Vector3i) -> bool:
	return get_voxel(coord.x, coord.y, coord.z)
