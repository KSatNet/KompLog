/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace KompLog
{
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny, false)]
	public class CelestialBodyData: MonoBehaviour
	{
		static public CelestialBodyData Instance
		{
			get { return _instance; }
		}
		static private CelestialBodyData _instance = null;

		private bool 		_active = false;
		private Rect 		_windowPos = new Rect();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_buttonStyle;
		private GUIStyle	_scrollStyle;
		private bool 		_hasInitStyles 	= false;
		private int 		_winID;
		private Vector2		_scrollPos = new Vector2();
		private Dictionary<CelestialBody,bool> _expanded = new Dictionary<CelestialBody,bool>();
		private CelestialBody _body = null;

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start()
		{
			if (_instance)
				Destroy (_instance);
			_instance = this;
			_winID = GUIUtility.GetControlID (FocusType.Passive);
		}

		/// <summary>
		/// Callback when this instance is destroyed.
		/// </summary>
		public void OnDestroy()
		{
			ControlUnlock ();
			if (_instance == this)
				_instance = null;
		}

		/// <summary>
		/// Toggles the window visibility.
		/// </summary>
		public void ToggleWindow()
		{
			_active = !_active;
			if (_active) {
				RenderingManager.AddToPostDrawQueue (0, OnDraw);
			} else {
				RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
				Invoke ("ControlUnlock", 1);
			}
		}

		/// <summary>
		/// Lock the Controls.
		/// </summary>
		private void ControlLock()
		{
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK|ControlTypes.EDITOR_EDIT_STAGES, "KompLog_CelestialData");
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("KompLog_CelestialData");
		}


		/// <summary>
		/// Drawing callback for the main window.
		/// </summary>
		private void OnDraw()
		{
			if (!_hasInitStyles)
				InitStyles ();
			_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Celestial Data",_windowStyle);
			if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
				_windowPos.y = Screen.height * 0.5f - _windowPos.height * 0.5f;
				_windowPos.x = Screen.width - _windowPos.width - 50.0f;
			}
			if (_windowPos.Contains (Event.current.mousePosition)) {
				ControlLock ();
			} else {
				ControlUnlock();
			}
		}

		/// <summary>
		/// Build the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical ();
			if (_body != null) {
				GUILayout.Label ("Celestial Body: " + _body.RevealName (),_labelStyle);
				GUILayout.Label ("Surface Gravity: " + 
					(_body.gMagnitudeAtCenter/(Math.Pow(_body.Radius,2))).ToString("G3") +" m/s²", _labelStyle);
				GUILayout.Label ("Mass: " + KompLogStyle.Instance.GetNumberString(_body.Mass)+"g", _labelStyle);
				GUILayout.Label ("GM (μ): " + _body.gravParameter.ToString("e6")+" m³/s²", _labelStyle);
				GUILayout.Label ("Radius: " + KompLogStyle.Instance.GetNumberString(_body.Radius)+"m", _labelStyle);
				if (_body.atmosphere) {
					GUILayout.Label ("Atmosphere",_centeredLabelStyle);
					GUILayout.Label ("Oxygen:" + (_body.atmosphereContainsOxygen ? "Yes" : "No"), _labelStyle);
					GUILayout.Label ("Height:" + KompLogStyle.Instance.GetNumberString(_body.atmosphereDepth)+"m", _labelStyle);
				}
			}
			_scrollPos = GUILayout.BeginScrollView(_scrollPos,_scrollStyle, GUILayout.MinWidth (300.0f), GUILayout.Height (250.0f) );
			CelestialBody sun = PSystemManager.Instance.sun.sun;
			GUILayout.BeginVertical ();
			// Create with the sun intially expanded
			if (!_expanded.ContainsKey (sun)) {
				_expanded.Add (sun, true);
			}
			DrawCelestialBodyGUI (sun, 0);
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			if (GUILayout.Button ("Close", _buttonStyle)) {
				// This should close the window since it by definition can only be pressed while visible.
				ToggleWindow ();
			}
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}


		/// <summary>
		/// Draws the celestial body GUI.
		/// </summary>
		/// <param name="body">Celestial Body.</param>
		/// <param name="depth">Depth.</param>
		private void DrawCelestialBodyGUI(CelestialBody body, int depth)
		{
			if (!_expanded.ContainsKey (body)) {
				_expanded.Add (body, false);
			}
			bool nextLevel = body.orbitingBodies.Count > 0;
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", _labelStyle, GUILayout.Width (20.0f * depth));
			DrawBodyData (body);
			if (nextLevel) {
				if (GUILayout.Button (_expanded [body] ? "-" : "+", _buttonStyle,GUILayout.Width(20.0f))) {
					_expanded [body] = !(_expanded [body]);
				}
			}
			GUILayout.EndHorizontal ();
			if (_expanded [body]) {
				foreach (CelestialBody child in body.orbitingBodies) {
					DrawCelestialBodyGUI (child, depth + 1);
				}
			}
		}

		/// <summary>
		/// Draws the body data button.
		/// </summary>
		/// <param name="body">Body.</param>
		private void DrawBodyData(CelestialBody body)
		{
			if (GUILayout.Button (body.RevealName (), _buttonStyle)) {
				_body = body;
				_windowPos.height = 0.0f;
			}
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
			_scrollStyle = KompLogStyle.Instance.ScrollView;
			_hasInitStyles = true;
		}


	}
}

