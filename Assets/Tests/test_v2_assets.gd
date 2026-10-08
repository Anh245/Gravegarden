@tool
extends McpTestSuite
func suite_name() -> String:
	return "v2_assets"
func test_stride_geometry() -> void:
	var report={}
	for direction in ["up","down","right"]:
		var path="res://assets/spritecook/v2/brute_walk_"+direction+".png"
		var picture=Image.load_from_file(path)
		var width=picture.get_width()/12
		var height=picture.get_height()
		var bounds=Rect2i()
		for frame in range(12):
			var used=picture.get_region(Rect2i(frame*width,0,width,height)).get_used_rect()
			bounds=used if frame==0 else bounds.merge(used)
		report["walk_"+direction]={"path":path,"frame_width":width,"frame_height":height,"columns":12,"rows":1,"count":12,"fps":8,"content_height":bounds.size.y,"bottom_padding":height-bounds.end.y,"offset_x":width*0.5-bounds.get_center().x}
		assert_gt(bounds.size.y,100)
	var f=FileAccess.open("res://docs/Feedback/V2/stride-geometry.json",FileAccess.WRITE)
	f.store_string(JSON.stringify(report,"\t"))
