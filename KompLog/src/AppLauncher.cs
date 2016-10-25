/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using UnityEngine;

namespace KompLog
{
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny | KSPAddonImproved.Startup.TrackingStation, false)]
	public class AppLauncher: MonoBehaviour
	{

		const float AUTO_HIDE_TIME = 5.0f;

		static public AppLauncher Instance
		{
			get { return _instance; }
		}
		static private AppLauncher _instance = null;
		private bool 		_active = false;
		private Rect 		_windowPos = new Rect();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_buttonStyle;
		private bool 		_hasInitStyles 	= false;
		private int 		_winID;
		private float 		_autoHideTime = 0.0f;
		public ApplicationLauncherButton _toolbarButton;

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start()
		{
			if (_instance)
				Destroy (_instance);
			_instance = this;
			_winID = GUIUtility.GetControlID (FocusType.Passive);
			Debug.Log ("Komp Log App Launch Start");
			GameEvents.onGUIApplicationLauncherReady.Add (OnAppLaunchReady);
			GameEvents.onGameSceneSwitchRequested.Add (OnSceneChange);
			GameEvents.OnMapEntered.Add (Resize);
			GameEvents.OnMapExited.Add (Resize);

			if (ApplicationLauncher.Ready) {
				OnAppLaunchReady ();
			}
		}

		/// <summary>
		/// Called when this object is destroyed.
		/// </summary>
		public void OnDestroy()
		{
			Debug.Log ("Kom Log App Launch Destroy");
			GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLaunchReady);
			GameEvents.onGameSceneSwitchRequested.Remove (OnSceneChange);
			GameEvents.OnMapEntered.Remove (Resize);
			GameEvents.OnMapExited.Remove (Resize);
			ApplicationLauncher.Instance.RemoveModApplication(_toolbarButton);
			ControlUnlock ();
			if (_instance == this)
				_instance = null;
		}

		/// <summary>
		/// Callback when the scene changes.
		/// </summary>
		/// <param name="evt">Evt.</param>
		public void OnSceneChange(GameEvents.FromToAction<GameScenes,GameScenes> evt)
		{
			Resize ();
		}

		/// <summary>
		/// Callback when the app launcher bar is ready.
		/// </summary>
		public void OnAppLaunchReady()
		{
			_toolbarButton = ApplicationLauncher.Instance.AddModApplication (
					ToggleWindow,
					ToggleWindow,
					noOp,
					noOp,
					noOp,
					noOp,
					ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | 
					ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB |
					ApplicationLauncher.AppScenes.TRACKSTATION,
					(Texture)GameDatabase.Instance.GetTexture ("KompLog/Textures/sat_calc", false)
				);
		}

		/// <summary>
		/// No op.
		/// </summary>
		public void noOp() {}

		/// <summary>
		/// Force a window resize.
		/// </summary>
		public void Resize()
		{
			_windowPos.height = 0.0f;
		}

		/// <summary>
		/// Toggles the window.
		/// </summary>
		public void ToggleWindow()
		{
			if (_active)
				onDeactivate ();
			else
				onActivate ();
		}

		/// <summary>
		/// Display the app launcher.
		/// </summary>
		public void onActivate()
		{
			Debug.Log ("KompLog App Launch Activate - Pos:"+_windowPos.x +" "+_windowPos.y);
			_windowPos.height = 0.0f;
			_active = true;
			RenderingManager.AddToPostDrawQueue (0, OnDraw);
		}

		/// <summary>
		/// Hide the launcher.
		/// </summary>
		public void onDeactivate()
		{
			Debug.Log ("KompLog App Launch Deactivate");
			_active = false;
			_autoHideTime = 0.0f;
			RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
			ControlUnlock ();
		}


		/// <summary>
		/// Lock the Controls.
		/// </summary>
		private void ControlLock()
		{
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK, "KompLog_Launch");
		}
		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("KompLog_Launch");
		}

		public void Update()
		{
			if (KompLogSettings.Instance.AutoHide && _autoHideTime != 0.0f && Time.time > _autoHideTime &&
				_active && !_windowPos.Contains (Event.current.mousePosition)) {
				_toolbarButton.enabled = false;
				onDeactivate ();
			}
		}

		/// <summary>
		/// Callback when a draw is requested.
		/// </summary>
		public void OnDraw()
		{
			if (!_hasInitStyles)
				InitStyles ();
			if (_active) {
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "KompLog", _windowStyle);
				if ((_windowPos.x == 0.0f && _windowPos.y == 0.0f) || _windowPos.yMax > Screen.height) {
					Vector3 toolPos = Camera.current.WorldToScreenPoint (_toolbarButton.GetAnchor ());
					_windowPos.x = toolPos.x - _windowPos.width * 0.5f;
					_windowPos.y = (Screen.height - toolPos.y);
					if (!ApplicationLauncher.Instance.IsPositionedAtTop) {
						_windowPos.y -= _windowPos.height;
					}
				}
			}
			if (_active && _windowPos.Contains (Event.current.mousePosition)) {
				ControlLock ();
			} else {
				ControlUnlock();
			}
		}

		/// <summary>
		/// Draws the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical (GUILayout.Width(150.0f));
			if (KompLogSettings.Instance != null && GUILayout.Button ("Settings",_buttonStyle)) {
				KompLogSettings.Instance.ToggleWindow ();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
			if (StageDataViewer.Instance != null && GUILayout.Button ("Stage Data",_buttonStyle)) {
				StageDataViewer.Instance.ToggleWindow ();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
			if (PartDataViewer.Instance != null && GUILayout.Button ("Part Data", _buttonStyle)) {
				PartDataViewer.Instance.ToggleWindow ();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
			if (OrbitData.Instance != null && GUILayout.Button ("Orbit Data", _buttonStyle)) {
				OrbitData.Instance.ToggleWindow ();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
			if (CelestialBodyData.Instance != null && GUILayout.Button ("Celestial Data", _buttonStyle)) {
				CelestialBodyData.Instance.ToggleWindow ();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
			if (Calculator.Instance != null && GUILayout.Button ("Calculator",_buttonStyle)) {
				Calculator.Instance.ToggleWindow();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
			if (Spreadsheet.Instance != null && GUILayout.Button ("Spreadsheet", _buttonStyle)) {
				Spreadsheet.Instance.ToggleWindow ();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
			GUILayout.EndVertical ();
		}

		/// <summary>
		/// Initializes the styles.
		/// </summary>
		private void InitStyles()
		{
			_windowStyle = KompLogStyle.Instance.Window;
			_labelStyle = KompLogStyle.Instance.Label;
			_centeredLabelStyle = KompLogStyle.Instance.CenteredLabel;
			_buttonStyle = KompLogStyle.Instance.Button;
			_hasInitStyles = true;
		}

	}
}

