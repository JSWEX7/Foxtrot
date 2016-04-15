using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ClickToAddWindow : EditorWindow {

	//Version 1.0 of click to add
	//Initial Release
	//Adds prefab with the click of a mouse button to the scene view
	//Option to randomly rotate objects on each axis
	//Will mass place randomly based on radius of circle and number of prefabs to place.
	//Optional setting to set parent of prefab on instantiation
	//Parenting is mostly done to help keep things organized
	//There is also the option to align vertical axis with the normal of the target
	
	//Version 1.1 
	//Fixed small error that caused Unity to crash when CTA was left open and Unity was closed
	
	//Version 1.2
	//Added the ability to spawn multiple prefabs
	//Added "weights" to prefab to adjust the randomness of placement
	//Added limits to the rotation around each axis
	
	public UnityEngine.Object assetToPlace = null;
	public UnityEngine.Object assetParent = null;
	public bool autoParent = true;
	public UnityEngine.Object assetTarget = null;
	public bool asPrefab = true;
	
	public int numPrefab = 1;
	public int newNumPrefab = 1;
	
	
	public List<prefabToAdd> preFabList = new List<prefabToAdd>();
	public List<float> preFabWeights = new List<float>();
	
	public bool randomRotation = true;
	public bool rotateX = false;
	public bool rotateY = false;
	public bool rotateZ = false;
	float rotationX = 180f;
	float rotationY = 180f;
	float rotationZ = 180f;
	
	public bool placing = false;
	
	//bool randomRotate = true;
	bool alignToNormal = false;
	bool massPlace = false;
	int numberToPlace = 10;
	float radiusToPlace = 5.0f;
	
	Vector2 scrollPos;

	Ray ray;
	RaycastHit hit;

	[MenuItem ("Window/Click To Add")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(ClickToAddWindow));
	}

	
	void OnEnable()
	{
		hideFlags = HideFlags.HideAndDontSave;
	
		//subscribe to onSceneGUI
		SceneView.onSceneGUIDelegate += SceneGUI;
		
		//Initialize prefablist
		if(preFabList.Count == 0)
		{
			prefabToAdd tempPTA = new prefabToAdd();
			preFabList.Add (tempPTA);
		}

		//ensure that number of slots displayed matches list size
		newNumPrefab = preFabList.Count;
	}
	
	void OnDisable()
	{
		SceneView.onSceneGUIDelegate -= SceneGUI;
	}

	void OnGUI()
	{
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
					
		EditorGUILayout.Space();
		EditorGUILayout.Space();
				
		//	Check if list is full
		//	adds to list of prefabs
		// Will only occur when GUI repaints to avoid errors
		if(Event.current.type == EventType.Layout)
		{
			int lastIndex;
			lastIndex = preFabList.Count - 1;
			
			if(preFabList[lastIndex].prefab != null)
			{
				newNumPrefab++;
			}			
		}
		
		
		
		//dynamically add to prefab list
		for(int i = 0; i < newNumPrefab; i++)
		{
		
		prefabToAdd tempPTA = new prefabToAdd();
		
			if(preFabList.Count != newNumPrefab)
			{
				if(Event.current.type == EventType.Layout)
				{
					preFabList.Add (tempPTA);
				}
			}

			if(preFabList.Count == newNumPrefab)
			{
				
				string fieldLabel;
				
				int tempInt = i + 1;
				fieldLabel = "Prefab # " + tempInt;					
				
				preFabList[i].prefab = EditorGUILayout.ObjectField(fieldLabel, preFabList[i].prefab, typeof(GameObject),false) as GameObject;

				EditorGUI.indentLevel++;
				preFabList[i].weight = EditorGUILayout.Slider("Weight", preFabList[i].weight, 0f,1f);
				//preFabList[i].connected = EditorGUILayout.ToggleLeft("Connected Prefab", preFabList[i].connected);
				EditorGUI.indentLevel--;
				
			}
			
		}
		

		EditorGUILayout.Space();
		
		GUILayout.BeginVertical();
		
		if(GUILayout.Button("Clear Prefabs"))
		{
			for(int i = 0; i < preFabList.Count; i++)
			{
				int prefabCount;
				prefabCount = preFabList.Count - 2 - i;
				
				if(prefabCount >= 0)
				{
					preFabList[prefabCount].prefab = null;
				}
			}
		
				
		}
		 
		EditorGUILayout.LabelField("Weight Settings");
		
		GUILayout.BeginHorizontal();

		if(GUILayout.Button("Zero"))
		{
			ZeroWeights();
		}
		if(GUILayout.Button("Equal"))
		{
			EqualWeghts();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		
		if(GUILayout.Button("Save Custom"))
		{
			SaveCustomWeights();
		}
		if(GUILayout.Button("Use Custom"))
		{
			UseCustomWeights();
		}
		
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		
		//Remove extra rows from prefab lists
		if(Event.current.type == EventType.Repaint)
		{
			int lastIndex;
			lastIndex = preFabList.Count - 1;
			
			if(preFabList.Count > 1)
			{
				if(preFabList[lastIndex - 1].prefab == null && preFabList[lastIndex].prefab == null)
				{
					newNumPrefab--;
					preFabList.RemoveAt(lastIndex);
				}
			}
		}
		EditorGUILayout.Space();
		autoParent = EditorGUILayout.ToggleLeft("Set Parent to Target", autoParent);
		EditorGUILayout.Space();
		assetParent = EditorGUILayout.ObjectField("Parent (Optional)", assetParent, typeof(GameObject), true);
			
		EditorGUILayout.Space();
		asPrefab = EditorGUILayout.ToggleLeft("Instantiate as connected Prefab", asPrefab);
			
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Randomly Rotate Asset on:");
		EditorGUI.indentLevel++;
		rotateX = EditorGUILayout.BeginToggleGroup("X axis", rotateX);
		EditorGUI.indentLevel++;
		rotationX = EditorGUILayout.Slider("Rotation Limit", rotationX,0f,180f);
		EditorGUI.indentLevel--;
		EditorGUILayout.EndToggleGroup();
		rotateY = EditorGUILayout.BeginToggleGroup("Y axis", rotateY);
		EditorGUI.indentLevel++;
		rotationY = EditorGUILayout.Slider("Rotation Limit", rotationY,0f,180f);
		EditorGUI.indentLevel--;
		EditorGUILayout.EndToggleGroup();
		rotateZ = EditorGUILayout.BeginToggleGroup("Z axis", rotateZ);
		EditorGUI.indentLevel++;
		rotationZ = EditorGUILayout.Slider("Rotation Limit", rotationZ,0f,180f);
		EditorGUI.indentLevel--;
		EditorGUILayout.EndToggleGroup();
		EditorGUI.indentLevel--;
		
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Align Prefab to Target Normal");
		EditorGUI.indentLevel++;
		alignToNormal = EditorGUILayout.ToggleLeft("Rotate Y-axis to Normal", alignToNormal);
		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField("Option to Mass Place Prefabs");
		EditorGUI.indentLevel++;
		
		//Mass Place UI
		massPlace = EditorGUILayout.BeginToggleGroup("Mass Place", massPlace);
		
			EditorGUI.indentLevel++;
			//Radius in which to instantiate prefabs
			radiusToPlace = EditorGUILayout.Slider("Radius To Place In", radiusToPlace,1f,50f);
			
			//Adjust max number of placements portional to area
			int maxToPlace;
			maxToPlace = Mathf.RoundToInt(radiusToPlace * radiusToPlace) / 2;
			
			//Number of prefabs to instantiate
			numberToPlace = EditorGUILayout.IntSlider("Number To Place", numberToPlace,1,maxToPlace);
			EditorGUI.indentLevel--;
			
		EditorGUILayout.EndToggleGroup();
		EditorGUI.indentLevel--;
		
		if(!placing)
		{
			if(GUILayout.Button("Start Placing"))
			{
				placing = true;
			}
		}
		else
		{
			if(GUILayout.Button("Stop Placing"))
			{
				placing = false;
			}
		}
		
		EditorGUILayout.Space ();
		
		EditorGUILayout.EndScrollView();
		
	}
	
	void SceneGUI(SceneView sceneView)
	{
		if(placing)
		{
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		}
		else
		{
			HandleUtility.Repaint();
		}
		
		if(preFabList.Count != 0)
		{	
			if(placing && preFabList[0].prefab != null)
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
	
				if (Physics.Raycast(ray, out hit, 1000.0f)) 
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
					{
						//Check if mass placing 
						if(massPlace)
						{
							for(int i = 0; i < numberToPlace; i++)
							{
								Vector3 tempPos;
//								GasmeObject tempGO;
//								tempGO = hit.transform.gameObject;
								tempPos = GetMassPlacePosition(hit);
															
								//If position returned is valid
								if(tempPos != Vector3.zero)
								{
									GameObject placedGO;
									placedGO = PlaceAsset(tempPos, hit.transform.gameObject);
									
									//align prefab with normal of object hit by raycast
									if(alignToNormal)
									{
										AlignWithNormal(hit, placedGO);
									}
								}
							}
						}
						else
						{
							GameObject placedGO;
							placedGO = PlaceAsset(hit.point, hit.transform.gameObject);
							
							//align prefab with normal of object hit by raycast
							if(alignToNormal)
							{
								AlignWithNormal(hit, placedGO);
							}
						}
						
					}
				}
			}
		}
	}
			
			
	
	GameObject PlaceAsset( Vector3 spawnPos, GameObject hitObject)
	{
		assetToPlace = ChooseAsset();
		//Debug.Log(assetToPlace.name);
	
		GameObject tempAsset;
		float rotX;
		float rotY;
		float rotZ;
		
		//Get Random Rotatation values
		if(rotateX)
		{
			rotX = UnityEngine.Random.Range(-rotationX,rotationX);
		}
		else
		{
			rotX = 0f;
		}
		
		if(rotateY)
		{
			rotY = UnityEngine.Random.Range(-rotationY, rotationY);
		}
		else
		{
			rotY = 0f;
		}
		
		if(rotateZ)
		{
			rotZ = UnityEngine.Random.Range(-rotationZ, rotationZ);
		}
		else
		{
			rotZ = 0f;
		}

		//Instantiate Object
		if(asPrefab)
		{
			tempAsset = PrefabUtility.InstantiatePrefab(assetToPlace) as GameObject;
			tempAsset.transform.position = spawnPos;
			Undo.RegisterCreatedObjectUndo(tempAsset, "Created GO");
		}
		else
		{
			tempAsset = Instantiate((GameObject)assetToPlace,spawnPos,Quaternion.identity) as GameObject;
			tempAsset.name = assetToPlace.name;
			Undo.RegisterCreatedObjectUndo(tempAsset, "Created GO");
		}

		//Rotation Object
		Undo.RecordObject(tempAsset.transform, "Change Rotation");
		tempAsset.transform.Rotate(rotX, rotY, rotZ);

		//Set parent
		if(assetParent != null && !autoParent)
		{
			GameObject tempGO;
			tempGO = (GameObject) assetParent;
			Undo.SetTransformParent(tempAsset.transform, tempGO.transform, "Set Parent to User Choice");

			tempAsset.transform.parent = tempGO.transform;
				
		}
		if(autoParent)
		{
			Undo.SetTransformParent(tempAsset.transform, hitObject.transform, "Set Parent to User Choice");
			tempAsset.transform.parent = hitObject.transform;
		}
		
		return tempAsset; 		
	}
	
	Vector3 GetMassPlacePosition(RaycastHit target)
	{
		//Get random position as a function of radius
		Vector2 randV2;
		randV2 = UnityEngine.Random.onUnitSphere * radiusToPlace;
		
		//Location of mouse click in scene		
		Vector3 centerPos;
		centerPos = target.point;
		
		//Raycast to get height at new random position
		float height;
		Ray tempRay;
		RaycastHit rayHit;
		Vector3 rayOrigin;
		rayOrigin = centerPos + new Vector3(randV2.x, 0, randV2.y) + new Vector3(0,100,0);
		tempRay = new Ray(rayOrigin, -Vector3.up);
		
		Vector3 newPosition = Vector3.zero;
		
		//Raycast to get position of mass placed prefab
		if(Physics.Raycast (tempRay, out rayHit))
		{
			//Check to that new target is the same as the clicked target
			if(rayHit.transform.name == target.transform.name)
			{
				height = rayHit.point.y;
				//Set new position
				newPosition = new Vector3(centerPos.x, 0, centerPos.z) + new Vector3(randV2.x, height, randV2.y);
			}
			else
			{
				//Returning vector (0,0,0) does not instantiate a new object
				newPosition = Vector3.zero;
			}
		}
		else
		{
			//Returning vector (0,0,0) does not instantiate a new object
			newPosition = Vector3.zero;
		}
		
		return newPosition;
	}
	
	void AlignWithNormal(RaycastHit rayHit, GameObject tempGO)
	{
		tempGO.transform.localRotation = Quaternion.FromToRotation(tempGO.transform.up, rayHit.normal);
	}
	
	//Normalize weights of prefab placements
	void NormalizeWeights()
	{
		float tempFloat = 0f; 
	
		//sum weights
		foreach(prefabToAdd prefab in preFabList)
		{
			tempFloat += prefab.weight;// * prefab.weight;
			
		}
		
		//tempFloat = Mathf.Sqrt(tempFloat);
		
		foreach(prefabToAdd prefab in preFabList)
		{
			prefab.weight = prefab.weight / tempFloat;
		}
		
	}
	
	
	//Choose asset to place based on random number and weighting
	GameObject ChooseAsset()
	{
		GameObject tempGO;
	
		//set default prefab
		//If all weights set to zero CTA will use first in the list
		if(preFabList[0].prefab != null)
		{
			tempGO = preFabList[0].prefab;
		}
		else
		{
			tempGO = new GameObject();
		}

		
		//sum weights
		float sumWeights = 0f; 

		for(int i = 0; i < preFabList.Count; i++)
		{
			if(preFabList[i].prefab != null)
			{
				sumWeights += preFabList[i].weight;
				preFabList[i].lotteryNumber = sumWeights - preFabList[i].weight/2f;
			}
		}
		
		float randomSeed;
		randomSeed = UnityEngine.Random.Range(0f, sumWeights);
		//Debug.Log("Random : " + randomSeed);
		
		for(int i = 0; i < preFabList.Count; i++)
		{
		
			if(preFabList[i].prefab != null)
			{
				float tempfloat;
				tempfloat = Mathf.Abs(preFabList[i].lotteryNumber - randomSeed);
				
				if(tempfloat < preFabList[i].weight/2 && preFabList[i].weight != 0)
				{
					tempGO = preFabList[i].prefab;
				} 
			}
		}	
	
		return tempGO;
	}
	
	void ZeroWeights()
	{
		
		int prefabCount;
		prefabCount = preFabList.Count;
		
		for(int i = 0; i < prefabCount; i++)
		{
			preFabList[i].weight = 0f;
		}
	}
	
	void EqualWeghts()
	{
		int prefabCount;
		prefabCount = preFabList.Count;
		
		for(int i = 0; i < prefabCount; i++)
		{
			preFabList[i].weight = 0.5f;
		}
	}
	
	void SaveCustomWeights()
	{
		int prefabCount;
		prefabCount = preFabList.Count;
		preFabWeights.Clear();
		
		for(int i = 0; i < prefabCount; i++)
		{
			preFabWeights.Add(preFabList[i].weight);
		}
	}
	
	void UseCustomWeights()
	{
		int prefabCount;
		prefabCount = preFabList.Count;
		
		if(prefabCount > 0){
			for(int i = 0; i < prefabCount; i++)
			{
				preFabList[i].weight = preFabWeights[i];
			}
		}
	}
}

[System.Serializable]
public class prefabToAdd 
{
	public GameObject prefab;
	public float weight = 0.5f;
	public bool connected = true;
	//used in selection of prefab
	public float lotteryNumber = 0f;
}


