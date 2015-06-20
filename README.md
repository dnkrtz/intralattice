# I N T R A L A T T I C E 

IntraLattice is a collection of generative CAD components for [Grasshopper](http://www.grasshopper3d.com/). It is used to generate parametric lattice structures within a 3D design space. It is written in C# with the RhinoCommon SDK. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.

![alt text][logo]
[logo]: ./preview.png "Logo Title Text 2"

## Core Components

The complete algorithm is divided into the following modules.

1. **GRID** - Generates point grid within the design space.
  * [GridBox](../master/src/IntraLattice/1 - GRID/GridBox.cs) - Simple Cartesian Grid (3D)
  * [GridCylinder](../master/src/IntraLattice/1 - GRID/GridCylinder.cs) - Simple Cylindrical Grid
  * [GridSphere](../master/src/IntraLattice/1 - GRID/GridSphere.cs) - Simple Spherical Grid
  * [ConformSS](../master/src/IntraLattice/1 - GRID/ConformSS.cs) - Conforming Surface-to-Surface Grid
  * [ConformSA](../master/src/IntraLattice/1 - GRID/ConformSA.cs) - Conforming Surface-to-Axis Grid
  * [UniformBDS](../master/src/IntraLattice/1 - GRID/UniformBDS.cs) - Uniform Trimmed Grid (within Brep Design Space)

2. **FRAME** - Generates a lattice frame by mapping unit cell topologies to the grid.
  * [FrameConform](../master/src/IntraLattice/2 - FRAME/FrameConform.cs) - Maps lattice topology
  * [FrameUniform](../master/src/IntraLattice/2 - FRAME/FrameUniform.cs) - Maps *and trims* lattice topology
    * [FrameTools](../master/src/IntraLattice/2 - FRAME/FrameTools.cs) - Topology Generation

3. **MESH** - Generates solid mesh of the lattice frame.
  * [LatticeMesh](../master/src/IntraLattice/3 - MESH/LatticeMesh.cs) - Under Development
  * [ViewReport](../master/src/IntraLattice/3 - MESH/ViewReport.cs) - Validification of Mesh
    * [MeshTools](../master/src/IntraLattice/3 - MESH/MeshTools.cs) - Mesh Stitching, 3D Convex Hull, Data Structure

## Feature Components

  * **FEA Interface** (NASTRAN, HYPERWORKS)
**



### Stuff that isn't done yet

Task | Description 
--- | --- 
`LatticeMesh` | Needs to be finished by friday
`ConformSP` | Point grid conforming from Surface to point
`ViewReport` | Add preview functionality
`FEA` | Add Nastran interface
`Icons` | Find icons for the component toolbar
