using UnityEngine;
using System.Collections;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public enum Side
{
	Front, Left, Right, Back, Top, Bottom, Null
}

public class Wall
{
	public string texture;
	public Side side;
	public int link;
}

public class RoomObject
{
	public string mesh;
	public string material;
	public Vector3 position;
	public Vector3 rotation;
	public Vector3 scale;

	public string sound;

	public Side side;
	public int link;
	public bool rayTrace;
	public bool isStatic;
	public int index;
}

public class Room
{
	public int id;
	public string name;
	public List<Wall> walls = new List<Wall>();
	public List<RoomObject> objects = new List<RoomObject>();

	public List<GameObject> gameObjects = new List<GameObject> ();

	public Room(int id)
	{
		this.id = id;

	}
}

public class OnStartup : MonoBehaviour 
{
	List<Room> rooms = new List<Room>();
	public GameObject selector; //selected in the editor
	
	Room currentRoom = null;
	Room previousRoom = null;
	bool crossRooms = true;

	void LoadXML()
	{
		rooms = new List<Room> ();

		XmlDocument doc = new XmlDocument ();
		doc.Load (Application.dataPath + "\\layout.xml");
		
		foreach (XmlNode roomNode in doc["root"])
		{
			Room room = new Room(int.Parse(roomNode["id"].InnerText));
			
			int index = 0;
			
			foreach (XmlNode wallNode in roomNode["walls"])
			{
				Wall wall = new Wall();
				if (wallNode["side"] != null)
				{
					switch (wallNode["side"].InnerText)
					{
					case "left":
						wall.side = Side.Left;
						break;
					case "right":
						wall.side = Side.Right;
						break;
					case "front":
						wall.side = Side.Front;
						break;
					case "back":
						wall.side = Side.Back;
						break;
					case "top":
						wall.side = Side.Top;
						break;
					case "bottom":
						wall.side = Side.Bottom;
						break;
					}
				}
				else
				{
					wall.side = Side.Top;
				}
				if (wallNode["link"] != null)
					wall.link = int.Parse(wallNode["link"].InnerText);
				else
					wall.link = -1;
				
				wall.texture = wallNode["texture"].InnerText;
				
				foreach (XmlNode possibleGameObjectNode in wallNode)
				{
					if (possibleGameObjectNode.Name == "object")
					{
						RoomObject roomObject = new RoomObject();
						roomObject.mesh = possibleGameObjectNode["mesh"].InnerText;

						roomObject.material = null;

						if( possibleGameObjectNode["material"] != null)
						{
							roomObject.material = possibleGameObjectNode["material"].InnerText;
						}

						if (possibleGameObjectNode["sound"] != null)
						{
							roomObject.sound = possibleGameObjectNode["sound"].InnerText;							
						}
						
						var c = possibleGameObjectNode["pos"].InnerText.Split(',');
						roomObject.position = new Vector3(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]));
						
						c = possibleGameObjectNode["rot"].InnerText.Split(',');
						roomObject.rotation = new Vector3(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]));
						
						c = possibleGameObjectNode["sca"].InnerText.Split(',');
						roomObject.scale = new Vector3(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]));

						if (possibleGameObjectNode["link"] != null)
							roomObject.link = int.Parse(possibleGameObjectNode["link"].InnerText);

						roomObject.side = wall.side;

						roomObject.rayTrace = true;
						
						roomObject.isStatic = true;
						
						roomObject.index = index++;
						
						room.objects.Add (roomObject);
					}
				}
				
				room.walls.Add (wall);
			}
			
			if (roomNode["objects"] != null)
			{
				foreach (XmlNode objectNode in roomNode["objects"])
				{
					RoomObject roomObject = new RoomObject();
					roomObject.mesh = objectNode["mesh"].InnerText;

					if( objectNode["material"] != null)
					{
						roomObject.material = objectNode["material"].InnerText;
					}
					
					if (objectNode["sound"] != null)
					{
						roomObject.sound = objectNode["sound"].InnerText;							
					}
					
					var c = objectNode["pos"].InnerText.Split(',');
					roomObject.position = new Vector3(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]));
					
					c = objectNode["rot"].InnerText.Split(',');
					roomObject.rotation = new Vector3(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]));
					
					c = objectNode["sca"].InnerText.Split(',');
					roomObject.scale = new Vector3(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]));
					
					roomObject.link = -1;
					roomObject.side = Side.Null;

					if (objectNode["static"] != null)
					{
						roomObject.isStatic = objectNode["static"].InnerText == "true" ? true : false;
					}
					else
					{
						roomObject.isStatic = true;
					}

					roomObject.rayTrace = false;
					
					roomObject.index = index++;
					
					room.objects.Add (roomObject);
				}
			}
			
			rooms.Add(room);
		}
	}

	void Start ()
	{
		Screen.showCursor = false;
		//Screen.lockCursor = true;

		var files = System.IO.Directory.GetFiles (Application.dataPath);

		LoadXML ();

		var firstRoom = rooms.Find (x => x.id == 1);
		SetupRoom (firstRoom, new Vector3(0,0,0), new Vector3(0,0,1));
	}

	GameObject CreateMeshFromFile(string str)
	{
		return (GameObject)Resources.Load (str, typeof(GameObject));
	}

	Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

	Texture2D CreateTextureFromFile(string str)
	{
		if (textures.ContainsKey (str))
		{
			return textures [str];
		}
		else
		{
			WWW www = new WWW ("file://" + Application.dataPath + "\\" + str);

			while (!www.isDone) 
			{
				if (!string.IsNullOrEmpty (www.error))
				{
					throw new UnityException ("Invalid image");
				}
			}

			Texture2D tex = new Texture2D (100, 100, TextureFormat.DXT1, false);

			www.LoadImageIntoTexture (tex);

			textures.Add (str, tex);

		}

		return textures[str];
	}

	void SetupNextRoom(RoomStartUpVariables obj)//GameObject wall, Room newRoom)
	{
		var bottom = currentRoom.gameObjects.Find (x => x.name == "Bottom");

		var vector = obj.wall.transform.position - bottom.transform.position;
		vector.y = 0;

		crossRooms = false;

		SetupRoom (obj.newRoom, bottom.transform.position + (vector * 2), vector / ( (selector.transform.localScale/2).z * bigScale.x)	);

		obj.wall.SetActive (false);
		currentRoom.gameObjects.Find (x => x.name == "Back").SetActive (false);
	}

	Vector3 bigScale = new Vector3(1.6f, 1.6f, 1.6f);

	void SetupRoom(Room room, Vector3 position, Vector3 vector)
	{
		if (currentRoom != null)
			previousRoom = currentRoom;
		currentRoom = room;

		GameObject lightObject = Instantiate((GameObject)Resources.Load ("Light", typeof(GameObject)), position + new Vector3(0, 6.2f, 0), Quaternion.identity) as GameObject;
		room.gameObjects.Add (lightObject);
		lightObject.name = "Light";

		for (int i=0; i < 6; i++)
		{
			//Half scale dependant on half of bigscale * transform
			var halfScale = new Vector3(bigScale.x * selector.transform.localScale.x, bigScale.y * selector.transform.localScale.y, bigScale.z * selector.transform.localScale.z) / 2;
			Vector3 p = new Vector3(0,0,0);
			Vector3 q= new Vector3();
			Texture2D texture = new Texture2D(100,100);

			Side side = Side.Front;
			var wallHeight = 4.5f;
			Vector3 scale = new Vector3(bigScale.x, bigScale.y, 0.9f);

			switch (i)
			{
			case 0: //Right
				p = new Vector3(vector.z, vector.y, -vector.x) * halfScale.z;
				p.y = wallHeight;
				q = new Vector3(-90,0,-90);
				side = Side.Right;
				break;
			case 1: //Left
				p = new Vector3(-vector.z, vector.y, vector.x) * halfScale.z;
				p.y = wallHeight;
				q = new Vector3(-90,0,90);
				side = Side.Left;
				break;
			case 2: //Front
				p = vector * halfScale.z;
				p.y = wallHeight;
				q = new Vector3(90,0,180);
				side = Side.Front; 
				break;
			case 3: //Back
				p = -vector * halfScale.z;
				p.y = wallHeight;
				q = new Vector3(-90,0,0);
				side = Side.Back;
				break;
			case 4: //Top
				p = new Vector3(0, 10 * scale.z, 0);
				q = new Vector3(0,180,0);
				side = Side.Top;
				scale = bigScale;
				break;
			case 5: //Bottom
				p = new Vector3(0, 0, 0);
				q = new Vector3(0,0,0);
				scale = bigScale;
				side = Side.Bottom;
				break;
			}

			if (p == new Vector3(halfScale.x, wallHeight, 0))
			{
				q = new Vector3(-90,0,-90);
			}
			if (p == new Vector3(-halfScale.x, wallHeight, 0))
			{
				q = new Vector3(-90,0,90);
			}
			if (p == new Vector3(0,wallHeight, halfScale.z))
			{
				q = new Vector3(-90,0,180);
			}
			if (p == new Vector3(0, wallHeight, -halfScale.z))
			{
				q = new Vector3(-90,0,0);
			}

			var possibleWall = room.walls.Find (x=>x.side == side);
			if (possibleWall != null)
			{
				texture = CreateTextureFromFile(possibleWall.texture);
			}

			GameObject go = Instantiate (selector, p + position, Quaternion.identity) as GameObject;

			
			if (i == 5 || i == 4)
			{
				go.renderer.material = (Material)Resources.Load("testmaterial");
				go.renderer.material.SetTexture(0, texture);
				go.renderer.material.SetTexture(1, CreateTextureFromFile("Right_illum.png"));
			}
			else
			{
				//go.renderer.material = (Material)Resources.Load ("test_material");
				go.renderer.material = (Material)Resources.Load ("wallmaterial");
				if (possibleWall != null)
				{
					texture = CreateTextureFromFile(possibleWall.texture);
					go.renderer.material.SetTexture(0, texture);
				}
				else
				{
					go.renderer.material.SetTexture(0, texture);
				}
				go.renderer.material.SetTexture(1, CreateTextureFromFile("Wall_illum.png"));
			}

			switch (i)
			{
				case 0: go.name = "Right"; break;
				case 1: go.name = "Left"; break;
				case 2: go.name = "Front"; break;
				case 3: go.name = "Back"; break;
				case 4: go.name = "Top"; break;
				case 5: go.name = "Bottom"; break;
			}

			go.renderer.material.mainTexture = texture;
			//go.transform.localScale = selector.transform.localScale;
			go.transform.localScale = new Vector3(selector.transform.localScale.x * scale.x, selector.transform.localScale.y * scale.y, selector.transform.localScale.z * scale.z);
			go.transform.localEulerAngles = q;
			go.layer = 9;
			go.renderer.enabled = true;

			currentRoom.gameObjects.Add (go);
		}

		foreach (var gameObject in room.objects)
		{
			var p = gameObject.position;
			var rotOffset = 0.0f;

			if (vector == new Vector3(1,0,0))
			{
				p = new Vector3(p.z, p.y, -p.x);
				rotOffset = 90;
			}
			else if (vector == new Vector3(-1,0,0))
			{
				p = new Vector3(-p.z, p.y, p.x);
				rotOffset = -90;
			}
			else if (vector == new Vector3(0,0,1))
			{
				p = new Vector3(p.x, p.y, p.z);
				rotOffset = 0;
			}
			else if (vector == new Vector3(0,0,-1))
			{
				p = new Vector3(-p.x, p.y, -p.z);
				rotOffset = 180;
			}

			GameObject go = Instantiate(CreateMeshFromFile(gameObject.mesh), position + p, Quaternion.identity) as GameObject;

			if (gameObject.isStatic)
				go.rigidbody.isKinematic = true;

			go.transform.localScale = gameObject.scale;
			go.transform.localEulerAngles = gameObject.rotation + new Vector3(0, rotOffset, 0);

			if (gameObject.sound != null)
			{
				go.AddComponent("AudioSource");
				go.audio.clip = (AudioClip)Resources.Load("Sounds/" + gameObject.sound);
				go.audio.loop = true;
			}

			if ( gameObject.material != null && gameObject.material != "")
			{
				go.renderer.material.mainTexture = CreateTextureFromFile(gameObject.material);
			}

			if (go.renderer != null)
			{
				go.renderer.material.shader = Shader.Find("Self-Illumin/Diffuse");
			}

			go.name = gameObject.index.ToString();

			currentRoom.gameObjects.Add (go);
		}
	}

	public GameObject mouse;

	public void UpdateMouse()
	{
		var targetLink = -1;
		Wall chosenWall = null;
		GameObject wallObj = null;

		RaycastHit hit;

		var ray = Camera.main.ViewportPointToRay (new Vector3 (0.5f, 0.5f, 0));

		if (Physics.Raycast (ray, out hit))
		{		
			if (hit.rigidbody != null && new string[]{"Left", "Right","Top","Bottom","Back","Front"}.Contains (hit.rigidbody.gameObject.name))
			{
				var name = hit.rigidbody.gameObject.name;
		
				switch (name) 
				{
					case "Left":
						chosenWall = currentRoom.walls.Find (x => x.side == Side.Left);
						break;
					case "Right":
						chosenWall = currentRoom.walls.Find (x => x.side == Side.Right);
						break;
					case "Top":
						chosenWall = currentRoom.walls.Find (x => x.side == Side.Top);
						break;
					case "Back":
						chosenWall = currentRoom.walls.Find (x => x.side == Side.Back);
						break;
					case "Front":
						chosenWall = currentRoom.walls.Find (x => x.side == Side.Front);
						break;
				}

				if (chosenWall != null && chosenWall.link != -1) 
				{
					targetLink = chosenWall.link;
					wallObj = hit.rigidbody.gameObject;
				}

			} 
			else 
			{
				int result;
				if (int.TryParse(hit.rigidbody.gameObject.name, out result))
				{
					var obj = currentRoom.objects.Find (x => x.index == int.Parse (hit.rigidbody.gameObject.name));
					if (obj != null)
					{
						if (obj.link != -1) 
						{
							targetLink = obj.link;
							wallObj = currentRoom.gameObjects.Find (x => x.name == obj.side.ToString());
						}
					}
				}
			}
			if (targetLink != -1) 
			{
				mouse.renderer.material.mainTexture = (Texture2D)Resources.Load("six-fingered-mouse-cursor");
			}

			else
			{
				mouse.renderer.material.mainTexture = (Texture2D)Resources.Load("normalmouse");
			}
		}
	}


	// Update is called once per frame
	void Update ()
	{

		foreach (GameObject obj in currentRoom.gameObjects) 
		{
			if( obj.audio != null )
			{
				if ((Camera.main.transform.position - obj.transform.position).magnitude < 8)
				{
					if(!obj.audio.isPlaying)
					{
						obj.audio.Play();
					}
				}
			}
		}


		if (Input.GetKeyDown (KeyCode.F5))
		{
			var c = this.currentRoom.id;


			var pos = currentRoom.gameObjects.Find (x=>x.name == "Bottom").transform.position;

			previousRoom.gameObjects.ForEach (x=>Destroy(x));
			previousRoom.gameObjects.Clear ();

			currentRoom.gameObjects.ForEach (x=>Destroy(x));
			currentRoom.gameObjects.Clear ();

			LoadXML ();

			this.SetupRoom(this.rooms.Find (x=>x.id == c), pos, new Vector3(0,0,1));
		}

		RaycastHit hit;
		
		var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f,0.5f,0));

		if (Physics.Raycast(ray, out hit))//, float.MaxValue, 11)) 
		{
			if (hit.rigidbody != null)
			{
				mouse.transform.position = Camera.main.transform.position + (ray.direction * (hit.distance - 0.5f));
				if (new string[]{"Left", "Right","Top","Bottom","Back","Front"}.Contains (hit.rigidbody.gameObject.name))
				{
					mouse.transform.rotation = hit.transform.rotation;
				}
				else
				{
					mouse.transform.LookAt (Camera.main.transform.position);
					mouse.transform.Rotate (new Vector3(-90,0,0));
				}
			}
		}

		UpdateMouse ();

		if (crossRooms) 
		{
			if (Input.GetMouseButtonDown (0)) 
			{
				if (hit.rigidbody != null)
				{
					var targetLink = -1;
					Wall chosenWall = null;
					GameObject wallObj = null;

					if (new string[]{"Left", "Right","Top","Bottom","Back","Front"}.Contains (hit.rigidbody.gameObject.name))
					{
						var name = hit.rigidbody.gameObject.name;

						switch (name) 
						{
						case "Left":
								chosenWall = currentRoom.walls.Find (x => x.side == Side.Left);
								break;
						case "Right":
								chosenWall = currentRoom.walls.Find (x => x.side == Side.Right);
								break;
						case "Top":
								chosenWall = currentRoom.walls.Find (x => x.side == Side.Top);
								break;
						case "Back":
								chosenWall = currentRoom.walls.Find (x => x.side == Side.Back);
								break;
						case "Front":
								chosenWall = currentRoom.walls.Find (x => x.side == Side.Front);
								break;
						}
						if (chosenWall != null && chosenWall.link != -1)
						{
							targetLink = chosenWall.link;
							wallObj = hit.rigidbody.gameObject;
						}
					}
					else
					{
						var obj = currentRoom.objects.Find (x=>x.index == int.Parse(hit.rigidbody.gameObject.name));
						if (obj.link != -1)
						{
							targetLink = obj.link;
							//chosenWall = currentRoom.walls.Find (x => x.side == obj.side);
							wallObj = currentRoom.gameObjects.Find (x=>x.name == obj.side.ToString ());
						}
					}

					if (targetLink != -1)
					{
						mouse.renderer.material.mainTexture = (Texture2D)Resources.Load("loading");


						SetupNextRoom(new RoomStartUpVariables(wallObj, rooms.Find (x => x.id == targetLink)));
						 //SetupNextRoom(wallObj, rooms.Find (x => x.id == targetLink));
					}
				}
			}
		}
		else 
		{
			float dist = (Camera.main.transform.position - currentRoom.gameObjects.Find (x=>x.name == "Bottom").transform.position).magnitude;

			var light = previousRoom.gameObjects.Find(x => x.name == "Light");
			if (light != null)
			{
				light.gameObject.light.intensity = 1 - (8.0f / dist);

				if ((dist < ((selector.transform.localScale.z * bigScale.x) / 2) - 0.5))
				{
					crossRooms = true;
					previousRoom.gameObjects.ForEach (x=>Destroy(x));
					previousRoom.gameObjects.Clear ();

					currentRoom.gameObjects.Find (x=>x.name == "Back").SetActive(true);
				}
			}
		}
	}

	class RoomStartUpVariables
	{
		public GameObject wall;
		public Room newRoom;

		public RoomStartUpVariables(GameObject obj, Room room)
		{
			this.wall = obj;
			this.newRoom = room;
		}
	}
}
