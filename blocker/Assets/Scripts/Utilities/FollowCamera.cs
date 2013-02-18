using UnityEngine;
using System.Collections;

public class FollowCamera : BlockerObject 
{
	public bool lockedCamera = false;
	public Transform targetLocation;
	private Vector3 oldLocation;
	public float speed = 0.03f;
	
	public Vector3 offset = new Vector3(0, 5, -10);
	public Vector3 lockedOffset = new Vector3(0, 1f, -2);
	
	private Vector3 targetPosition;
	private Quaternion targetRotation;
	
	bool isLocked = false;
	float timeSinceLockRequested = 0f;
	
	public override void Start () 
	{
		base.Start();
		targetLocation = transform.parent.Find("Doll/Model/Arms");
		updateOldLocation();
		calculateTarget(offset);
		
		transform.position = targetPosition;
		transform.rotation = targetRotation;
	}
	
	void LateUpdate ()
	{
		if(targetLocation)
		{
			if(lockedCamera)
			{
				calculateTarget(lockedOffset);
				if(timeSinceLockRequested == 0)//Makes the camera not jump when the target location changes abruptly
				{
					updateOldLocation();
				}
				timeSinceLockRequested += Time.deltaTime;
				if(Vector3.Distance(targetPosition, transform.position) > 0.5f && !isLocked)
				{
					float compSpeed = speed * Mathf.Pow(timeSinceLockRequested * 10, 2);
					
					transform.position = Vector3.Lerp(transform.position - (targetPosition - oldLocation), targetPosition, compSpeed) ;//+ (targetPosition - oldLocation);
					transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, compSpeed * 2);
					
				}
				else
				{				
					transform.position = targetPosition;
					transform.rotation = targetRotation;
					isLocked = true;
				}
				
			}
			else
			{
				calculateTarget(offset);
				float compSpeed = speed * (Vector3.Distance(targetPosition, transform.position)/5);
				
				transform.position = Vector3.Lerp(transform.position, targetPosition, compSpeed);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, compSpeed ); //*2
				isLocked = false;
				timeSinceLockRequested = 0f;
			}
			
			
			updateOldLocation();
		}
				
	}
	
	void calculateTarget(Vector3 inputOffset)
	{
		targetPosition = targetLocation.position;
		targetPosition += targetLocation.forward * inputOffset.z;
		targetPosition += targetLocation.right * inputOffset.x;
		targetPosition += targetLocation.up * inputOffset.y;
		
		targetRotation = Quaternion.identity;
		if((targetLocation.position - targetPosition) != Vector3.zero)
		{
			targetRotation.SetLookRotation( targetLocation.position - targetPosition, targetLocation.up);
		}
	}
	
	void updateOldLocation()
	{
		oldLocation = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
	}
	
	
}
