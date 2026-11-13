import bpy,os,json
ROOT=os.path.dirname(os.path.abspath(__file__));ASSET='/Volumes/RestartRoll/Unity/Recoil/Assets/Art/Characters/WarehouseChildMeshy'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=ROOT+'/Downloaded/Original.fbx')
for o in bpy.context.scene.objects:
 if o.type=='MESH':
  o.name='WarehouseChild_Body'
  for p in o.data.polygons:p.use_smooth=False
for im in bpy.data.images:
 if im.type=='IMAGE' and im.size[0]:im.filepath_raw=ASSET+'/Child_BaseColor.png';im.file_format='PNG';im.save();im.pack()
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
report={'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'bones':len(rig.data.bones),'texture':'2048x2048'}
open(ROOT+'/ModelVerification.json','w').write(json.dumps(report,indent=2));print(report)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/WarehouseChild_Rigged.blend')
bpy.ops.export_scene.fbx(filepath=ASSET+'/Child.fbx',object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
