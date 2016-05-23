using UnityEngine;
using System.Collections;

public class LeaderScript : MonoBehaviour {

	public GameObject Light;
	// Use this for initialization
	void Start () {
	
	}

	Vector3 newPosition;
	// Update is called once per frame
	void Update () {
		newPosition = new Vector3 (Light.transform.position.x, transform.position.y, Light.transform.position.z);
		transform.position = newPosition;
	}	
}
