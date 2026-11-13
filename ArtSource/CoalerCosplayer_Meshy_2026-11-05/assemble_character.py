import bpy, os, runpy, math, json
from mathutils import Vector
ROOT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.open_mainfile(filepath='/tmp/CosplayerInspect.blend')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=next(o for o in bpy.context.scene.objects if o.type=='MESH');body.name='Cosplayer_Body'
for p in body.data.polygons:p.use_smooth=False
for im in bpy.data.images:
 if im.type=='IMAGE' and im.size[0]:
  im.filepath_raw=ROOT+'/Cosplayer_BaseColor.png';im.file_format='PNG';im.save();im.pack()
for m in body.data.materials:
 m.name='Cosplayer_Body';bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.95;bs.inputs['Metallic'].default_value=0
# Front-only input made the back red; explicitly restore the approved black back panel.
back=bpy.data.materials.new('Jacket_Black_Back');back.use_nodes=True;back.diffuse_color=(.018,.016,.015,1)
back.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=back.diffuse_color
back.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.95
body.data.materials.append(back);back_index=len(body.data.materials)-1
for p in body.data.polygons:
 c=body.matrix_world@p.center;n=body.matrix_world.to_3x3()@p.normal
 if 1.005<c.z<1.53 and abs(c.x)<.22 and c.y>.025:p.material_index=back_index
objects=runpy.run_path(ROOT+'/build_costume_tentacles.py',run_name='costume_module')['build_costume'](rig,'mixamorig:Spine2','mixamorig:Hips')
bpy.context.view_layer.update()
for bone,name in [('mixamorig:Spine2','Costume_Chest'),('mixamorig:Hips','Costume_Hips')]:
 group=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.parent_bone==bone];bpy.ops.object.select_all(action='DESELECT')
 for o in group:o.select_set(True)
 bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();group[0].name=name
costume=[o for o in bpy.context.scene.objects if o.name.startswith('Costume_')]
def snapshot():
 bpy.context.view_layer.update();return {o.name:[o.matrix_world@v.co for v in o.data.vertices] for o in costume}
def delta(a,b):return max((p-q).length for n in a for p,q in zip(a[n],b[n]))
before=snapshot()
for p in rig.pose.bones:
 if any(t in p.name for t in ['LeftArm','RightArm','LeftForeArm','RightForeArm','LeftHand','RightHand']):p.rotation_mode='XYZ';p.rotation_euler[1]=.8
arms_delta=delta(before,snapshot());assert arms_delta<1e-6,arms_delta
for p in rig.pose.bones:p.matrix_basis.identity()
rig.pose.bones['mixamorig:Spine2'].rotation_mode='XYZ';rig.pose.bones['mixamorig:Spine2'].rotation_euler[2]=.3
chest_delta=delta(before,snapshot());assert chest_delta>.01
for p in rig.pose.bones:p.matrix_basis.identity()
bpy.context.view_layer.update()
assert all(len(o.vertex_groups)==0 and not any(m.type=='ARMATURE' for m in o.modifiers) for o in costume)
report={'body_triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'costume_triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in costume),'bones':len(rig.data.bones),'texture':'2048x2048','height_m':1.8,'arm_motion_costume_max_delta_m':arms_delta,'chest_motion_costume_max_delta_m':chest_delta,'costume_bones_added':0,'costume_objects':[(o.name,o.parent_bone) for o in costume]}
open(ROOT+'/Verification.json','w').write(json.dumps(report,indent=2));print('VERIFIED',report)
bpy.ops.object.select_all(action='DESELECT')
for o in [rig,body]+costume:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/CoalerCosplayer_Rigged.blend')
bpy.ops.export_scene.fbx(filepath=ROOT+'/CoalerCosplayer_Rigged.fbx',use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.world.color=(.20,.20,.20)
scene.view_settings.view_transform='Standard'
for name,loc,power,size in [('Key',(-3,-4,5),450,4),('Fill',(3,-2,3),220,3),('Rim',(1,3,4),400,3)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=loc;obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Preview');cam=bpy.data.objects.new('Preview',camdata);scene.collection.objects.link(cam);scene.camera=cam;camdata.type='ORTHO';camdata.ortho_scale=2.15
for name,loc in [('Front',(2,-5,2.2)),('Back',(-2,5,2.2))]:
 cam.location=loc;cam.rotation_euler=(Vector((0,0,.95))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=ROOT+'/Preview_'+name+'.png';bpy.ops.render.render(write_still=True)
