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
	/// <summary>
	/// Calculator class.
	/// </summary>
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny | KSPAddonImproved.Startup.TrackingStation, false)]
	public class Calculator: MonoBehaviour
	{
		static public Calculator Instance
		{
			get { return _instance; }
		}
		const int MAX_HISTORY = 10;

		static private Calculator _instance = null;
		private bool	 		_active = false;
		private bool 			_simple = true;
		private GUIStyle 		_windowStyle;
		private GUIStyle	 	_labelStyle;
		private GUIStyle 		_centeredLabelStyle;
		private GUIStyle		_textFieldStyle;
		private GUIStyle		_textAreaStyle;
		private GUIStyle		_buttonStyle;
		private bool 			_hasInitStyles 	= false;
		private string			_equation = "";
		private string			_xmin = "";
		private string			_xmax = "";
		private List<string>	_history = new List<string>();
		static private Rect 	_windowPos = new Rect(0f,30f,0f,0f);
		static private string 	_globalScratchPad = "";
		private Graph			_graph = null;
		private Equation		_geq = null;
		private double 			_gxmin = 0.0d;
		private double			_gxmax = 0.0d;
		private double 			_ymin = 0.0d;
		private double 			_ymax = 0.0d;
		private int 			_menuSelection = 0;
		private int 			_winID;
		private int 			_hIdx = -1;
		private int 			_width = 0;
		private const string SUPPORTED = 
			"Constants\n"+
			"pi,e\n\n"+
			"Math Functions:\n"+
			"ln,sin,cos,tan,asin,acos,atan,sinh,cosh,tanh,\n"+
			"sqrt\n"+
			"NOTE: trig terms are in radians";
		private const string CHEAT_SHEET = 
			"delta-v = ln(start_mass/end_mass) * 9.81 * isp\n\n" +
			"isp = (F1 + ... + Fn)/(F1/isp1 + ... + Fn/ispn)\n\n" +
			"TWR = total_thrust/(total_mass*g)\n\n" +
			"burn-time = (start_mass * 9.81 * isp / Thrust) *\n"+
			"             (1 - e^(-delta-v/(9.81 * isp))) \n\n"+
			"radians = degrees * pi/180\n\n" +
			SUPPORTED;
		private const string HELP =
			"To calculate a value write an equation in the 'Equation Input' field\n" +
			"and press 'Calculate'.\n\n"+
			"To graph select the 'Graph' tab, type in an minimum and maximum X value,\n"+
			"write an equation in the 'Equation Input' field, and press 'Graph'.\n"+
			"The equation should have at least one x term.\n\n"+
			SUPPORTED;

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start()
		{
			if (_instance)
				Destroy (_instance);
			_instance = this;
			string pos = "CalcWindowPos";
			if (HighLogic.LoadedSceneIsFlight) {
				pos = "FlightCalcWindowPos";
			}
			_winID = GUIUtility.GetControlID (FocusType.Passive);
			PluginConfiguration config = PluginConfiguration.CreateForType<KompLogSettings> ();
			config.load ();
			_windowPos = config.GetValue<Rect> (pos,new Rect(0f,30f,0f,0f));
			_windowPos.width = 0.0f;
			_windowPos.height = 0.0f;
			_globalScratchPad = config.GetValue<string> ("CalcGlobalNotes","");
			_simple = config.GetValue<bool> ("CalcSimple", true);
			_width = config.GetValue<int> ("CalcWidth", 0);
		}

		/// <summary>
		/// Called when this instance is destroyed.
		/// </summary>
		public void OnDestroy()
		{
			if (_active)
				Hide ();
			if (_instance == this)
				_instance = null;
			PluginConfiguration config = PluginConfiguration.CreateForType<KompLogSettings> ();
			config.load ();
			string pos = "CalcWindowPos";
			if (HighLogic.LoadedSceneIsFlight) {
				pos = "FlightCalcWindowPos";
			}
			config.SetValue (pos, _windowPos);
			config.SetValue ("CalcGlobalNotes", _globalScratchPad);
			config.SetValue ("CalcSimple", _simple);
			config.SetValue ("CalcWidth", _width);

			config.save ();
		}

		/// <summary>
		/// Sets the simple/complex state.
		/// </summary>
		/// <param name="simple">If set to <c>true</c> simple.</param>
		public void SetSimple(bool simple)
		{
			_simple = simple;
		}

		/// <summary>
		/// Show this instance.
		/// </summary>
		public void Show()
		{
			if (!_hasInitStyles) InitStyles();
			RenderingManager.AddToPostDrawQueue (0, OnDraw);
			_active = true;
		}

		/// <summary>
		/// Hide this instance.
		/// </summary>
		public void Hide()
		{
			_active = false;
			RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
			Invoke ("ControlUnlock", 1);
		}

		/// <summary>
		/// Toggles the window visibility.
		/// </summary>
		public void ToggleWindow()
		{
			_active = !_active;
			if (_active)
				Show ();
			else
				Hide ();
		}

		/// <summary>
		/// Drawing callback for the main window.
		/// </summary>
		private void OnDraw()
		{
			if (_active) {
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Calculator",_windowStyle);
			}
			if (_windowPos.Contains (Event.current.mousePosition) && _active) {
				ControlLock ();
			} else {
				ControlUnlock();
			}
		}

		/// <summary>
		/// Lock the Controls.
		/// </summary>
		private void ControlLock()
		{
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK|ControlTypes.EDITOR_EDIT_STAGES, "KompLog_Calc");
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("KompLog_Calc");
		}

		/// <summary>
		/// Calculate the equation.
		/// </summary>
		private void Calculate()
		{
			double value = 0.0d;
			try {
				Equation equation = Equation.ParseEquation (_equation);

				value = equation.GetValue ();
				_history.Add(_equation + "=" + value.ToString("G5"));
				_windowPos.height = 0.0f;
				_windowPos.width = 0.0f;

				if (_history.Count > MAX_HISTORY)
				{
					_history.RemoveRange(0, _history.Count-MAX_HISTORY);
				}
			} catch (ParsingException e) {
				Debug.Log (_equation + ":" + e.Message);
				_history.Add(_equation + "\n"+ e.Message);
			}
		}

		/// <summary>
		/// Graph the equation.
		/// </summary>
		private void Graph()
		{
			try {
				Equation equation = Equation.ParseEquation (_equation);
				Equation xmin = Equation.ParseEquation (_xmin);
				Equation xmax = Equation.ParseEquation (_xmax);
				_history.Add(_equation);

				double min = xmin.GetValue ();
				double max = xmax.GetValue ();

				double ymin = Double.NaN;
				double ymax = Double.NaN;

				_graph = new Graph (250, 250);
				double delta = (max - min) / 250.0d;
				if (max < min)
				{
					Debug.Log("Max Less Than Min:" + max + " < " + min);
					return;
				}
				for (double x = min; x <= max; x += delta) {
					double y = equation.GetValue (x);
					if (Double.IsNaN (y)) {
						continue;
					}
					if (Double.IsNaN (ymin) || y < ymin) {
						ymin = y;
					}
					if (Double.IsNaN (ymax) || y > ymax) {
						ymax = y;
					}
				}
				_ymax = ymax;
				_ymin = ymin;
				_gxmin = min;
				_gxmax = max;
				Debug.Log ("Y min:" + ymin + " max:" + ymax + " delta:" + delta);
				_graph.drawGraph (x => equation.GetValue (min + x * delta), ymax, ymin, 250);
				_geq = equation;
			} catch (ParsingException e) {
				Debug.Log (_equation + ":" + e.Message);
				_history.Add(_equation + "\n" + e.Message);
			}
		}

		/// <summary>
		/// Raises the window event.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginHorizontal (GUILayout.MinWidth(300.0f));
			GUILayout.BeginVertical (GUILayout.MaxWidth (300.0f+(_width*50.0f)));
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("History:", _labelStyle);
			if (GUILayout.Button ("Clear", _buttonStyle)) {
				_history.Clear();
				_windowPos.height = 0.0f;
				_windowPos.width = 0.0f;
			}
			GUILayout.EndHorizontal ();
			GUILayout.TextArea (String.Join("\n",_history.ToArray()), _textAreaStyle);
			GUILayout.BeginHorizontal (GUILayout.MinWidth(300.0f+(_width*50.0f)));
			GUILayout.Label ("Equation Input:", _labelStyle);
			if (GUILayout.Button ("Wider", _buttonStyle) && _width < 6) {
				_width++;
			}
			if (GUILayout.Button ("Narrower", _buttonStyle) && _width > 0) {
				_width--;
				_windowPos.width = 0.0f;
			}
			GUILayout.EndHorizontal ();
			GUI.SetNextControlName ("EquationIn");
			_equation = GUILayout.TextField (_equation,_textFieldStyle);
			if (Event.current.isKey && GUI.GetNameOfFocusedControl () == "EquationIn") {
				if (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow) {
					if (Event.current.keyCode == KeyCode.UpArrow) {
						if (_hIdx < 0)
							_hIdx = _history.Count - 1;
						else
							_hIdx--;
					} else if (Event.current.keyCode == KeyCode.DownArrow) {
						_hIdx++;
						if (_hIdx >= _history.Count) {
							_hIdx = -1;
						}
					}
					if (_hIdx < 0) {
						_hIdx = -1;
						_equation = "";
					} else {
						string hist = _history [_hIdx];
						if (hist.IndexOf ('=') > 0) {
							hist = hist.Remove (hist.IndexOf ('='));
						}
						if (hist.IndexOf ('\n') > 0) {
							hist = hist.Remove (hist.IndexOf ('\n'));
						}
						_equation = hist;
					}
				} else if (Event.current.keyCode == KeyCode.Return) {
					Calculate ();
				}
			}
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Calculate", _buttonStyle)) {
				Calculate ();
			}
			if (GUILayout.Button(_simple ? "More" : "Less",_buttonStyle)) {
				_simple = !_simple;
				_windowPos.width = 0.0f;
				_windowPos.height = 0.0f;
			}
			if (GUILayout.Button ("Close", _buttonStyle)) {
				Hide ();
			}
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();

			if (!_simple) {
				GUILayout.BeginVertical (GUILayout.MaxWidth (300.0f));
				int oldMenu = _menuSelection;
				_menuSelection = GUILayout.SelectionGrid (_menuSelection, 
					new string[]{ "Graph", "Cheat Sheet", "Notes", "Help" }, 2, _buttonStyle,
					GUILayout.MinWidth(300.0f));
				if (_menuSelection == 0) {
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("X Min:", _labelStyle, GUILayout.MaxWidth (50.0f));
					_xmin = GUILayout.TextField (_xmin, _textFieldStyle, GUILayout.MaxWidth (250f));
					GUILayout.EndHorizontal ();
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("X Max:", _labelStyle, GUILayout.MaxWidth (50.0f));
					_xmax = GUILayout.TextField (_xmax, _textFieldStyle, GUILayout.MaxWidth (250f));
					GUILayout.EndHorizontal ();
					GUILayout.BeginHorizontal ();

					if (GUILayout.Button ("Graph", _buttonStyle)) {
						Graph ();
					}
					if (GUILayout.Button ("Reset Graph", _buttonStyle)) {
						_graph = null;
						_geq = null;
						_windowPos.height = 0.0f;
						_windowPos.width = 0.0f;
					}

					GUILayout.EndHorizontal ();
					if (_graph != null) {
						GUILayout.Label ("Y max:" + _ymax.ToString("G10"), _labelStyle);
						GUILayout.Box (_graph.getImage ());
						Rect boxRect = GUILayoutUtility.GetLastRect ();
						string mouseOverMessage = "";
						if (boxRect.Contains (Event.current.mousePosition)) {
							float x = Event.current.mousePosition.x - boxRect.x - 25.0f;
							if (x >= 0 && x <= 250.0f) {
								double delta = (_gxmax - _gxmin) / 250.0d;

								mouseOverMessage = "X:" + (_gxmin + x * delta).ToString ("G5") +
									" Y:" + _geq.GetValue (_gxmin + x * delta).ToString ("G5");
							}
						}
						GUILayout.Label ("Y min:" + _ymin.ToString("G10"), _labelStyle);
						GUILayout.Label (mouseOverMessage, _labelStyle);
					}
				} else if (_menuSelection == 1) {
					GUILayout.Label ("Cheat Sheet", _labelStyle);
					GUILayout.TextArea (CHEAT_SHEET, _textAreaStyle);
				} else if (_menuSelection == 2) {
					GUILayout.Label ("Notes", _labelStyle);
					_globalScratchPad = GUILayout.TextArea (_globalScratchPad, _textAreaStyle);
				} else if (_menuSelection == 3) {
					GUILayout.Label ("Help", _labelStyle);
					GUILayout.TextArea (HELP, _textAreaStyle);
				}
				if (_menuSelection != oldMenu) {
					_windowPos.height = 0.0f;
				}
				GUILayout.EndVertical ();
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
			_centeredLabelStyle = KompLogStyle.Instance.CenteredLabel;
			_textFieldStyle = KompLogStyle.Instance.TextField;
			_textAreaStyle = KompLogStyle.Instance.TextArea;
			_buttonStyle = KompLogStyle.Instance.Button;
			_hasInitStyles = true;
		}

	}
}

