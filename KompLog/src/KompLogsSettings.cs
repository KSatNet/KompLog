/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using UnityEngine;
using KSP.IO;

namespace KompLog
{
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny | KSPAddonImproved.Startup.TrackingStation, false)]
	public class KompLogSettings: MonoBehaviour
	{
		static public KompLogSettings Instance
		{
			get { return _instance; }
		}
		static private KompLogSettings _instance = null;
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_textFieldStyle;
		private GUIStyle	_textAreaStyle;
		private GUIStyle	_buttonStyle;
		private bool 		_hasInitStyles 	= false;
		private bool		_active = false;
		static private Rect _windowPos = new Rect();
		private bool _openFlightDataOnDestroyed = true;
		public bool OpenFlightDataOnVesselDeath {
			get { return _openFlightDataOnDestroyed; }
		}
		private bool _autoHide = true;
		public bool AutoHide { get { return _autoHide; } }


		private int _winID = 0;

		/// <summary>
		/// Toggles the window.
		/// </summary>
		public void ToggleWindow()
		{
			_active = !_active;
			if (_active)
				RenderingManager.AddToPostDrawQueue (0, OnDraw);
			else
				RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
		}

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start()
		{
			if (_instance)
				Destroy (_instance);
			_instance = this;
			_winID = GUIUtility.GetControlID (FocusType.Passive);
			PluginConfiguration config = PluginConfiguration.CreateForType<KompLogSettings> ();
			config.load ();
			_openFlightDataOnDestroyed = config.GetValue<bool> ("OpenFlightData", true);
			_autoHide = config.GetValue<bool> ("AutoHide", true);
		}

		/// <summary>
		/// Called when destroying this instance.
		/// </summary>
		public void OnDestroy()
		{

			if (_instance == this)
				_instance = null;
			PluginConfiguration config = PluginConfiguration.CreateForType<KompLogSettings> ();
			config.load ();
			config.SetValue ("OpenFlightData", _openFlightDataOnDestroyed);
			config.SetValue ("AutoHide", _autoHide);
			config.save ();
		}

		/// <summary>
		/// Draw the window.
		/// </summary>
		public void OnDraw()
		{
			if (!_hasInitStyles)
				InitStyles ();
			if (_active) {
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Settings", _windowStyle);
				if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
					_windowPos.y = Screen.height * 0.5f - _windowPos.height * 0.5f;
					_windowPos.x = 50.0f;
				}
			}
		}

		/// <summary>
		/// Draw the main window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical ();
			GUILayout.Label ("Plugin: " + typeof(KompLogSettings).Assembly.GetName ().Name, _labelStyle);
			GUILayout.Label ("Version: "+ Util.VERSION, _labelStyle);
			GUILayout.Label ("Build: "+ typeof(KompLogSettings).Assembly.GetName ().Version, _labelStyle);
			_openFlightDataOnDestroyed = GUILayout.Toggle (_openFlightDataOnDestroyed, "Open Flight Recorder Data on Vessel Destruction");
			_autoHide = GUILayout.Toggle (_autoHide, "Auto Hide Utilities Launcher");
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		/// <summary>
		/// Initializes the styles.
		/// </summary>
		private void InitStyles()
		{
			_windowStyle = KompLogStyle.Instance.Window;
			_labelStyle = KompLogStyle.Instance.Label;
			_centeredLabelStyle = KompLogStyle.Instance.CenteredLabel;
			_textFieldStyle = KompLogStyle.Instance.TextField;
			_textAreaStyle = KompLogStyle.Instance.TextArea;
			_buttonStyle = KompLogStyle.Instance.Button;

			_hasInitStyles = true;
		}
	}
}

