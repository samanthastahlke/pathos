using UnityEngine;
using System.Collections;

public class SwitchView : MonoBehaviour {
	
	public Camera mainCam,subCam01,subCam02;
	public Vector2 pos,size;
	
	void OnGUI()
	{
		if(GUI.Button(new Rect(pos.x *Screen.width,pos.y*Screen.height,size.x*Screen.width,size.y*Screen.height),"Camera01"))
		{
			DisableCam();
			mainCam.enabled = true;

		}
		
		if(GUI.Button(new Rect((pos.x *Screen.width+200),pos.y*Screen.height,size.x*Screen.width,size.y*Screen.height),"Camera02"))
		{
			DisableCam();
			subCam01.enabled = true;

		}
		
		if(GUI.Button(new Rect((pos.x *Screen.width)+400,pos.y*Screen.height,size.x*Screen.width,size.y*Screen.height),"Camera03"))
		{
			DisableCam();
			subCam02.enabled = true;

		}
	}
	
	void DisableCam()
	{
			mainCam.enabled = false;
			subCam01.enabled = false;
			subCam02.enabled = false;
	}
}
