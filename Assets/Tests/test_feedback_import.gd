@tool
extends McpTestSuite
func suite_name() -> String:
	return "feedback_import"
func test_normalize_terrain_encoding() -> void:
	var report=[]
	for path in ["res://assets/spritecook/world/frostmoor_ground.png","res://assets/spritecook/world/emberfen_ground.png"]:
		var bytes=FileAccess.get_file_as_bytes(path)
		var picture=Image.new()
		var error=picture.load_jpg_from_buffer(bytes) if bytes[0]==255 else picture.load_png_from_buffer(bytes)
		assert_eq(error,OK)
		var original=picture.get_data()
		assert_eq(picture.save_png(path),OK)
		var decoded=Image.load_from_file(path)
		assert_eq(decoded.get_data(),original,"Lossless format normalization preserves every decoded pixel")
		report.append({"path":path,"width":picture.get_width(),"height":picture.get_height()})
	var file=FileAccess.open("res://docs/Feedback/terrain-intake.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"\t"))
