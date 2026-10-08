@tool
extends McpTestSuite
func suite_name() -> String:
	return "feedback_assets"
func test_intake() -> void:
	var report={"animations":{},"images":{}}
	var directory=DirAccess.open("res://assets/spritecook/animations")
	for filename in directory.get_files():
		if filename.ends_with(".png"):
			var picture=Image.load_from_file("res://assets/spritecook/animations/"+filename)
			var width=picture.get_width()/8
			var height=picture.get_height()
			var crop=Rect2i()
			for index in range(8):
				var used=picture.get_region(Rect2i(index*width,0,width,height)).get_used_rect()
				crop=used if index==0 else crop.merge(used)
			report.animations[filename.get_basename()]={"path":"res://assets/spritecook/animations/"+filename,"frame_width":width,"frame_height":height,"columns":8,"rows":1,"count":8,"fps":8,"content_height":crop.size.y,"bottom_padding":height-crop.end.y,"offset_x":width*0.5-crop.get_center().x,"crop":{"x":crop.position.x,"y":crop.position.y,"w":crop.size.x,"h":crop.size.y}}
	for filename in ["violet_ground","golden_ground"]:
		var path="res://assets/spritecook/world/"+filename+".png"
		var bytes=FileAccess.get_file_as_bytes(path)
		var picture=Image.new()
		var error=picture.load_jpg_from_buffer(bytes) if bytes[0]==255 else picture.load_png_from_buffer(bytes)
		assert_eq(error,OK)
		if bytes[0]==255:
			assert_eq(picture.save_png(path),OK)
	for path in ["res://assets/spritecook/buildings/violet_crypt.png","res://assets/spritecook/buildings/golden_crypt.png","res://assets/spritecook/ui/mara_speech_bubble.png"]:
		var picture=Image.load_from_file(path)
		var rect=picture.get_used_rect()
		report.images[path]={"width":picture.get_width(),"height":picture.get_height(),"crop":{"x":rect.position.x,"y":rect.position.y,"w":rect.size.x,"h":rect.size.y}}
	var file=FileAccess.open("res://docs/Feedback/asset-geometry.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(report,"\t"))
	assert_gt(report.animations.size(),0)
