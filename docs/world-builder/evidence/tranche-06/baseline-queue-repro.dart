import 'dart:io';
import 'colony.dart';import 'model.dart';import 'residents.dart';
void main(){
 final c=Colony.decode(File('/tmp/roag-tranche06-fixtures/market-day.json').readAsStringSync());
 final host=c.people.values.firstWhere((r)=>r.activity==Activity.working && c.kind(r.workId)==Place.market).homeId;
 for(final id in c.people.keys.where((id)=>id!=host).toList()){
  c.session.beginStroke(Tool.remove,c.world.cottages[id]!);if(!c.session.finishStroke())throw StateError('Rejected');c.reconcile(c.session.world);
 }
 stdout.writeln('baseline active=${c.people.length}; queued=${c.planning.length}');
 try{Colony.decode(c.encode());stdout.writeln('baseline roundtrip passed');}catch(e){stdout.writeln('baseline roundtrip failed: $e');}
}
