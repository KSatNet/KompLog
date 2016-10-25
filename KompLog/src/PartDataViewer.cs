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
	internal class PartData
	{
		public float DryMass { get { return _dryMass; }}
		private float _dryMass;

		public float ResourceMass { get { return _resMass; } }
		private float _resMass;

		public string Title { get { return _title; } }
		private string _title;

		public int Stage { get { return _stage; } }
		private int _stage;

		public PartData(Part pt)
		{
			_title = pt.partInfo.title;
			_dryMass = pt.mass;
			_resMass = pt.GetResourceMass ();
			_stage = Util.DetermineStage (pt);
		}
	}
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny, false)]
	public class PartDataViewer:MonoBehaviour
	{
		static public PartDataViewer Instance
		{
			get { return _instance; }
		}

		private bool 		_active = false;
		private Rect 		_windowPos = new Rect();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle	_buttonStyle;
		private GUIStyle	_scrollStyle;
		private bool 		_hasInitStyles 	= false;
		private int 		_winID;
		private Vector2		_scrollPos = new Vector2();
		static private PartDataViewer _instance = null;
		private PartDataComparer.CompareType _type = PartDataComparer.CompareType.MASS;
		private bool		_ascend = true;

		class PartDataComparer:IComparer<PartData>
		{
			public enum CompareType
			{
				MASS,
				STAGE,
				TITLE,
				RES_MASS
			}
			private CompareType _type;
			private bool 		_ascend;

			public PartDataComparer(bool ascend = true, CompareType type=CompareType.MASS)
			{
				_type = type;
				_ascend = ascend;
			}
			public int Compare(PartData x, PartData y)
			{
				if (!_ascend) {
					PartData tmp = x;
					x = y;
					y = tmp;
				}
				if (_type == CompareType.TITLE) {
					return x.Title.CompareTo (y.Title);
				}
				if (_type == CompareType.RES_MASS) {
					return x.ResourceMass.CompareTo (y.ResourceMass);
				}
				float xTotal = x.DryMass + x.ResourceMass;
				float yTotal = y.DryMass + y.ResourceMass;
				if (_type == CompareType.STAGE) {
					int stageCompare = x.Stage.CompareTo (y.Stage);
					if (stageCompare != 0)
						return stageCompare;
				}
				return yTotal.CompareTo (xTotal);
			}
		}

		private List<PartData> _partData = new List<PartData>();

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start()
		{
			if (_instance)
				Destroy (_instance);
			_instance = this;
			_winID = GUIUtility.GetControlID (FocusType.Passive);
			GameEvents.onEditorShipModified.Add (EditorUpdate);
			GameEvents.onStageActivate.Add (StageActivate);
			GameEvents.onVesselChange.Add (VesselChange);
			GameEvents.onPartDestroyed.Add (PartDestroyed);
			GameEvents.onVesselWasModified.Add (VesselChange);
		}

		/// <summary>
		/// Called when destorying this instance.
		/// </summary>
		public void OnDestroy()
		{
			ControlUnlock ();
			GameEvents.onEditorShipModified.Remove (EditorUpdate);
			GameEvents.onStageActivate.Remove (StageActivate);
			GameEvents.onVesselChange.Remove (VesselChange);
			GameEvents.onPartDestroyed.Remove (PartDestroyed);
			GameEvents.onVesselWasModified.Remove (VesselChange);
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
				DataUpdate ();
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
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK|ControlTypes.EDITOR_EDIT_STAGES, "KompLog_PartData");
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("KompLog_PartData");
		}

		/// <summary>
		/// Callback for event reports.
		/// </summary>
		/// <param name="rpt">Report.</param>
		private void EventReport(EventReport rpt)
		{
			FlightUpdate ();
		}

		/// <summary>
		/// Callback for a destroyed part.
		/// </summary>
		/// <param name="pt">Part.</param>
		private void PartDestroyed(Part pt)
		{
			FlightUpdate ();
		}

		/// <summary>
		/// Callback for a vessel change.
		/// </summary>
		/// <param name="vs">Vessel.</param>
		private void VesselChange(Vessel vs)
		{
			FlightUpdate ();
		}

		/// <summary>
		/// Stage activation callback.
		/// </summary>
		/// <param name="stage">Stage.</param>
		private void StageActivate(int stage)
		{
			FlightUpdate ();
		}

		/// <summary>
		/// Callback for flight changes that affect stage data. Updates the stage data.
		/// </summary>
		private void FlightUpdate ()
		{
			if (FlightGlobals.ActiveVessel == null)
				return;
			UpdateData (FlightGlobals.ActiveVessel.Parts);
		}

		/// <summary>
		/// Callback for editor changes. Updates the stage data.
		/// </summary>
		/// <param name="ship">Ship.</param>
		private void EditorUpdate(ShipConstruct ship)
		{
			if (ship == null)
				return;
			UpdateData (ship.Parts);
		}

		/// <summary>
		/// Update the data.
		/// </summary>
		private void DataUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight) {
				FlightUpdate ();
			} else if (HighLogic.LoadedSceneIsEditor) {
				EditorUpdate (EditorLogic.fetch.ship);
			}
		}

		/// <summary>
		/// Updates the data.
		/// </summary>
		/// <param name="parts">Parts.</param>
		private void UpdateData(List<Part> parts)
		{
			_partData.Clear ();
			foreach (Part part in parts) {
				_partData.Add (new PartData (part));
			}
			_partData.Sort (new PartDataComparer (_ascend, _type));
		}

		/// <summary>
		/// Drawing callback for the main window.
		/// </summary>
		private void OnDraw()
		{
			if (!_hasInitStyles)
				InitStyles ();
			_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Part Data",_windowStyle);
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
		/// Draws the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Title", _labelStyle, GUILayout.MinWidth (150.0f))) {
				if (_type == PartDataComparer.CompareType.TITLE) {
					_ascend = !_ascend;
				} else {
					_ascend = true;
					_type = PartDataComparer.CompareType.TITLE;
				}
				DataUpdate ();
			}
			if (GUILayout.Button ("Mass", _labelStyle, GUILayout.MinWidth (30.0f))) {
				if (_type == PartDataComparer.CompareType.MASS) {
					_ascend = !_ascend;
				} else {
					_ascend = true;
					_type = PartDataComparer.CompareType.MASS;
				}
				DataUpdate ();
			}
			GUILayout.Label ("Dry", _labelStyle,GUILayout.MinWidth(30.0f));
			if (GUILayout.Button ("Res", _labelStyle, GUILayout.MinWidth (30.0f))) {
				if (_type == PartDataComparer.CompareType.RES_MASS) {
					_ascend = !_ascend;
				} else {
					_ascend = true;
					_type = PartDataComparer.CompareType.RES_MASS;
				}
				DataUpdate ();
			}
			if (GUILayout.Button ("Stage", _labelStyle, GUILayout.MinWidth (30.0f))) {
				if (_type == PartDataComparer.CompareType.STAGE) {
					_ascend = !_ascend;
				} else {
					_ascend = true;
					_type = PartDataComparer.CompareType.STAGE;
				}
				DataUpdate ();
			}

			GUILayout.EndHorizontal ();
			_scrollPos = GUILayout.BeginScrollView(_scrollPos,_scrollStyle, GUILayout.MinWidth (450.0f), GUILayout.Height (300.0f) );
			foreach (PartData part in _partData) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label (part.Title, _labelStyle, GUILayout.MinWidth(150.0f));

				GUILayout.Label ("" + (part.DryMass+part.ResourceMass), _labelStyle,GUILayout.MinWidth(30.0f));
				GUILayout.Label ("" + part.DryMass, _labelStyle,GUILayout.MinWidth(30.0f));
				GUILayout.Label ("" + part.ResourceMass, _labelStyle,GUILayout.MinWidth(30.0f));
				GUILayout.Label ("" + part.Stage, _labelStyle,GUILayout.MinWidth(30.0f));

				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Refresh", _buttonStyle)) {
				DataUpdate ();
			}
			if (GUILayout.Button ("Close", _buttonStyle)) {
				ToggleWindow ();
			}
			GUILayout.EndHorizontal ();
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
			_buttonStyle = KompLogStyle.Instance.Button;
			_scrollStyle = KompLogStyle.Instance.ScrollView;
			_hasInitStyles = true;
		}
	}
}
