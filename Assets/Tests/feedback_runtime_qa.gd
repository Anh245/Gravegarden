extends RefCounted
## Runtime verification uses the authored game and native input events only.
var reports: Array=[]
func touch(g: Node,index: int,point: Vector2,down: bool) -> void:
	var event=InputEventScreenTouch.new()
	event.index=index
	event.position=point*Vector2(g.get_tree().root.size)/g.get_viewport_rect().size
	event.pressed=down
	Input.parse_input_event(event)
func drag(g: Node,index: int,from: Vector2,to: Vector2) -> void:
	var event=InputEventScreenDrag.new()
	event.index=index
	var factor=Vector2(g.get_tree().root.size)/g.get_viewport_rect().size
	event.position=to*factor
	event.relative=(to-from)*factor
	Input.parse_input_event(event)
func mouse(g: Node,point: Vector2,button: int,down: bool) -> void:
	var event=InputEventMouseButton.new()
	event.position=point*Vector2(g.get_tree().root.size)/g.get_viewport_rect().size
	event.button_index=button
	event.pressed=down
	Input.parse_input_event(event)
func capture(g: Node,name: String) -> Dictionary:
	await g.get_tree().create_timer(0.4).timeout
	var picture=g.get_tree().root.get_texture().get_image()
	var window=g.get_tree().root.size
	var render_size=picture.get_size()
	if render_size!=window:
		picture.resize(window.x,window.y,Image.INTERPOLATE_LANCZOS)
	var path="res://docs/Feedback/"+name+".png"
	picture.save_png(path)
	var report=audit(g)
	report["path"]=path
	report["render_size"]=render_size
	report["image_size"]=picture.get_size()
	reports.append(report)
	return report
func audit(g: Node) -> Dictionary:
	var stack: Array=[g.ui]
	var problems: Array=[]
	var buttons: Array=[]
	var labels: int=0
	var viewport=Rect2(Vector2.ZERO,g.get_viewport_rect().size)
	while not stack.is_empty():
		var node: Node=stack.pop_back()
		stack.append_array(node.get_children())
		if not node is Control or not node.is_visible_in_tree() or node.modulate.a<0.95:
			continue
		var control: Control=node
		var rect=control.get_global_rect()
		if not rect.intersects(viewport):
			continue
		if node is BaseButton:
			buttons.append({"path":str(g.ui.get_path_to(node)),"size":rect.size})
			if minf(rect.size.x,rect.size.y)<95.0:
				problems.append({"path":str(node.get_path()),"kind":"small_touch","size":rect.size})
		if node is Label and not node.text.is_empty():
			labels+=1
			if node.get_visible_line_count()<node.get_line_count():
				problems.append({"path":str(node.get_path()),"kind":"clipped_lines","visible":node.get_visible_line_count(),"total":node.get_line_count()})
			var parent=node.get_parent()
			if parent is Control and not str(parent.name).begins_with("WorldHealth"):
				if not Rect2(Vector2.ZERO,parent.size).grow(2).encloses(node.get_rect()):
					problems.append({"path":str(node.get_path()),"kind":"text_outside_parent","rect":node.get_rect(),"parent_size":parent.size})
	return {"viewport":g.get_viewport_rect().size,"problems":problems,"buttons":buttons,"labels":labels}
func save_report(path: String,data: Variant) -> void:
	var f=FileAccess.open("res://docs/Feedback/"+path,FileAccess.WRITE)
	f.store_string(JSON.stringify(data,"\t"))
