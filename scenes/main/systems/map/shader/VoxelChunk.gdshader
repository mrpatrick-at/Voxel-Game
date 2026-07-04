shader_type spatial;

uniform sampler2D TextureAtlas : source_color, filter_nearest;
uniform float TextureRowLength = 3.0;

varying flat float VoxelId;
varying flat vec2 FaceDimensions;

void vertex() {
	// Called for every vertex the material is visible on.
	VoxelId = CUSTOM0.r;
	FaceDimensions.x = CUSTOM0.g;
	FaceDimensions.y = CUSTOM0.b;
}

void fragment() {
	// Called for every pixel the material is visible on.
	//vec2 TiledUv = UV * 
	vec2 TiledUvs = fract(UV * FaceDimensions);
	float Row = TiledUvs.x / TextureRowLength;
	float AtlasRow = Row + (VoxelId / TextureRowLength);
	vec2 AtlasUv = vec2(AtlasRow, TiledUvs.y);
	
	vec4 Texture = texture(TextureAtlas, AtlasUv);
	//vec4 FinalTex = Texture / FaceDimensions;
	
	
	
	
	ALBEDO = Texture.rgb;
	
	//ALBEDO += COLOR.rgb;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
