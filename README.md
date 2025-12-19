# HybridProceduralTerrain_FLaxEngine
HybridProceduralTerrain is a procedural sculpting pipeline for Flax Engine supporting 3D brushes, spline-guided sculpting, multi-patch terrain, directional ridge warping, curvature sharpening, hydraulic/thermal erosion, and beach falloff at patch edges

<img width="1495" height="741" alt="image" src="https://github.com/user-attachments/assets/293efffa-8c05-4d9d-b94a-539cb175583b" />

Video Demo : https://www.youtube.com/watch?v=yX1UY8cSv8s


# Components & Roles
HybridProceduralTerrain (orchestrator): UI/editor buttons, change hashing, triggers builds, gathers brushes/splines, calls generator, applies heightmaps to terrain patches

TerrainPatchGenerator: Builds per-patch heightmaps (multi-patch) with pipeline: noise + brush/spline offsets + curvature (optional) + hydraulic erosion + thermal erosion + beach falloff + saturate

BrushSampler: Computes height offsets from SculptBrush (sphere/box/capsule, add/sub, falloff, smoothing)

SplineSampler: Computes height offsets from SculptSplineSample (built-in Flax spline) with width/falloff/smoothing and add/sub mode

BeachFalloff: Lowers patch edges to zero with configurable width/curve

Data/helpers: PatchHeightmap (coord, map), TerrainCurvature, WorldHydrology, WorldNoise (noise + directional warp)

# Key Parameters
Resolution & Scale: SampleSpacing (meters between samples), PatchGrid (X,Y patch count)

Noise/Shape: NoiseScale, Roughness, UseMountainMask, MountainMaskScale, UseTerrace, TerraceSteps, RidgeSharpness

Directional Ridges: UseDirectionalWarp, WarpAngleDeg, WarpStrength, WarpFrequency

Sculpt Control: GlobalSmoothing (brush falloff blend)

Curvature: UseCurvatureDetail, RidgeGain, ValleyRelax, CurvatureIterations

Erosion: Droplets, ErosionRate, DepositionRate, Gravity (hydraulic), ThermalStrength, AngleThreshold

Beach: BeachEnabled, BeachWidth (meters), BeachSmoothness

Realtime: AutoRebuild, RebuildDebounce

# Workflow
Add HybridProceduralTerrain to the terrain actor,
Configure noise/shape/erosion/curvature/beach params

Add sculpt entities:
Brush: “Add Sculpt Brush”, choose shape, Add/Sub mode, radius/falloff/intensity
Spline: “Add Sculpt Spline”, edit built-in Flax spline points, set width/falloff/intensity/mode

Build terrain:
Manual: click “Build Procedural World”
Realtime: enable AutoRebuild (hash-based) with RebuildDebounce

Output: heightmaps per patch applied via AddPatch + SetupPatchHeightMap (grid from PatchGrid), pipeline = noise → sculpt → curvature → erosion → beach → saturate

# Performance Notes
Multi-patch plus hydraulic erosion can be heavy: tune Droplets, PatchGrid, and SampleSpacing.
AutoRebuild runs the full pipeline: disable while tweaking heavy settings.
Beach falloff/curvature are light: hydraulic erosion is the most expensive.

