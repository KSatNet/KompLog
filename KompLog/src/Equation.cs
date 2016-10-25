/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using System.Collections.Generic;



namespace KompLog
{
	/// <summary>
	/// Operator priority.
	/// </summary>
	enum OperatorPriority
	{
		EQUATION = -1, // Parenthesis are treated as equations within equations.
		CONSTANT = -1,
		UNARY_FUNC = 0,
		NEGATION = 1,
		EXPONENT = 1,
		MULTIPLY = 2,
		DIVISION = MULTIPLY,
		ADDITION = 3,
		SUBTRACTION = ADDITION
	}

	/// <summary>
	/// Term defines a portion of an equation which will return a value when requested.
	/// It also has a priority which is used to determine the oder of evaluation.
	/// </summary>
	public interface Term
	{
		double GetValue();
		void SetNextTerm (Term term);
		int GetPriority();
		bool IsComplete();
	}
	/// <summary>
	/// X function is an object which takes an X value.
	/// </summary>
	public interface XFunction
	{
		void SetXTerm(double x);
	}

	public class ParsingException: System.Exception
	{
		public ParsingException()
		{
		}

		public ParsingException(string message)
			:base(message)
		{
		}
		public ParsingException(string message, Exception inner)
			:base(message, inner)
		{
		}
	}

	/// <summary>
	/// Equation is a collection of Terms (and itself a term). It defines methods for parsing a string and translating it into a
	/// set of Term objects. When GetValue is called these terms are evaluated in the proper order and the result is returned. The
	/// Equation object derives from XFunction which allows it to change its value based on a provided X value.
	/// </summary>
	public class Equation: Term,XFunction
	{
		private Term _root = null; // Current term which will be evaluated first.
		private Term _fixed = null; // Term which will be evaluated where it is irrespective of the terms around it.
		private List<XFunction> _xterms = new List<XFunction>(); //List of terms that depend on X.
		public Equation() {
		}
		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <returns>The value.</returns>
		public double GetValue() {
			if (_root != null)
				return _root.GetValue ();
			return Double.NaN;
		}
		/// <summary>
		/// Used to determine the left term for operands with a left and right term.
		/// </summary>
		/// <returns>The previous term.</returns>
		/// <param name="priority">Priority of the current term.</param>
		public Term GetPrevTerm(int priority) {
			if (_root == null || priority >= _root.GetPriority ())
				return _root;
			return _fixed;
		}
		/// <summary>
		/// Sets the next term in the equation. Where the object is added depends on its priority relative to other Terms.
		/// </summary>
		/// <param name="term">Term object to be set.</param>
		public void SetNextTerm (Term term) {
			// Only fixed terms have a negative priority.
			if (term.GetPriority () <= 0) {
				_fixed = term;
			}
			// This is the first term. It will be the first evaluated.
			if (_root == null) {
				_root = term;
			}
			// The current root still has the highest priority. This new term must be evaluated as part of it.
			else if (_root.GetPriority () > term.GetPriority ()) {
				_root.SetNextTerm (term);
			}
			// The new term has equal or greater priority. It should be evaluated first.
			else {
				_root = term;
			}
			// The term is a function of X, so add it to the collection in case we are given an X value.
			if (term is XFunction) {
				AddXFunction ((XFunction)term);
			}
		}
		public int GetPriority() {
			return (int)OperatorPriority.EQUATION;
		}
		/// <summary>
		/// Determines whether equation is complete.
		/// </summary>
		/// <returns><c>true</c> if this instance is complete; otherwise, <c>false</c>.</returns>
		public bool IsComplete() {
			if (_root == null)
				return false;
			else
				return _root.IsComplete ();
		}
		/// <summary>
		/// Adds a X function.
		/// </summary>
		/// <param name="xt">Xt.</param>
		private void AddXFunction(XFunction xt) {
			_xterms.Add (xt);
		}

		/// <summary>
		/// Sets the X term.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		public void SetXTerm(double x) {
			foreach (XFunction xt in _xterms) {
				xt.SetXTerm (x);
			}
		}

		/// <summary>
		/// Parses the equation string returning an Equation object. Error checking is minimal and will evaluate 
		/// invalid strings in unique and meaningless ways.
		/// </summary>
		/// <returns>The equation object.</returns>
		/// <param name="eqText">Equation text.</param>
		static public Equation ParseEquation(string eqText)
		{
			char[] eq = eqText.ToCharArray ();
			string constant = "";
			Equation equation = new Equation ();
			for (int index = 0;index < eqText.Length; index++) {
				// Ignore whitespace.
				if (Char.IsWhiteSpace(eq[index]))
					continue;
				if (eq [index] == ',')
					continue;
				if (eq [index] == '=')
					continue;

				bool addConstant = false;
				// Determine if this looks like a constant (0-9, -, ., or e surrounded by digits).
				if (Char.IsDigit (eq [index]) ||
					(Char.ToLower (eq [index]) == 'e' && constant!="") ||
					eq[index] == '.') {
					constant += eq [index];
					addConstant = true;
					// We are at the end of the constant, make it a term.
				} else if (constant != "") {
					ConstantTerm ct = new ConstantTerm (Double.Parse (constant));
					equation.SetNextTerm (ct);
					constant = "";
				}
				// We are at the end of the string. Turn the last constant into a term if applicable.
				if (index + 1 == eqText.Length && constant != "") {
					ConstantTerm ct = new ConstantTerm (Double.Parse (constant));
					equation.SetNextTerm (ct);
					constant = "";
				}

				if (eq [index] == '-') {
					int k = index + 1;
					while (k < eqText.Length) {
						if (!Char.IsWhiteSpace (eq [k])) {
							break;
						}
						k++;
					}
					if (k >= eqText.Length) {
						throw new ParsingException ("Ends with a - sign.");
					}
					// First term is negative or it is part of a negative exponent in a constant.
					if (Char.IsDigit (eq [k]) && 
						(equation.GetPrevTerm(-1) == null || constant != "")) {
						constant += eq [index];
						addConstant = true;
					} else {
						int j = index - 1;
						while (j > 0) {
							if (!Char.IsWhiteSpace (eq [j])) {
								break;
							}
							j--;
						}
						// First term, but not followed by digit.
						if (j < 0)
							j = 0;

						// Not a digit and not a paren, must be an operation, so this must be a negation.
						if (!Char.IsDigit (eq [j]) && eq[j] != ')') {
							if (Char.IsDigit (eq [k])) {
								constant += eq [index];
								addConstant = true;
							} else {
								NegationTerm neg = new NegationTerm();
								equation.SetNextTerm (neg);
								continue;
							}
						}
					}
				}

				// We have a parenthetical statement. Find the end of this parenthesis and parse it as its own equation.
				if (eq [index] == '(') {
					int nesting = 1;
					int j = index + 1;
					for (; j < eqText.Length; j++) {
						if (eq [j] == '(')
							nesting++;
						if (eq [j] == ')')
							nesting--;
						if (nesting == 0)
							break;
					}
					equation.SetNextTerm (ParseEquation (eqText.Substring (index + 1, j - index - 1)));
					index = j;
					// Ignore the end parenthesis.
				} else if (eq [index] == ')') {
				} else if (eq [index] == '^') {
					ExponentTerm exp = new ExponentTerm (equation.GetPrevTerm ((int)OperatorPriority.EXPONENT));
					equation.SetNextTerm (exp);
				} else if (eq [index] == '*') {
					MultiplyTerm mult = new MultiplyTerm (equation.GetPrevTerm ((int)OperatorPriority.MULTIPLY));
					equation.SetNextTerm (mult);
				} else if (eq [index] == '/') {
					DivideTerm div = new DivideTerm (equation.GetPrevTerm ((int)OperatorPriority.DIVISION));
					equation.SetNextTerm (div);
				} else if (eq [index] == '%') {
					ModuloTerm div = new ModuloTerm (equation.GetPrevTerm ((int)OperatorPriority.DIVISION));
					equation.SetNextTerm (div);
				} else if (eq [index] == '+') {
					AdditionTerm addition = new AdditionTerm (equation.GetPrevTerm ((int)OperatorPriority.ADDITION));
					equation.SetNextTerm (addition);
				} else if (eq [index] == '-' && constant == "") {
					SubtractionTerm sub = new SubtractionTerm (equation.GetPrevTerm ((int)OperatorPriority.SUBTRACTION));
					equation.SetNextTerm (sub);
				} else if (eq [index] == 'l' && eq [index + 1] == 'n') {
					NaturalLogTerm ln = new NaturalLogTerm ();
					equation.SetNextTerm (ln);
					index += 1;
				} else if (eq [index] == 's' && eq [index + 1] == 'i' && eq [index + 2] == 'n') {
					if (eq [index + 3] == 'h') {
						SinhTerm sin = new SinhTerm ();
						equation.SetNextTerm (sin);
						index += 1;
					}
					else { 
						SinTerm sin = new SinTerm ();
						equation.SetNextTerm (sin);
					}
					index += 2;
				} else if (eq [index] == 's' && eq [index + 1] == 'q' && eq [index + 2] == 'r' && eq [index + 3] == 't') {
					SqrtTerm sqrt = new SqrtTerm ();
					equation.SetNextTerm (sqrt);
					index += 3;
				} else if (eq [index] == 'c' && eq [index + 1] == 'o' && eq [index + 2] == 's') {
					if (eq [index + 3] == 'h') {
						CoshTerm cosh = new CoshTerm ();
						equation.SetNextTerm (cosh);
						index += 1;
					} else {
						CosTerm cos = new CosTerm ();
						equation.SetNextTerm (cos);
					}
					index += 2;
				} else if (eq [index] == 't' && eq [index + 1] == 'a' && eq [index + 2] == 'n') {
					if (eq [index + 3] == 'h') {
						TanhTerm tanh = new TanhTerm ();
						equation.SetNextTerm (tanh);
						index += 1;
					} else {
						TanTerm tan = new TanTerm ();
						equation.SetNextTerm (tan);
					}
					index += 2;
				} else if (eq [index] == 'p' && eq [index + 1] == 'i') {
					ConstantTerm ct = new ConstantTerm (Math.PI);
					equation.SetNextTerm (ct);
					index += 1;
				} else if (eq [index] == 'x') {
					XTerm xt = new XTerm ();
					equation.SetNextTerm (xt);
				} else if (eq [index] == 'e' && constant == "") {
					ConstantTerm ct = new ConstantTerm (Math.E);
					equation.SetNextTerm (ct);
				} else if (eq [index] == 'a') {
					index += 1;
					if (eq [index] == 's' && eq [index + 1] == 'i' && eq [index + 2] == 'n') {
						ASinTerm asin = new ASinTerm ();
						equation.SetNextTerm (asin);
						index += 2;
					} else if (eq [index] == 'c' && eq [index + 1] == 'o' && eq [index + 2] == 's') {
						ACosTerm acos = new ACosTerm ();
						equation.SetNextTerm (acos);
						index += 2;
					} else if (eq [index] == 't' && eq [index + 1] == 'a' && eq [index + 2] == 'n') {
						ATanTerm atan = new ATanTerm ();
						equation.SetNextTerm (atan);
						index += 2;
					}
				} else if (!addConstant) {
					int start = index;
					if (start > 5)
						start -= 5;
					else
						start = 0;
					int end = index;
					if (end + 5 < eqText.Length)
						end += 5;
					else
						end = eqText.Length - 1;
					throw new ParsingException ("Unable to parse equation at " + index +
						". Unknown character near:" + eqText.Substring (start, end));
				}
			}
			if (!equation.IsComplete ())
				throw new ParsingException ("Incomplete equation, maybe missing a term?");
			return equation;
		}
		public double GetValue(double x) {
			SetXTerm (x);
			return GetValue ();
		}
	}
	/// <summary>
	/// X term.
	/// </summary>
	public class XTerm: Term,XFunction
	{
		private double _x;
		public XTerm() {
		}
		public double GetValue() {
			return _x;
		}
		public void SetNextTerm (Term term) {
		}
		public int GetPriority() {
			// This is constant because it doesn't depend on any other term.
			return (int)OperatorPriority.CONSTANT;
		}
		virtual public void SetXTerm(double x) {
			_x = x;
		}
		public bool IsComplete() {
			return true;
		}
	}
	/// <summary>
	/// Constant term.
	/// </summary>
	public class ConstantTerm: Term
	{
		private double _value;
		public ConstantTerm(double val) {
			_value = val;
		}
		public double GetValue() {
			return _value;
		}
		public void SetNextTerm (Term term) {
		}
		public int GetPriority() {
			return (int)OperatorPriority.CONSTANT;
		}
		public bool IsComplete() {
			return true;
		}
	}
	/// <summary>
	/// Negation term returns the negative of whatever term that follows.
	/// </summary>
	public class NegationTerm: Term
	{
		Term _value;
		public NegationTerm() {
		}
		public double GetValue() {
			return -(_value.GetValue());
		}
		public void SetNextTerm (Term term) {
			if (_value == null) {
				_value = term;
			} else {
				_value.SetNextTerm (term);
			}
		}
		public int GetPriority() {
			return (int)OperatorPriority.NEGATION;
		}
		public bool IsComplete() {
			return _value != null;
		}
	}
	/// <summary>
	/// Addition term.
	/// </summary>
	public class AdditionTerm: Term
	{
		private Term _addend1 = null;
		private Term _addend2 = null;

		public AdditionTerm(Term m1) {
			_addend1 = m1;
		}
		public double GetValue() {
			return _addend1.GetValue () + _addend2.GetValue ();
		}
		public void SetNextTerm (Term term) {
			if (_addend2 == null || _addend2.GetPriority () < term.GetPriority ())
				_addend2 = term;
			else
				_addend2.SetNextTerm (term);
		}
		public int GetPriority() {
			return (int)OperatorPriority.ADDITION;
		}
		public bool IsComplete() {
			return _addend1 != null && _addend2 != null;
		}
	}
	/// <summary>
	/// Subtraction term.
	/// </summary>
	public class SubtractionTerm: Term
	{
		private Term _addend1 = null;
		private Term _addend2 = null;

		public SubtractionTerm(Term m1) {
			_addend1 = m1;
		}
		public double GetValue() {
			return _addend1.GetValue () - _addend2.GetValue ();
		}
		public void SetNextTerm (Term term) {
			if (_addend2 == null || _addend2.GetPriority () < term.GetPriority ())
				_addend2 = term;
			else
				_addend2.SetNextTerm (term);
		}
		public int GetPriority() {
			return (int)OperatorPriority.SUBTRACTION;
		}
		public bool IsComplete() {
			return _addend1 != null && _addend2 != null;
		}
	}
	/// <summary>
	/// Multiply term.
	/// </summary>
	public class MultiplyTerm: Term
	{
		private Term _multiplicand1 = null;
		private Term _multiplicand2 = null;

		public MultiplyTerm(Term m1) {
			_multiplicand1 = m1;
		}
		public double GetValue() {
			return _multiplicand1.GetValue () * _multiplicand2.GetValue ();
		}
		public void SetNextTerm (Term term) {
			if (_multiplicand2 == null || _multiplicand2.GetPriority () < term.GetPriority ())
				_multiplicand2 = term;
			else
				_multiplicand2.SetNextTerm (term);
		}
		public int GetPriority() {
			return (int)OperatorPriority.MULTIPLY;
		}
		public bool IsComplete() {
			return _multiplicand1 != null && _multiplicand2 != null;
		}
	}
	/// <summary>
	/// Divide term.
	/// </summary>
	public class DivideTerm : Term
	{
		private Term _dividend = null;
		private Term _divisor = null;

		public DivideTerm(Term dividend) {
			_dividend = dividend;
		}
		public double GetValue() {
			return _dividend.GetValue () / _divisor.GetValue ();
		}
		public void SetNextTerm (Term term) {
			if (_divisor == null || _divisor.GetPriority () < term.GetPriority ())
				_divisor = term;
			else
				_divisor.SetNextTerm (term);
		}
		public int GetPriority() {
			return (int)OperatorPriority.DIVISION;
		}
		public bool IsComplete() {
			return _divisor != null && _dividend != null;
		}
	}
	/// <summary>
	/// Modulo term.
	/// </summary>
	public class ModuloTerm : Term
	{
		private Term _dividend = null;
		private Term _divisor = null;

		public ModuloTerm(Term dividend) {
			_dividend = dividend;
		}
		public double GetValue() {
			return _dividend.GetValue () % _divisor.GetValue ();
		}
		public void SetNextTerm (Term term) {
			if (_divisor == null || _divisor.GetPriority () < term.GetPriority ())
				_divisor = term;
			else
				_divisor.SetNextTerm (term);
		}
		public int GetPriority() {
			return (int)OperatorPriority.DIVISION;
		}
		public bool IsComplete() {
			return _divisor != null && _dividend != null;
		}
	}


	/// <summary>
	/// Natural log term.
	/// </summary>
	public class NaturalLogTerm: Term
	{
		private Term _term = null;
		public NaturalLogTerm() {

		}
		public double GetValue() {
			return Math.Log (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}
	/// <summary>
	/// Sin term.
	/// </summary>
	public class SinTerm: Term
	{
		private Term _term = null;
		public SinTerm() {

		}
		public double GetValue() {
			return Math.Sin (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}

	/// <summary>
	/// Sin term.
	/// </summary>
	public class SinhTerm: Term
	{
		private Term _term = null;
		public SinhTerm() {

		}
		public double GetValue() {
			return Math.Sinh (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}

	/// <summary>
	/// Cos term.
	/// </summary>
	public class CosTerm: Term
	{
		private Term _term = null;
		public CosTerm() {

		}
		public double GetValue() {
			return Math.Cos (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}

	/// <summary>
	/// Cos term.
	/// </summary>
	public class CoshTerm: Term
	{
		private Term _term = null;
		public CoshTerm() {

		}
		public double GetValue() {
			return Math.Cosh (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}

	/// <summary>
	/// Tangent term.
	/// </summary>
	public class TanTerm: Term
	{
		private Term _term = null;
		public TanTerm() {

		}
		public double GetValue() {
			return Math.Tan (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}

	/// <summary>
	/// Tangent term.
	/// </summary>
	public class TanhTerm: Term
	{
		private Term _term = null;
		public TanhTerm() {

		}
		public double GetValue() {
			return Math.Tanh (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}

	/// <summary>
	/// Arc Sine term.
	/// </summary>
	public class ASinTerm: Term
	{
		private Term _term = null;
		public ASinTerm() {

		}
		public double GetValue() {
			return Math.Asin (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}


	/// <summary>
	/// Arc Cosine term.
	/// </summary>
	public class ACosTerm: Term
	{
		private Term _term = null;
		public ACosTerm() {

		}
		public double GetValue() {
			return Math.Acos (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}


	/// <summary>
	/// Arc Tangent term.
	/// </summary>
	public class ATanTerm: Term
	{
		private Term _term = null;
		public ATanTerm() {

		}
		public double GetValue() {
			return Math.Atan (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}

	/// <summary>
	/// Sqrt term.
	/// </summary>
	public class SqrtTerm: Term
	{
		private Term _term = null;
		public SqrtTerm() {

		}
		public double GetValue() {
			return Math.Sqrt (_term.GetValue ());
		}
		public void SetNextTerm (Term term) {
			_term = term;
		}
		public int GetPriority() {
			return (int)OperatorPriority.UNARY_FUNC;
		}
		public bool IsComplete() {
			return _term != null;
		}
	}
	/// <summary>
	/// Exponent term.
	/// </summary>
	public class ExponentTerm: Term
	{
		private Term _base = null;
		private Term _power = null;

		public ExponentTerm(Term exBase) {
			_base = exBase;
		}
		public double GetValue() {
			return Math.Pow (_base.GetValue(), _power.GetValue ());
		}
		public void SetNextTerm (Term term) {
			if (_power == null || _power.GetPriority () < term.GetPriority ())
				_power = term;
			else
				_power.SetNextTerm (term);
		}
		public int GetPriority() {
			return (int)OperatorPriority.EXPONENT;
		}
		public bool IsComplete() {
			return _base != null && _power != null;
		}
	}
}

