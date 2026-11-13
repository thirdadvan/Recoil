Retro Water Blaster — modular weapon assets

Meshy 6 image-to-3D generated the body and reservoir source GLB files in Models/.
The final body uses the Meshy geometry and textures. Following the requested
correction, the reservoir was re-surfaced as a smooth circular cylinder with a
clean opaque teal material. The original Meshy tank is retained as source.

Prefabs/RetroWaterBlaster.prefab is the complete assembly. Small is equipped by
default. Prefabs/TankSmall.prefab, TankMedium.prefab, and TankLarge.prefab are
independent reusable models; pivot at the rear connector, extending along +X.

Body length: 0.50 m. Tank lengths: 0.22 / 0.36 / 0.50 m.
ALL three tank diameters: 0.084 m. Neck, shoulder, and closed end profiles are
identical; only the straight cylindrical wall length changes. The large tank
matches the body's overall length. No exposed threads and no tank-shaped cradle.

The RetroWaterBlaster component exposes Small / Medium / Large and SetTankSize.
The sizes are visual assets only; ammo capacity and recoil gameplay are unchanged.
MainGrip / SupportGrip / Muzzle / TankSocket are editable attachment anchors.
The two grip anchors are full palm frames: +Z follows the fingers, +Y points
out of the palm. Both hand positions and orientations are rigidly attached.
DawnRigidWeaponGrip solves each upper/lower arm after animation evaluation,
preserving bone lengths and placing the shared gun pose on both almost-full
arm-reach surfaces. Anatomical outward elbow poles and additional rear-elbow
bend keep the sleeves clear of the torso. Level aim is constrained to chest
height while staying on the same rigid two-hand reach circle. Recoil moves the
entire gun/hand assembly. Vertical aim keeps the last
facing side to avoid hand flips at straight up/down. Running/flight preserve the
same grip frames; climbing releases them and hides the weapon.

Recoil > Water Blaster menu rebuilds assets or installs them into 3D scenes.
15 held weapons and 2 pickups were replaced across 15 scenes. Existing scene
files were backed up to Backups/RetroWaterBlaster/2026-11-07/Scenes/.

Verification and rendered previews: Captures/RetroWaterBlaster/.
Checked equal diameters and end geometry, exact model lengths, material support,
one active tank, two-hand grip alignment, left-facing orientation, and hiding
the weapon while climbing.
See Captures/WeaponGrip/Verification.txt for the updated three-photo grip tests:
12 aim directions and running/flight samples, fixed palm rotations, arm-length
invariance, shared recoil, nozzle-aligned firing, and climbing release.
The down/level/up reference poses also check arm centreline and inward sleeve
probes against the baked torso surface. Level muzzle/chest height difference
is approximately 0.017 m.
Captures/WeaponGrip/Reference-Poses.png shows down / level / up.

GLB import dependency: com.unity.cloud.gltfast 6.14.1.
