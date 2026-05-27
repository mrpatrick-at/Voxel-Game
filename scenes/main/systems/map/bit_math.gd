extends RefCounted
## enums
## consts
## exports
## public vars
## private vars
## onready vars
# obj_ for node refrences
## built-in override methods
## public methods
func get_trailing_zeros(v: int) -> int:
	if v == 0: return 64
	var n: int = 0
	if (v & 0xFFFFFFFF) == 0: n += 32; v >>= 32
	if (v & 0xFFFF) == 0: n += 16; v >>= 16
	if (v & 0xFF) == 0: n += 8; v >>= 8
	if (v & 0xF) == 0: n += 4; v >>= 4
	if (v & 0x3) == 0: n += 2; v >>= 2
	if (v & 0x1) == 0: n += 1
	return n

func get_trailing_ones(v: int) -> int:
	return get_trailing_zeros(~v)

func set_voxel(x: int, y: int, z: int, bitboard:PackedInt64Array):
	var bit_index = x + (z << 4) + (y << 8)
	
	var array_index = bit_index >> 6
	
	var bit_pos = bit_index & 63
	
	bitboard[array_index] |= (1 << bit_pos)

#func get_voxel(x: int, y: int, z: int) -> bool:
	#var bit_idx = x + (z << 4) + (y << 8)
	#return bool(bitboard[bit_idx >> 6] & (1 << (bit_idx & 63)))

## private methods
