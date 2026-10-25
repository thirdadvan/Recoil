WAREHOUSE LIGHTING STUDY

Open: Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity
Or menu: Recoil > Art Tests > Open Warehouse Lighting Test

Play normally. F6 switches ORIGINAL LOOK / TEST LOOK at the same position.
The comparison includes light intensity, ambient, distance fog, decorative key/bounce
lights, local atmospheric scattering and static rack contact shadows.
Existing gameplay visibility masking remains active in both versions.

Edit the Warehouse Atmosphere Test component on Main Camera:
- Test Look: compare in Edit mode too
- Scattering: visible local dust-light strength
- Soft Glow: extra diffuse spread of atmospheric light (not HDR source bloom)
- Test Ambient / Test Fog / Test Start / Test End: scene depth and dark baseline

Added key and reflected lights are children of hanging lamp fixtures.
Soft contact patches are under INSIDE lighting study - presentation only.
New decorative lights do not register as gameplay exposure sources.
The original warehouse scene and its shared materials are unchanged.

Scope: a first visual study, not the complete INSIDE rendering pipeline.
Local scattering is bounded by spotlight cones and camera depth; it does NOT sample
shadow maps. Direct key lights do cast real shadows. No TAA or SSR is installed.
Contact patches ground static racks only; moving-character AO is not implemented.
No frame-rate claim is made; GPU profiling is required before a general rollout.
