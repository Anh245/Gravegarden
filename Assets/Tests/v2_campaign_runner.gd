extends RefCounted
## Plays through public game actions with earned currency and the normal 2x control.
var g: Node
var actions: Array=[]
var plan: Array=[]
var last_tick: int=0
var last_collect: int=0
var last_phase: String=""
var last_wave: int=-1
var prep_started: int=0
var done: bool=false
func start(game: Node) -> void:
	g=game
	plan=JSON.parse_string(FileAccess.get_file_as_string("res://docs/Feedback/full-playtest.json"))[-1].snapshot.buildings
	g.ui._new_game()
	g.skip_tutorial()
	g.set_speed(2.0)
	g.set_process(true)
	g.get_tree().process_frame.connect(tick)
func record(kind: String,extra: Dictionary={}) -> void:
	var row={"event":kind,"time":g.chapter_seconds,"state":g.get_run_status()}
	row.merge(extra)
	actions.append(row)
	var f=FileAccess.open("res://docs/Feedback/V2/campaign-playtest.json",FileAccess.WRITE)
	f.store_string(JSON.stringify(actions,"\t"))
func tick() -> void:
	if done or not is_instance_valid(g):
		return
	var now=Time.get_ticks_msec()
	if now-last_tick<250:
		return
	last_tick=now
	if g.phase!=last_phase or g.completed_wave!=last_wave:
		last_phase=g.phase
		last_wave=g.completed_wave
		prep_started=now
		record("phase")
		if g.phase=="victory":
			g.get_tree().root.get_texture().get_image().save_png("res://docs/Feedback/V2/victory-run.png")
	if g.phase=="defeat":
		done=true
		record("failed",{"snapshot":g.make_snapshot()})
		return
	if g.completed_wave>=8 and g.phase=="prep":
		g.paused=true
		done=true
		record("passed",{"snapshot":g.make_snapshot()})
		return
	if g.phase=="victory":
		if now-prep_started>1000:
			g.ui._modal_primary()
			record("keep_growing")
		return
	if not g.can_act():
		return
	if now-last_collect>1500:
		last_collect=now
		g.collect_all()
	# Secure the first choke before expanding production.
	var first=g.buildings.filter(func(b):return b.active and b.definition.id=="bone_lily" and b.cell.y>=0 and b.cell.y<48)
	if not first.is_empty() and first[0].level<2 and g.currency>=first[0].definition.upgrade_cost(first[0].level):
		g.upgrade_building(first[0].slot_id)
		record("upgrade",{"slot":first[0].slot_id})
		return
	for item in plan:
		var cell=Vector2i(item.cell[0],item.cell[1])
		if (cell.y<0 or cell.y>=48) and not g.expanded:
			continue
		if not g.is_unlocked(item.id):
			continue
		var existing=g.buildings.filter(func(b):return b.active and b.definition.id==item.id and b.cell==cell)
		if not existing.is_empty():
			continue
		if g.currency<g.purchase_cost(item.id):
			continue
		if not g.placement_reason(item.id,cell).is_empty():
			record("invalid_plan",{"id":item.id,"cell":str(cell),"reason":g.placement_reason(item.id,cell)})
			done=true
			g.paused=true
			return
		if g.place_building(item.id,cell):
			g.ui._close_building()
			record("build",{"id":item.id,"cell":str(cell)})
			return
	for b in g.buildings:
		if not b.active or b.definition.id not in ["bone_lily","ossuary_xbow"]:
			continue
		var desired=3 if b.definition.id=="bone_lily" else 2
		if b.level<desired and g.currency>=b.definition.upgrade_cost(b.level):
			g.upgrade_building(b.slot_id)
			record("upgrade",{"slot":b.slot_id,"level":b.level})
			return
	if g.phase=="prep" and now-prep_started>2500:
		g.start_wave()
