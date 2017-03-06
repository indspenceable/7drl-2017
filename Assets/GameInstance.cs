﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInstance : MonoBehaviour {

	private LevelMap[] levels;

	private MapTileComponent[][] tiles;
	private MapTileComponent GetTile(Coord c) {
		return tiles[c.x][c.y];
	}

	private int currentLevelNumber;
	private LevelMap CurrentLevel {
		get {
			return levels[currentLevelNumber];
		}
	}

	public Player player;
	private GameObject targettingReticle;

	private List<MonsterComponent> monsters = new List<MonsterComponent>();

	private GameManager.MapConfig mapConfig;

	public void Startup(GameManager.MapConfig mapConfig, GameManager.PrefabConfig prefabs) {
		// Save the map config for use with making Djikstra maps
		this.mapConfig = mapConfig;

		// Build the level maps
		levels = new LevelMap[mapConfig.totalNumberOfLevels];
		for (int i = 0; i < mapConfig.totalNumberOfLevels; i += 1) {
			levels[i] = new LevelMap(mapConfig);
		}


		// Now, actually build out the map gameobjects
		tiles = new MapTileComponent[mapConfig.width][];
		for ( int x = 0; x < mapConfig.width; x+=1) {
			tiles[x] = new MapTileComponent[mapConfig.height];
			for (int y = 0; y < mapConfig.height; y +=1) {
				tiles[x][y] = Instantiate(prefabs.tilePrefab).GetComponent<MapTileComponent>();
				tiles[x][y].SetCoords(x,y);
			}
		}

		// Set their terrain
		for ( int x = 0; x < mapConfig.width; x+=1) {
			for (int y = 0; y < mapConfig.height; y +=1) {
				tiles[x][y].SetTerrain(CurrentLevel.GetAt(new Coord(x,y)));
			}
		}

		// Set up the player
		player = Instantiate(prefabs.playerPrefab).GetComponent<Player>();
		player.SetCoords(new Coord(1,1));

		// Aaaand the reticle
		targettingReticle = Instantiate(prefabs.reticle);
		targettingReticle.SetActive(false);

		// Fill in the monsters for this level
		for (int i = 0; i < 1; i+=1) {
			Coord c = new Coord(0,0);

			while (CurrentLevel.GetAt(c).blocked || !CurrentLevel.GetAt(c).passable || c.Equals(player.pos)) {
				c = new Coord(Random.Range(0, 10), Random.Range(0, 10));
			}
			Monster monsterType = prefabs.monsterdefs[Random.Range(0, prefabs.monsterdefs.Length)];
			MonsterComponent mc = Instantiate(prefabs.monster).GetComponent<MonsterComponent>();
			mc.Setup(monsterType, c);
			monsters.Add(mc);
		}
//
		// Now that we have all this junk established, we can begin listening to input.
		StartCoroutine(ListenForPlayerInput());
	}


	private IEnumerator ListenForPlayerInput() {
		yield return null;
		if (Input.GetKeyDown(KeyCode.A)) {
			yield return AttemptMove(new Coord(-1, 0) + player.pos);
		} else if (Input.GetKeyDown(KeyCode.D)) {
			yield return AttemptMove(new Coord(1, 0) + player.pos);
		} else if (Input.GetKeyDown(KeyCode.S)) {
			yield return AttemptMove(new Coord(0, -1) + player.pos);
		} else if (Input.GetKeyDown(KeyCode.W)) {
			yield return AttemptMove(new Coord(0, 1) + player.pos);
		} else if (Input.GetKeyDown(KeyCode.I)) { 
			yield return player.GetItem(0).BeginActivation(this, TakeAllMonstersTurn(), ListenForPlayerInput());
		} else if (Input.GetKeyDown(KeyCode.J)) { 
			yield return player.GetItem(1).BeginActivation(this, TakeAllMonstersTurn(), ListenForPlayerInput());
		} else if (Input.GetKeyDown(KeyCode.K)) {
			yield return player.GetItem(2).BeginActivation(this, TakeAllMonstersTurn(), ListenForPlayerInput());
		} else if (Input.GetKeyDown(KeyCode.L)) {
			yield return player.GetItem(3).BeginActivation(this, TakeAllMonstersTurn(), ListenForPlayerInput());
		} else if (Input.GetKeyDown(KeyCode.Period)) {
			yield return TakeAllMonstersTurn();
		} else {
			yield return ListenForPlayerInput();
		}
	}

	private IEnumerator AttemptMove(Coord dest) {
//		Coord dest = player.pos + new Coord(dx, dy);
		if (GetTile(dest).interactable) {
		} else if (GetTile(dest).passable) {
			yield return SlowMove(player.gameObject, dest, GameManager.StandardDelay);
			player.SetCoords(dest);
			yield return TakeAllMonstersTurn();
		} else {
			yield return ListenForPlayerInput();
		}
	}
	/* ------------------------------- *
	 * Monster related stuff goes here *
 	 * ------------------------------- */
	public Entity GetEntityAt(Coord a) {
		if (a.Equals(player.pos)) {
			return player;
		}
		foreach(MonsterComponent m in monsters) {
			if (m.pos.Equals(a)) {
				return m;
			}
		}
		return null;
	}

	private int Comp(Coord a, Coord b) {
		int ad = a.DistanceTo(player.pos);
		int bd = b.DistanceTo(player.pos);
		if (ad < bd) {
			return -1;
		} else if (bd < ad) {
			return 1;
		} else {
			return 0;
		}
	}
	// Find a path from the monster to the player
	public List<Coord> AStarMonsterToPlayer(MonsterComponent m) {
		List<Coord> openList = new List<Coord>{ m.pos };
		Dictionary<Coord, List<Coord>> closedList = new Dictionary<Coord, List<Coord>> {{ m.pos, new List<Coord>() }};
		// 
		while (openList.Count > 0) {
			openList.Sort(Comp);
			Coord current = openList[0];
			openList.RemoveAt(0);
			List<Coord> currentPath = closedList[current];
		
			if (current.Equals(player.pos)) {
				return currentPath;
			}

			Coord[] offsets = new Coord[] {
				new Coord(-1, 0),
				new Coord( 1, 0),
				new Coord( 0, 1),
				new Coord( 0,-1)
			};
			foreach (Coord offset in offsets) {
				Coord c = current + offset;
				if(!closedList.ContainsKey(c) && CurrentLevel.GetAt(c).passable) {
					List<Coord> newPath = new List<Coord>(currentPath);
					newPath.Add(c);
					closedList[c] = newPath;
					openList.Add(c);
				}
			}
		}

		return null;
	}

	public void ApplyLabels(DjikstraMap map) {
		for ( int x = 0; x < mapConfig.width; x+=1) {
			for (int y = 0; y < mapConfig.height; y +=1) {
				tiles[x][y].name = map.Value(x,y).ToString();
			}
		}
	}

	private IEnumerator TakeMonsterTurn(MonsterComponent m) {
		return m.ExecuteStrategy(this);
	}

	public bool Passable(Coord c) {
		return CurrentLevel.GetAt(c).passable;
	}
	public DjikstraMap BuildPlayerMap() {
		DjikstraMap map = new DjikstraMap(mapConfig.width, mapConfig.height);
		map.SetGoal(player.pos);
		map.Calculate(Passable);
		return map;
	}

	public IEnumerator TakeAllMonstersTurn() {
		foreach( MonsterComponent m in monsters) {
			yield return TakeMonsterTurn(m);
		}

		yield return TakePassivesTurn();
	}

	private IEnumerator TakePassivesTurn() {
		yield return ListenForPlayerInput();
	}

	public IEnumerator SlowMove(GameObject go, Coord target, float time) {
		float dt = 0;
		Vector3 startPosition = go.transform.position;
		Vector3 endPosition = target.toVec();
		while (dt < time) {
			yield return null;
			dt += Time.deltaTime;
			go.transform.position = Vector3.Lerp(startPosition, endPosition, dt/time);
		}
		go.transform.position = endPosition;
	}

	public delegate IEnumerator TargettedAction(GameInstance instance, Coord c, IEnumerator success);
	public IEnumerator SelectTarget(KeyCode selectKeyCode, TargettedAction callback, IEnumerator success, IEnumerator cancel) {
		yield return null;
		targettingReticle.transform.position = player.transform.position;
		// TODO these should probably be provided.
		int x = (int)player.transform.position.x;
		int y = (int)player.transform.position.y;
		targettingReticle.SetActive(true);
		while (true) {
			if (Input.GetKeyDown(KeyCode.W)) {
				y += 1;
				yield return SlowMove(targettingReticle, new Coord(x, y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(KeyCode.A)) {
				x -= 1;
				yield return SlowMove(targettingReticle, new Coord(x, y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(KeyCode.S)) {
				y -= 1;
				yield return SlowMove(targettingReticle, new Coord(x, y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(KeyCode.D)) {
				x += 1;
				yield return SlowMove(targettingReticle, new Coord(x, y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(selectKeyCode)) {
				break;
			}
			yield return null;
		}
		targettingReticle.SetActive(false);
		yield return callback(this, new Coord(x, y), success);
	}

	public IEnumerator SelectCardinalDirection(KeyCode selectKeyCode, TargettedAction callback, IEnumerator success, IEnumerator cancel) {
		yield return null;
		targettingReticle.transform.position = player.transform.position;
		// TODO these should probably be provided.
		int oX = (int)player.transform.position.x;
		int oY = (int)player.transform.position.y;
		targettingReticle.SetActive(true);
		int x = 0;
		int y = -1;
		while (true) {
			if (Input.GetKeyDown(KeyCode.W)) {
				x = 0;
				y = 1;
				yield return SlowMove(targettingReticle, new Coord(oX+x, oY+y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(KeyCode.A)) {
				x = -1;
				y = 0;
				yield return SlowMove(targettingReticle, new Coord(oX+x, oY+y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(KeyCode.S)) {
				x = 0;
				y = -1;
				yield return SlowMove(targettingReticle, new Coord(oX+x, oY+y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(KeyCode.D)) {
				x = 1;
				y = 0;
				yield return SlowMove(targettingReticle, new Coord(oX+x, oY+y), GameManager.StandardDelay);
			} else if (Input.GetKeyDown(selectKeyCode)) {
				break;
			}
			yield return null;
		}
		targettingReticle.SetActive(false);
		yield return callback(this, new Coord(x, y), success);
	}

	private IEnumerator HookInDirection(Coord offset) {
		Coord c = player.pos;
		while (!CurrentLevel.GetAt(c).blocked) {
			c = c + offset;
		}
		c = c - offset;
		yield return AttemptMove(c);
	}
}
