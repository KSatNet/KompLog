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
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny | KSPAddonImproved.Startup.TrackingStation, false)]
	public class OrbitData:MonoBehaviour
	{
		static public OrbitData Instance
		{
			get { return _instance; }
		}
		static private OrbitData _instance = null;	

		private bool 		_active = false;
		private Rect 		_windowPos = new Rect();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_buttonStyle;
		private GUIStyle	_scrollStyle;
		private bool 		_hasInitStyles 	= false;
		private int 		_winID;
		private int 		_menuSelection = 0;
		private Vector2		_scrollPos = new Vector2();
		private Dictionary<CelestialBody,bool> _expanded = new Dictionary<CelestialBody,bool>();
		private Orbit 		_orbit = null;
		private IDiscoverable _orbiter = null;
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
		/// Called when destroying this instance.
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
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK|ControlTypes.EDITOR_EDIT_STAGES, "KompLog_OrbitData");
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("KompLog_OrbitData");
		}

		/// <summary>
		/// Drawing callback for the main window.
		/// </summary>
		private void OnDraw()
		{
			if (!_hasInitStyles)
				InitStyles ();
			_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Orbit Data",_windowStyle);
			if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
				_windowPos.y = Screen.height * 0.5f - _windowPos.height * 0.5f;
				_windowPos.x = Screen.width  * 0.5f;
			}
			if (_windowPos.Contains (Event.current.mousePosition)) {
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
			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();

			GUILayout.BeginVertical ();
			int oldMenu = _menuSelection;
			_menuSelection = GUILayout.SelectionGrid (_menuSelection, 
				new string[]{ "Vessels", "Celestial Bodies", "Kerbals", "Asteroids" }, 2, _buttonStyle,
				GUILayout.MinWidth(300.0f));
			if (oldMenu != _menuSelection) {
				_windowPos.height = 0.0f;
				_scrollPos.Set (0.0f, 0.0f);
			}
			_scrollPos = GUILayout.BeginScrollView(_scrollPos,_scrollStyle, GUILayout.MinWidth (300.0f), GUILayout.Height (300.0f) );
			if (_menuSelection == 0) {
				DrawVesselSelect (VesselType.Base, VesselType.Lander, VesselType.Probe, VesselType.Rover, VesselType.Ship,
					VesselType.Station);
			} else if (_menuSelection == 1) {
				DrawCelestialBodySelect ();
			} else if (_menuSelection == 2) {
				DrawVesselSelect (VesselType.EVA);
			} else if (_menuSelection == 3) {
				DrawVesselSelect (VesselType.SpaceObject);
			}
			GUILayout.EndScrollView ();
			GUILayout.EndVertical ();

			GUILayout.BeginVertical (GUILayout.MinWidth(250.0f));
			DisplayOrbit (_orbit, _orbiter);
			GUILayout.EndVertical ();

			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Close", _buttonStyle)) {
				// This should close the window since it by definition can only be pressed while visible.
				ToggleWindow ();
			}
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		/// <summary>
		/// Draws the vessel select GUI for the specified vessel type(s).
		/// </summary>
		/// <param name="type">Type - Vessel types to show.</param>
		private void DrawVesselSelect (params VesselType[] type)
		{
			foreach (Vessel v in FlightGlobals.Vessels) {
				if ((v.DiscoveryInfo.Level < DiscoveryLevels.Name && v.DiscoveryInfo.Level != DiscoveryLevels.Owned) ||
				    Array.IndexOf (type, v.vesselType) == -1) {
					continue;
				}
				if (GUILayout.Button(v.RevealName(),_buttonStyle)) {
					_orbit = v.orbit;
					_orbiter = v;
				}
			}
		}

		/// <summary>
		/// Draws the celestial body select GUI.
		/// </summary>
		private void DrawCelestialBodySelect()
		{
			CelestialBody sun = PSystemManager.Instance.sun.sun;
			// Create with the sun intially expanded
			if (!_expanded.ContainsKey (sun)) {
				_expanded.Add (sun, true);
			}
			DrawCelestialBodyGUI (sun, 0);
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
			if (GUILayout.Button(body.RevealName(),_buttonStyle)) {
				_orbit = body.orbit;
				_orbiter = body;
			}
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
		/// Displays the orbit.
		/// </summary>
		/// <param name="obt">Orbit.</param>
		/// <param name="orbiter">Orbiter.</param>
		private void DisplayOrbit(Orbit obt, IDiscoverable orbiter)
		{
			if (obt == null || orbiter == null) {
				GUILayout.Label ("", _labelStyle);
				return;
			}

			GUILayout.Label (orbiter.RevealName (), _centeredLabelStyle);
			GUILayout.Label ("", _labelStyle);
			GUILayout.Label ("Apoapsis:"+KompLogStyle.Instance.GetNumberString(obt.ApA)+"m",_labelStyle);
			GUILayout.Label ("Periapsis:"+KompLogStyle.Instance.GetNumberString(obt.PeA)+"m",_labelStyle);
			GUILayout.Label ("Semi-major Axis:" + KompLogStyle.Instance.GetNumberString(obt.semiMajorAxis)+"m", _labelStyle);
			GUILayout.Label ("Semi-minor Axis:" + KompLogStyle.Instance.GetNumberString(obt.semiMinorAxis)+"m", _labelStyle);
			GUILayout.Label ("Semi Latus Rectum:" + KompLogStyle.Instance.GetNumberString(obt.semiLatusRectum)+"m", _labelStyle);
			GUILayout.Label ("Inclination:"+obt.inclination.ToString("0.00##") + "°", _labelStyle);
			GUILayout.Label ("Longitude of AN:" + obt.LAN.ToString("0.00##") + "°", _labelStyle);
			GUILayout.Label ("Argument of Periapsis:" + obt.argumentOfPeriapsis.ToString ("0.00##") + "°", _labelStyle);
			GUILayout.Label ("Time to Periapsis:" + KompLogStyle.Instance.GetTimeString (obt.timeToPe),_labelStyle);
			GUILayout.Label ("Time to Periapsis:" + obt.timeToPe.ToString ("0.00") + " s", _labelStyle);
			GUILayout.Label ("Eccentricity:" + obt.eccentricity.ToString("g4"), _labelStyle);
			if (obt.referenceBody != null) {
				if (GUILayout.Button ("Reference Body:" + obt.referenceBody.RevealName (), _buttonStyle)) {
					_orbit = obt.referenceBody.orbit;
					_orbiter = obt.referenceBody;
				}
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

