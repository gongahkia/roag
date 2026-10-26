// Static C# port of src/world.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 7a7b5e4a8b6fbf6c2d177ec2a55fb8ce0d7b8f2719f95e2b4e3c6ee43c10c977
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_world { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rewards"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results((V)"roag-world/1"));
var obj_8 = v_6;
V key_9 = (V)"version";
R.Set(obj_8, key_9, values_7[0]);
var values_10 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:secret/cracked_cache"), new Entry((V)2, (V)"core:secret/moss_sigil"))));
var obj_11 = v_6;
V key_12 = (V)"order";
R.Set(obj_11, key_12, values_10[0]);
var values_13 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:secret/cracked_cache", R.Table(new Entry((V)"id", (V)"core:secret/cracked_cache"), new Entry((V)"trigger", (V)"AttackPerformed"), new Entry((V)"ability_id", (V)"core:ability/basic_attack"), new Entry((V)"clue", (V)"cracked_moss_wall"), new Entry((V)"reward", (V)"free_chest"), new Entry((V)"reward_definition_id", (V)"core:chest/free_elite"), new Entry((V)"description", (V)"A thin moss-lined crack yields to a sword strike."))), new Entry((V)"core:secret/moss_sigil", R.Table(new Entry((V)"id", (V)"core:secret/moss_sigil"), new Entry((V)"trigger", (V)"TileEntered"), new Entry((V)"clue", (V)"moss_sigil"), new Entry((V)"reward", (V)"currency"), new Entry((V)"amount", (V)4.0d), new Entry((V)"description", (V)"A repeated moss spiral releases a small cache underfoot."))))));
var obj_14 = v_6;
V key_15 = (V)"definitions";
R.Set(obj_14, key_15, values_13[0]);
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_16 => {

var values_17 = R.Join(new V[] {}, new Results(R.Table()));
V v_18 = values_17[0];
foreach (var row_19 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"order"))))))) {
V v_20 = row_19[0];
V v_21 = row_19[1];
var values_22 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_6, (V)"definitions"), v_21)}, new Results((V)"missing secret definition"))));
V v_23 = values_22[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(!R.Truth(R.Get(v_18, v_21))), () => (V)(R.Get(v_23, (V)"id") == v_21))}, new Results((V)"duplicate secret ID")));
var values_24 = R.Join(new V[] {}, new Results((V)true));
var obj_25 = v_18;
V key_26 = v_21;
R.Set(obj_25, key_26, values_24[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or(R.Or((V)(R.Get(v_23, (V)"trigger") == (V)"AttackPerformed"), () => (V)(R.Get(v_23, (V)"trigger") == (V)"TileEntered")), () => (V)(R.Get(v_23, (V)"trigger") == (V)"ItemUsed"))}, new Results((V)"invalid secret event trigger")));
if (R.Truth((V)(R.Get(v_23, (V)"trigger") == (V)"ItemUsed"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"item_id")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_23, (V)"item_id")) > (V)0.0d))}, new Results((V)"item-triggered secret needs item ID")));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"clue")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_23, (V)"clue")) > (V)0.0d))}, new Results((V)"secret needs learnable clue")));
if (R.Truth((V)(R.Get(v_23, (V)"reward") == (V)"currency"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"amount")))).First == (V)"number"), () => (V)((V)(R.Get(v_23, (V)"amount") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_23, (V)"amount") > (V)0.0d))}, new Results((V)"invalid secret currency")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_23, (V)"reward") == (V)"free_chest"), () => (V)(R.Get(R.Call(R.Get(v_2, (V)"get_chest"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"reward_definition_id")))).First, (V)"price") == (V)0.0d))}, new Results((V)"invalid secret free chest")));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
V v_27 = V.Nil;
v_27 = new V((RuleFunction)(args_28 => {
V v_29 = args_28[0];
V v_30 = args_28[1];
V v_31 = args_28[2];
V v_32 = args_28[3];
V v_33 = args_28[4];
var values_34 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_29, (V)"rows"), v_31)));
V v_35 = values_34[0];
var values_36 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_35, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_30 - (V)1.0d)))).First, R.Concat(v_32, R.Invoke(v_35, "sub", R.Join(new V[] {}, new Results((V)(v_30 + (V)1.0d)))).First))));
var obj_37 = R.Get(v_29, (V)"rows");
V key_38 = v_31;
R.Set(obj_37, key_38, values_36[0]);
var values_39 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_29, (V)"visual_rows"), v_31)));
V v_40 = values_39[0];
var values_41 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_40, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_30 - (V)1.0d)))).First, R.Concat(v_33, R.Invoke(v_40, "sub", R.Join(new V[] {}, new Results((V)(v_30 + (V)1.0d)))).First))));
var obj_42 = R.Get(v_29, (V)"visual_rows");
V key_43 = v_31;
R.Set(obj_42, key_43, values_41[0]);
return Results.Empty;
}));
R.Set(v_6, (V)"populate", new V((RuleFunction)(args_44 => {
V v_45 = args_44[0];
V v_46 = args_44[1];
if (R.Truth((V)(!R.Truth(v_46)))) {
var values_47 = R.Join(new V[] {}, new Results(R.Table()));
var obj_48 = v_45;
V key_49 = (V)"secrets";
R.Set(obj_48, key_49, values_47[0]);
}
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_45, (V)"generated_map"), () => R.Get(R.Get(R.Get(v_45, (V)"generated_map"), (V)"placements"), (V)"secrets")), () => R.Table()))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
var values_53 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_52, (V)"id")), new Entry((V)"definition_id", R.Get(v_52, (V)"definition_id")), new Entry((V)"floor_id", R.Get(v_45, (V)"active_floor_id")), new Entry((V)"x", R.Get(v_52, (V)"x")), new Entry((V)"y", R.Get(v_52, (V)"y")), new Entry((V)"wall_x", R.Get(v_52, (V)"wall_x")), new Entry((V)"wall_y", R.Get(v_52, (V)"wall_y")), new Entry((V)"cache_x", R.Get(v_52, (V)"cache_x")), new Entry((V)"cache_y", R.Get(v_52, (V)"cache_y")), new Entry((V)"room_id", R.Get(v_52, (V)"room_id")), new Entry((V)"revealed", (V)false), new Entry((V)"reward_id", V.Nil))));
V v_54 = values_53[0];
var values_55 = R.Join(new V[] {}, new Results(v_54));
var obj_56 = R.Get(v_45, (V)"secrets");
V key_57 = (V)((V)R.Length(R.Get(v_45, (V)"secrets")) + (V)1.0d);
R.Set(obj_56, key_57, values_55[0]);
}
return Results.Empty;
})));
R.Set(v_6, (V)"on_event", new V((RuleFunction)(args_58 => {
V v_59 = args_58[0];
V v_60 = args_58[1];
V v_61 = args_58[2];
V v_62 = args_58[3];
if (R.Truth(R.And(R.And((V)(R.Get(v_60, (V)"kind") != (V)"AttackPerformed"), () => (V)(R.Get(v_60, (V)"kind") != (V)"TileEntered")), () => (V)(R.Get(v_60, (V)"kind") != (V)"ItemUsed")))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth((V)(R.Get(v_60, (V)"source_id") != R.Get(v_59, (V)"player_id")))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_63 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_59, (V)"secrets"), () => R.Table()))))))) {
V v_64 = row_63[0];
V v_65 = row_63[1];
var values_66 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"definitions"), R.Get(v_65, (V)"definition_id"))));
V v_67 = values_66[0];
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_65, (V)"revealed"))), () => v_67), () => (V)(R.Get(v_67, (V)"trigger") == R.Get(v_60, (V)"kind"))), () => R.Or((V)(R.Get(v_60, (V)"floor_id") == R.Get(v_65, (V)"floor_id")), () => R.And(R.Get(v_60, (V)"target_position"), () => (V)(R.Get(R.Get(v_60, (V)"target_position"), (V)"floor_id") == R.Get(v_65, (V)"floor_id"))))))) {
var values_68 = R.Join(new V[] {}, new Results((V)false));
V v_69 = values_68[0];
if (R.Truth(R.And(R.And((V)(R.Get(v_60, (V)"kind") == (V)"AttackPerformed"), () => (V)(!R.Truth(R.Get(v_60, (V)"bump")))), () => (V)(R.Get(v_60, (V)"ability_id") == R.Get(v_67, (V)"ability_id"))))) {
foreach (var row_70 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_60, (V)"target_cells"), () => R.Table()))))))) {
V v_71 = row_70[0];
V v_72 = row_70[1];
if (R.Truth(R.And((V)(R.Get(v_72, (V)"x") == R.Get(v_65, (V)"wall_x")), () => (V)(R.Get(v_72, (V)"y") == R.Get(v_65, (V)"wall_y"))))) {
var values_73 = R.Join(new V[] {}, new Results((V)true));
v_69 = values_73[0];

break;
}
}
} else if (R.Truth((V)(R.Get(v_60, (V)"kind") == (V)"TileEntered"))) {
var values_74 = R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_60, (V)"x") == R.Get(v_65, (V)"x")), () => (V)(R.Get(v_60, (V)"y") == R.Get(v_65, (V)"y")))));
v_69 = values_74[0];
} else if (R.Truth((V)(R.Get(v_60, (V)"kind") == (V)"ItemUsed"))) {
var values_75 = R.Join(new V[] {}, new Results(R.Get(v_60, (V)"target_position")));
V v_76 = values_75[0];
var values_77 = R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_60, (V)"item_id") == R.Get(v_67, (V)"item_id")), () => v_76), () => (V)(R.Get(v_76, (V)"x") == R.Get(v_65, (V)"x"))), () => (V)(R.Get(v_76, (V)"y") == R.Get(v_65, (V)"y")))));
v_69 = values_77[0];
}
if (R.Truth(v_69)) {
var values_78 = R.Join(new V[] {}, new Results((V)true));
var obj_79 = v_65;
V key_80 = (V)"revealed";
R.Set(obj_79, key_80, values_78[0]);
if (R.Truth((V)(R.Get(v_67, (V)"reward") == (V)"free_chest"))) {
var values_81 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_59, (V)"generated_map")}, new Results((V)"wall secret needs concrete floor"))));
V v_82 = values_81[0];
R.Call(v_27, R.Join(new V[] {v_82, R.Get(v_65, (V)"wall_x"), R.Get(v_65, (V)"wall_y"), (V)"."}, new Results((V)"m")));
R.Call(v_27, R.Join(new V[] {v_82, R.Get(v_65, (V)"cache_x"), R.Get(v_65, (V)"cache_y"), (V)"."}, new Results((V)"m")));
} else {
R.Call(v_27, R.Join(new V[] {R.Get(v_59, (V)"generated_map"), R.Get(v_65, (V)"x"), R.Get(v_65, (V)"y"), (V)"."}, new Results((V)"m")));
}
R.Call(v_61, R.Join(new V[] {(V)"SecretDiscovered", R.Table(new Entry((V)"secret_id", R.Get(v_65, (V)"id")), new Entry((V)"definition_id", R.Get(v_65, (V)"definition_id")), new Entry((V)"trigger", R.Get(v_60, (V)"kind")), new Entry((V)"floor_id", R.Get(v_65, (V)"floor_id")), new Entry((V)"x", R.Get(v_65, (V)"x")), new Entry((V)"y", R.Get(v_65, (V)"y")), new Entry((V)"wall_x", R.Get(v_65, (V)"wall_x")), new Entry((V)"wall_y", R.Get(v_65, (V)"wall_y")), new Entry((V)"cache_x", R.Get(v_65, (V)"cache_x")), new Entry((V)"cache_y", R.Get(v_65, (V)"cache_y")), new Entry((V)"parent_event_id", R.Get(v_60, (V)"id")))}, new Results(R.Get(v_60, (V)"source_id"))));
if (R.Truth((V)(R.Get(v_67, (V)"reward") == (V)"free_chest"))) {
var values_83 = R.Join(new V[] {}, R.Call(v_62, R.Join(new V[] {R.Get(v_67, (V)"reward_definition_id"), R.Get(v_65, (V)"cache_x"), R.Get(v_65, (V)"cache_y")}, new Results(R.Concat((V)"secret:", R.Get(v_65, (V)"id"))))));
V v_84 = values_83[0];
var values_85 = R.Join(new V[] {}, new Results(R.Get(v_84, (V)"id")));
var obj_86 = v_65;
V key_87 = (V)"reward_id";
R.Set(obj_86, key_87, values_85[0]);
R.Call(v_61, R.Join(new V[] {(V)"SecretRewardSpawned", R.Table(new Entry((V)"secret_id", R.Get(v_65, (V)"id")), new Entry((V)"reward", (V)"free_chest"), new Entry((V)"chest_id", R.Get(v_84, (V)"id")), new Entry((V)"floor_id", R.Get(v_65, (V)"floor_id")), new Entry((V)"x", R.Get(v_65, (V)"cache_x")), new Entry((V)"y", R.Get(v_65, (V)"cache_y")))}, new Results(R.Get(v_60, (V)"source_id"))));
} else {
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"spawn_currency"), R.Join(new V[] {v_59, R.Get(v_67, (V)"amount"), R.Get(v_65, (V)"floor_id"), R.Get(v_65, (V)"x"), R.Get(v_65, (V)"y"), v_61, R.Get(v_60, (V)"source_id")}, new Results(R.Get(v_65, (V)"id")))));
V v_89 = values_88[0];
var values_90 = R.Join(new V[] {}, new Results(R.Get(v_89, (V)"id")));
var obj_91 = v_65;
V key_92 = (V)"reward_id";
R.Set(obj_91, key_92, values_90[0]);
var values_93 = Results.Empty;
V v_94 = values_93[0];
foreach (var row_95 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_59, (V)"entities"))))))) {
V v_96 = row_95[0];
V v_97 = row_95[1];
if (R.Truth((V)(R.Get(v_97, (V)"id") == R.Get(v_59, (V)"player_id")))) {
var values_98 = R.Join(new V[] {}, new Results(v_97));
v_94 = values_98[0];

break;
}
}
R.Call(R.Get(v_4, (V)"pickup_at"), R.Join(new V[] {v_59, v_94, R.Get(v_65, (V)"x"), R.Get(v_65, (V)"y")}, new Results(v_61)));
}
return R.Join(new V[] {}, new Results((V)true));
}
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
