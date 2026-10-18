package sandbox

import "core:testing"
import "core:os"
import "core:mem"
import "core:sync"
import "core:time"
import "core:fmt"

@(test)
test_coordinates :: proc(t:^testing.T) {
    testing.expect(t,floor_div(-1,32)==-1)
    testing.expect(t,floor_div(-32,32)==-1)
    testing.expect(t,floor_div(-33,32)==-2)
    testing.expect(t,chunk_of({-1,255,-33})==I3{-1,7,-2})
}
@(test)
test_world_commands_and_snapshot :: proc(t:^testing.T) {
    w:World;world_init(&w,42);defer world_destroy(&w)
    testing.expect(t,world_apply(&w,{1,{-1,100,0},.Wood}))
    testing.expect(t,!world_apply(&w,{1,{-1,100,0},.Stone}))
    testing.expect(t,!world_apply(&w,{2,{0,0,0},.Air}))
    testing.expect(t,!world_apply(&w,{2,{0,256,0},.Stone}))
    testing.expect(t,world_get(&w,{-1,100,0})==.Wood)
    testing.expect(t,world_apply(&w,{2,{2000,100,2000},.Stone}))
    snapshot:=world_snapshot(&w,{0,3,0});defer delete(snapshot)
    testing.expect(t,len(snapshot)==1)
    if len(snapshot)==1 {testing.expect(t,snapshot[0].position==I3{-1,100,0})}
}
@(test)
test_deterministic_terrain_and_padding :: proc(t:^testing.T) {
    for x in -36..<36 {for z in -2..<2 {
        a:=terrain_height(42,i32(x),i32(z));b:=terrain_height(42,i32(x),i32(z))
        testing.expect(t,a==b && a>=38 && a<128)
        testing.expect(t,generated_block(42,{i32(x),0,i32(z)})==.Bedrock)
        testing.expect(t,generated_block(42,{i32(x),256,i32(z)})==.Air)
    }}
    // Worker column generation must exactly match random-access world queries.
    w:World;world_init(&w,42);defer world_destroy(&w)
    job:=Mesh_Job{key={-1,2,0},revision=9,seed=42}
    result:=build_job(job);defer mesh_destroy(&result.mesh)
    // Check generated mesh area against the authoritative sampler, including halo.
    data:=make([]u8,PAD*PAD*PAD);defer delete(data)
    for z in 0..<PAD {for y in 0..<PAD {for x in 0..<PAD {
        p:=job.key*32+I3{i32(x)-1,i32(y)-1,i32(z)-1}
        data[block_index(x,y,z)]=u8(world_get(&w,p))
    }}}
    reference:=mesh_build(data);defer mesh_destroy(&reference)
    testing.expect(t,result.mesh.faces==reference.faces)
    testing.expect(t,len(result.mesh.vertices)==len(reference.vertices))
    for v,i in reference.vertices {if i<len(result.mesh.vertices) {testing.expect(t,v==result.mesh.vertices[i])}}
}
mesh_area :: proc(m:Mesh)->int {
    area:=0
    for q:=0;q<len(m.vertices);q+=4 {
        a,b,c:=m.vertices[q],m.vertices[q+1],m.vertices[q+3]
        u:=abs(int(a.x)-int(b.x))+abs(int(a.y)-int(b.y))+abs(int(a.z)-int(b.z))
        v:=abs(int(a.x)-int(c.x))+abs(int(a.y)-int(c.y))+abs(int(a.z)-int(c.z))
        area+=u*v
    };return area
}
reference_faces :: proc(data:[]u8)->int {
    count:=0
    for z in 0..<CHUNK {for y in 0..<CHUNK {for x in 0..<CHUNK {
        p:=I3{i32(x),i32(y),i32(z)}
        if sample(data,p)!=0 {for n in NORMALS {if sample(data,p+n)==0 {count+=1}}}
    }}};return count
}
check_winding :: proc(t:^testing.T,m:Mesh) {
    for i:=0;i<len(m.indices);i+=3 {
        a,b,c:=m.vertices[m.indices[i]],m.vertices[m.indices[i+1]],m.vertices[m.indices[i+2]]
        u:=V3{f64(b.x)-f64(a.x),f64(b.y)-f64(a.y),f64(b.z)-f64(a.z)}
        v:=V3{f64(c.x)-f64(a.x),f64(c.y)-f64(a.y),f64(c.z)-f64(a.z)}
        n:=cross(u,v);dir:=int(a.data&7);expected:=NORMALS[dir]
        testing.expect(t,n.x*f64(expected.x)+n.y*f64(expected.y)+n.z*f64(expected.z)>0)
    }
}
@(test)
test_mesher_shapes_and_winding :: proc(t:^testing.T) {
    data:=make([]u8,PAD*PAD*PAD);defer delete(data)
    empty:=mesh_build(data);defer mesh_destroy(&empty)
    testing.expect(t,len(empty.vertices)==0)
    for z in 1..=CHUNK {for y in 1..=CHUNK {for x in 1..=CHUNK {data[block_index(x,y,z)]=u8(Block.Stone)}}}
    solid_mesh:=mesh_build(data);defer mesh_destroy(&solid_mesh)
    testing.expect(t,len(solid_mesh.indices)==36)
    testing.expect(t,mesh_area(solid_mesh)==6*CHUNK*CHUNK)
    check_winding(t,solid_mesh)
    // Adjacent solid chunk suppresses an entire shared face.
    for z in 1..=CHUNK {for y in 1..=CHUNK {data[block_index(PAD-1,y,z)]=u8(Block.Stone)}}
    neighbor:=mesh_build(data);defer mesh_destroy(&neighbor)
    testing.expect(t,mesh_area(neighbor)==5*CHUNK*CHUNK)
    check_winding(t,neighbor)
    for &b in data {b=0}
    for z in 1..=CHUNK {for y in 1..=CHUNK {for x in 1..=CHUNK {
        if hash3(83,i32(x),i32(y),i32(z))%7==0 {data[block_index(x,y,z)]=u8(Block.Stone)}
    }}}
    irregular:=mesh_build(data);defer mesh_destroy(&irregular)
    testing.expect(t,mesh_area(irregular)==reference_faces(data))
    check_winding(t,irregular)
}
@(test)
test_checkerboard_is_bounded_and_complete :: proc(t:^testing.T) {
    data:=make([]u8,PAD*PAD*PAD);defer delete(data)
    for z in 1..=CHUNK {for y in 1..=CHUNK {for x in 1..=CHUNK {
        if (x+y+z)%2==0 {data[block_index(x,y,z)]=u8(Block.Stone)}
    }}}
    m:=mesh_build(data);defer mesh_destroy(&m)
    testing.expect(t,!m.overflow)
    testing.expect(t,mesh_area(m)==reference_faces(data))
    testing.expect(t,len(m.indices)/6==98304)
    testing.expect(t,cap(m.vertices)*size_of(Vertex)+cap(m.indices)*4<=6*MiB)
}
@(test)
test_raycast :: proc(t:^testing.T) {
    w:World;world_init(&w,42);defer world_destroy(&w)
    world_apply(&w,{1,{-2,200,0},.Stone})
    hit:=raycast(&w,{0.5,200.5,0.5},{-1,0,0},8)
    testing.expect(t,hit.hit && hit.block==I3{-2,200,0} && hit.adjacent==I3{-1,200,0})
    miss:=raycast(&w,{0.5,200.5,0.5},{-1,0,0},1)
    testing.expect(t,!miss.hit)
    inside:=raycast(&w,{-1.5,200.5,0.5},{0,1,0},8)
    testing.expect(t,inside.hit && inside.block==I3{-2,200,0})
}
@(test)
test_save_roundtrip_and_rejection :: proc(t:^testing.T) {
    dir,err:=os.make_directory_temp("","voxel-test-*",context.allocator)
    testing.expect(t,err==nil);if err!=nil {return};defer delete(dir);defer os.remove(dir)
    path:=fmt.tprintf("%s/world.vox",dir);defer os.remove(path)
    w:World;world_init(&w,99);defer world_destroy(&w)
    world_apply(&w,{1,{-33,150,8},.Leaves});world_apply(&w,{2,{16,77,-1},.Air})
    testing.expect(t,world_save(&w,{2.5,95,-33},path))
    testing.expect(t,world_save(&w,{2.5,95,-33},path))
    loaded:World;world_init(&loaded,0);defer world_destroy(&loaded);p:V3
    testing.expect(t,world_load(&loaded,&p,path))
    testing.expect(t,loaded.seed==99 && loaded.sequence==2 && loaded.edit_count==2 && p==V3{2.5,95,-33})
    testing.expect(t,world_get(&loaded,{-33,150,8})==.Leaves)
    testing.expect(t,world_get(&loaded,{16,77,-1})==.Air)
    testing.expect(t,os.write_entire_file(path,"VOX1-broken")==nil)
    testing.expect(t,!world_load(&loaded,&p,path))
    testing.expect(t,loaded.seed==99 && loaded.edit_count==2)
}
@(test)
test_stale_jobs_and_corner_invalidation :: proc(t:^testing.T) {
    s:Stream;s.chunks=make(map[I3]^Chunk_State);defer delete(s.chunks)
    for z in -1..=0 {for y in 1..=2 {for x in -1..=0 {
        k:=I3{i32(x),i32(y),i32(z)};c:=new(Chunk_State);s.next_revision+=1;c.revision=s.next_revision;c.complete=true;s.chunks[k]=c
    }}}
    defer for _,c in s.chunks {free(c)}
    key:=I3{0,2,0};old:=s.chunks[key].revision
    testing.expect(t,result_current(&s,key,old))
    stream_invalidate(&s,{0,64,0})
    testing.expect(t,!result_current(&s,key,old))
    for _,c in s.chunks {testing.expect(t,!c.complete)}
    free(s.chunks[key]);delete_key(&s.chunks,key);testing.expect(t,!result_current(&s,key,old))
    // The new resident at an identical coordinate must never accept an old job.
    c:=new(Chunk_State);s.next_revision+=1;c.revision=s.next_revision;s.chunks[key]=c
    testing.expect(t,!result_current(&s,key,old))
}
@(test)
test_worker_shutdown_and_accounting :: proc(t:^testing.T) {
    tracker:mem.Tracking_Allocator;mem.tracking_allocator_init(&tracker,context.allocator)
    defer mem.tracking_allocator_destroy(&tracker)
    context.allocator=mem.tracking_allocator(&tracker)
    for pass in 0..<4 {
        w:World;world_init(&w,42)
        world_apply(&w,{1,{0,64,0},.Stone})
        s:Stream;r:Renderer
        stream_init(&s,2,1,8*MiB,128*MiB,MiB,&tracker)
        stream_reconcile(&s,&r,{0,70,0})
        stream_schedule(&s,&w,{0,70,0})
        if pass%2==0 {time.sleep(10*time.Millisecond)}
        stream_destroy(&s,&r);world_destroy(&w)
    }
    sync.mutex_lock(&tracker.mutex);remaining:=tracker.current_memory_allocated;sync.mutex_unlock(&tracker.mutex)
    testing.expect(t,remaining==0,fmt.tprintf("leaked %d bytes",remaining))
}
