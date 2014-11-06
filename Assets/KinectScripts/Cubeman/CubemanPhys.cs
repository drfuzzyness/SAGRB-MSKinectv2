using UnityEngine;
//using Windows.Kinect;

using System;
using System.Collections;

public class CubemanPhys : MonoBehaviour 
{
	public bool MoveVertically = false;
	public bool MirroredMovement = false;

	//public GameObject debugText;

	public float AppendageMovementSpeed = 1;
	public ForceMode TypeOfAppendageMovement = ForceMode.VelocityChange;
	
	public GameObject Hip_Center;
	public GameObject Spine;
	public GameObject Neck;
	public GameObject Head;
	public GameObject Shoulder_Left;
	public GameObject Elbow_Left;
	public GameObject Wrist_Left;
	public GameObject Hand_Left;
	public GameObject Shoulder_Right;
	public GameObject Elbow_Right;
	public GameObject Wrist_Right;
	public GameObject Hand_Right;
	public GameObject Hip_Left;
	public GameObject Knee_Left;
	public GameObject Ankle_Left;
	public GameObject Foot_Left;
	public GameObject Hip_Right;
	public GameObject Knee_Right;
	public GameObject Ankle_Right;
	public GameObject Foot_Right;
	public GameObject Spine_Shoulder;
    public GameObject Hand_Tip_Left;
    public GameObject Thumb_Left;
    public GameObject Hand_Tip_Right;
    public GameObject Thumb_Right;
	
	public LineRenderer LinePrefab;
//	public LineRenderer DebugLine;

	private GameObject[] bones;
	private LineRenderer[] lines;

	private LineRenderer lineTLeft;
	private LineRenderer lineTRight;
	private LineRenderer lineFLeft;
	private LineRenderer lineFRight;

	private Vector3 initialPosition;
	private Quaternion initialRotation;
	private Vector3 initialPosOffset = Vector3.zero;
	private Int64 initialPosUserID = 0;
	
	
	void Start () 
	{
		//store bones in a list for easier access
		bones = new GameObject[] {
			Hip_Center,
            Spine,
            Neck,
            Head,
            Shoulder_Left,
            Elbow_Left,
            Wrist_Left,
            Hand_Left,
            Shoulder_Right,
            Elbow_Right,
            Wrist_Right,
            Hand_Right,
            Hip_Left,
            Knee_Left,
            Ankle_Left,
            Foot_Left,
            Hip_Right,
            Knee_Right,
            Ankle_Right,
            Foot_Right,
            Spine_Shoulder,
            Hand_Tip_Left,
            Thumb_Left,
            Hand_Tip_Right,
            Thumb_Right
		};
		
		// array holding the skeleton lines
		lines = new LineRenderer[bones.Length];
		
		if(LinePrefab)
		{
			for(int i = 0; i < lines.Length; i++)
			{
				lines[i] = Instantiate(LinePrefab) as LineRenderer;
			}
		}

//		if(DebugLine)
//		{
//			lineTLeft = Instantiate(DebugLine) as LineRenderer;
//			lineTRight = Instantiate(DebugLine) as LineRenderer;
//		}
//
//		if(LinePrefab)
//		{
//			lineFLeft = Instantiate(LinePrefab) as LineRenderer;
//			lineFRight = Instantiate(LinePrefab) as LineRenderer;
//		}
		
		initialPosition = transform.position;
		initialRotation = transform.rotation;
	}
	

	void Update () 
	{
		KinectManager manager = KinectManager.Instance;
		
		// get 1st player
		Int64 userID = manager ? manager.GetPrimaryUserID() : 0;

		// No user found logic
		if(userID <= 0)
		{
			// reset the pointman position and rotation if no user detected
			if(transform.position != initialPosition)
				transform.position = initialPosition;
			
			if(transform.rotation != initialRotation)
				transform.rotation = initialRotation;

			for(int i = 0; i < bones.Length; i++) 
			{
				bones[i].gameObject.SetActive(true);

				bones[i].transform.localPosition = Vector3.zero;
				bones[i].transform.localRotation = Quaternion.identity;
				
				if(LinePrefab)
				{
					lines[i].gameObject.SetActive(false);
				}
			}
			
			return;
		}
		
		// set the position in space, getting the center of the user in space from the kinect
		Vector3 posPointMan = manager.GetUserPosition(userID);
		// inverts forward/back if not mirrored
		posPointMan.z = !MirroredMovement ? -posPointMan.z : posPointMan.z;
		
		// store the initial position
		if(initialPosUserID != userID)
		{
			initialPosUserID = userID;
			// if you're allowed to move in depth, uses all the posPointMan data, otherwise scraps the the depth
			initialPosOffset = transform.position - (MoveVertically ? posPointMan : new Vector3(posPointMan.x, 0, posPointMan.z));
		}
		
		transform.position = initialPosOffset + (MoveVertically ? posPointMan : new Vector3(posPointMan.x, 0, posPointMan.z));
		
		// update the local positions of the bones
		// for each bone...
		for(int i = 0; i < bones.Length; i++) 
		{
			if(bones[i] != null)
			{
				// GetJointAtIndex uses the standard Kinect naming convention of joints for humans
				int joint = (int)manager.GetJointAtIndex(
					!MirroredMovement ? i : (int)KinectInterop.GetMirrorJoint((KinectInterop.JointType)i));

				// Break if bad joint
				if(joint < 0)
					continue;

				// Only for joints that could be tracked...
				if(manager.IsJointTracked(userID, joint))
				{
					// Enables the bone to be displayed
					bones[i].gameObject.SetActive(true);

					// Get how the kinect thinks the joint is TRANSLATED
					Vector3 posJoint = manager.GetJointPosition(userID, joint);
					posJoint.z = !MirroredMovement ? -posJoint.z : posJoint.z;

					// Get how the kinect thinks the joint is ROTATED
					Quaternion rotJoint = manager.GetJointOrientation(userID, joint, !MirroredMovement);
					
					posJoint -= posPointMan;

					// Extra manipulation for MirroredMovement
					if(MirroredMovement)
					{
						posJoint.x = -posJoint.x;
						posJoint.z = -posJoint.z;
					}

					// Move the BONE to the JOINT
					bones[i].transform.localPosition = posJoint;
					bones[i].transform.localRotation = rotJoint;

					// Apply a force to the BONE based on the JOINT position
//					Vector3 forceToAdd = new Vector3( posJoint - bones[i].transform.localPosition );
//					Quaternion rotToAdd = new Quaternion( rotJoint - bones[i].transform.localRotation );
//					bones[i].rigidbody.AddRelativeForce( forceToAdd, TypeOfAppendageMovement)

					// If we would like to place lines between the bones...
					if(LinePrefab)
					{
						// Set our line to be active
						lines[i].gameObject.SetActive(true);

						// Get where the bone is currently positioned
						// disconnected from where the Kinect thinks the joint is
						Vector3 posJoint2 = bones[i].transform.position;

						// Gets the direction from the Kinect, not from the bone
						Vector3 dirFromParent = manager.GetJointDirection(userID, joint, false, false);
						dirFromParent.z = !MirroredMovement ? -dirFromParent.z : dirFromParent.z;
						Vector3 posParent = posJoint2 - dirFromParent;
						
						//lines[i].SetVertexCount(2);
						lines[i].SetPosition(0, posParent);
						lines[i].SetPosition(1, posJoint2);
					}

//					KinectInterop.BodyData bodyData = manager.GetUserBodyData(userID);
//					if(lineTLeft != null && bodyData.liTrackingID != 0 && joint == (int)JointType.HandLeft)
//					{
//						Vector3 leftTDir = bodyData.leftThumbDirection.normalized;
//						leftTDir.z = !MirroredMovement ? -leftTDir.z : leftTDir.z;
//
//						Vector3 posStart = bones[i].transform.position;
//						Vector3 posTEnd = posTStart + leftTDir;
//
//						lineTLeft.SetPosition(0, posTStart);
//						lineTLeft.SetPosition(1, posTEnd);
//
//						if(lineFLeft != null)
//						{
//							Vector3 leftFDir = bodyData.leftThumbForward.normalized;
//							leftFDir.z = !MirroredMovement ? -leftFDir.z : leftFDir.z;
//							
//							Vector3 posFStart = bones[i].transform.position;
//							Vector3 posFEnd = posTStart + leftFDir;
//							
//							lineFLeft.SetPosition(0, posFStart);
//							lineFLeft.SetPosition(1, posFEnd);
//						}
//					}
//					
//					if(lineTRight != null && bodyData.liTrackingID != 0 && joint == (int)JointType.HandRight)
//					{
//						Vector3 rightTDir = bodyData.rightThumbDirection.normalized;
//						rightTDir.z = !MirroredMovement ? -rightTDir.z : rightTDir.z;
//						
//						Vector3 posTStart = bones[i].transform.position;
//						Vector3 posTEnd = posTStart + rightTDir;
//						
//						lineTRight.SetPosition(0, posTStart);
//						lineTRight.SetPosition(1, posTEnd);
//						
//						if(lineFRight != null)
//						{
//							Vector3 rightFDir = bodyData.rightThumbForward.normalized;
//							rightFDir.z = !MirroredMovement ? -rightFDir.z : rightFDir.z;
//							
//							Vector3 posFStart = bones[i].transform.position;
//							Vector3 posFEnd = posTStart + rightFDir;
//							
//							lineFRight.SetPosition(0, posFStart);
//							lineFRight.SetPosition(1, posFEnd);
//						}
//					}
				}
				else
				{
					// disables bones that couldn't be connected to joints
					//bones[i].gameObject.SetActive(false);

					// disables the line drawing if the bone couldn't be connected to a joint
					if(LinePrefab)
					{
						lines[i].gameObject.SetActive(false);
					}
				}
			}	
		}
	}

}
