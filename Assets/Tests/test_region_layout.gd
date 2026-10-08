@tool
extends McpTestSuite
func suite_name() -> String:
	return "region_layout"
func test_trace_and_grid() -> void:
	var shapes: Dictionary=JSON.parse_string('{"VioletUpper":[[0,540],[1000,540],[2100,540],[2900,540],[3170,570],[3400,810],[3690,1110],[4050,1480],[4300,1536],[4730,1536]],"VioletLower":[[0,2390],[1050,2390],[2200,2390],[2920,2390],[3170,2310],[3470,2070],[3750,1800],[4050,1536],[4300,1536],[4730,1536]],"GoldenUpper":[[5504,535],[4700,535],[3920,535],[3720,640],[3530,900],[3280,1220],[2990,1500],[2750,1536],[1700,1536]],"GoldenLower":[[5504,2400],[4630,2400],[3890,2400],[3660,2300],[3410,2010],[3140,1730],[2960,1560],[2750,1536],[1700,1536]],"MainUpper":[[5504,538],[3924,538],[3789,578],[3682,658],[3548,806],[3359,994],[3171,1209],[3010,1357],[2956,1438],[2876,1497],[2768,1532],[2150,1532],[1747,1532]],"MainLower":[[5504,2392],[3951,2392],[3816,2365],[3709,2325],[3628,2258],[3494,2123],[3333,1962],[3198,1828],[3064,1693],[2956,1613],[2876,1559],[2768,1532],[2150,1532],[1747,1532]]}')
	var sources={"Main":"res://assets/spritecook/world/gravegarden_ground.png","Violet":"res://assets/spritecook/world/violet_ground.png","Golden":"res://assets/spritecook/world/golden_ground.png"}
	var curves: Dictionary={}
	var routes: Dictionary={}
	for key in shapes:
		var image=Image.load_from_file(sources["Main" if key.begins_with("Main") else ("Violet" if key.begins_with("Violet") else "Golden")])
		var rough=Curve2D.new()
		for pair in shapes[key]:
			rough.add_point(Vector2(pair[0],pair[1]))
		var traced=[]
		var length=rough.get_baked_length()
		for distance in range(0,int(length)+1,96):
			var point=rough.sample_baked(minf(distance,length))
			var tangent=(rough.sample_baked(minf(distance+32,length))-rough.sample_baked(maxf(distance-32,0))).normalized()
			var normal=Vector2(-tangent.y,tangent.x)
			var offsets=[]
			for step in range(-190,191,8):
				var sample=point+normal*step
				if sample.x<2 or sample.y<2 or sample.x>=image.get_width()-2 or sample.y>=image.get_height()-2:
					continue
				var color=image.get_pixelv(Vector2i(sample))
				if color.r>0.16 and color.r>color.g*1.12 and color.b>color.g*0.8 and color.b<color.g*1.2:
					offsets.append(float(step))
			if offsets.size()>4:
				point+=normal*(offsets.reduce(func(total,item):return total+item,0.0)/offsets.size())
			traced.append(point)
		traced.append(Vector2(shapes[key][-1][0],shapes[key][-1][1]))
		var smooth=[]
		for index in range(traced.size()):
			var sum: Vector2=Vector2.ZERO
			var count: int=0
			for neighbor in range(maxi(0,index-2),mini(traced.size(),index+3)):
				sum+=traced[neighbor]
				count+=1
			smooth.append(sum/count if index>0 and index<traced.size()-1 else traced[index])
		var curve=Curve2D.new()
		var points=[]
		for index in range(smooth.size()):
			var before: Vector2=smooth[maxi(0,index-1)]
			var after: Vector2=smooth[mini(smooth.size()-1,index+1)]
			var tangent: Vector2=(after-before)/6.0
			var point: Vector2=smooth[index]
			curve.add_point(point,-tangent,tangent)
			points.append({"position":{"x":point.x,"y":point.y},"in":{"x":-tangent.x,"y":-tangent.y},"out":{"x":tangent.x,"y":tangent.y}})
		curves[key]=curve
		routes[key]=points
	var scene=EditorInterface.get_edited_scene_root()
	var cells=[]
	for region in range(3):
		var image=Image.load_from_file(["res://assets/spritecook/world/gravegarden_ground.png","res://assets/spritecook/world/violet_ground.png","res://assets/spritecook/world/golden_ground.png"][region])
		var offset: int=[0,-2944,2944][region]
		var names: Array=[["MainUpper","MainLower"],["VioletUpper","VioletLower"],["GoldenUpper","GoldenLower"]][region]
		for y in range(48):
			for x in range(86):
				var point=Vector2(x*64+32,y*64+32)
				var valid=true
				for route in names:
					if curves[route].get_closest_point(point).distance_to(point)<150.0:
						valid=false
						break
				if not valid:
					continue
				for corner in [Vector2.ZERO,Vector2(-22,-22),Vector2(22,-22),Vector2(-22,22),Vector2(22,22)]:
					var color=image.get_pixelv(Vector2i(point+corner))
					if color.b>color.g*1.35 and color.r<color.g*0.8:
						valid=false
						break
				if valid:
					var encoded=(y+int(offset/64.0)+46)*86+x
					if not cells.has(encoded):
						cells.append(encoded)
	var output={"routes":routes,"cells":cells,"offsets":{"Violet":-2944,"Golden":2944}}
	var file=FileAccess.open("res://docs/Feedback/region-layout.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(output,"\t"))
	assert_gt(cells.size(),1600,"Expanded placement grid contains broad grass areas")
