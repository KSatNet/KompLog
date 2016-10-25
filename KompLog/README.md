Komputational Logistics provides some computational tools and information helpful in planning missions. The intention is to minimize
the cases where you need to put the game in the background to run calculations or look something up. By design it does not provide much
information that is directly useful, but instead gives you the raw data and the tools to turn that into something useful.

# Contents
1. Installation
2. Utilities
3. Basic Usage

# Installation
Copy the contents of the GameData folder into <KSP>/GameData/.

# Utilities
## Calculator
Calculates the results of an equation. Designed to be closer to a graphing calculator than a scientific calculator you input the 
algebraic equation instead of operating on intermediate results. It has full operator precedence support you do not need to order your
statements to ensure a specific result. It has a graphing mode where it will graph over a range of x values (to be interesting your
equation needs to have an x in it). There is a cheat sheet with a list of common equations and a persistent notes tab to add your own.

## Spreadsheet
Basically a grid of calculators with the ability to use cell references (i.e. A1 == contents of the upper left cell). Equations must
start with "=" or they will be displayed as text. You can save and load *.xkls files (XML Komputational Logistics Spreadheet file) a
custom format for this spreadsheet application. While the syntax is similar to other spreadsheet programs it doesn't support cell ranges. 

The spreadsheet has a graphing capability, though it is different from most other spreadsheets. You specify the equation for the line
with an x term and range for x instead of a data set. This can also use cell references, so you can create re-usable spreadsheets and
plug in the terms for your situation.

## Stage Data
For the current vessel gives you the total mass, dry mass, resource mass, and payload mass per stage. When you expand a stage you can
see the mass per resource, engine type, engine isp, and engine thrust of that stage. Basically everything you need to calculate delta-v,
burn time, and many other important pieces of information.

## Part Data
Displays all parts in the vessel. Their name, mass, resource mass, stage, etc. Useful in determining which parts are the heaviest when
you are trying to reduce mass. It can also tell you if the stage calculation is putting a part in the wrong stage. By design this uses
the same stage determination routine as the stage data.

## Celestial Body Data
Gives you access to the planetary data for every celestial body in the game. Primarily useful in the VAB/SPH where you can't otherwise
get to this data. With this and the stage data you can calculate the TWR of your craft.

## Orbit Data
Gives you a number of orbital parameters for vessels and celestial bodies. Making sense of them is left as an exercise for the reader. 

# Basic Usage
## Calculator
The calculator has two modes. In the simple mode you type the equation, hit calculate, and the results appear in the history. In the
complex mode you have the ability to graph, take notes, view the cheat sheet, and get help.

## Spreadsheet
At the top of the spreadsheet is the current filename, buttons for creating a new spreadheet, loading an existing spreadsheet, saving
the current spreadsheet, and closing the application. Below this is a grid of cells. The number can be changed, but defaults
to 2 columns by 6 rows. Clicking on a cell will let you modify its content. There is an expanded editor window below the grid for
handling longer equations. While you are editing the content any equation will be in its raw, uncomputed form (i.e. =9.81*B1*ln(B2/B3) )
when you click on something else the result will be displayed (i.e. 1234.56). Anything the calculator supports is supported here since
they both use the same equation parser, the spreadsheet just has a pre-step that dereferences any cell references. Cell references
copy the content, not the results of a cell and are in column letter row number format. This means that B4 is the second columen and
4th row just as it would be in most other spreadsheet programs. Below the expanded cell editor you have buttons to cut, copy, and paste
cells.

## Stage Data
At the top are the column headers followed by the total for the entire craft. If you click on the "+" button it will expand and give you
the resource totals for the entire craft. It gives the current mass and the resource capacity mass. If the part is full these will be
equal, but if not capacity mass can be used for planning. The resource capacity mass is particularly useful for ore mining where it is
usually launched empty, but needs to fly full at some point its lifetime. Below this is the same output for each stage. Stages are 
numbered the same as they are in the editor (final stage is 0 and the initial stage is the highest numbered stage). There is a refresh
button that will update the data and a close button. Generally the data will automatically refresh for most changes in the VAB/SPH 
editors. In flight it may be necessary to flush to get up to date information.

## Part Data
Lists all parts on the craft. Gives the total mass, resource mass, dry mass, and stage for each part. The title, mass, and stage headers
are also buttons that changes the sorting.

## Celestial Body Data
Lists all celestial bodies. Clicking on a celestial body will give you various statistics for the body. The data is the same as you can
get from the tracking station or map view, but it is useful to have in the editor where it can be used for calculating TWR and other
critical missiong parameters while you are in the editor.

## Orbit Data
Select what type of orbiting entity you want data for (Vessel, Celestial Body, Kerbal, Asteroid). Below this a list of things that fall
in that category are shown. Click on one and the orbital data will be displayed to the right.