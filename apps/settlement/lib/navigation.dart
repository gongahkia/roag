import 'dart:typed_data';
import 'model.dart';
import 'rules.dart';

double roadCost(RoadKind road, bool rain) => road == RoadKind.stone ? 1/Rules.stoneSpeed : rain ? Rules.rainCost : 1;
double edgeTravelCost(Landscape world, Cell from, Cell to, bool rain) =>
    (roadCost(world.at(from).road,rain)+roadCost(world.at(to).road,rain))/2;

class _Field {
  final int revision;
  final Float64List distance;
  final Int32List next;
  _Field(this.revision, int length): distance=Float64List(length)..fillRange(0,length,double.infinity),next=Int32List(length)..fillRange(0,length,-1);
}
class _Heap {
  final List<(double,int)> data=[];
  void add((double,int) v) { var i=data.length;data.add(v);while(i>0){final parent=(i-1)~/2;if(data[parent].$1<=v.$1)break;data[i]=data[parent];i=parent;}data[i]=v; }
  (double,int) pop() { final result=data.first,last=data.removeLast();if(data.isNotEmpty){var i=0;while(i*2+1<data.length){var child=i*2+1;if(child+1<data.length&&data[child+1].$1<data[child].$1)child++;if(last.$1<=data[child].$1)break;data[i]=data[child];i=child;}data[i]=last;}return result; }
}

// reverse weighted fields share venue searches across people; caches are derived.
class Navigation {
  Landscape world;
  bool rain;
  int revision;
  int searches=0,hits=0,expansions=0,fieldsBuilt=0,stale=0;
  final Map<Cell,_Field> _fields={};
  Navigation(this.world,{this.rain=false,this.revision=0});
  void invalidate(Landscape next,{required bool raining}) { world=next;rain=raining;revision++;_fields.clear(); }
  bool valid(Cell c)=>world.contains(c)&&world.at(c).path;
  _Field field(Cell goal) {
    final cached=_fields.remove(goal);
    if(cached!=null&&cached.revision==revision){hits++;_fields[goal]=cached;return cached;}
    fieldsBuilt++;
    final f=_Field(revision,world.lots.length),heap=_Heap();
    final end=goal.y*world.width+goal.x;f.distance[end]=0;heap.add((0,end));
    while(heap.data.isNotEmpty) {
      final (distance,index)=heap.pop();if(distance>f.distance[index])continue;
      expansions++;final cell=Cell(index%world.width,index~/world.width);
      for(final n in world.pathNeighbours(cell)) {
        final i=n.y*world.width+n.x,cost=distance+edgeTravelCost(world,n,cell,rain);
        if(cost<f.distance[i]-1e-9){f.distance[i]=cost;f.next[i]=index;heap.add((cost,i));}
      }
    }
    if(_fields.length>=96)_fields.remove(_fields.keys.first);
    _fields[goal]=f;return f;
  }
  double distance(Cell start,Cell goal,{int? expectedRevision}) {
    if(expectedRevision!=null&&expectedRevision!=revision){stale++;return double.infinity;}
    if(!valid(start)||!valid(goal))return double.infinity;
    return field(goal).distance[start.y*world.width+start.x];
  }
  List<Cell>? route(Cell start,Cell goal,{int? expectedRevision}) {
    searches++;
    if(expectedRevision!=null&&expectedRevision!=revision){stale++;return null;}
    if(!valid(start)||!valid(goal))return null;
    final f=field(goal);var i=start.y*world.width+start.x;
    if(!f.distance[i].isFinite)return null;
    final result=<Cell>[start],end=goal.y*world.width+goal.x;
    while(i!=end) { i=f.next[i];if(i<0||result.length>world.lots.length)return null;result.add(Cell(i%world.width,i~/world.width)); }
    return result;
  }
}
