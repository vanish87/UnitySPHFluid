# UnitySPHFluid

Follows the [Smoothed Particle Hydrodynamics Techniques for the Physics Based Simulation of Fluids and Solids](https://interactivecomputergraphics.github.io/SPH-Tutorial/) to implement 3D SPH Fluid in Unity. 

**It is still under development.**

- [x] State Equation Pressure Term
- [x] Cubic spline kernel
- [x] Uniform Space Grid Hashing/Sorting
- [x] Vorticity Confinement Term
- [x] Micropolar Model Vorticity Term
- [x] 13K(1024x128)/26K(1024x256)/54K(1024x512)/1M(1024x1024) number of particles in real time
- [x] SDF/Obstacle Particles for boundary
- [ ] Poisson solver for pressure term
- [ ] Surface tension term
- [ ] Solid/Fluid Coupling

https://user-images.githubusercontent.com/1934796/141450369-9d4fc7a3-bae1-42e2-aedf-298c3904c149.mp4

![](Gifs/sph1m.gif)
![](Gifs/sph1.gif)
![](Gifs/waterfall_fluid.gif)
![](Gifs/sph2.gif)
![](Gifs/sph3.gif)
![](Gifs/sph4.gif)
![](Gifs/sph5.gif)
