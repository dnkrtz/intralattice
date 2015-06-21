# I N T R A L A T T I C E 

IntraLattice is a collection of generative CAD components for [Grasshopper](http://www.grasshopper3d.com/). It is used to generate parametric lattice structures within a 3D design space. It is written in C# with the RhinoCommon SDK.

![alt text][logo]
[logo]: ./preview.png "Logo Title Text 2"

The basic workflow is illustrated above. We begin with a design space, a cube in this case. The first module (GRID) generates a structured grid of points within the design space, which define the corners of the unit cells. The second module (FRAME) maps the unit cell topology to this grid. Finally, the third module (MESH) converts the lattice wireframe into a solid part, ready for 3D printing.

## Core Components

The complete algorithm is divided into the following modules.

1. **GRID** - Generates point grid within the design space.
  * [GridBox](../master/src/IntraLattice/1 - GRID/GridBox.cs) - Simple Cartesian Grid (3D)
  * [GridCylinder](../master/src/IntraLattice/1 - GRID/GridCylinder.cs) - Simple Cylindrical Grid
  * [GridSphere](../master/src/IntraLattice/1 - GRID/GridSphere.cs) - Simple Spherical Grid
  * [GridConformSS](../master/src/IntraLattice/1 - GRID/GridConformSS.cs) - Conforming Surface-to-Surface Grid
  * [GridConformSA](../master/src/IntraLattice/1 - GRID/GridConformSA.cs) - Conforming Surface-to-Axis Grid
  * [GridUniform](../master/src/IntraLattice/1 - GRID/GridUniform.cs) - Uniform Trimmed Grid (within Brep or Mesh)

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
