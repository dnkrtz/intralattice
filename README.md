## Overview

Intralattice is a collection of generative CAD components for [Grasshopper](http://www.grasshopper3d.com/), which are used to generate parametric lattice structures within a 3D design space. It is written in C# with the RhinoCommon SDK.


## Motivation

The freedom of form enabled by 3D printing allows integration of new orders of complexity into designs. Lattice structures can be used as a means of

  + Volume reduction (i.e. minimize weight)
  + Increase of surface area (i.e. maximize heat transfer)
  + Porosity generation (i.e. bone graft implants)
  + Topology optimization (i.e. when combined with FEA)

## Workflow

![alt text][logo]
[logo]: ./preview.png "Logo Title Text 2"

The basic workflow is illustrated above. We begin with a design space, a cube in this case. The first module (GRID) generates a structured grid of points within the design space, which define the corners of the unit cells. The second module (FRAME) maps the unit cell topology to this grid. Finally, the third module (MESH) converts the lattice wireframe into a solid part, ready for 3D printing.

## Core Components

The complete algorithm is divided into the following modules.

1. **CELL** - Defines a unit cell topology
  * [PresetCell](../master/src/IntraLattice/CELL/PresetCell.cs) - Library of built-in unit cells
  * [CustomCell](../master/src/IntraLattice/CELL/CustomCell.cs) - Input for user-defined unit cell (includes validation)
    * [CellTools](../master/src/IntraLattice/CELL/CellTools.cs) - Unit cell validification and formatting methods
  

2. **FRAME** - Generates a lattice frame by mapping the unit cell topology
  * [ConformBox](../master/src/IntraLattice/FRAME/ConformBox.cs) - Simple box lattice
  * [ConformCylinder](../master/src/IntraLattice/FRAME/ConformCylinder.cs) - Simple cylinder lattice
  * [ConformSS](../master/src/IntraLattice/FRAME/ConformSS.cs) - Conforming Surface-to-Surface lattice
  * [ConformSA](../master/src/IntraLattice/FRAME/ConformSA.cs) - Conforming Surface-to-Axis lattice
  * [ConformSP](../master/src/IntraLattice/FRAME/ConformSP.cs) - Conforming Surface-to-Point lattice
  * [UniformDS](../master/src/IntraLattice/FRAME/UniformDS.cs) - Trimmed Uniform Lattice (within Brep or Mesh)
    * [FrameTools](../master/src/IntraLattice/FRAME/FrameTools.cs) - Topology Generation

3. **MESH** - Generates solid mesh of the lattice frame.
  * [LatticeMesh](../master/src/IntraLattice/MESH/LatticeMesh.cs) - Under Development
  * [ViewReport](../master/src/IntraLattice/MESH/ViewReport.cs) - Validification of Mesh
    * [MeshTools](../master/src/IntraLattice/MESH/MeshTools.cs) - Mesh Stitching, 3D Convex Hull, Data Structure

## Feature Components

  * **FEA Interface** (NASTRAN, HYPERWORKS)
**



### Stuff that isn't done yet

Task | Description 
--- | --- 
`LatticeMesh` | Needs to be finished by friday
`ViewReport` | Add preview functionality
`FEA` | Add Nastran interface
`Icons` | Find icons for the component toolbar
