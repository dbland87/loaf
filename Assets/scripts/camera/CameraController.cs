using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CameraController : NetworkBehaviour {

    public Player targetPlayer;
	/// True if the camera should follow the player
	public bool FollowsPlayer{get;set;}

		
	/// How far ahead from the Player the camera is supposed to be		
	public float HorizontalLookDistance = 3;
	/// Vertical Camera Offset	
	public Vector3 CameraOffset ;
	/// Minimal distance that triggers look ahead
	public float LookAheadTrigger = 0.1f;
	/// How high (or low) from the Player the camera should move when looking up/down
	public float ManualUpDownLookDistance = 3;
		
	/// How fast the camera goes back to the Player
	public float ResetSpeed = 0.5f;
	/// How fast the camera moves
	public float CameraSpeed = 0.3f;
		
	/// the minimum camera zoom
	public float MinimumZoom = 5f;
	//[Range (1, 20)]
	/// the maximum camera zoom
	public float MaximumZoom = 10f;
	/// the speed at which the camera zooms	
	public float ZoomSpeed = 0.4f;

	/// if this is true, the script will resize the camera's orthographic size on start to match the desired size and PPU
	/// if you want to know more about that trick, have a look at Unity's post there : https://blogs.unity3d.com/2015/06/19/pixel-perfect-2d/
	public bool PixelPerfect = false;
	/// the vertical resolution for which you've created your visual assets
	public int ReferenceVerticalResolution = 768;
	/// the reference PPU value (the one you set on your sprites)
	public float ReferencePixelsPerUnit = 32;
		
	/// If set to false, all Cinematic Effects on the camera will be removed at start on mobile targets
	public bool EnableEffectsOnMobile = false;
		
	// Private variables		
	protected Transform _target;
	//protected CorgiController _targetController;
	protected Bounds _levelBounds;

	protected float _xMin;
	protected float _xMax;
	protected float _yMin;
	protected float _yMax;	 
		
	protected float _offsetZ;
	protected Vector3 _lastTargetPosition;
	protected Vector3 _currentVelocity;
	protected Vector3 _lookAheadPos;

	protected float _shakeIntensity;
	protected float _shakeDecay;
	protected float _shakeDuration;
		
	protected float _currentZoom;	
	protected Camera _camera;

	protected Vector3 _lookDirectionModifier = new Vector3(0,0,0);

    public void SetTargetPlayer(Player player) {
        targetPlayer = player;
    }

    public void initCamera() {
        _target = targetPlayer.transform;
        _lastTargetPosition = _target.position;
        _offsetZ = (transform.position - _target.position).z;
    }
	
	protected virtual void Start ()
	{		
		// we get the camera component
		_camera=GetComponent<Camera>();

		// We make the camera follow the player
		FollowsPlayer=true;
		_currentZoom=MinimumZoom;
	}

	/// <summary>
	/// Every frame, we move the camera if needed
	/// </summary>
	protected virtual void LateUpdate () 
	{
        if (targetPlayer == null) {
            Debug.LogWarning("CameraController : Target Player is null");
            return;
        }

		GetLevelBounds();
        //if the camera is not supposed to follow the player, we do nothing

        //if (!FollowsPlayer || _targetController == null) {
        //    return;
        //}

        if (!PixelPerfect)
		{
			Zoom();	
		}
				
		FollowPlayer ();
	}	

	/// <summary>
	/// Use this method to shake the camera, passing in a Vector3 for intensity, duration and decay
	/// </summary>
	/// <param name="shakeParameters">Shake parameters : intensity, duration and decay.</param>
	public virtual void Shake(Vector3 shakeParameters)
	{
		_shakeIntensity = shakeParameters.x;
		_shakeDuration=shakeParameters.y;
		_shakeDecay=shakeParameters.z;
	}

	/// <summary>
	/// Moves the camera up
	/// </summary>
	public virtual void LookUp()
	{
		_lookDirectionModifier = new Vector3(0,ManualUpDownLookDistance,0);
	}

	/// <summary>
	/// Moves the camera down
	/// </summary>
	public virtual void LookDown()
	{
		_lookDirectionModifier = new Vector3(0,-ManualUpDownLookDistance,0);
	}

	/// <summary>
	/// Resets the look direction modifier
	/// </summary>
	public virtual void ResetLookUpDown()
	{	
		_lookDirectionModifier = new Vector3(0,0,0);
	}

	/// <summary>
	/// Makes the camera pixel perfect by resizing its orthographic size according to the current screen's size.
	/// </summary>
	protected virtual void MakeCameraPixelPerfect ()
	{
		int screenHeight = Screen.height;
		float newOrthographicSize = (screenHeight / ReferencePixelsPerUnit) * 0.5f;
		float referenceSize = (ReferenceVerticalResolution/ ReferencePixelsPerUnit) * 0.5f;

		float rounder = Mathf.Max(1, Mathf.Round(newOrthographicSize / referenceSize));
		newOrthographicSize = newOrthographicSize / rounder;

		_camera.orthographicSize = newOrthographicSize;
	}

	/// <summary>
	/// Moves the camera around so it follows the player
	/// </summary>
	protected virtual void FollowPlayer()
	{

    // if the player has moved since last update
    float xMoveDelta = (_target.position - _lastTargetPosition).x;
		bool updateLookAheadTarget = Mathf.Abs(xMoveDelta) > LookAheadTrigger;

		if (updateLookAheadTarget) 
		{
			_lookAheadPos = HorizontalLookDistance * Vector3.right * Mathf.Sign(xMoveDelta);
		} 
		else 
		{
			_lookAheadPos = Vector3.MoveTowards(_lookAheadPos, Vector3.zero, Time.deltaTime * ResetSpeed);	
		}

		Vector3 aheadTargetPos = _target.position + _lookAheadPos + Vector3.forward * _offsetZ + _lookDirectionModifier + CameraOffset;
		Vector3 newCameraPosition = Vector3.SmoothDamp(transform.position, aheadTargetPos, ref _currentVelocity, CameraSpeed);
		Vector3 shakeFactorPosition = Vector3.zero;

		// If shakeDuration is still running.
		if (_shakeDuration>0)
		{
			shakeFactorPosition= Random.insideUnitSphere * _shakeIntensity * _shakeDuration;
			_shakeDuration-=_shakeDecay*Time.deltaTime ;
		}		
		newCameraPosition = newCameraPosition+shakeFactorPosition;		


		if (_camera.orthographic==true)
		{
			float posX,posY,posZ=0f;
			// Clamp to level boundaries
			if (_levelBounds.size != Vector3.zero)
			{
				posX = Mathf.Clamp(newCameraPosition.x, _xMin, _xMax);
				posY = Mathf.Clamp(newCameraPosition.y, _yMin, _yMax);
			}
			else
			{
				posX = newCameraPosition.x;
				posY = newCameraPosition.y;
			}
			posZ = newCameraPosition.z;
			// We move the actual transform
			transform.position=new Vector3(posX, posY, posZ);
		}
		else
		{
			transform.position=newCameraPosition;
		}		

		_lastTargetPosition = _target.position;	
	}

	/// <summary>
	/// Handles the zoom of the camera based on the main character's speed
	/// </summary>
	protected virtual void Zoom()
	{
    //// if we're in pixel perfect mode, we do nothing and exit.
    //if (PixelPerfect) {
    //    return;
    //}

    //float characterSpeed = Mathf.Abs(_targetController.Speed.x);
    //float currentVelocity = 0f;

    //_currentZoom = Mathf.SmoothDamp(_currentZoom, (characterSpeed / 10) * (MaximumZoom - MinimumZoom) + MinimumZoom, ref currentVelocity, ZoomSpeed);

    //_camera.orthographicSize = _currentZoom;
    ////GetLevelBounds();
}

    /// <summary>
    /// Gets the levelbounds coordinates to lock the camera into the level
    /// </summary>
    protected virtual void GetLevelBounds()
	{
		if (_levelBounds.size==Vector3.zero)
		{
			return;
		}

		// camera size calculation (orthographicSize is half the height of what the camera sees.
		float cameraHeight = Camera.main.orthographicSize * 2f;		
		float cameraWidth = cameraHeight * Camera.main.aspect;

		_xMin = _levelBounds.min.x+(cameraWidth/2);
		_xMax = _levelBounds.max.x-(cameraWidth/2); 
		_yMin = _levelBounds.min.y+(cameraHeight/2); 
		_yMax = _levelBounds.max.y-(cameraHeight/2);

		// if the level is too narrow, we center the camera on the levelbound's horizontal center
		if (_levelBounds.max.x - _levelBounds.min.x <= cameraWidth)
		{
			_xMin = _levelBounds.center.x;
			_xMax = _levelBounds.center.x;
		}

		// if the level is not high enough, we center the camera on the levelbound's vertical center
		if (_levelBounds.max.y - _levelBounds.min.y <= cameraHeight)
		{
			_yMin = _levelBounds.center.y;
			_yMax = _levelBounds.center.y;
		}	
	}
}
