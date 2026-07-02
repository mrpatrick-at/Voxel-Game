shader_type spatial;

uniform sampler2D TextureAtlas : source_color, filter_nearest;
uniform float TextureRowLength = 3.0;

varying flat float VoxelId;

void vertex() {
	// Called for every vertex the material is visible on.
	VoxelId = CUSTOM0.r;
	
}

void fragment() {
	// Called for every pixel the material is visible on.
	//vec2 TiledUv = UV * 
	float Row = UV.x / TextureRowLength;
	float AtlasRow = Row + (VoxelId / TextureRowLength);
	vec2 AtlasUv = vec2(AtlasRow, UV.y);
	
	vec4 Texture = texture(TextureAtlas, AtlasUv);
	
	
	
	ALBEDO = Texture.rgb;
	
	//ALBEDO += COLOR.rgb;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
