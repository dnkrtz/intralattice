## Project structure

The algorithm is divided into the following modules.

1. **GRID** - Generates point grid within the design space.
  * [GridBox](../master/GridBox/GridBox/GridBoxComponent.cs) - Simple Cartesian Grid (3D)
  * [GridCylinder](../master/GridCylinder/GridCylinder/GridCylinderComponent.cs) - Simple Cylindrical Grid
  * [GridSphere](../master/GridSphere/GridSphere/GridSphereComponent.cs) - Simple Spherical Grid
  * [ConformSS](../master/ConformSS/ConformSS/ConformSSComponent.cs) - Conforming Surface-to-Surface Grid
  * [ConformSA](../master/ConformSA/ConformSA/ConformSAComponent.cs) - Conforming Surface-to-Axis Grid

2. **FRAME** - Generates a lattice frame by mapping unit cell topologies to the grid.
  * [CellMapper](../master/CellMapper/CellMapper/CellMapperComponent.cs) - Maps unit cell

3. **MESH** - Generates solid mesh of the lattice frame.
  * [LatticeMesh](../master/LatticeMesh/LatticeMesh/LatticeMeshComponent.cs) - Under Development
  * [ViewReport](../master/ViewReport/ViewReport/ViewReportComponent.cs) - Validification of Mesh

