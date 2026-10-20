FACTORY 3D ART STUDY

Open Assets/Scenes/RecoilFactoryArt3D.unity.
This is a separate visual study based on the saved RecoilFactoryInterior layout.
The original playable factory and doorway destinations were not changed.
Characters are static 3D scale references, not gameplay actors.

Play controls: A/D or arrow keys = horizontal camera travel;
mouse wheel = lens zoom; Home = reset camera.
Select Art Camera to edit framing, field of view and movement limits.

Scene hierarchy:
01 Structure - existing XY layout: floor, low roof, end walls, maintenance platform.
02 Depth - machinery and lighting Z 2 to 12: pressure vessels, catwalk, pipes,
    door, cabinets, distant window. Each lamp owns its light as a child.
03 Foreground - Z minus 4: pillars, near pipes and barrier.
04 Surface details: joints, drainage, damp patches, wall stains.
05 Scale reference characters - art only: original positions, simplified models.
Main Light: weak cold fill.
Art Camera: perspective camera plus optional film grading material.

Scripts:
FactoryArtCamera: camera navigation and single-pass image grading.
FactoryArtLamp: smooth emergency light pulse, no gameplay sound-wave coupling.
Editor/FactoryArtTestBuilder: one-time construction scaffold. Refuses to overwrite
an existing art scene. Further edits belong in the saved scene itself.

Materials and procedural surface maps are editable in RecoilArtTest/Materials.
This scene uses the project's built-in render pipeline and adds no packages.
No gameplay, monster perception, exposure coefficient or GDD rules were changed.

Before switching, the unsaved outdoor scene was preserved as a separate scene:
Assets/Scenes/ArtTestBackups/RecoilPrototype_20261008_160207.unity
Open this backup to recover those unsaved edits; it is not a replacement of the
original outdoor scene on disk.
