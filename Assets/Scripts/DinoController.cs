using UnityEngine;
using System.Collections;

public class DinoController : MonoBehaviour {
	GameObject player;
	// Use this for initialization
	Vector3 initialPosition;
	public float speed = 0.1F;

	public AnimationClip idleAnimation01 ;
	public AnimationClip idleAnimation02 ;
	public AnimationClip idleAnimation03 ;
	public AnimationClip jump ;
	public AnimationClip leftJump ;
	public AnimationClip rightJump ;
	public AnimationClip attack1 ;
	public AnimationClip attack2 ;
	public AnimationClip hit1 ;
	public AnimationClip hit2 ;
	public AnimationClip die ;

	int idleRnd;
	private Animation _animation;
	public Action action;

	public enum Action {
		Jumping,
		Attack,
		LeftJump,
		RightJump,
		Hit,
		Die,
		Idle,
		LookLeft,
		LookRight
	}
	void Start () {
		player = GameObject.FindGameObjectWithTag ("Player");
		initialPosition = transform.position;
		//gameController = GameObject.FindGameObjectWithTag ("GameController");
		//littlebitsData = gameController.GetComponent<LittlebitsWSDino> ();
		action = Action.Idle;
	}


	float minValue = 5f;
	float maxValue = 2f;
	public float growthSpeed = 0.5F;
	float scaleValue;
	public float value;
	Vector3 vector;

	void Awake ()
	{
		//moveDirection = transform.TransformDirection (Vector3.forward);
		
		_animation = GetComponent<Animation>();
		vector = Vector3.zero;
	}

	// Update is called once per frame
	void Update () {

		//scaleValue = (minValue + value * 0.001f);
		//vector.Set (scaleValue, scaleValue, -scaleValue);
		//transform.localScale = Vector3.Lerp (transform.localScale, vector, growthSpeed * Time.deltaTime);

		switch (action) {
		case Action.Idle:
			DoIdle();
			break;
		case Action.Jumping:
			//_animation.CrossFade (jump.name);
			switch (Random.Range (0, 3)) {
			case 0:
				_animation.CrossFade (jump.name);
				break;
			case 1:
				_animation.CrossFade (leftJump.name);
				break;
			case 2:
				_animation.CrossFade (rightJump.name);
				break;
			}
			break;
			break;
		case Action.Hit:
		//Debug.Log (idleRnd);
			switch (Random.Range (0, 2)) {
			case 0:
				_animation.CrossFade (hit1.name);
				break;
			case 1:
				_animation.CrossFade (hit2.name);
				break;
			}
			break;
		case Action.Attack:
			switch (Random.Range (0, 2)) {
			case 0:
				_animation.CrossFade (attack1.name);
				break;
			case 1:
				_animation.CrossFade (attack2.name);
				break;
			}
			break;
		case Action.Die:
			_animation.CrossFade (die.name);
			break;
		case Action.LeftJump:
			_animation.CrossFade (leftJump.name);
			break;
		case Action.RightJump:
			_animation.CrossFade (rightJump.name);
			break;
		case Action.LookLeft:
			//_animation.CrossFade (rightJump.name);
			//_animation.c
			//_animation.GetClip(
			vector.Set(0,transform.rotation.y + 1,0);
			transform.Rotate(vector);
			DoIdle();
			//transform.rotation = Quaternion.LookRotation(vector);
			break;
		case Action.LookRight:
			//_animation.CrossFade (rightJump.name);
			vector.Set(0,transform.rotation.y - 1,0);
			transform.Rotate(vector);
			DoIdle();
			//transform.rotation = Quaternion.LookRotation(vector);
			break;
		}

	}

	void DoIdle(){
		idleRnd = Random.Range (0, 3);
		//Debug.Log (idleRnd);
		switch (idleRnd) {

		case 0:
			_animation.CrossFade (idleAnimation01.name);
			break;
		case 1:
			_animation.CrossFade (idleAnimation02.name);
			break;
		case 2:
			_animation.CrossFade (idleAnimation03.name);
			break;
		}
	}
}
