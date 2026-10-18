package sandbox

import "core:time"

MAX_QUADS :: 6*CHUNK*CHUNK*CHUNK
Vertex :: struct #packed { x,y,z,data:u16 }
Mesh :: struct { vertices:[dynamic]Vertex, indices:[dynamic]u32, overflow:bool, faces:int }
Face :: struct { material:u8, ao:[4]u8 }
face_equal :: proc(a,b:Face)->bool {return a.material==b.material && a.ao==b.ao}
face_mergeable :: proc(a:Face)->bool {return a.ao[0]==a.ao[1] && a.ao[0]==a.ao[2] && a.ao[0]==a.ao[3]}
NORMALS :=  [6]I3{{1,0,0},{-1,0,0},{0,1,0},{0,-1,0},{0,0,1},{0,0,-1}}
U_AXIS :=  [6]int{1,2,2,0,0,1}
V_AXIS :=  [6]int{2,1,0,2,1,0}
sample :: proc(data:[]u8,p:I3)->u8 {return data[block_index(int(p.x+1),int(p.y+1),int(p.z+1))]}
solid :: proc(data:[]u8,p:I3)->int {if sample(data,p)!=0{return 1};return 0}
face_ao :: proc(data:[]u8,p:I3,dir:int)->[4]u8 {
    q:=p+NORMALS[dir];u,v:=U_AXIS[dir],V_AXIS[dir]
    result:[4]u8
    for c in 0..<4 {
        su,sv:i32=-1,-1;if c==1||c==2{su=1};if c>=2{sv=1}
        a,b:=q,q;a[u]+=su;b[v]+=sv;corner:=a;corner[v]+=sv
        s1,s2:=solid(data,a),solid(data,b)
        if s1==1 && s2==1 {result[c]=0}else{result[c]=u8(3-s1-s2-solid(data,corner))}
    };return result
}
mesh_destroy :: proc(m:^Mesh) {delete(m.vertices);delete(m.indices);m^={}}
mesh_build :: proc(data:[]u8,greedy:bool=true)->Mesh {
    m:Mesh
    // Reserve the exposed-face upper bound once; greedy merging only shrinks it.
    face_count:=0
    for z in 0..<CHUNK {for y in 0..<CHUNK {for x in 0..<CHUNK {
        p:=I3{i32(x),i32(y),i32(z)}
        if sample(data,p)==0 {continue}
        for n in NORMALS {if sample(data,p+n)==0 {face_count+=1}}
    }}}
    if face_count==0 {return m}
    if face_count>MAX_QUADS {m.overflow=true;return m}
    m.vertices=make([dynamic]Vertex,0,face_count*4)
    m.indices=make([dynamic]u32,0,face_count*6)
    mask:[CHUNK*CHUNK]Face
    for dir in 0..<6 {
        axis:=dir/2;u,v:=U_AXIS[dir],V_AXIS[dir]
        for plane in 0..<CHUNK {
            mask={}
            for j in 0..<CHUNK {for i in 0..<CHUNK {
                p:I3;p[axis]=i32(plane);p[u]=i32(i);p[v]=i32(j)
                b:=sample(data,p)
                if b!=0 && sample(data,p+NORMALS[dir])==0 {mask[i+j*CHUNK]={b,face_ao(data,p,dir)};m.faces+=1}
            }}
            for j:=0;j<CHUNK;j+=1 {for i:=0;i<CHUNK; {
                f:=mask[i+j*CHUNK];if f.material==0 {i+=1;continue}
                width,height:=1,1
                if greedy && face_mergeable(f) {
                    for i+width<CHUNK && face_equal(f,mask[i+width+j*CHUNK]) {width+=1}
                    grow: for j+height<CHUNK {
                        for k in 0..<width {if !face_equal(f,mask[i+k+(j+height)*CHUNK]) {break grow}}
                        height+=1
                    }
                }
                if len(m.indices)/6>=MAX_QUADS {m.overflow=true;return m}
                p:I3;p[axis]=i32(plane);if dir%2==0 {p[axis]+=1};p[u]=i32(i);p[v]=i32(j)
                base:=u32(len(m.vertices))
                for c in 0..<4 {
                    q:=p;if c==1||c==2{q[u]+=i32(width)};if c>=2{q[v]+=i32(height)}
                    packed:=u16(dir)|(u16(f.material)<<3)|(u16(f.ao[c])<<7)
                    append(&m.vertices,Vertex{u16(q.x),u16(q.y),u16(q.z),packed})
                }
                if int(f.ao[0])+int(f.ao[2])>int(f.ao[1])+int(f.ao[3]) {
                    append(&m.indices,base,base+1,base+3,base+1,base+2,base+3)
                }else{append(&m.indices,base,base+1,base+2,base,base+2,base+3)}
                for y in 0..<height {for x in 0..<width {mask[i+x+(j+y)*CHUNK]={}}}
                i+=width
            }}
        }
    }
    return m
}
Mesh_Job :: struct { key:I3, revision:u64, seed:u64, edits:[dynamic]Edit_Command, naive:bool }
Mesh_Result :: struct { key:I3, revision:u64, mesh:Mesh, generation_ms,meshing_ms:f64 }
build_job :: proc(job:Mesh_Job)->Mesh_Result {
    start:=time.tick_now()
    data:=make([]u8,PAD*PAD*PAD);defer delete(data)
    // Height/tree work is shared by each vertical column.
    for z in 0..<PAD {for x in 0..<PAD {
        wx,wz:=job.key.x*32+i32(x)-1,job.key.z*32+i32(z)-1
        h:=terrain_height(job.seed,wx,wz)
        trees:[9]Tree;ti:=0
        for dz in -1..=1 {for dx in -1..=1 {trees[ti]=tree_at(job.seed,floor_div(wx,8)+i32(dx),floor_div(wz,8)+i32(dz));ti+=1}}
        for y in 0..<PAD {
            wy:=job.key.y*32+i32(y)-1
            b:=base_block(job.seed,{wx,wy,wz},h)
            if b==.Air && wy>0 && wy<HEIGHT {for t in trees {
                if !t.active {continue}
                if wx==t.x && wz==t.z && wy>t.h && wy<=t.h+5 {b=.Wood;break}
                if abs(wx-t.x)<=2 && abs(wz-t.z)<=2 && wy>=t.h+3 && wy<=t.h+6 && abs(wx-t.x)+abs(wz-t.z)+abs(wy-(t.h+4))<=5 {b=.Leaves}
            }}
            data[block_index(x,y,z)]=u8(b)
        }
    }}
    for e in job.edits {p:=e.position-job.key*32+I3{1,1,1};data[block_index(int(p.x),int(p.y),int(p.z))]=u8(e.block)}
    generated:=time.tick_now();mesh:=mesh_build(data,!job.naive)
    return {job.key,job.revision,mesh,time.duration_milliseconds(time.tick_diff(start,generated)),time.duration_milliseconds(time.tick_since(generated))}
}
