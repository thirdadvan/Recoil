# Blender: build rigid costume appendages, with UVs, and no armature modifiers/weights.
import bpy, math
from mathutils import Vector

def build_costume(rig=None, chest_bone=None, hips_bone=None, stature=1.8):
    scale=stature/1.8
    materials=[]
    for name,color in [('Costume charcoal',(.025,.024,.023,1)),('Costume charcoal facets',(.037,.033,.030,1)),('Costume grey stitches',(.19,.16,.12,1))]:
        m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True
        bsdf=m.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=color;bsdf.inputs['Roughness'].default_value=.95
        materials.append(m)
    created=[]
    def attach(obj,bone):
        if rig and bone:
            world=obj.matrix_world.copy();obj.parent=rig;obj.parent_type='BONE';obj.parent_bone=bone;obj.matrix_world=world
        obj['attachment']='Rigid torso accessory; no arm/hand weights, no tentacle bones'
        created.append(obj)
    def tube(name,points,radii,bone,sides=6):
        pts=[Vector(p)*scale for p in points];verts=[];faces=[]
        for j,p in enumerate(pts):
            tangent=(pts[min(j+1,len(pts)-1)]-pts[max(j-1,0)]).normalized();axis=tangent.cross(Vector((0,1,0)))
            if axis.length<.01:axis=tangent.cross(Vector((1,0,0)))
            axis.normalize();other=tangent.cross(axis).normalized()
            for k in range(sides):
                a=k*2*math.pi/sides;verts.append(p+(axis*math.cos(a)+other*math.sin(a))*radii[j]*scale)
        for j in range(len(pts)-1):
            for k in range(sides):faces.append((j*sides+k,j*sides+(k+1)%sides,(j+1)*sides+(k+1)%sides,(j+1)*sides+k))
        faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(pts)-1)*sides+k for k in range(sides))])
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
        for mat in materials[:2]:mesh.materials.append(mat)
        uv=mesh.uv_layers.new(name='UVMap')
        for p in mesh.polygons:
            p.use_smooth=False;p.material_index=p.index%7==0
            for li in p.loop_indices:
                vi=mesh.loops[li].vertex_index;uv.data[li].uv=((vi%sides)/sides,(vi//sides)/(len(pts)-1))
        attach(obj,bone)
    for side in [-1,1]:
        s=lambda x,y,z:(side*x,y,z)
        tube('Shoulder outer cloth '+str(side),[s(.17,.09,1.49),s(.26,.12,1.49),s(.32,.17,1.32),s(.36,.20,1.12),s(.46,.22,.99),s(.54,.20,1.01)],[.045,.052,.046,.035,.017,.002],chest_bone)
        tube('Back hanging cloth '+str(side),[s(.11,.14,1.47),s(.18,.20,1.42),s(.22,.24,1.20),s(.26,.27,.98),s(.32,.26,.87)],[.043,.047,.038,.022,.002],chest_bone)
        tube('Hip outer cloth '+str(side),[s(.16,.01,1.07),s(.22,.035,1.01),s(.27,.05,.82),s(.34,.09,.60),s(.43,.10,.43)],[.032,.040,.034,.020,.002],hips_bone)
        tube('Hip rear cloth '+str(side),[s(.13,.13,1.00),s(.19,.19,.92),s(.22,.21,.73),s(.25,.22,.55),s(.30,.20,.48)],[.029,.035,.029,.016,.002],hips_bone)
        tube('Thin shoulder cord '+str(side),[s(.24,.15,1.40),s(.34,.18,1.31),s(.38,.20,1.13),s(.40,.21,.97)],[.010,.011,.008,.001],chest_bone,sides=4)
        for px,py,pz,bone in [(side*.18,.08,1.49,chest_bone),(side*.12,.14,1.47,chest_bone),(side*.16,0,1.07,hips_bone),(side*.13,.13,1.0,hips_bone)]:
            for k in range(3):
                bpy.ops.mesh.primitive_cube_add(size=1,location=Vector((px+(k-1)*.016,py-.025,pz)) *scale);o=bpy.context.object;o.name='Visible attachment stitch';o.dimensions=Vector((.007,.035,.007))*scale;o.rotation_euler[1]=.45*side;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(materials[2]);attach(o,bone)
    return created

if __name__=='__main__':
    import os
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    objs=build_costume()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(__file__),'Costume_Tentacles_Unrigged.blend'))
    print('STATIC COSTUME OBJECTS',len(objs),'TRIANGLES',sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objs))
