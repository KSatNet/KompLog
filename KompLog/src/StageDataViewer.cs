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
	internal class EngineData
	{
		private float _asl = 0.0f;
		private float _vac = 0.0f;
		private float _current = 0.0f;
		private float _fuelFlowRate = 0.0f;
		private float _fuelDensity = 0.0f;
		private float _g = 9.80665f;

		public float VacuumIsp { get { return _vac; } }
		public float ASLIsp { get { return _asl; } }
		public float CurrentIsp { get { return _current; } }
		public float FuelFlowRate  { get { return _fuelFlowRate; } }
		public float FuelDensity { get { return _fuelDensity; } }
		public float g0 { get { return _g; } }

		public EngineData(ModuleEngines engine)
		{
			_current = engine.realIsp;
			_vac = engine.atmosphereCurve.Evaluate (0f);
			_asl = engine.atmosphereCurve.Evaluate (1f);
			_fuelFlowRate = engine.maxFuelFlow;
			_fuelDensity = engine.mixtureDensity;
			_g = engine.g;
		}


	}

	internal class ResourceData
	{
		private float _mass = 0.0f;
		public float Mass { get { return _mass; } }

		private float _maxMass = 0.0f;
		public float MaxMass { get { return _maxMass; } }

		private string _name = "";
		public string Name { get { return _name; } }

		public ResourceData(String name)
		{
			_name = name;
		}

		public void Add(PartResource res)
		{
			PartResourceDefinition def = res.info;
			if (def != null && def.density != 0.0f) {
				_mass += (float)res.amount * def.density;
				_maxMass += (float)res.maxAmount * def.density;
			}
		}
	}

	internal class StageData
	{
		private int _stage = 0;
		private float _dryMass = 0.0f;
		private float _resMass = 0.0f;
		private int _partCount = 0;
		private SortedList<string, float> _resources = new SortedList<string, float>();
		private SortedList<string, float> _maxresources = new SortedList<string, float>();
		private SortedList<string, ResourceData> _resData = new SortedList<string, ResourceData>();
		private SortedList<string, EngineData> _engineData = new SortedList<string, EngineData> ();

		public StageData(int stage)
		{
			_stage = stage;
		}

		public void AddPartData(Part pt)
		{
			_dryMass += pt.mass;
			_resMass += pt.GetResourceMass ();
			_partCount++;
			foreach (PartResource resource in pt.Resources) {
				PartResourceDefinition def = resource.info;
				if (def != null && def.density != 0) {
					string res = def.name;
					float mass = (float)resource.amount * def.density;
					if (!_resData.ContainsKey (res)) {
						_resData.Add (res, new ResourceData (res));
						_resources.Add (res, 0.0f);
					}
					_resData [res].Add (resource);
					_resources [res] += mass;
				}
			}
			List<ModuleEngines> eng = pt.FindModulesImplementing<ModuleEngines> ();
			foreach (ModuleEngines engine in eng) {
				string engId = pt.partInfo.title;
				if (eng.Count > 1) {
					engId +="-"+ engine.engineID;
				}
				if (!_engineData.ContainsKey (engId)) {
					_engineData.Add(engId,new EngineData(engine));
				}
			}
		}

		public float DryMass
		{
			get { return _dryMass; }
		}
		public float ResourceMass
		{
			get { return _resMass; }
		}
		public int PartCount
		{
			get { return _partCount; }
		}
		public SortedList<string, float> ResourceMassList
		{
			get { return _resources; }
		}
		public SortedList<string, ResourceData> ResourceDataList
		{
			get { return _resData; }
		}
		public SortedList<string, EngineData> EngineDataList
		{
			get { return _engineData; }
		}
	}

	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny, false)]
	public class StageDataViewer:MonoBehaviour
	{
		static public StageDataViewer Instance
		{
			get { return _instance; }
		}

		private bool 		_active = false;
		private Rect 		_windowPos = new Rect();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle 	_totalLabelStyle;
		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle 	_rightLabelStyle;
		private GUIStyle	_buttonStyle;
		private GUIStyle	_scrollStyle;
		private bool 		_hasInitStyles 	= false;
		private int 		_winID;
		private Vector2		_scrollPos = new Vector2();
		static private StageDataViewer _instance = null;
		private SortedList<int,StageData> _data = new SortedList<int,StageData>();
		private SortedList<int,bool> _detail = new SortedList<int,bool>();

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
		/// Called when the event was destroyed.
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
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK|ControlTypes.EDITOR_EDIT_STAGES, "KompLog_ResViewer");
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("KompLog_ResViewer");
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
			_windowPos.height = 0.0f;
			_data.Clear ();
			StageData total = new StageData (-1);
			_data.Add (-1, total);
			if (!_detail.ContainsKey (-1)) {
				_detail.Add (-1, false);
			}
			foreach (Part part in parts) {
				int stage = Util.DetermineStage(part);
				StageData entry = null;
				if (_data.ContainsKey (stage)) {
					entry = _data[stage];
				} else {
					entry = new StageData(stage);
					_data.Add (stage, entry);
				}
				if (!_detail.ContainsKey (stage)) {
					_detail.Add (stage, false);
				}
				entry.AddPartData (part);
				total.AddPartData (part);
			}
		}

		/// <summary>
		/// Drawing callback for the main window.
		/// </summary>
		private void OnDraw()
		{
			if (!_hasInitStyles)
				InitStyles ();
			_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Stage Data",_windowStyle);
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
		/// Draw the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Stage", _rightLabelStyle, GUILayout.Width (75.0f));
			GUILayout.Label ("Dry Mass: ", _rightLabelStyle, GUILayout.Width(75.0f));
			GUILayout.Label ("Resources: ", _rightLabelStyle, GUILayout.Width(75.0f));
			GUILayout.Label ("Full Mass: ",	_rightLabelStyle, GUILayout.Width(75.0f));
			GUILayout.Label ("Payload: ",	_rightLabelStyle, GUILayout.Width(75.0f));
			GUILayout.EndHorizontal ();

			_scrollPos = GUILayout.BeginScrollView(_scrollPos,_scrollStyle, GUILayout.MinWidth (450.0f), GUILayout.Height (200.0f) );
			GUILayout.BeginVertical (GUILayout.MinWidth(400.0f));
			double payload = 0.0d;
			foreach (KeyValuePair<int, StageData> kvp in _data) {
				GUILayout.BeginHorizontal ();
				GUIStyle style = _rightLabelStyle;
				string label = "" + kvp.Key;
				if (kvp.Key == -1) {
					style = _totalLabelStyle;
					label = "Total";
				}
				GUILayout.Label (label, style, GUILayout.Width (60.0f));
				GUILayout.Label (KompLogStyle.Instance.GetNumberString(kvp.Value.DryMass) + "t", style, GUILayout.Width(75.0f));
				GUILayout.Label (KompLogStyle.Instance.GetNumberString(kvp.Value.ResourceMass) +"t", style, GUILayout.Width(75.0f));
				GUILayout.Label (KompLogStyle.Instance.GetNumberString(kvp.Value.DryMass+kvp.Value.ResourceMass) +"t", 
					style, GUILayout.Width(75.0f));
				if (kvp.Key >= 0) {
					GUILayout.Label (KompLogStyle.Instance.GetNumberString (payload) + "t",	style, GUILayout.Width (75.0f));
					payload += kvp.Value.DryMass + kvp.Value.ResourceMass;
				} else {
					GUILayout.Label ("",	style, GUILayout.Width (75.0f));
				}
				if (kvp.Value.ResourceMass > 0 || kvp.Value.EngineDataList.Count > 0) {
					if (GUILayout.Button (_detail[kvp.Key] ? "-" : "+", _buttonStyle, GUILayout.Height(18.0f),GUILayout.Width (20.0f))) {
						// Re-calc height if we hide details.
						if (_detail[kvp.Key])
							_windowPos.height = 0.0f;
						_detail[kvp.Key] = !_detail[kvp.Key];
					}
				}
				GUILayout.EndHorizontal ();
				if (_detail[kvp.Key]) {
					SortedList<string, ResourceData> resList = kvp.Value.ResourceDataList;
					foreach (KeyValuePair<string, ResourceData> rmkvp in resList) {
						if (rmkvp.Value.Mass < 0.01f && rmkvp.Value.MaxMass < 0.01f)
							continue;
						GUILayout.BeginHorizontal ();
						GUILayout.Label ("", _labelStyle, GUILayout.MinWidth (10.0f));
						GUILayout.Label (rmkvp.Key + ":", _labelStyle, GUILayout.MinWidth (140.0f));
						GUILayout.Label (KompLogStyle.Instance.GetNumberString(rmkvp.Value.Mass) + "t", _labelStyle, GUILayout.MinWidth (125.0f));
						GUILayout.Label (KompLogStyle.Instance.GetNumberString(rmkvp.Value.MaxMass) + "t", _labelStyle, GUILayout.MinWidth (125.0f));
						GUILayout.EndHorizontal ();
					}
					SortedList<string, EngineData> engIspList = kvp.Value.EngineDataList;
					foreach (KeyValuePair<string, EngineData> edkvp in engIspList) {

						GUILayout.BeginHorizontal ();
						GUILayout.Label (edkvp.Key + ":", _labelStyle, GUILayout.MinWidth (220.0f));
						GUILayout.Label (KompLogStyle.Instance.GetNumberString(edkvp.Value.VacuumIsp) + "s(vac)", _rightLabelStyle, GUILayout.MinWidth (85.0f));
						GUILayout.Label (KompLogStyle.Instance.GetNumberString(edkvp.Value.ASLIsp) + "s(asl)", _rightLabelStyle, GUILayout.MinWidth (85.0f));
						GUILayout.EndHorizontal ();
						GUILayout.BeginHorizontal ();

						GUILayout.Label ("", _labelStyle, GUILayout.MinWidth (10.0f));
						float engineConstant = edkvp.Value.FuelFlowRate * edkvp.Value.g0;
						GUILayout.Label ("Thrust:" + (engineConstant*edkvp.Value.VacuumIsp).ToString("0.0##") + " kN(vac)", _rightLabelStyle,
							GUILayout.MinWidth(210.0f));
						GUILayout.Label ("" + (engineConstant*edkvp.Value.ASLIsp).ToString("0.0##") + " kN(asl)", _rightLabelStyle,
							GUILayout.MinWidth(180.0f));
						GUILayout.EndHorizontal ();


					}
				}
			}
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button("Refresh",_buttonStyle)) {
				DataUpdate();
			}
			if (GUILayout.Button ("Close", _buttonStyle)) {
				// This should close the window since it by definition can only be pressed while visible.
				ToggleWindow ();
			}
			GUILayout.EndHorizontal ();
			GUI.DragWindow ();
		}

		/// <summary>
		/// Initializes the styles.
		/// </summary>
		private void InitStyles()
		{
			_windowStyle = KompLogStyle.Instance.Window;
			_labelStyle = KompLogStyle.Instance.Label;
			_totalLabelStyle = new GUIStyle (KompLogStyle.Instance.RightLabel);
			_totalLabelStyle.fontStyle = FontStyle.BoldAndItalic;
			_centeredLabelStyle = KompLogStyle.Instance.CenteredLabel;
			_rightLabelStyle = KompLogStyle.Instance.RightLabel;
			_buttonStyle = KompLogStyle.Instance.Button;
			_scrollStyle = KompLogStyle.Instance.ScrollView;
			_hasInitStyles = true;
		}
	}
}

