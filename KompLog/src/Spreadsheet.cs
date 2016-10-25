/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP.IO;

namespace KompLog
{
	internal class Cell
	{
		public string Content { get; set; }
	}
	internal class GraphLine
	{
		private string _content = "";
		public string Content 
		{
			get { return _content; }
			set { 
				if (value != _content) {
					_content = value;
					Dirty = true;
				}
			}
		}
		private string _XMin = "";
		public string XMin
		{
			get { return _XMin; }
			set {
				if (value != _XMin) {
					_XMin = value;
					Dirty = true;
				}
			}
		}
		private string _XMax = "";
		public string XMax 
		{
			get { return _XMax; }
			set {
				if (value != _XMax) {
					_XMax = value;
					Dirty = true;
				}
			}
		}
		public Color LineColor { get; set; }
		public bool Dirty { get; set; }
		// The x value for which the y below is defined.
		public double x { get; set; }
		// The y value corresponding to x. Just one point on the line. 
		// Used for displaying the value at a user specified point on the line.
		public double y { get; set; }
	}

	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny | KSPAddonImproved.Startup.TrackingStation, false)]
	public class Spreadsheet: MonoBehaviour
	{
		static public Spreadsheet Instance
		{
			get { return _instance; }
		}
		static private Spreadsheet _instance = null;

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
		private Vector2		_sheetScrollPos = new Vector2();
		private XmlDocument _sheet = new XmlDocument();
		private SortedList<int,SortedList<int,Cell>> _cells = new SortedList<int,SortedList<int,Cell>>();
		private SortedList<int,GraphLine> _graphLines = new SortedList<int,GraphLine> ();
		private int			_rows = 6;
		private int			_columns = 2;
		private int			_editRow = 0;
		private int 		_editCol = 0;
		private string		_editContent = "";
		private string		_error = "";
		private int			_errorRow = -1;
		private int 		_errorCol = -1;
		private GUIStyle	_textFieldStyle;
		private string		_filename = "";
		private bool		_load = false;
		private Rect		_loadPos = new Rect();
		private int 		_loadID;
		private string 		_selectedFile = "";
		private string		_clipboard = "";
		private int			_graphLineCnt = 1;
		private int 		_graphLineEdit = -1;
		private Graph 		_graph = new Graph (300,300);
		private int 		_graphID;
		private bool		_graphActive = false;
		private Rect		_graphPos = new Rect();
		private bool 		_lockX = true;
		private bool 		_lockY = true;
		private double 		_ymax = Double.NaN;
		private double 		_ymin = Double.NaN;
		private bool		_refreshGraph = false;
		private string 		_xmin = "";
		private string 		_xmax = "";
		private float 		_slider = 0.0f;
		private	bool 		_cntlLock = false;
		private string _subDir = "";

		private Color[] _lineColor = { Color.red, Color.cyan, Color.yellow, Color.green, Color.white };
		// Look for common spreadsheet reference format <column as uppercase letter><row number>.
		// Only look at one preceeded by non-number chars to avoid 123E567, which is a number.
		Regex _rxAlpha = new Regex (@"\D([A-Z])(\d+)");

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start()
		{
			if (_instance)
				Destroy (_instance);
			_instance = this;
			_winID = GUIUtility.GetControlID (FocusType.Passive);
			_loadID = GUIUtility.GetControlID (FocusType.Passive);
			_graphID = GUIUtility.GetControlID (FocusType.Passive);
			_graph.reset ();
			_graph.Apply ();
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
			_cntlLock = true;
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK|ControlTypes.EDITOR_EDIT_STAGES, "KompLog_Spreadsheet");
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			if (_cntlLock) {
				InputLockManager.RemoveControlLock ("KompLog_Spreadsheet");
			}
			_cntlLock = false;
		}

		/// <summary>
		/// Clears the error.
		/// </summary>
		private void ClearError()
		{
			_error = "";
			_errorCol = -1;
			_errorRow = -1;
		}

		/// <summary>
		/// Load the specified filename.
		/// </summary>
		/// <param name="filename">Filename.</param>
		private void Load(string filename)
		{
			try {
				_cells.Clear ();
				_graphLines.Clear ();
				_graphActive = false;
				_editContent = "";
				string basepath = KSP.IO.IOUtils.GetFilePathFor (typeof(Spreadsheet), "");
				string path = basepath + System.IO.Path.DirectorySeparatorChar + filename + ".xkls";
				System.IO.TextReader reader = new StreamReader (path);
				string xml = reader.ReadToEnd ();
				reader.Close();
				reader = null;

				_sheet.LoadXml (xml);
				int maxRow = 0;
				int maxCol = 0;
				foreach (XmlNode node in _sheet.ChildNodes) {
					if (node.Name == "Sheet") {
						foreach (XmlNode childNode in node.ChildNodes) {
							if (childNode.Name == "Cell") {
								int row = 0;
								int col = 0;
								foreach (XmlAttribute attr in childNode.Attributes)	{
									if (attr.Name == "row")
										row = int.Parse (attr.Value);
									else if (attr.Name == "column")
										col = int.Parse (attr.Value);
								}
								string content = childNode.InnerText;
								SetCell (row, col, content);
								if (row > maxRow)
									maxRow = row;
								if (col > maxCol)
									maxCol = col;
							} else if (childNode.Name == "Graph") {
								foreach (XmlAttribute attr in childNode.Attributes)
								{
									if (attr.Name == "graphlines")
										_graphLineCnt = int.Parse(attr.Value);
									else if (attr.Name ==  "lockx")
										_lockX = bool.Parse(attr.Value);
									else if (attr.Name == "locky")
										_lockY = bool.Parse(attr.Value);
									else if (attr.Name == "show")
										_graphActive = bool.Parse(attr.Value);
								}
								foreach (XmlNode gChildNode in childNode.ChildNodes) {
									if (gChildNode.Name == "GraphLine") {
										int line = 0;
										if (gChildNode.Attributes["line"] != null)
											line = int.Parse(gChildNode.Attributes["line"].Value);
										GraphLine graphLine = new GraphLine();
										foreach (XmlNode xNode in gChildNode.ChildNodes) {
											if (xNode.Name == "XMin") {
												graphLine.XMin = xNode.InnerText;
												_xmin = xNode.InnerText;
											} else if (xNode.Name == "XMax") {
												graphLine.XMax = xNode.InnerText;
												_xmax = xNode.InnerText;
											} else if (xNode.Name == "Equation") {
												graphLine.Content = xNode.InnerText;
											}
										}
										graphLine.LineColor = _lineColor[line];
										_graphLines.Add(line, graphLine);
									}
								}
							}
						}
						foreach (XmlAttribute attr in node.Attributes) {
							if (attr.Name == "rows")
								_rows = int.Parse (attr.Value);
							else if (attr.Name == "columns")
								_columns = int.Parse (attr.Value);
						}
						if (_rows == 0)
							_rows = 6;
						if (_columns == 0)
							_columns = 2;
						if (_rows < maxRow)
							_rows = maxRow;
						if (_columns < maxCol)
							_columns = maxCol;
					}
				}
				ClearError();
				_windowPos.height = 0.0f;
			} catch (Exception e) {
				_error = e.Message;
				Debug.Log (e.Message + "\n" + e.StackTrace);
			}
		}

		/// <summary>
		/// Save spreadsheet to the specified filename.
		/// </summary>
		/// <param name="filename">Filename.</param>
		private void Save(string filename)
		{
			if (filename.Contains ("..")) {
				_error = "The filename can't contain \"..\". Files must be in the PluginData directory.";
				return;
			}

			_sheet.RemoveAll ();
			XmlElement root = _sheet.CreateElement ("Sheet");
			foreach (KeyValuePair<int,SortedList<int, Cell>> colsKvp in _cells) {
				foreach (KeyValuePair<int,Cell> cellKvp in colsKvp.Value) {
					XmlElement cellNode = _sheet.CreateElement ("Cell");
					cellNode.SetAttribute ("row", colsKvp.Key.ToString ());
					cellNode.SetAttribute ("column", cellKvp.Key.ToString ());
					cellNode.InnerText = cellKvp.Value.Content;
					root.AppendChild (cellNode);
				}
			}
			if (_graphLines.Count > 0) {
				XmlElement graphNode = _sheet.CreateElement ("Graph");
				graphNode.SetAttribute ("graphlines", _graphLineCnt.ToString ());
				graphNode.SetAttribute ("lockx", _lockX.ToString());
				graphNode.SetAttribute ("locky", _lockY.ToString());
				graphNode.SetAttribute ("show", _graphActive.ToString ());
				int glines = 0;
				foreach (KeyValuePair<int, GraphLine> gl in _graphLines) {
					if (gl.Value.Content.Length == 0)
						continue;
					XmlElement graphLineNode = _sheet.CreateElement ("GraphLine");
					graphLineNode.SetAttribute ("line", gl.Key.ToString ());
					graphLineNode.SetAttribute ("color_r", gl.Value.LineColor.r.ToString());
					graphLineNode.SetAttribute ("color_g", gl.Value.LineColor.g.ToString());
					graphLineNode.SetAttribute ("color_b", gl.Value.LineColor.b.ToString());
					XmlElement content = _sheet.CreateElement ("Equation");
					content.InnerText = gl.Value.Content;
					graphLineNode.AppendChild (content);
					XmlElement xmin = _sheet.CreateElement ("XMin");
					xmin.InnerText = gl.Value.XMin;
					graphLineNode.AppendChild (xmin);
					XmlElement xmax = _sheet.CreateElement ("XMax");
					xmax.InnerText = gl.Value.XMax;
					graphLineNode.AppendChild (xmax);
					graphNode.AppendChild (graphLineNode);
					glines++;
				}
				if (glines > 0)
					root.AppendChild (graphNode);
			}
			root.SetAttribute ("rows", _rows.ToString());
			root.SetAttribute ("columns", _columns.ToString());

			_sheet.AppendChild (root);

			string basepath = KSP.IO.IOUtils.GetFilePathFor (typeof(Spreadsheet), "");
			string path = basepath + System.IO.Path.DirectorySeparatorChar + filename + ".xkls";
			System.IO.TextWriter writer = new StreamWriter (path);
			writer.WriteLine (FormatXml(_sheet));
			writer.Flush ();
			writer.Close ();
		}

		/// <summary>
		/// Sets contents of a cell. This will create the cell object if needed.
		/// </summary>
		/// <param name="row">Row of the cell.</param>
		/// <param name="column">Column of the cell.</param>
		/// <param name="content">Updated content.</param>
		private void SetCell(int row, int column, string content)
		{
			Cell editCell = null;
			if (_cells.ContainsKey (row)) {
				if (_cells [row].ContainsKey (column)) {
					editCell = _cells [row] [column];
				} else {
					editCell = new Cell ();
					_cells [row].Add (column, editCell);
				}
			} else {
				editCell = new Cell ();
				_cells.Add (row, new SortedList<int, Cell> ());
				_cells [row].Add (column, editCell);
			}
			editCell.Content = content;
			_refreshGraph = true;
		}

		/// <summary>
		/// Gets the value after any evaluation and equation parsing.
		/// </summary>
		/// <returns>The value.</returns>
		/// <param name="cell">Cell.</param>
		private string GetValue(Cell cell)
		{
			return GetValue (cell.Content);
		}

		/// <summary>
		/// Gets the value after any evaluation and equation parsing.
		/// </summary>
		/// <returns>The value.</returns>
		/// <param name="content">Content.</param>
		private string GetValue(string content)
		{
			if (content.StartsWith ("=")) {
				string equation = EvalCell(content);
				Equation eq = Equation.ParseEquation (equation);
				double value = eq.GetValue ();
				if (value > 1e12d || value < 1e-3d)
					return value.ToString ("e6");
				return value.ToString ("0.####");
			}
			return content;
		}

		/// <summary>
		/// Evaluates the cell. Converts any spreadsheet specific data into data for equation parsing.
		/// This means replacing cell references with the contents of those cells.
		/// </summary>
		/// <returns>The cell content after evaluation.</returns>
		/// <param name="content">Content of the cell.</param>
		private string EvalCell(string content)
		{
			string equation = content;
			if (content.StartsWith ("=")) {
				equation = content;

				MatchCollection matchesAlpha = _rxAlpha.Matches (equation);
				foreach (Match matchAlpha in matchesAlpha) {
					int col = matchAlpha.Groups [1].Value.ToCharArray () [0] - 'A';
					int row = int.Parse (matchAlpha.Groups [2].Value) - 1;
					equation = equation.Replace (matchAlpha.Groups [1].Value + matchAlpha.Groups [2].Value, GetCellContent (row, col));
				}
				equation = equation.TrimStart('=');

			}
			return equation;
		}

		/// <summary>
		/// Gets the value of a cell in a format intended for inclusion in another equation.
		/// </summary>
		/// <returns>The value.</returns>
		/// <param name="row">Row.</param>
		/// <param name="column">Column.</param>
		private string GetCellContent(int row, int column)
		{
			string content = "";
			if (_cells.ContainsKey (row)) {
				if (_cells [row].ContainsKey (column)) {
					if (_cells [row] [column].Content.Contains ("R" + row + "C" + column)) {
						_error = "Self Reference";
						return "0";
					}
					if (_cells [row] [column].Content.Contains ("" + Convert.ToChar('A'+ row) + column)) {
						_error = "Self Reference";
						return "0";
					}
					content = EvalCell(_cells [row] [column].Content);
				}
			}
			if (content.Length == 0) {
				content = "0";
			}
			return "(" + content + ")";
		}

		/// <summary>
		/// Drawing callback for the main window.
		/// </summary>
		private void OnDraw()
		{
			if (!_hasInitStyles)
				InitStyles ();
			_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Spreadsheet",_windowStyle);
			if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
				_windowPos.y = Screen.height * 0.5f - _windowPos.height * 0.5f;
				_windowPos.x = Screen.width - _windowPos.width - 50.0f;
			}
			if (_load) {
				_loadPos = GUILayout.Window (_loadID, _loadPos, OnLoadWindow, "Load",_windowStyle);
				if (_loadPos.x == 0.0f && _loadPos.y == 0.0f) {
					_loadPos.y = Screen.height * 0.5f - _loadPos.height * 0.5f;
					_loadPos.x = Screen.width - _windowPos.width - _loadPos.width - 50.0f;
				}
			}
			if (_graphActive) {
				_graphPos = GUILayout.Window (_graphID, _graphPos, OnGraphWindow, "Graph",_windowStyle);
				if (_graphPos.x == 0.0f && _graphPos.y == 0.0f) {
					_graphPos.y = Screen.height * 0.5f - _graphPos.height * 0.5f;
					_graphPos.x = Screen.width - _windowPos.width - _graphPos.width - 50.0f;
				}
			}
			if (_windowPos.Contains (Event.current.mousePosition) || (_load && _loadPos.Contains(Event.current.mousePosition)) 
				|| (_graphActive && _graphPos.Contains(Event.current.mousePosition))) {
				ControlLock ();
			} else {
				ControlUnlock();
			}
		}

		private void OnGraphWindow(int windowId)
		{
			bool draw = false;
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Lines", _labelStyle);
			if (GUILayout.Button ("+", _buttonStyle) && _graphLineCnt < _lineColor.Length) {
				_graphLineCnt++;
			}
			if (GUILayout.Button ("-", _buttonStyle) && _graphLineCnt > 1) {
				_graphLineCnt--;
				_graphPos.height = 0.0f;
				if (_graphLines.ContainsKey (_graphLineCnt)) {
					_graphLines.Remove (_graphLineCnt);
				}
				draw = true;
			}
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			bool lockX = GUILayout.Toggle (_lockX, "Lock X");
			if (lockX != _lockX) {
				_refreshGraph = true;
				_lockX = lockX;
			}
			bool lockY = GUILayout.Toggle (_lockY, "Lock Y");
			if (lockY != _lockY) {
				_refreshGraph = true;
				_lockY = lockY;
			}
			GUILayout.EndHorizontal ();
			try {
				if (_lockX)	{
					GUILayout.BeginHorizontal();
					string updated = "";
					string minPos = "XMin";
					GUILayout.Label("Xmin:",_labelStyle,GUILayout.Width(40.0f));
					GUI.SetNextControlName (minPos);
					updated = GUILayout.TextField (
						_graphLineEdit == -1 ? _xmin : GetValue(_xmin), _textFieldStyle,
						GUILayout.MinWidth(40.0f));
					if (GUI.GetNameOfFocusedControl () == minPos) {
						if (_graphLineEdit == -1 && updated != _xmin) {
							_xmin = updated;
							draw = true;
						}
						_graphLineEdit = -1;
					}

					GUILayout.Label("Xmax:",_labelStyle,GUILayout.Width(40.0f));
					string maxPos = "XMax";
					GUI.SetNextControlName (maxPos);
					updated = GUILayout.TextField (
						_graphLineEdit == -1 ? _xmax : GetValue(_xmax), _textFieldStyle,
						GUILayout.MinWidth(40.0f));
					if (GUI.GetNameOfFocusedControl () == maxPos) {
						if (_graphLineEdit == -1 && updated != _xmax) {
							_xmax = updated;
							draw = true;
						}
						_graphLineEdit = -1;
					}
					GUILayout.EndHorizontal ();
				}
				for (int line = 0; line < _graphLineCnt; line++) {
					if (!_graphLines.ContainsKey (line)) {
						_graphLines.Add (line, new GraphLine ());
						_graphLines [line].LineColor = _lineColor [line];
					}
					GUILayout.BeginHorizontal ();
					GUIStyle temp = new GUIStyle(_labelStyle);
					Texture2D color = new Texture2D(1,1);
					color.wrapMode=TextureWrapMode.Repeat;
					color.SetPixel (0, 0, _graphLines [line].LineColor);
					color.Apply ();
					temp.normal.background = color;
					GUILayout.Label("", temp, GUILayout.Width(20.0f));
					string cPos = "CG" + line;
					GUI.SetNextControlName (cPos);
					string updated = GUILayout.TextField (
						_graphLines [line].Content , _textFieldStyle, GUILayout.MinWidth(150.0f));

					if (GUI.GetNameOfFocusedControl () == cPos) {
						if (_graphLineEdit == line) {
							_graphLines [line].Content = updated;
						}
						_graphLineEdit = line;
					}

					if (!_lockX) {
						string minPos = "MinG" + line;
						GUI.SetNextControlName (minPos);
						updated = GUILayout.TextField (
							_graphLineEdit == line ? _graphLines [line].XMin : GetValue(_graphLines [line].XMin), _textFieldStyle,
							GUILayout.MinWidth(20.0f));
						if (GUI.GetNameOfFocusedControl () == minPos) {
							if (_graphLineEdit == line) {
								_graphLines [line].XMin = updated;
							}
							_graphLineEdit = line;
						}

						string maxPos = "MaxG" + line;
						GUI.SetNextControlName (maxPos);
						updated = GUILayout.TextField (
							_graphLineEdit == line ? _graphLines [line].XMax : GetValue(_graphLines [line].XMax), _textFieldStyle,
							GUILayout.MinWidth(20.0f));
						if (GUI.GetNameOfFocusedControl () == maxPos) {
							if (_graphLineEdit == line) {
								_graphLines [line].XMax = updated;
							}
							_graphLineEdit = line;
						}
					} else {
						_graphLines [line].XMin = _xmin;
						_graphLines [line].XMax = _xmax;
					}
					GUILayout.EndHorizontal ();
					if (_slider > 1e-2d) {
						GUILayout.BeginHorizontal();
						GUILayout.Label("X:"+_graphLines[line].x.ToString("0.0###"),_labelStyle);
						GUILayout.Label("Y:"+_graphLines[line].y.ToString("0.0###"),_labelStyle);
						GUILayout.EndHorizontal();
					}
					draw = draw || _graphLines [line].Dirty;
				}
				float slider = GUILayout.HorizontalSlider (_slider, 0.0f, 300.0f);
				if (slider != _slider) {
					_slider = slider;
					draw = true;
					_graphPos.height=0.0f;
				}

				if (draw) {
					_ymax = Double.NaN;
					_ymin = Double.NaN;
				}
				if (draw || _refreshGraph) {
					_refreshGraph = false;
					_graph.reset ();
					for (int line = 0; line < _graphLineCnt; line++) {
						_graphLines[line].Dirty = false;

						if (_graphLines [line].Content.Length == 0)
							continue;
						if (_graphLines [line].XMin.Length == 0)
							continue;
						if (_graphLines [line].XMax.Length == 0)
							continue;

						Equation equation = Equation.ParseEquation (EvalCell(_graphLines [line].Content));
						Equation xmin = Equation.ParseEquation (EvalCell(_graphLines [line].XMin));
						Equation xmax = Equation.ParseEquation (EvalCell(_graphLines [line].XMax));

						double min = xmin.GetValue ();
						double max = xmax.GetValue ();

						double ymin = Double.NaN;
						double ymax = Double.NaN;

						if (max <= min || Double.IsNaN(max) || Double.IsNaN(min))
							continue;

						double delta = (max - min) / 300.0d;

						int px = 0;
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
							if (px == (int)_slider) {
								_graphLines[line].x = x;
								_graphLines[line].y = y;
							}
							px++;
						}

						if (Double.IsNaN (_ymin) || ymin < _ymin) {
							_ymin = ymin;
							_refreshGraph = true;
						}
						if (Double.IsNaN (_ymax) || ymax > _ymax) {
							_ymax = ymax;
							_refreshGraph = true;
						}
						if (_lockY)	{
							ymin = _ymin;
							ymax = _ymax;
						}
						_graph.drawLineOnGraph (x => equation.GetValue (min + x * delta), ymax, ymin, 300,
							_graphLines [line].LineColor);
					}
					if (_slider > 1e-2d) {
						_graph.drawVerticalLine((int)_slider, Color.grey);
					}
					_graph.Apply ();
				}
			} catch (Exception e) {
				Debug.Log (e.Message + "\n" + e.StackTrace);
			}
			if (_graph != null) {
				GUILayout.Label ("Y max:" + _ymax.ToString("G10"), _labelStyle);
				GUILayout.Box (_graph.getImage ());
				GUILayout.Label ("Y min:" + _ymin.ToString("G10"), _labelStyle);
			}
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}


		/// <summary>
		/// Draws the load window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnLoadWindow(int windowId)
		{
			GUILayout.BeginVertical (GUILayout.MinWidth(200.0f));
			string basepath = KSP.IO.IOUtils.GetFilePathFor (typeof(Spreadsheet), "");
			string path = basepath + _subDir;
			string[] files = System.IO.Directory.GetFiles (path, "*.xkls");
			string[] subDirs = System.IO.Directory.GetDirectories (path);
			string subPath = "";
			if (_subDir.Length > 0) {
				subPath = _subDir + System.IO.Path.DirectorySeparatorChar;
			}
			_scrollPos = GUILayout.BeginScrollView(_scrollPos,_scrollStyle, GUILayout.MinWidth (200.0f), GUILayout.Height (200.0f) );

			foreach (string file in files) {
				string filename = System.IO.Path.GetFileNameWithoutExtension(file);
				if (GUILayout.Button (filename, _buttonStyle)) {
					_selectedFile = filename;
				}
			}
			if (_subDir.Length > 0 && GUILayout.Button("..",_buttonStyle)) {
				_subDir = "";
			}
			foreach (string dir in subDirs) {
				string dirname = dir.Replace(path,"");
				if (GUILayout.Button ("Dir:"+dirname, _buttonStyle)) {
					_subDir = dirname;
				}
			}
			GUILayout.EndScrollView ();
			GUILayout.Label (_selectedFile, _labelStyle);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Load", _buttonStyle)) {
				_filename = subPath + _selectedFile;
				Load (_filename);
				_load = false;
			}
			if (GUILayout.Button ("Delete", _buttonStyle) && _selectedFile.Length > 0) {
				string fileToDelete = basepath + subPath +_selectedFile+".xkls";
				if (System.IO.File.Exists (fileToDelete)) {
					System.IO.File.Delete (fileToDelete);
					_selectedFile = "";
				}
			}
			if (GUILayout.Button ("Cancel", _buttonStyle)) {
				_load = false;
			}
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		/// <summary>
		/// Build the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("File:",_labelStyle, GUILayout.MinWidth(40.0f));
			GUI.SetNextControlName ("Filename");
			_filename = GUILayout.TextField (_filename, _textFieldStyle, GUILayout.MinWidth(250.0f));
			if (GUILayout.Button ("New", _buttonStyle, GUILayout.MinWidth (50.0f))) {
				_cells.Clear ();
				_graphLines.Clear ();
				_filename = "";
				_xmax = "";
				_xmin = "";
				_subDir = "";
				_editContent = "";
				_graph.reset ();
				_graph.Apply ();
				ClearError ();
			}
			if (GUILayout.Button ("Load", _buttonStyle, GUILayout.MinWidth(50.0f))) {
				_load = true;
				GUI.FocusControl ("Filename");
			}
			if (GUILayout.Button ("Save", _buttonStyle, GUILayout.MinWidth(50.0f))) {
				if (_filename.Length > 0) {
					Save (_filename);
				} else {
					_error = "Specify a filename.";
				}
			}

			GUILayout.EndHorizontal ();
			if (_rows > 8 || _columns > 3)
				_sheetScrollPos = GUILayout.BeginScrollView(_sheetScrollPos,_scrollStyle, GUILayout.MinWidth (470.0f), GUILayout.Height (250.0f) );
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", _labelStyle, GUILayout.Width (20.0f));
			for (int column = 0; column < _columns; column++) {
				GUILayout.Label (Convert.ToChar('A'+ column).ToString(), _centeredLabelStyle, GUILayout.MinWidth (150.0f));
			}
			GUILayout.EndHorizontal ();
			for (int row = 0; row < _rows; row++) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ((row+1).ToString (), _labelStyle, GUILayout.Width (20.0f));
				for (int column = 0; column < _columns; column++) {
					string content = "";
					if (_cells.ContainsKey (row)) {
						if (_cells [row].ContainsKey (column)) {
							if (_editRow == row && _editCol == column) {
								content = _cells [row] [column].Content;
							} else {
								try {
									content = GetValue(_cells [row] [column]);
									// The content was changed and we no longer get an error.
									if (row == _errorRow && column == _errorCol)
										ClearError();
								} catch(Exception e) {
									_error = e.Message;
									_errorCol = column;
									_errorRow = row;
									Debug.Log (e.Message+":"+e.StackTrace);
									content = _cells [row] [column].Content;
								}
							}
						}
					}
					string pos = "R" + row + "C" + column;
					GUI.SetNextControlName (pos);
					string updated = GUILayout.TextField (content,_textFieldStyle, GUILayout.MinWidth (150.0f));
					if (GUI.GetNameOfFocusedControl () == pos) {
						_editRow = row;
						_editCol = column;
						_editContent = updated;

						if (updated != content ) {
							if (updated.Length > 0) {
								SetCell (row, column, updated);
							} else {
								_cells [row].Remove (column);
							}
						}
					}
				}
				GUILayout.EndHorizontal ();
			}
			if (_rows > 8 || _columns > 3)
				GUILayout.EndScrollView ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("" + Convert.ToChar('A'+ _editCol) + (_editRow + 1), _labelStyle,GUILayout.MinWidth(20.0f));
			string edited = GUILayout.TextField (_editContent,_textFieldStyle, GUILayout.MinWidth (400.0f));
			if (edited != _editContent) {
				SetCell (_editRow, _editCol, edited);
				_editContent = edited;
			}
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Rows ",_labelStyle);
			if (GUILayout.Button("+",_buttonStyle)) {
				_rows++;
			}
			if (GUILayout.Button("-",_buttonStyle) && _rows > 1) {
				_rows--;
				_windowPos.height = 0.0f;
			}
			GUILayout.Label ("Columns ",_labelStyle);
			// Don't allow more columns than letters. The parser doesn't support it and this isn't intended to be a full featured
			// spreadsheet program, just enough for in game use.
			if (GUILayout.Button("+",_buttonStyle) && _columns < 26) {
				_columns++;
			}
			if (GUILayout.Button("-",_buttonStyle) && _columns > 1) {
				_columns--;
				_windowPos.height = 0.0f;
				_windowPos.width = 0.0f;
			}
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Graph", _buttonStyle)) {
				_graphActive = !_graphActive;
			}
			if (GUILayout.Button ("Copy", _buttonStyle)) {
				_clipboard = _editContent;
			}
			if (GUILayout.Button ("Cut", _buttonStyle)) {
				_clipboard = _editContent;
				SetCell (_editRow, _editCol, "");
				_editContent = "";
			}
			if (GUILayout.Button ("Paste", _buttonStyle)) {
				SetCell (_editRow, _editCol, _clipboard);
				_editContent = _clipboard;
			}
			if (GUILayout.Button("Clear Error",_buttonStyle)) {
				ClearError ();
			}
			if (GUILayout.Button("Close",_buttonStyle)) {
				ToggleWindow ();
			}
			GUILayout.EndHorizontal ();

			if (_errorCol >= 0)
				GUILayout.Label ("" + Convert.ToChar('A'+ _errorCol) + (_errorRow + 1)+":", _labelStyle,GUILayout.MinWidth(20.0f));
			GUILayout.Label (_error, _labelStyle);
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		/// <summary>
		/// Formats the xml.
		/// </summary>
		/// <returns>The xml.</returns>
		/// <param name="doc">Document.</param>
		private string FormatXml(XmlDocument doc)
		{
			StringWriter writer = new StringWriter ();
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.NewLineChars = System.Environment.NewLine;
			XmlWriter xmlWrite = XmlWriter.Create (writer, settings);
			doc.Save (xmlWrite);
			return writer.ToString ();
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
			_textFieldStyle = KompLogStyle.Instance.TextField;
			_hasInitStyles = true;
		}
	}
}

