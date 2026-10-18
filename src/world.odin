package sandbox

import "core:math"
import "core:os"
import "core:fmt"
import "core:mem"
import "core:sync"

CHUNK :: 32
PAD :: CHUNK+2
HEIGHT :: 256
MiB :: 1024*1024
Block :: enum u8 { Air, Grass, Dirt, Stone, Sand, Wood, Leaves, Bedrock }
I3 :: [3]i32
V3 :: [3]f64
Edit_Command :: struct { sequence: u64, position: I3, block: Block }
Edit_Bucket :: struct { blocks: map[I3]Block }
World :: struct {
    seed: u64,
    edits: map[I3]^Edit_Bucket,
    sequence: u64,
    edit_count: int,
    tracker:^mem.Tracking_Allocator,
    allocation_limit:int,
}
MAX_EDITS :: 262144
floor_div :: proc(x, d: i32) -> i32 { q := x/d; if x%d < 0 { q -= 1 }; return q }
chunk_of :: proc(p: I3) -> I3 { return {floor_div(p.x,32),floor_div(p.y,32),floor_div(p.z,32)} }
block_index :: proc(x,y,z: int) -> int { return x+PAD*(y+PAD*z) }
hash :: proc(x: u64) -> u64 {
    v := x
    v = (v ~ (v>>30))*0xbf58476d1ce4e5b9
    v = (v ~ (v>>27))*0x94d049bb133111eb
    return v ~ (v>>31)
}
hash3 :: proc(seed: u64,x,y,z: i32) -> u64 {
    return hash(seed ~ u64(i64(x))*0x9e3779b185ebca87 ~ u64(i64(y))*0xc2b2ae3d27d4eb4f ~ u64(i64(z))*0x165667b19e3779f9)
}
lerp :: proc(a,b,t:f64)->f64 { return a+(b-a)*t }
smooth :: proc(t:f64)->f64 { return t*t*(3-2*t) }
noise2 :: proc(seed:u64,x,z:f64)->f64 {
    ix,iz := i32(math.floor(x)),i32(math.floor(z))
    fx,fz := smooth(x-f64(ix)),smooth(z-f64(iz))
    a := f64(hash3(seed,ix,0,iz)&65535)/65535
    b := f64(hash3(seed,ix+1,0,iz)&65535)/65535
    c := f64(hash3(seed,ix,0,iz+1)&65535)/65535
    d := f64(hash3(seed,ix+1,0,iz+1)&65535)/65535
    return lerp(lerp(a,b,fx),lerp(c,d,fx),fz)
}
terrain_height :: proc(seed:u64,x,z:i32)->i32 {
    a := noise2(seed,f64(x)/160,f64(z)/160)
    b := noise2(seed+1,f64(x)/43,f64(z)/43)
    c := noise2(seed+2,f64(x)/17,f64(z)/17)
    return 38+i32(a*a*66+b*19+c*5)
}
cave :: proc(seed:u64,x,y,z:i32)->bool {
    // Interpolated 3-D value field: continuous caves across chunk boundaries.
    ix,iy,iz := floor_div(x,12),floor_div(y,12),floor_div(z,12)
    fx,fy,fz := smooth(f64(x-ix*12)/12),smooth(f64(y-iy*12)/12),smooth(f64(z-iz*12)/12)
    v:[2]f64
    for k in 0..<2 {
        a:=f64(hash3(seed+91,ix,iy,iz+i32(k))&65535)/65535
        b:=f64(hash3(seed+91,ix+1,iy,iz+i32(k))&65535)/65535
        c:=f64(hash3(seed+91,ix,iy+1,iz+i32(k))&65535)/65535
        d:=f64(hash3(seed+91,ix+1,iy+1,iz+i32(k))&65535)/65535
        v[k]=lerp(lerp(a,b,fx),lerp(c,d,fx),fy)
    }
    return lerp(v[0],v[1],fz)>0.69
}
Tree :: struct { x,z,h:i32, active:bool }
tree_at :: proc(seed:u64,gx,gz:i32)->Tree {
    h:=hash3(seed+700,gx,0,gz)
    x,z:=gx*8+2+i32(h%4),gz*8+2+i32((h>>8)%4)
    return {x,z,terrain_height(seed,x,z),h%5==0}
}
base_block :: proc(seed:u64,p:I3,h:i32)->Block {
    if p.y<0 || p.y>=HEIGHT { return .Air }
    if p.y==0 { return .Bedrock }
    if p.y<=h {
        if p.y>5 && p.y<h-5 && cave(seed,p.x,p.y,p.z) { return .Air }
        if p.y==h { if h<53 {return .Sand}; return .Grass }
        if p.y>=h-3 { return .Dirt }; return .Stone
    }
    return .Air
}
generated_block :: proc(seed:u64,p:I3)->Block {
    h:=terrain_height(seed,p.x,p.z)
    b:=base_block(seed,p,h)
    if b!=.Air || p.y<0 || p.y>=HEIGHT {return b}
    gx,gz:=floor_div(p.x,8),floor_div(p.z,8)
    for dz in -1..=1 { for dx in -1..=1 {
        t:=tree_at(seed,gx+i32(dx),gz+i32(dz)); if !t.active {continue}
        if p.x==t.x && p.z==t.z && p.y>t.h && p.y<=t.h+5 {return .Wood}
        if abs(p.x-t.x)<=2 && abs(p.z-t.z)<=2 && p.y>=t.h+3 && p.y<=t.h+6 {
            if abs(p.x-t.x)+abs(p.z-t.z)+abs(p.y-(t.h+4))<=5 {b=.Leaves}
        }
    }}
    return b
}
world_init :: proc(w:^World,seed:u64) { w.seed=seed; w.edits=make(map[I3]^Edit_Bucket) }
world_destroy :: proc(w:^World) {
    for _,b in w.edits { delete(b.blocks);free(b) };delete(w.edits)
}
world_get :: proc(w:^World,p:I3)->Block {
    if b,ok:=w.edits[chunk_of(p)]; ok { if v,exists:=b.blocks[p];exists{return v} }
    return generated_block(w.seed,p)
}
world_apply :: proc(w:^World,e:Edit_Command)->bool {
    if e.sequence<=w.sequence || e.position.y<=0 || e.position.y>=HEIGHT || u8(e.block)>u8(Block.Bedrock) { return false }
    k:=chunk_of(e.position)
    b,ok:=w.edits[k]
    if w.tracker!=nil {
        sync.mutex_lock(&w.tracker.mutex);used:=w.tracker.current_memory_allocated;sync.mutex_unlock(&w.tracker.mutex)
        if int(used)+4*MiB>w.allocation_limit {return false}
    }
    if !ok {
        if w.edit_count>=MAX_EDITS {return false}
        b=new(Edit_Bucket);b.blocks=make(map[I3]Block);w.edits[k]=b
    }
    _,exists:=b.blocks[e.position]
    if !exists && w.edit_count>=MAX_EDITS{return false}
    if !exists {w.edit_count+=1}
    b.blocks[e.position]=e.block;w.sequence=e.sequence
    return true
}
// Snapshot only edits in the 27 touching chunks, never scan the entire world.
world_snapshot :: proc(w:^World,k:I3)->[dynamic]Edit_Command {
    out:=make([dynamic]Edit_Command)
    for z in -1..=1 {for y in -1..=1 {for x in -1..=1 {
        n:=k+I3{i32(x),i32(y),i32(z)}
        if b,ok:=w.edits[n];ok {for p,v in b.blocks {
            local:=p-k*32
            if local.x>=-1 && local.x<=32 && local.y>=-1 && local.y<=32 && local.z>=-1 && local.z<=32 {
                append(&out,Edit_Command{0,p,v})
            }
        }}
    }}}
    return out
}
Ray_Hit :: struct { hit:bool, block, adjacent:I3 }
raycast :: proc(w:^World,origin,direction:V3,reach:f64)->Ray_Hit {
    p:=I3{i32(math.floor(origin.x)),i32(math.floor(origin.y)),i32(math.floor(origin.z))}
    step:I3; delta,tmax:V3
    for a in 0..<3 {
        if abs(direction[a])<1e-12 {delta[a]=1e30;tmax[a]=1e30;continue}
        step[a]=1;if direction[a]<0 {step[a]=-1}
        delta[a]=abs(1/direction[a]);boundary:=f64(p[a]);if step[a]>0 {boundary+=1}
        tmax[a]=(boundary-origin[a])/direction[a]
    }
    prev:=p
    for _ in 0..<128 {
        if world_get(w,p)!=.Air {return {true,p,prev}}
        a:=0;if tmax[1]<tmax[a]{a=1};if tmax[2]<tmax[a]{a=2}
        if tmax[a]>reach {break}
        prev=p;p[a]+=step[a];tmax[a]+=delta[a]
    }
    return {}
}
// Portable binary save: explicit little endian fields, never raw struct padding.
put32 :: proc(out:^[dynamic]u8,v:u32) {for s in 0..<4 {append(out,u8(v>>u32(s*8)))}}
put64 :: proc(out:^[dynamic]u8,v:u64) {for s in 0..<8 {append(out,u8(v>>u32(s*8)))}}
get32 :: proc(data:[]u8,at:^int)->u32 {v:u32;for s in 0..<4 {v|=u32(data[at^])<<u32(s*8);at^+=1};return v}
get64 :: proc(data:[]u8,at:^int)->u64 {v:u64;for s in 0..<8 {v|=u64(data[at^])<<u32(s*8);at^+=1};return v}
world_save :: proc(w:^World,pos:V3,path:string)->bool {
    out:=make([dynamic]u8);defer delete(out)
    append(&out,..([]u8{'V','O','X','1'}));put64(&out,w.seed);put64(&out,w.sequence)
    for v in pos {put64(&out,transmute(u64)v)}
    put32(&out,u32(w.edit_count))
    for _,b in w.edits {for p,v in b.blocks {for c in p {put32(&out,transmute(u32)c)};append(&out,u8(v))}}
    temporary:=fmt.tprintf("%s.tmp",path)
    if os.write_entire_file(temporary,out[:])!=nil {return false}
    return os.rename(temporary,path)==nil
}
world_load :: proc(w:^World,pos:^V3,path:string)->bool {
    file,err:=os.open(path);if err!=nil {return false};defer os.close(file)
    length,size_err:=os.file_size(file);if size_err!=nil||length<48||length>48+MAX_EDITS*13 {return false}
    data,ok:=os.read_entire_file(file,context.allocator);if ok!=nil {return false};defer delete(data)
    if len(data)<48 || string(data[:4])!="VOX1" {return false}
    at:=4;seed:=get64(data,&at);sequence:=get64(data,&at)
    p:V3;for &v in p {v=transmute(f64)get64(data,&at);if math.is_nan(v)||math.is_inf(v)||abs(v)>100000000 {return false}}
    count:=int(get32(data,&at));if count>MAX_EDITS || len(data)!=48+count*13 {return false}
    candidate:World;world_init(&candidate,seed);candidate.tracker=w.tracker;candidate.allocation_limit=w.allocation_limit
    for _ in 0..<count {
        q:I3;for &v in q {v=transmute(i32)get32(data,&at)}
        b:=Block(data[at]);at+=1
        if !world_apply(&candidate,{candidate.sequence+1,q,b}) {world_destroy(&candidate);return false}
    }
    candidate.sequence=max(sequence,candidate.sequence)
    world_destroy(w);w^=candidate;pos^=p;return true
}
