using UnityEngine;
using System.Collections.Generic;

//this will be responsable for downloading the map and players when the game starts
//currently just downloads players, as that's all there is right now

//also, unloads players and map on disconnect
public class MapManager : BlockerObject
{
	//public GameObject mapToUse;
	//public GameObject loadedMap;
	
	void OnServerInitialized()
	{
		menuManager.bgMap.AddComponent<WorldBounds>();
	}
	
	
    void OnPlayerConnected (NetworkPlayer player)
    {
		//send player characters to load
        for (var i = 0; i < playerManager.players.Count; i++)
        {
            NetworkPlayer computer = (playerManager.players[i].GetComponent("NetPlayer") as NetPlayer).networkPlayer;
            int playerNumber = (playerManager.players[i].GetComponent("NetPlayer") as NetPlayer).localPlayerNumber;
            networkView.RPC("AddNewPlayer", player, computer, playerNumber);
        }
		if(menuManager.gameState == MenuManager.GameState.Game)
		{
			networkView.RPC("LoadMap", player, menuManager.bgMap.name);
			networkView.RPC("initializeGame", player);	
		}
		for(var i = 0; i < world.transform.FindChild("Bullets").childCount; i++)
		{
			networkView.RPC("spawnObject", player, world.transform.FindChild("Bullets").GetChild(i).position, world.transform.FindChild("Bullets").GetChild(i).rotation.eulerAngles, world.transform.FindChild("Bullets").GetChild(i).name, "testBullet", "World/Bullets");
			networkView.RPC ("setBulletVelocity", player, world.transform.FindChild("Bullets").GetChild(i).rigidbody.velocity, "World/Bullets/"+world.transform.FindChild("Bullets").GetChild(i).name);
			networkView.RPC ("setObjectGravity", player, world.transform.FindChild("Bullets").GetChild(i).GetComponent<ObjectStats>().grav, "World/Bullets/"+world.transform.FindChild("Bullets").GetChild(i).name);		
		}
		
		
		
    }

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        //remove the map
		RemoveMap ();
		
        //remove all characters
        playerManager.players = new List<NetPlayer>();
        playerManager.localPlayers = new List<NetPlayer>();
        Transform rootTeam = gameObject.transform.Find("RootTeam");
        for (int i = 0; i < rootTeam.GetChildCount(); i++)
        {
            GameObject toGo = rootTeam.GetChild(i).gameObject;
            Destroy(toGo);
        }
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        for (int i = 0; i < playerManager.players.Count; i++)
        {
            if (playerManager.players[i].networkPlayer == player)
            {
                networkView.RPC("RemovePlayer", RPCMode.Others, player, playerManager.players[i].localPlayerNumber);
                if (Network.peerType == NetworkPeerType.Server)
                {
                    playerManager.RemovePlayer(player, playerManager.players[i].localPlayerNumber);
                    i--;
                } 
            }
        }
    }
	
	
	
	[RPC]
	void initializeGame()
	{
		//load map
		LoadMap(menuManager.bgMap.name);
		//move all players to spawn
		if(Network.peerType == NetworkPeerType.Server)
		{
			foreach(NetPlayer player in playerManager.players)
			{
				respawnPlayer(player.name);
			}
		}
		//switch to player camera
		if(playerManager.localPlayers.Count > 0)
		{
			playerManager.setToLocalCameras();
			playerManager.UpdateCameraSplit();	
		}
		playerManager.RevealPlayers();
	}
	
	public void respawnPlayer(string name) //called on server, sets the players position in random area around spawn
	{
		Transform spawnArea = menuManager.bgMap.transform.FindChild("Spawn").transform;
		GameObject player = world.transform.FindChild("RootTeam/" + name).gameObject;
		Vector3 spawnLocation = spawnArea.transform.position;
		Quaternion spawnRotation = spawnArea.transform.rotation;
		
		int attempts = 0;
		while(attempts < 50)
		{
			//get random point in spawn area.
			spawnLocation = new Vector3(Random.Range(spawnArea.position.x - spawnArea.localScale.x/2 ,spawnArea.position.x + spawnArea.localScale.x/2), 
									Random.Range(spawnArea.position.y - spawnArea.localScale.y/2 ,spawnArea.position.y + spawnArea.localScale.y/2),  
									Random.Range(spawnArea.position.z - spawnArea.localScale.z/2 ,spawnArea.position.z + spawnArea.localScale.z/2));
			//check if putting the player there causes a collision
			if(Physics.OverlapSphere(spawnLocation, player.collider.bounds.max.y).Length <= 1)
			{
				break;
			}
			player.transform.rotation = spawnArea.transform.rotation;
			attempts++;
			
		}
		player.rigidbody.velocity = new Vector3();
		networkView.RPC("setPlayerPos", RPCMode.All, name, spawnLocation, spawnRotation);
		 
	}
	[RPC]
	void setPlayerPos(string name, Vector3 pos, Quaternion rot) //called on clients, copies players location from server
	{
		world.transform.FindChild("RootTeam/" + name).transform.position = pos;
		world.transform.FindChild("RootTeam/" + name).transform.rotation = rot;
	}
	
	
	[RPC]
	void LoadMap(string maptoLoad)
	{
		// instantiate the map on the local machine.
		//Ball spawning and some other junk
		maptoLoad = maptoLoad.Remove(maptoLoad.Length-7);
		Destroy (menuManager.bgMap);
		GameObject newMap = Instantiate(Resources.Load("Maps/" + maptoLoad), Vector3.zero, Quaternion.identity) as GameObject;
		newMap.AddComponent<WorldBounds>();
		menuManager.bgMap = newMap;
		menuManager.bgMap.GetComponent<GameManager>().ToggleAllCheckpoints(true);
		
		newMap.AddComponent<NetworkView>();
		newMap.GetComponent<GameManager>().init();
		newMap.networkView.RPC ("setCheckpoint", RPCMode.All, newMap.GetComponent<GameManager>().index);
	}
	
	// making this an rpc seemed very reasonable to me
	[RPC]
	void RemoveMap()
	{
		Destroy (menuManager.bgMap);
	}
}
