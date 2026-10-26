// Static C# port of src/finale.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 6e10b13465b87d1255977e49f5ee6b9cc27db67100ec06b9a9b0db67e23dcbd2
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_finale { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:map/final_sanctum"), new Entry((V)"floor_id", (V)"core:floor/final_sanctum"), new Entry((V)"width", (V)23.0d), new Entry((V)"height", (V)17.0d), new Entry((V)"start_x", (V)12.0d), new Entry((V)"start_y", (V)14.0d), new Entry((V)"boss_x", (V)12.0d), new Entry((V)"boss_y", (V)5.0d), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"#######################"), new Entry((V)2, (V)"#.....................#"), new Entry((V)3, (V)"#.....................#"), new Entry((V)4, (V)"#.....##.....##.......#"), new Entry((V)5, (V)"#.....................#"), new Entry((V)6, (V)"#.....................#"), new Entry((V)7, (V)"#.....................#"), new Entry((V)8, (V)"#....##.........##....#"), new Entry((V)9, (V)"#.....................#"), new Entry((V)10, (V)"#.....................#"), new Entry((V)11, (V)"#....##.........##....#"), new Entry((V)12, (V)"#.....................#"), new Entry((V)13, (V)"#.....................#"), new Entry((V)14, (V)"#.....................#"), new Entry((V)15, (V)"#.....##.....##.......#"), new Entry((V)16, (V)"#.....................#"), new Entry((V)17, (V)"#######################"))))));
var obj_4 = v_2;
V key_5 = (V)"map";
R.Set(obj_4, key_5, values_3[0]);
R.Set(v_2, (V)"validate_map", new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_7, (V)"id") == R.Get(R.Get(v_2, (V)"map"), (V)"id")), () => (V)(R.Get(v_7, (V)"floor_id") == R.Get(R.Get(v_2, (V)"map"), (V)"floor_id"))), () => (V)(R.Get(v_7, (V)"width") == R.Get(R.Get(v_2, (V)"map"), (V)"width"))), () => (V)(R.Get(v_7, (V)"height") == R.Get(R.Get(v_2, (V)"map"), (V)"height"))), () => (V)(R.Get(v_7, (V)"start_x") == R.Get(R.Get(v_2, (V)"map"), (V)"start_x"))), () => (V)(R.Get(v_7, (V)"start_y") == R.Get(R.Get(v_2, (V)"map"), (V)"start_y"))), () => (V)(R.Get(v_7, (V)"boss_x") == R.Get(R.Get(v_2, (V)"map"), (V)"boss_x"))), () => (V)(R.Get(v_7, (V)"boss_y") == R.Get(R.Get(v_2, (V)"map"), (V)"boss_y")))}, new Results((V)"invalid final chamber anchors")));
var values_8 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, R.Get(v_7, (V)"start_x")), new Entry((V)2, R.Get(v_7, (V)"start_y")))))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)((V)(R.Get(v_7, (V)"start_y") * R.Get(v_7, (V)"width")) + R.Get(v_7, (V)"start_x")), (V)true))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, new Results((V)1.0d));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_15 = values_14[0];
while (R.Truth((V)(v_13 <= (V)R.Length(v_9)))) {
var values_16 = R.Join(new V[] {}, new Results(R.Get(v_9, v_13)));
V v_17 = values_16[0];

var values_18 = R.Join(new V[] {}, new Results((V)(v_13 + (V)1.0d)));
v_13 = values_18[0];
foreach (var row_19 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_15)))))) {
V v_20 = row_19[0];
V v_21 = row_19[1];
var values_22 = R.Join(new V[] {(V)(R.Get(v_17, (V)1.0d) + R.Get(v_21, (V)1.0d))}, new Results((V)(R.Get(v_17, (V)2.0d) + R.Get(v_21, (V)2.0d))));
V v_23 = values_22[0];
V v_24 = values_22[1];
var values_25 = R.Join(new V[] {}, new Results((V)((V)(v_24 * R.Get(v_7, (V)"width")) + v_23)));
V v_26 = values_25[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(v_23 >= (V)1.0d), () => (V)(v_23 <= R.Get(v_7, (V)"width"))), () => (V)(v_24 >= (V)1.0d)), () => (V)(v_24 <= R.Get(v_7, (V)"height"))), () => (V)(!R.Truth(R.Get(v_11, v_26)))), () => (V)(R.Invoke(R.Get(R.Get(v_7, (V)"rows"), v_24), "sub", R.Join(new V[] {v_23}, new Results(v_23))).First == (V)".")))) {
var values_27 = R.Join(new V[] {}, new Results((V)true));
var obj_28 = v_11;
V key_29 = v_26;
R.Set(obj_28, key_29, values_27[0]);
var values_30 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_23), new Entry((V)2, v_24))));
var obj_31 = v_9;
V key_32 = (V)((V)R.Length(v_9) + (V)1.0d);
R.Set(obj_31, key_32, values_30[0]);
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_11, (V)((V)(R.Get(v_7, (V)"boss_y") * R.Get(v_7, (V)"width")) + R.Get(v_7, (V)"boss_x"))), () => (V)((V)R.Length(v_9) >= (V)200.0d))}, new Results((V)"final chamber boss is unreachable or lacks combat clearance")));
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
V v_33 = V.Nil;
v_33 = new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
V v_36 = args_34[1];
foreach (var row_37 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"entities"))))))) {
V v_38 = row_37[0];
V v_39 = row_37[1];
if (R.Truth((V)(R.Get(v_39, (V)"id") == v_36))) {
return R.Join(new V[] {}, new Results(v_39));
}
}
return Results.Empty;
}));
R.Set(v_2, (V)"init", new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
V v_42 = args_40[1];
V v_43 = args_40[2];
var values_44 = R.Join(new V[] {}, new Results(R.Get(v_2, (V)"map")));
V v_45 = values_44[0];
var values_46 = R.Join(new V[] {}, R.Call(v_42, R.Join(new V[] {v_41, (V)"core:enemy/hollow_regent", R.Get(v_45, (V)"boss_x"), R.Get(v_45, (V)"boss_y")}, new Results((V)0.0d))));
V v_47 = values_46[0];
var values_48 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_49 = R.Get(v_47, (V)"ai_memory");
V key_50 = (V)"attacks";
R.Set(obj_49, key_50, values_48[0]);
var values_51 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"phase", (V)"active"), new Entry((V)"boss_id", R.Get(v_47, (V)"id")), new Entry((V)"route_history", v_43), new Entry((V)"index", (V)4.0d), new Entry((V)"defeat_time", V.Nil))));
var obj_52 = v_41;
V key_53 = (V)"finale";
R.Set(obj_52, key_53, values_51[0]);
return Results.Empty;
})));
R.Set(v_2, (V)"on_event", new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
V v_57 = args_54[2];
var values_58 = R.Join(new V[] {}, new Results(R.Get(v_55, (V)"finale")));
V v_59 = values_58[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_59)), () => (V)(R.Get(v_59, (V)"phase") != (V)"active")), () => (V)(R.Get(v_56, (V)"target_id") != R.Get(v_59, (V)"boss_id"))))) {
return Results.Empty;
}
if (R.Truth((V)(R.Get(v_56, (V)"kind") == (V)"DamageTaken"))) {
var values_60 = R.Join(new V[] {}, R.Call(v_33, R.Join(new V[] {v_55}, new Results(R.Get(v_59, (V)"boss_id")))));
V v_61 = values_60[0];
if (R.Truth(R.And(R.And(R.And(v_61, () => (V)(R.Get(v_61, (V)"hp") > (V)0.0d)), () => (V)(!R.Truth(R.Get(v_61, (V)"phase_two")))), () => (V)((V)(R.Get(v_61, (V)"hp") * (V)2.0d) <= R.Get(v_61, (V)"max_hp"))))) {
var values_62 = R.Join(new V[] {}, new Results((V)true));
var obj_63 = v_61;
V key_64 = (V)"phase_two";
R.Set(obj_63, key_64, values_62[0]);
var values_65 = R.Join(new V[] {}, new Results((V)(R.Get(v_61, (V)"speed") + (V)25.0d)));
var obj_66 = v_61;
V key_67 = (V)"speed";
R.Set(obj_66, key_67, values_65[0]);
R.Call(v_57, R.Join(new V[] {(V)"RegentPhaseChanged", R.Table(new Entry((V)"boss_id", R.Get(v_61, (V)"id")), new Entry((V)"phase", (V)2.0d), new Entry((V)"floor_id", R.Get(v_55, (V)"active_floor_id")), new Entry((V)"x", R.Get(R.Get(v_61, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_61, (V)"position"), (V)"y")))}, new Results(R.Get(v_61, (V)"id"))));
}
} else if (R.Truth((V)(R.Get(v_56, (V)"kind") == (V)"EntityKilled"))) {
var values_68 = R.Join(new V[] {}, new Results((V)"boss_defeated"));
var obj_69 = v_59;
V key_70 = (V)"phase";
R.Set(obj_69, key_70, values_68[0]);
var values_71 = R.Join(new V[] {}, new Results(R.Get(v_56, (V)"time")));
var obj_72 = v_59;
V key_73 = (V)"defeat_time";
R.Set(obj_72, key_73, values_71[0]);
R.Call(v_57, R.Join(new V[] {(V)"RegentDefeated", R.Table(new Entry((V)"boss_id", R.Get(v_59, (V)"boss_id")), new Entry((V)"floor_id", R.Get(v_55, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_56, (V)"x")), new Entry((V)"y", R.Get(v_56, (V)"y")))}, new Results(R.Get(v_56, (V)"source_id"))));
}
return Results.Empty;
})));
R.Set(v_2, (V)"finish", new V((RuleFunction)(args_74 => {
V v_75 = args_74[0];
V v_76 = args_74[1];
var values_77 = R.Join(new V[] {}, new Results(R.Get(v_75, (V)"finale")));
V v_78 = values_77[0];
if (R.Truth(R.Or((V)(!R.Truth(v_78)), () => (V)(R.Get(v_78, (V)"phase") != (V)"boss_defeated")))) {
return Results.Empty;
}
var values_79 = R.Join(new V[] {}, R.Call(v_33, R.Join(new V[] {v_75}, new Results(R.Get(v_75, (V)"player_id")))));
V v_80 = values_79[0];
if (R.Truth(R.Or((V)(!R.Truth(v_80)), () => (V)(R.Get(v_80, (V)"hp") <= (V)0.0d)))) {
return Results.Empty;
}
var values_81 = R.Join(new V[] {}, new Results((V)"victory"));
var obj_82 = v_78;
V key_83 = (V)"phase";
R.Set(obj_82, key_83, values_81[0]);
var values_84 = R.Join(new V[] {}, new Results((V)true));
var obj_85 = v_75;
V key_86 = (V)"game_over";
R.Set(obj_85, key_86, values_84[0]);
var values_87 = R.Join(new V[] {}, new Results(R.Get(v_75, (V)"clock")));
V v_88 = values_87[0];
foreach (var row_89 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_75, (V)"retired_stages"))))))) {
V v_90 = row_89[0];
V v_91 = row_89[1];
var values_92 = R.Join(new V[] {}, new Results((V)(v_88 + R.Get(v_91, (V)"clock"))));
v_88 = values_92[0];
}
var values_93 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"boss_id", R.Get(v_78, (V)"boss_id")), new Entry((V)"boss_name", (V)"Hollow Regent"), new Entry((V)"seed", R.Get(v_75, (V)"root_seed")), new Entry((V)"route_history", R.Get(v_78, (V)"route_history")), new Entry((V)"class_id", R.Get(v_80, (V)"definition_id")), new Entry((V)"level", R.Get(R.Get(v_75, (V)"progression"), (V)"level")), new Entry((V)"xp_total", R.Get(R.Get(v_75, (V)"progression"), (V)"xp_total")), new Entry((V)"hp", R.Get(v_80, (V)"hp")), new Entry((V)"max_hp", R.Get(v_80, (V)"max_hp")), new Entry((V)"currency", R.Get(v_75, (V)"currency")), new Entry((V)"boons", R.Get(v_80, (V)"boons")), new Entry((V)"world_time", v_88), new Entry((V)"defeat_time", R.Get(v_78, (V)"defeat_time")))));
var obj_94 = v_75;
V key_95 = (V)"victory";
R.Set(obj_94, key_95, values_93[0]);
R.Call(v_76, R.Join(new V[] {(V)"Victory", R.Table(new Entry((V)"boss_id", R.Get(v_78, (V)"boss_id")), new Entry((V)"floor_id", R.Get(v_75, (V)"active_floor_id")), new Entry((V)"world_time", v_88))}, new Results(R.Get(v_75, (V)"player_id"))));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_96 => {
V v_97 = args_96[0];
if (R.Truth((V)(!R.Truth(R.Get(v_97, (V)"finale"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
var values_98 = R.Join(new V[] {}, new Results(R.Get(v_97, (V)"finale")));
V v_99 = values_98[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_97, (V)"stage") == V.Nil), () => (V)(R.Get(v_97, (V)"map_id") == R.Get(R.Get(v_2, (V)"map"), (V)"id"))), () => (V)(R.Get(v_97, (V)"active_floor_id") == R.Get(R.Get(v_2, (V)"map"), (V)"floor_id"))), () => R.Get(v_97, (V)"final_map")), () => (V)(R.Get(R.Get(v_97, (V)"final_map"), (V)"id") == R.Get(R.Get(v_2, (V)"map"), (V)"id"))), () => (V)(R.Get(v_99, (V)"index") == (V)4.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_99, (V)"route_history")))).First == (V)"table")), () => (V)((V)R.Length(R.Get(v_99, (V)"route_history")) == (V)3.0d)), () => (V)((V)R.Length(R.Get(v_97, (V)"retired_stages")) == (V)3.0d))}, new Results((V)"invalid finale identity")));
R.Call(R.Get(v_2, (V)"validate_map"), R.Join(new V[] {}, new Results(R.Get(v_97, (V)"final_map"))));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(R.Get(v_99, (V)"route_history"), (V)3.0d) == (V)"core:route/gateway")}, new Results((V)"invalid final route")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or((V)(R.Get(v_99, (V)"phase") == (V)"active"), () => (V)(R.Get(v_99, (V)"phase") == (V)"boss_defeated")), () => (V)(R.Get(v_99, (V)"phase") == (V)"victory"))}, new Results((V)"invalid finale phase")));
var values_100 = R.Join(new V[] {}, R.Call(v_33, R.Join(new V[] {v_97}, new Results(R.Get(v_99, (V)"boss_id")))));
V v_101 = values_100[0];
if (R.Truth((V)(R.Get(v_99, (V)"phase") == (V)"active"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(v_101, () => (V)(R.Get(v_101, (V)"definition_id") == (V)"core:enemy/hollow_regent")), () => (V)(R.Get(v_101, (V)"hp") > (V)0.0d)), () => (V)(!R.Truth(R.Get(v_97, (V)"victory")))), () => R.Or((V)(!R.Truth(R.Get(v_97, (V)"game_over"))), () => (V)(R.Get(R.Call(v_33, R.Join(new V[] {v_97}, new Results(R.Get(v_97, (V)"player_id")))).First, (V)"hp") == (V)0.0d)))}, new Results((V)"missing active Regent")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(!R.Truth(v_101)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_99, (V)"defeat_time")))).First == (V)"number"))}, new Results((V)"defeated Regent still active")));
}
if (R.Truth((V)(R.Get(v_99, (V)"phase") == (V)"victory"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Get(v_97, (V)"game_over"), () => R.Get(v_97, (V)"victory")), () => (V)(R.Get(R.Get(v_97, (V)"victory"), (V)"boss_id") == R.Get(v_99, (V)"boss_id")))}, new Results((V)"missing terminal victory")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
