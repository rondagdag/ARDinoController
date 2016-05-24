using UnityEngine;

using System.Collections;

using System.Collections.Generic;

//using CielaSpike;

using System.Text;

using System;



public class socketScript : MonoBehaviour {

	

	//variables

	private TCPConnection myTCP;

	private string serverMsg;

	public string msgToServer;

	public GameObject Dino;

	DinoController dinoController;

	public string command;

	void Awake() {

		//add a copy of TCPConnection to this game object

		myTCP = gameObject.AddComponent<TCPConnection>();

	}



	void Start () {

		dinoController = Dino.GetComponent<DinoController> ();
		if (myTCP.socketReady == false) {
			
			myTCP.setupSocket ();
		}
	}



	void Update () {

		

		//keep checking the server for messages, if a message is received from server, it gets logged in the Debug console (see function below)

		SocketResponse ();



	}



	/*void OnGUI() {

		

		//if connection has not been made, display button to connect

		if (myTCP.socketReady == false) {

			

			if (GUILayout.Button ("Connect")) {

				//try to connect

				Debug.Log("Attempting to connect..");

				myTCP.setupSocket();

			}

		

		}

		

		//once connection has been made, display editable text field with a button to send that string to the server (see function below)

		if (myTCP.socketReady == true) {

		

			msgToServer = GUILayout.TextField(msgToServer);

							

			if (GUILayout.Button ("Write to server", GUILayout.Height(30))) {

					SendToServer(msgToServer);

			}

		

		}





	}*/

	

	//socket reading script

	void SocketResponse() {

		string serverSays = myTCP.readSocket();
		//serverSays = serverSays + "";
		if (serverSays != "") {

			Debug.Log("[SERVER]" + serverSays);
			//string[] rot = serverSays.Split(',');
			/*transform.Rotate(
				//new Vector3(Convert.ToSingle(rot[0]),Convert.ToSingle(rot[1]),Convert.ToSingle(rot[2])));
				new Vector3(Convert.ToSingle(rot[0]),0,0));*/
			//Dino.transform.

			command = serverSays.Substring(0,serverSays.IndexOf("\r\n"));

			switch (command) {
			case "FAR":
				//dinoController.action = DinoController.Action.Idle;
				dinoController.action = DinoController.Action.Hit;
				break;
			case "NEAR":
				dinoController.action = DinoController.Action.Jumping;
				break;
			case "LEFT":
				dinoController.action = DinoController.Action.LookLeft;
				break;
			case "RIGHT":
				dinoController.action = DinoController.Action.LookRight;
				break;
			case "UP":
				//dinoController.value += 0.25f;
				//dinoController.action = DinoController.Action.Hit;
				dinoController.action = DinoController.Action.Jumping;
				break;
			case "DOWN":
				//dinoController.value -= 0.25f;
				//dinoController.action = DinoController.Action.Attack;
				dinoController.action = DinoController.Action.Idle;
				break;
			}

		}



	}



	//send message to the server

	public void SendToServer(string str) {

		myTCP.writeSocket(str);

		Debug.Log ("[CLIENT] -> " + str);

	}



}

