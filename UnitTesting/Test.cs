/*
 * This file is subject to the included LICENSE.md file. 
 */

using NUnit.Framework;
using System;
using KompLog;

namespace UnitTesting
{
	[TestFixture ()]
	public class Test
	{
		/// <summary>
		/// Add and multiply. The result depends on evaluating multiplication first, even though it second in the equation.
		/// </summary>
		[Test ()]
		public void AddMult ()
		{
			Equation eq = Equation.ParseEquation ("1+2*4");
			Assert.AreEqual (9.0d, eq.GetValue (),0.0,"");
		}
		/// <summary>
		/// Multiply and add. Verify that the commutative property holds.
		/// </summary>
		[Test ()]
		public void MultAdd ()
		{
			Equation eq = Equation.ParseEquation ("2*4+1");
			Assert.AreEqual (9.0d, eq.GetValue (),0.0,"");
		}

		/// <summary>
		/// Multiply and add. Verify that the commutative property holds.
		/// </summary>
		[Test ()]
		public void MultSub ()
		{
			Equation eq = Equation.ParseEquation ("2*4-1");
			Assert.AreEqual (7.0d, eq.GetValue (),0.0,"");
		}

		/// <summary>
		/// Multiply and divide.
		/// </summary>
		[Test ()]
		public void MultDiv ()
		{
			Equation eq = Equation.ParseEquation ("2*4/2");
			Assert.AreEqual (4.0d, eq.GetValue (),0.0,"");
			Equation eq1 = Equation.ParseEquation ("2/2*4");
			Assert.AreEqual (4.0d, eq1.GetValue (),0.0,"");
			Equation eq2 = Equation.ParseEquation ("2/(2*4)");
			Assert.AreEqual (0.25d, eq2.GetValue (),0.0,"");
			Equation eq3 = Equation.ParseEquation ("4/2*2");
			Assert.AreEqual (4.0d, eq3.GetValue (),0.0,"");
			Equation eq4 = Equation.ParseEquation ("4*2/2");
			Assert.AreEqual (4.0d, eq4.GetValue (),0.0,"");
		}

		/// <summary>
		/// Use parenthesis to change the order of evaluation.
		/// </summary>
		[Test ()]
		public void ParenMult ()
		{
			Equation eq = Equation.ParseEquation ("(1+1)*3");
			Assert.AreEqual (6.0d, eq.GetValue (),0.0,"");
		}
		/// <summary>
		/// Verify that the sin term works.
		/// </summary>
		[Test ()]
		public void Sin ()
		{
			Equation eq = Equation.ParseEquation ("sin(pi/2)");
			Assert.AreEqual (1.0d, eq.GetValue (),0.0,"");
		}

		/// <summary>
		/// Verify that the sin term works.
		/// </summary>
		[Test ()]
		public void ASin ()
		{
			Equation eq = Equation.ParseEquation ("asin(1)");
			Assert.AreEqual (Math.PI/2, eq.GetValue (),0.01,"");
		}

		/// <summary>
		/// Verify that the x function is evaluated and the result changes when the x value changes.
		/// </summary>
		[Test ()]
		public void XFunc ()
		{
			Equation eq = Equation.ParseEquation ("x");
			eq.SetXTerm (1);
			Assert.AreEqual (1.0d, eq.GetValue (),0.0,"");
			eq.SetXTerm (2);
			Assert.AreEqual (2.0d, eq.GetValue (),0.0,"");
		}
		/// <summary>
		/// Verify that a bad equation throws an exception.
		/// </summary>
		[Test ()]
		public void ParseError ()
		{
			Assert.Throws(typeof(ParsingException), new TestDelegate( delegate {Equation.ParseEquation ("Bad data");} ));
		}

		/// <summary>
		/// Verify we can differntiate negation from subtraction at least in some cases.
		/// </summary>
		[Test ()]
		public void Negation ()
		{
			Equation eq = Equation.ParseEquation ("-asin(1)");
			Assert.AreEqual (-(Math.PI/2), eq.GetValue (),0.01,"");
			Equation eq2 = Equation.ParseEquation ("2*-sin(pi/2)");
			Assert.AreEqual (-2.0d, eq2.GetValue (),0.01,"");
			Equation eq3 = Equation.ParseEquation ("2*-2");
			Assert.AreEqual (-4.0d, eq3.GetValue (),0.01,"");
			Equation eq4 = Equation.ParseEquation ("-2*-2");
			Assert.AreEqual (4.0d, eq4.GetValue (),0.01,"");
		}


		/// <summary>
		/// Verify we can differntiate negation from subtraction at least in some cases.
		/// </summary>
		[Test ()]
		public void Incomplete ()
		{
			Assert.Throws(typeof(ParsingException), new TestDelegate( delegate {Equation.ParseEquation ("1*");} ));
			Assert.Throws(typeof(ParsingException), new TestDelegate( delegate {Equation.ParseEquation ("1*2-");} ));
			Assert.Throws(typeof(ParsingException), new TestDelegate( delegate {Equation.ParseEquation ("sin");} ));
			Assert.Throws(typeof(ParsingException), new TestDelegate( delegate {Equation.ParseEquation ("ln");} ));
			Assert.Throws(typeof(ParsingException), new TestDelegate( delegate {Equation.ParseEquation ("*");} ));
			Assert.Throws(typeof(ParsingException), new TestDelegate( delegate {Equation.ParseEquation ("/2");} ));

		}
	}
}

