// Static C# port of tests/idea_boon_handoff_fixture.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 9e15173c66f1a23b9815cded900144054b5d64b71fff67f12bb26f22c4cfb3ed
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_boon_handoff_fixture { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table()));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_12 = values_11[0];
R.Set(v_10, (V)"submit", new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
var values_16 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"submit"), R.Join(new V[] {v_14}, new Results(v_15))));
V v_17 = values_16[0];
V v_18 = values_16[1];
V v_19 = values_16[2];

R.Call(rt.Global("assert"), R.Join(new V[] {v_17}, new Results(v_18)));

return R.Join(new V[] {}, new Results(v_19));
return Results.Empty;
})));
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
foreach (var row_23 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_22, (V)"entities"))))))) {
V v_24 = row_23[0];
V v_25 = row_23[1];
if (R.Truth((V)(R.Get(v_25, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_4, (V)"remove_for_actor"), R.Join(new V[] {v_22}, new Results(R.Get(v_25, (V)"id"))));
}
}
return Results.Empty;
}));
V v_26 = V.Nil;
v_26 = new V((RuleFunction)(args_27 => {
V v_28 = args_27[0];
V v_29 = args_27[1];
V v_30 = args_27[2];
if (R.Truth((V)(!R.Truth(R.Call(R.Get(v_6, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_6, (V)"map_for"), R.Join(new V[] {}, new Results(v_28))).First, v_29}, new Results(v_30))).First)))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_31 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_28, (V)"entities"))))))) {
V v_32 = row_31[0];
V v_33 = row_31[1];
if (R.Truth(R.And(R.And(R.And(R.Get(v_33, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_33, (V)"position"), (V)"floor_id") == R.Get(v_28, (V)"active_floor_id"))), () => (V)(R.Get(R.Get(v_33, (V)"position"), (V)"x") == v_29)), () => (V)(R.Get(R.Get(v_33, (V)"position"), (V)"y") == v_30)))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_34 = V.Nil;
v_34 = new V((RuleFunction)(args_35 => {
V v_36 = args_35[0];
V v_37 = args_35[1];
var values_38 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_36))));
V v_39 = values_38[0];
foreach (var row_40 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_41 = row_40[0];
V v_42 = row_40[1];
var values_43 = R.Join(new V[] {(V)(R.Get(R.Get(v_37, (V)"position"), (V)"x") + R.Get(v_42, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_37, (V)"position"), (V)"y") + R.Get(v_42, (V)2.0d))));
V v_44 = values_43[0];
V v_45 = values_43[1];
if (R.Truth(R.Call(v_26, R.Join(new V[] {v_36, v_44}, new Results(v_45))).First)) {
var values_46 = R.Join(new V[] {v_44}, new Results(v_45));
var obj_47 = R.Get(v_39, (V)"position");
V key_48 = (V)"x";
var obj_49 = R.Get(v_39, (V)"position");
V key_50 = (V)"y";
R.Set(obj_47, key_48, values_46[0]);
R.Set(obj_49, key_50, values_46[1]);

R.Call(R.Get(v_8, (V)"recompute"), R.Join(new V[] {}, new Results(v_36)));

return R.Join(new V[] {(-R.Get(v_42, (V)1.0d))}, new Results((-R.Get(v_42, (V)2.0d))));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"no prepared approach")));
return Results.Empty;
}));
R.Set(v_10, (V)"ready", new V((RuleFunction)(args_51 => {

var values_52 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"new_stage"), R.Join(new V[] {}, new Results((V)8.0d))));
V v_53 = values_52[0];

R.Call(v_20, R.Join(new V[] {}, new Results(v_53)));
var values_54 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_53}, new Results(R.Get(R.Get(v_53, (V)"stage"), (V)"elite_id")))));
V v_55 = values_54[0];

var values_56 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"player"), R.Join(new V[] {}, new Results(v_53))));
V v_57 = values_56[0];
var values_58 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_55, (V)"hp") + R.Get(v_55, (V)"defense")) + (V)1.0d)));
var obj_59 = v_57;
V key_60 = (V)"damage";
R.Set(obj_59, key_60, values_58[0]);
var values_61 = R.Join(new V[] {}, R.Call(v_34, R.Join(new V[] {v_53}, new Results(v_55))));
V v_62 = values_61[0];
V v_63 = values_61[1];
R.Call(R.Get(v_10, (V)"submit"), R.Join(new V[] {v_53}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", v_62), new Entry((V)"dy", v_63)))));
R.Call(v_20, R.Join(new V[] {}, new Results(v_53)));
R.Call(R.Get(v_10, (V)"submit"), R.Join(new V[] {v_53}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(R.Get(v_10, (V)"submit"), R.Join(new V[] {v_53}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(v_20, R.Join(new V[] {}, new Results(v_53)));
var values_64 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"entity"), R.Join(new V[] {v_53}, new Results(R.Get(R.Get(v_53, (V)"stage"), (V)"exit_id")))));
V v_65 = values_64[0];

R.Call(v_34, R.Join(new V[] {v_53}, new Results(v_65)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"route_options"), R.Join(new V[] {v_53}, new Results(R.Get(v_65, (V)"id"))))));
return R.Join(new V[] {v_53, v_57}, new Results(R.Get(v_65, (V)"id")));
return Results.Empty;
})));
R.Set(v_10, (V)"route", new V((RuleFunction)(args_66 => {
V v_67 = args_66[0];
V v_68 = args_66[1];
return R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"submit"), R.Join(new V[] {v_67}, new Results(R.Table(new Entry((V)"kind", (V)"choose_route"), new Entry((V)"exit_id", v_68), new Entry((V)"route_index", (V)1.0d))))));
return Results.Empty;
})));
R.Set(v_10, (V)"burning", new V((RuleFunction)(args_69 => {

var values_70 = R.Join(new V[] {}, R.Call(R.Get(v_10, (V)"ready"), Results.Empty));
V v_71 = values_70[0];
V v_72 = values_70[1];
V v_73 = values_70[2];

var values_74 = Results.Empty;
V v_75 = values_74[0];
foreach (var row_76 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_12)))))) {
V v_77 = row_76[0];
V v_78 = row_76[1];
var values_79 = R.Join(new V[] {(V)(R.Get(R.Get(v_72, (V)"position"), (V)"x") + R.Get(v_78, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_72, (V)"position"), (V)"y") + R.Get(v_78, (V)2.0d))));
V v_80 = values_79[0];
V v_81 = values_79[1];
if (R.Truth(R.Call(v_26, R.Join(new V[] {v_71, v_80}, new Results(v_81))).First)) {
var values_82 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"spawn_enemy"), R.Join(new V[] {v_71, (V)"core:enemy/mossbound_guard", v_80, v_81}, new Results(R.Get(v_71, (V)"clock")))));
v_75 = values_82[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_75)));
R.Call(R.Get(v_2, (V)"set_boon_stacks"), R.Join(new V[] {v_71, R.Get(v_75, (V)"id"), (V)"core:boon/emberbrand", (V)"common"}, new Results((V)1.0d)));
R.Call(R.Get(v_10, (V)"submit"), R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));

R.Call(R.Get(v_10, (V)"submit"), R.Join(new V[] {v_71}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_72, (V)"statuses"), () => R.Get(R.Get(v_72, (V)"statuses"), (V)"burning")))));
R.Call(v_20, R.Join(new V[] {}, new Results(v_71)));
return R.Join(new V[] {v_71, v_72}, new Results(v_73));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_10));
return Results.Empty;
} }
