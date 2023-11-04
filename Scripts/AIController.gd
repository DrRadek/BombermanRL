extends AIController3D

var move_x : float = 0
var move_y : float = 0
var place_bomb : int = 0

#@onready var game_manager get_tree().current_scene
var game_manager

#var move_action:float = 0

func init(player: Node3D):
	_player = player
	game_manager = player.gameManager
	
func get_obs() -> Dictionary:
	var player = []
	var players = []
	

	var id = _player.playerIndex
	
	for i in range(game_manager.maxPlayerCount):
		if i == id:
			var pos :Vector3 = game_manager.to_local(_player.position) / 10
			var pos_change :Vector3 = _player.posChange
			
			player = [_player.GetID() , _player.Lives,int(_player.IsInvulnerable),_player.InvulnerabilityTime ,pos.x,pos.z, pos_change.x, pos_change.z]
			continue
		elif i >= game_manager.playerCount:
			players.append_array([0,0,0,0,0,0,0,0])
		else:
			var player_i = game_manager.players[i]
			var pos =  game_manager.to_local(player_i.position) / 10
			var pos_change :Vector3 = player_i.posChange
			players.append_array([player_i.GetID(),player_i.Lives, int(player_i.IsInvulnerable),player_i.InvulnerabilityTime ,pos.x,pos.z, pos_change.x, pos_change.z])
		
			
			
		
		#print(game_manager.players[i])
	
	#print(game_manager.mapSensor)
	#print(player)
	#print(players)
	
	"""return {
		"obs":{
			"map":game_manager.mapSensor,
			"player":player, 
			"players":players
		}
	}"""
	
	var test = []
	test.append_array(game_manager.mapSensor)
	test.append_array(game_manager.playerMapSensor)
	
	var obs = []
	
	var arr_size = len(test)/2
	for i in range(arr_size):
		obs.append(test[i])
		obs.append(test[i] + arr_size)
	
	
	
	#obs.append_array(game_manager.mapSensor)
	#obs.append_array(game_manager.playerMapSensor)
	obs.append_array(player)
	obs.append_array(players)
	#print(obs)
	#print(len(obs))
	
	return {"obs": obs}
	#var obs = [1,2,3,4]
	#return {"obs":obs}
	#return {"obs":game_manager.mapSensor, "player":player, "players":players}
	#return {"obs":[5]}
	
"""func get_obs_space():
	# may need overriding if the obs space is complex
	var obs = get_obs()
	#print(obs)
	#print(obs["player"])
	#print(obs["players"])
	#print(len(obs["player"]))
	#print(len(obs["players"]))
	
	
	return {
		"obs": {
			"size": [len(obs["obs"]),len(obs["obs"]),2],
			"space": "box"
		},
		"player":{
			"size": [len(obs["player"])],
			"space": "box"
		},
		"players": {
			"size": [len(obs["players"]),len(obs["player"])],
			"space": "box"
		},
	}"""

func get_reward() -> float:
	return reward
	
func get_action_space() -> Dictionary:
	"""return {
		"move_x_pos" : {
			"size": 2,
			"action_type": "discrete"
		},
		"move_x_neg" : {
			"size": 2,
			"action_type": "discrete"
		},
		"move_y_pos" : {
			"size": 2,
			"action_type": "discrete"
		},
		"move_y_neg" : {
			"size": 2,
			"action_type": "discrete"
		},
		"place_bomb" : {
			"size": 2,
			"action_type": "discrete"
		}
	}"""
	"""return {
		"move_action" : {
			"size": 1,
			"action_type": "continuous"
		},
	}"""
	
	return {
		"move" : {
			"size": 2,
			"action_type": "continuous"
		},
		"place_bomb" : {
			"size": 1,
			"action_type": "continuous"
		}
	}
	
func set_action(action) -> void:
	#move_action = clamp(action["move_action"][0], -1.0, 1.0)
	#return
	
	
	#print(action)
	#print(clamp(action["move"][0],0,1))
	#print(action["move_x_pos"])
	#move_x = action["move_x_pos"] - action["move_x_neg"] #- 1 #clamp(action["move_x"],0,10)-1
	#move_y = action["move_y_pos"] - action["move_y_neg"] #clamp(action["move_y"],0,10)-1
	#place_bomb = action["place_bomb"] #clamp(action["place_bomb"],0,1)
	
	move_x = clamp(action["move"][0], -1.0, 1.0)
	move_y = clamp(action["move"][1], -1.0, 1.0)
	
	#print(move_x)
	#print(move_y)
	#place_bomb = action["place_bomb"][0]
	#if move_x > 0.5:
	#	move_x = 1
	#elif move_x < -0.5:
	#	move_x = -1
	#else:
	#	move_x = 0
	#	
	#if move_y > 0.5:
	#	move_y = 1
	#elif move_y < -0.5:
	#	move_y = -1
	#else:
	#	move_y = 0
	place_bomb = clamp(action["place_bomb"][0],0, 1) > 0
	#if clamp(action["place_bomb"][0],0, 1) > 0:
	#	place_bomb = 1
	#else:
	#	place_bomb = 0
	#print(move_x)
	#print(move_y)
	
