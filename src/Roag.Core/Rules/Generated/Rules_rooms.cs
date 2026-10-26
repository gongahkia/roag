// Static C# port of src/rooms.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 93753e8eccfa234d961b40f7306ae019eb047fbbcced02f7b246beb5bb60db75
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_rooms { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table()));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results((V)"roag-rooms/3"));
var obj_10 = v_8;
V key_11 = (V)"version";
R.Set(obj_10, key_11, values_9[0]);
var values_12 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:biome/overgrown_ruins"), new Entry((V)"floor_id", (V)"core:floor/overgrown_ruins"), new Entry((V)"width", (V)140.0d), new Entry((V)"height", (V)90.0d), new Entry((V)"room_min", (V)8.0d), new Entry((V)"room_max", (V)12.0d), new Entry((V)"standard_chests", (V)5.0d), new Entry((V)"retry_limit", (V)2.0d), new Entry((V)"enemy_ids", R.Table(new Entry((V)1, (V)"core:enemy/mossbound_guard"), new Entry((V)2, (V)"core:enemy/thornspitter"), new Entry((V)3, (V)"core:enemy/ruin_skitter"))))));
var obj_13 = v_8;
V key_14 = (V)"biome";
R.Set(obj_13, key_14, values_12[0]);
var values_15 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:room/entry"), new Entry((V)2, (V)"core:room/watch"), new Entry((V)3, (V)"core:room/gallery"), new Entry((V)4, (V)"core:room/cache"), new Entry((V)5, (V)"core:room/shrine"), new Entry((V)6, (V)"core:room/arena"))));
var obj_16 = v_8;
V key_17 = (V)"order";
R.Set(obj_16, key_17, values_15[0]);
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)((V)(v_20 + (V)1.0d) / (V)2.0d)), new Entry((V)"y", (V)1.0d), new Entry((V)"facing", (V)"N"), new Entry((V)"width", (V)1.0d))), new Entry((V)2, R.Table(new Entry((V)"x", v_20), new Entry((V)"y", (V)((V)(v_21 + (V)1.0d) / (V)2.0d)), new Entry((V)"facing", (V)"E"), new Entry((V)"width", (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)((V)(v_20 + (V)1.0d) / (V)2.0d)), new Entry((V)"y", v_21), new Entry((V)"facing", (V)"S"), new Entry((V)"width", (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)((V)(v_21 + (V)1.0d) / (V)2.0d)), new Entry((V)"facing", (V)"W"), new Entry((V)"width", (V)1.0d))))));
return Results.Empty;
}));
var values_22 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"core:room/entry", R.Table(new Entry((V)"id", (V)"core:room/entry"), new Entry((V)"roles", R.Table(new Entry((V)1, (V)"start"))), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"####.####"), new Entry((V)2, (V)"#.......#"), new Entry((V)3, (V)"#..#....#"), new Entry((V)4, (V)"........."), new Entry((V)5, (V)"#....#..#"), new Entry((V)6, (V)"#.......#"), new Entry((V)7, (V)"####.####"))), new Entry((V)"sockets", R.Call(v_18, R.Join(new V[] {(V)9.0d}, new Results((V)7.0d))).First), new Entry((V)"markers", R.Table(new Entry((V)"player", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)4.0d))))), new Entry((V)"enemy", R.Table()), new Entry((V)"chest", R.Table()), new Entry((V)"landmark", R.Table()), new Entry((V)"secret_anchor", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))))))), new Entry((V)"editable", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)6.0d), new Entry((V)"y", (V)5.0d))))), new Entry((V)"rotations", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d))), new Entry((V)"mirror", (V)true), new Entry((V)"protected_default", (V)true))), new Entry((V)"core:room/watch", R.Table(new Entry((V)"id", (V)"core:room/watch"), new Entry((V)"roles", R.Table(new Entry((V)1, (V)"combat"))), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"####.####"), new Entry((V)2, (V)"#.......#"), new Entry((V)3, (V)"#.#...#.#"), new Entry((V)4, (V)"........."), new Entry((V)5, (V)"#...#...#"), new Entry((V)6, (V)"#.......#"), new Entry((V)7, (V)"####.####"))), new Entry((V)"sockets", R.Call(v_18, R.Join(new V[] {(V)9.0d}, new Results((V)7.0d))).First), new Entry((V)"markers", R.Table(new Entry((V)"player", R.Table()), new Entry((V)"enemy", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)3.0d))))), new Entry((V)"chest", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)5.0d))))), new Entry((V)"landmark", R.Table()), new Entry((V)"secret_anchor", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)6.0d))))), new Entry((V)"secret_wall", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)6.0d))))))), new Entry((V)"editable", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)5.0d))))), new Entry((V)"rotations", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d))), new Entry((V)"mirror", (V)true), new Entry((V)"protected_default", (V)true))), new Entry((V)"core:room/gallery", R.Table(new Entry((V)"id", (V)"core:room/gallery"), new Entry((V)"roles", R.Table(new Entry((V)1, (V)"combat"), new Entry((V)2, (V)"landmark"))), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"#######.#######"), new Entry((V)2, (V)"#.............#"), new Entry((V)3, (V)"#..#......#...#"), new Entry((V)4, (V)"#.............#"), new Entry((V)5, (V)"#.....#.......#"), new Entry((V)6, (V)"..............."), new Entry((V)7, (V)"#.......#.....#"), new Entry((V)8, (V)"#.............#"), new Entry((V)9, (V)"#...#......#..#"), new Entry((V)10, (V)"#.............#"), new Entry((V)11, (V)"#######.#######"))), new Entry((V)"sockets", R.Call(v_18, R.Join(new V[] {(V)15.0d}, new Results((V)11.0d))).First), new Entry((V)"markers", R.Table(new Entry((V)"player", R.Table()), new Entry((V)"enemy", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)11.0d), new Entry((V)"y", (V)8.0d))))), new Entry((V)"chest", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)12.0d), new Entry((V)"y", (V)3.0d))))), new Entry((V)"landmark", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)8.0d), new Entry((V)"y", (V)6.0d))))), new Entry((V)"secret_anchor", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))))), new Entry((V)"secret_wall", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)2.0d))))))), new Entry((V)"editable", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)11.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)5.0d))), new Entry((V)4, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)7.0d))), new Entry((V)5, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)9.0d))))), new Entry((V)"rotations", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d))), new Entry((V)"mirror", (V)true), new Entry((V)"protected_default", (V)true))), new Entry((V)"core:room/cache", R.Table(new Entry((V)"id", (V)"core:room/cache"), new Entry((V)"roles", R.Table(new Entry((V)1, (V)"treasure"), new Entry((V)2, (V)"combat"))), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"#######.#######"), new Entry((V)2, (V)"#.............#"), new Entry((V)3, (V)"#..###........#"), new Entry((V)4, (V)"#..#..........#"), new Entry((V)5, (V)"#..#....###...#"), new Entry((V)6, (V)"..............."), new Entry((V)7, (V)"#.......#.....#"), new Entry((V)8, (V)"#.............#"), new Entry((V)9, (V)"#...###.......#"), new Entry((V)10, (V)"#.............#"), new Entry((V)11, (V)"#######.#######"))), new Entry((V)"sockets", R.Call(v_18, R.Join(new V[] {(V)15.0d}, new Results((V)11.0d))).First), new Entry((V)"markers", R.Table(new Entry((V)"player", R.Table()), new Entry((V)"enemy", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)11.0d), new Entry((V)"y", (V)8.0d))))), new Entry((V)"chest", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)12.0d), new Entry((V)"y", (V)3.0d))))), new Entry((V)"landmark", R.Table()), new Entry((V)"secret_anchor", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))))), new Entry((V)"secret_wall", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)2.0d))))))), new Entry((V)"editable", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)4.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)10.0d), new Entry((V)"y", (V)5.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)9.0d))))), new Entry((V)"rotations", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d))), new Entry((V)"mirror", (V)true), new Entry((V)"protected_default", (V)true))), new Entry((V)"core:room/shrine", R.Table(new Entry((V)"id", (V)"core:room/shrine"), new Entry((V)"roles", R.Table(new Entry((V)1, (V)"landmark"), new Entry((V)2, (V)"treasure"))), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"####.####"), new Entry((V)2, (V)"#.......#"), new Entry((V)3, (V)"#.###...#"), new Entry((V)4, (V)"........."), new Entry((V)5, (V)"#...###.#"), new Entry((V)6, (V)"#.......#"), new Entry((V)7, (V)"####.####"))), new Entry((V)"sockets", R.Call(v_18, R.Join(new V[] {(V)9.0d}, new Results((V)7.0d))).First), new Entry((V)"markers", R.Table(new Entry((V)"player", R.Table()), new Entry((V)"enemy", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)5.0d))))), new Entry((V)"chest", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)2.0d))))), new Entry((V)"landmark", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)4.0d))))), new Entry((V)"secret_anchor", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)2.0d), new Entry((V)"y", (V)2.0d))))), new Entry((V)"secret_wall", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)1.0d), new Entry((V)"y", (V)2.0d))))))), new Entry((V)"editable", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)7.0d), new Entry((V)"y", (V)5.0d))))), new Entry((V)"rotations", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d))), new Entry((V)"mirror", (V)true), new Entry((V)"protected_default", (V)true))), new Entry((V)"core:room/arena", R.Table(new Entry((V)"id", (V)"core:room/arena"), new Entry((V)"roles", R.Table(new Entry((V)1, (V)"elite"))), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"###########.###########"), new Entry((V)2, (V)"#.....................#"), new Entry((V)3, (V)"#.....................#"), new Entry((V)4, (V)"#....###.......###....#"), new Entry((V)5, (V)"#.....................#"), new Entry((V)6, (V)"#.....................#"), new Entry((V)7, (V)"#.....................#"), new Entry((V)8, (V)"#..##.............##..#"), new Entry((V)9, (V)"......................."), new Entry((V)10, (V)"#..##.............##..#"), new Entry((V)11, (V)"#.....................#"), new Entry((V)12, (V)"#.....................#"), new Entry((V)13, (V)"#.....................#"), new Entry((V)14, (V)"#....###.......###....#"), new Entry((V)15, (V)"#.....................#"), new Entry((V)16, (V)"#.....................#"), new Entry((V)17, (V)"###########.###########"))), new Entry((V)"sockets", R.Call(v_18, R.Join(new V[] {(V)23.0d}, new Results((V)17.0d))).First), new Entry((V)"markers", R.Table(new Entry((V)"player", R.Table()), new Entry((V)"enemy", R.Table()), new Entry((V)"chest", R.Table()), new Entry((V)"landmark", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)12.0d), new Entry((V)"y", (V)9.0d))))), new Entry((V)"elite", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)12.0d), new Entry((V)"y", (V)9.0d))))), new Entry((V)"secret_anchor", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)3.0d), new Entry((V)"y", (V)3.0d))))))), new Entry((V)"editable", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)6.0d), new Entry((V)"y", (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)"x", (V)18.0d), new Entry((V)"y", (V)4.0d))), new Entry((V)3, R.Table(new Entry((V)"x", (V)6.0d), new Entry((V)"y", (V)14.0d))), new Entry((V)4, R.Table(new Entry((V)"x", (V)18.0d), new Entry((V)"y", (V)14.0d))))), new Entry((V)"rotations", R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)2.0d), new Entry((V)4, (V)3.0d))), new Entry((V)"mirror", (V)true), new Entry((V)"protected_default", (V)true))))));
var obj_23 = v_8;
V key_24 = (V)"definitions";
R.Set(obj_23, key_24, values_22[0]);
V v_25 = V.Nil;
v_25 = new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
V v_28 = args_26[1];
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_31))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_31));
}
var values_32 = R.Join(new V[] {}, new Results(R.Table()));
V v_33 = values_32[0];
foreach (var row_34 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_31)))))) {
V v_35 = row_34[0];
V v_36 = row_34[1];
var values_37 = R.Join(new V[] {}, R.Call(v_29, R.Join(new V[] {}, new Results(v_36))));
var obj_38 = v_33;
V key_39 = v_35;
R.Set(obj_38, key_39, values_37[0]);
}
return R.Join(new V[] {}, new Results(v_33));
return Results.Empty;
}));
var values_40 = R.Join(new V[] {}, R.Call(v_29, R.Join(new V[] {}, new Results(v_27))));
V v_41 = values_40[0];
var values_42 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_28, (V)"room_prefix"), R.Invoke(R.Get(v_27, (V)"id"), "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First)));
var obj_43 = v_41;
V key_44 = (V)"id";
R.Set(obj_43, key_44, values_42[0]);
var values_45 = R.Join(new V[] {(V)R.Length(R.Get(R.Get(v_41, (V)"rows"), (V)1.0d))}, new Results((V)R.Length(R.Get(v_41, (V)"rows"))));
V v_46 = values_45[0];
V v_47 = values_45[1];
var values_48 = R.Join(new V[] {}, new Results(R.Table()));
V v_49 = values_48[0];
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_41, (V)"sockets"))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
var values_53 = R.Join(new V[] {}, new Results((V)true));
var obj_54 = v_49;
V key_55 = R.Concat(R.Get(v_52, (V)"y"), R.Concat((V)":", R.Get(v_52, (V)"x")));
R.Set(obj_54, key_55, values_53[0]);
var values_56 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_52, (V)"facing") == (V)"W"), () => (V)1.0d), () => R.And((V)(R.Get(v_52, (V)"facing") == (V)"E"), () => (-(V)1.0d))), () => (V)0.0d)));
V v_57 = values_56[0];
var values_58 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_52, (V)"facing") == (V)"N"), () => (V)1.0d), () => R.And((V)(R.Get(v_52, (V)"facing") == (V)"S"), () => (-(V)1.0d))), () => (V)0.0d)));
V v_59 = values_58[0];
var values_60 = R.Join(new V[] {}, new Results((V)true));
var obj_61 = v_49;
V key_62 = R.Concat((V)(R.Get(v_52, (V)"y") + v_59), R.Concat((V)":", (V)(R.Get(v_52, (V)"x") + v_57)));
R.Set(obj_61, key_62, values_60[0]);
}
foreach (var row_63 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_41, (V)"markers"))))))) {
V v_64 = row_63[0];
V v_65 = row_63[1];
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_65)))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
var values_69 = R.Join(new V[] {}, new Results((V)true));
var obj_70 = v_49;
V key_71 = R.Concat(R.Get(v_68, (V)"y"), R.Concat((V)":", R.Get(v_68, (V)"x")));
R.Set(obj_70, key_71, values_69[0]);
foreach (var row_72 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_73 = row_72[0];
V v_74 = row_72[1];
var values_75 = R.Join(new V[] {}, new Results((V)true));
var obj_76 = v_49;
V key_77 = R.Concat((V)(R.Get(v_68, (V)"y") + R.Get(v_74, (V)2.0d)), R.Concat((V)":", (V)(R.Get(v_68, (V)"x") + R.Get(v_74, (V)1.0d))));
R.Set(obj_76, key_77, values_75[0]);
}
}
}
var values_78 = R.Join(new V[] {}, new Results((V)true));
var obj_79 = v_49;
V key_80 = R.Concat(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_47 + (V)1.0d) / (V)2.0d)))).First, R.Concat((V)":", R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_46 + (V)1.0d) / (V)2.0d)))).First));
R.Set(obj_79, key_80, values_78[0]);
var values_81 = R.Join(new V[] {}, new Results(R.Table()));
V v_82 = values_81[0];
var values_83 = R.Join(new V[] {}, new Results(R.Table()));
V v_84 = values_83[0];
foreach (var row_85 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_41, (V)"rows"))))))) {
V v_86 = row_85[0];
V v_87 = row_85[1];
var values_88 = R.Join(new V[] {}, new Results(R.Table()));
var obj_89 = v_82;
V key_90 = v_86;
R.Set(obj_89, key_90, values_88[0]);
{ double end_93 = R.Num(v_46), step_94 = R.Num((V)1.0d);
for (double i_92 = R.Num((V)1.0d); step_94 > 0 ? i_92 <= end_93 : i_92 >= end_93; i_92 += step_94) {
V v_91 = i_92;
var values_95 = R.Join(new V[] {}, R.Invoke(v_87, "sub", R.Join(new V[] {v_91}, new Results(v_91))));
var obj_96 = R.Get(v_82, v_86);
V key_97 = v_91;
R.Set(obj_96, key_97, values_95[0]);
}
}
}
V v_98 = V.Nil;
v_98 = new V((RuleFunction)(args_99 => {
V v_100 = args_99[0];
V v_101 = args_99[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(v_100 > (V)1.0d), () => (V)(v_100 < v_46)), () => (V)(v_101 > (V)1.0d)), () => (V)(v_101 < v_47)), () => (V)(!R.Truth(R.Get(v_49, R.Concat(v_101, R.Concat((V)":", v_100)))))))) {
var values_102 = R.Join(new V[] {}, new Results((V)"#"));
var obj_103 = R.Get(v_82, v_101);
V key_104 = v_100;
R.Set(obj_103, key_104, values_102[0]);
var values_105 = R.Join(new V[] {}, new Results((V)true));
var obj_106 = v_84;
V key_107 = R.Concat(v_101, R.Concat((V)":", v_100));
R.Set(obj_106, key_107, values_105[0]);
}
return Results.Empty;
}));
if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"warrens"))) {
foreach (var row_108 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, (V)2.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)(v_46 - (V)2.0d)), new Entry((V)2, (V)2.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)3.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)(v_46 - (V)1.0d)), new Entry((V)2, (V)3.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)(v_47 - (V)2.0d)))), new Entry((V)6, R.Table(new Entry((V)1, (V)(v_46 - (V)1.0d)), new Entry((V)2, (V)(v_47 - (V)2.0d)))), new Entry((V)7, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, (V)(v_47 - (V)1.0d)))), new Entry((V)8, R.Table(new Entry((V)1, (V)(v_46 - (V)2.0d)), new Entry((V)2, (V)(v_47 - (V)1.0d))))))))))) {
V v_109 = row_108[0];
V v_110 = row_108[1];
R.Call(v_98, R.Join(new V[] {R.Get(v_110, (V)1.0d)}, new Results(R.Get(v_110, (V)2.0d))));
}
if (R.Truth((V)(v_46 >= (V)15.0d))) {
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)4.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)(v_46 - (V)5.0d)), new Entry((V)2, (V)(v_47 - (V)3.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (V)(v_46 - (V)6.0d)), new Entry((V)2, (V)(v_47 - (V)3.0d))))))))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
R.Call(v_98, R.Join(new V[] {R.Get(v_113, (V)1.0d)}, new Results(R.Get(v_113, (V)2.0d))));
}
}
} else if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"canals"))) {
if (R.Truth((V)(v_46 >= (V)15.0d))) {
{ double end_116 = R.Num((V)(v_47 - (V)2.0d)), step_117 = R.Num((V)1.0d);
for (double i_115 = R.Num((V)3.0d); step_117 > 0 ? i_115 <= end_116 : i_115 >= end_116; i_115 += step_117) {
V v_114 = i_115;
if (R.Truth(R.And(R.And((V)(v_114 != R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_47 + (V)1.0d) / (V)2.0d)))).First), () => (V)(v_114 != (V)4.0d)), () => (V)(v_114 != (V)(v_47 - (V)3.0d))))) {
R.Call(v_98, R.Join(new V[] {(V)4.0d}, new Results(v_114)));

R.Call(v_98, R.Join(new V[] {(V)(v_46 - (V)3.0d)}, new Results(v_114)));
}
}
}
} else {
R.Call(v_98, R.Join(new V[] {(V)3.0d}, new Results((V)3.0d)));

R.Call(v_98, R.Join(new V[] {(V)(v_46 - (V)2.0d)}, new Results((V)(v_47 - (V)2.0d))));
}
{ double end_120 = R.Num((V)(v_47 - (V)1.0d)), step_121 = R.Num((V)1.0d);
for (double i_119 = R.Num((V)2.0d); step_121 > 0 ? i_119 <= end_120 : i_119 >= end_120; i_119 += step_121) {
V v_118 = i_119;
{ double end_124 = R.Num((V)(v_46 - (V)1.0d)), step_125 = R.Num((V)1.0d);
for (double i_123 = R.Num((V)2.0d); step_125 > 0 ? i_123 <= end_124 : i_123 >= end_124; i_123 += step_125) {
V v_122 = i_123;
if (R.Truth((V)(R.Get(R.Get(v_82, v_118), v_122) == (V)"#"))) {
var values_126 = R.Join(new V[] {}, new Results((V)"~"));
var obj_127 = R.Get(v_82, v_118);
V key_128 = v_122;
R.Set(obj_127, key_128, values_126[0]);
}
}
}
}
}
} else if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"courtyards"))) {
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)(v_46 - (V)2.0d)), new Entry((V)2, (V)3.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, (V)(v_47 - (V)2.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (V)(v_46 - (V)2.0d)), new Entry((V)2, (V)(v_47 - (V)2.0d))))))))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
R.Call(v_98, R.Join(new V[] {R.Get(v_131, (V)1.0d)}, new Results(R.Get(v_131, (V)2.0d))));
}
if (R.Truth((V)(v_46 >= (V)15.0d))) {
{ double end_134 = R.Num((V)(v_46 - (V)4.0d)), step_135 = R.Num((V)1.0d);
for (double i_133 = R.Num((V)5.0d); step_135 > 0 ? i_133 <= end_134 : i_133 >= end_134; i_133 += step_135) {
V v_132 = i_133;
if (R.Truth(R.And((V)(v_132 != R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_46 + (V)1.0d) / (V)2.0d)))).First), () => (V)((V)(v_132 % (V)3.0d) != (V)0.0d)))) {
R.Call(v_98, R.Join(new V[] {v_132}, new Results((V)3.0d)));

R.Call(v_98, R.Join(new V[] {v_132}, new Results((V)(v_47 - (V)2.0d))));
}
}
}
}
} else if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"observatory"))) {
if (R.Truth((V)(v_46 >= (V)15.0d))) {
foreach (var row_136 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)(v_46 - (V)3.0d)), new Entry((V)2, (V)4.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)(v_47 - (V)3.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (V)(v_46 - (V)3.0d)), new Entry((V)2, (V)(v_47 - (V)3.0d)))), new Entry((V)5, R.Table(new Entry((V)1, (V)6.0d), new Entry((V)2, (V)3.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)(v_46 - (V)5.0d)), new Entry((V)2, (V)(v_47 - (V)2.0d))))))))))) {
V v_137 = row_136[0];
V v_138 = row_136[1];
R.Call(v_98, R.Join(new V[] {R.Get(v_138, (V)1.0d)}, new Results(R.Get(v_138, (V)2.0d))));
}
} else {
R.Call(v_98, R.Join(new V[] {(V)3.0d}, new Results((V)3.0d)));

R.Call(v_98, R.Join(new V[] {(V)(v_46 - (V)2.0d)}, new Results((V)(v_47 - (V)2.0d))));
}
} else if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"archive"))) {
if (R.Truth((V)(v_46 >= (V)15.0d))) {
{ double end_141 = R.Num((V)(v_47 - (V)2.0d)), step_142 = R.Num((V)1.0d);
for (double i_140 = R.Num((V)3.0d); step_142 > 0 ? i_140 <= end_141 : i_140 >= end_141; i_140 += step_142) {
V v_139 = i_140;
if (R.Truth((V)((V)(v_139 % (V)3.0d) != (V)1.0d))) {
R.Call(v_98, R.Join(new V[] {(V)4.0d}, new Results(v_139)));

R.Call(v_98, R.Join(new V[] {(V)(v_46 - (V)3.0d)}, new Results((V)((V)(v_47 + (V)1.0d) - v_139))));
}
}
}
} else {
R.Call(v_98, R.Join(new V[] {(V)3.0d}, new Results((V)3.0d)));

R.Call(v_98, R.Join(new V[] {(V)(v_46 - (V)2.0d)}, new Results((V)(v_47 - (V)2.0d))));
}
} else if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"forge"))) {
if (R.Truth((V)(v_46 >= (V)15.0d))) {
{ double end_145 = R.Num((V)(v_46 - (V)3.0d)), step_146 = R.Num((V)1.0d);
for (double i_144 = R.Num((V)4.0d); step_146 > 0 ? i_144 <= end_145 : i_144 >= end_145; i_144 += step_146) {
V v_143 = i_144;
if (R.Truth((V)((V)(v_143 % (V)4.0d) != (V)0.0d))) {
R.Call(v_98, R.Join(new V[] {v_143}, new Results((V)3.0d)));
if (R.Truth((V)((V)(v_143 % (V)4.0d) != (V)2.0d))) {
R.Call(v_98, R.Join(new V[] {(V)((V)(v_46 + (V)1.0d) - v_143)}, new Results((V)(v_47 - (V)2.0d))));
}
}
}
}
} else {
R.Call(v_98, R.Join(new V[] {(V)3.0d}, new Results((V)(v_47 - (V)2.0d))));

R.Call(v_98, R.Join(new V[] {(V)(v_46 - (V)2.0d)}, new Results((V)3.0d)));
}
}
var values_147 = R.Join(new V[] {}, new Results(R.Table()));
V v_148 = values_147[0];
foreach (var row_149 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_27, (V)"editable"), () => R.Table()))))))) {
V v_150 = row_149[0];
V v_151 = row_149[1];
var values_152 = R.Join(new V[] {}, new Results((V)true));
var obj_153 = v_148;
V key_154 = R.Concat(R.Get(v_151, (V)"y"), R.Concat((V)":", R.Get(v_151, (V)"x")));
R.Set(obj_153, key_154, values_152[0]);
}
V v_155 = V.Nil;
v_155 = new V((RuleFunction)(args_156 => {
V v_157 = args_156[0];
V v_158 = args_156[1];
V v_159 = args_156[2];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_157 > (V)1.0d), () => (V)(v_157 < v_46)), () => (V)(v_158 > (V)1.0d)), () => (V)(v_158 < v_47)), () => (V)(R.Get(R.Get(v_82, v_158), v_157) == (V)".")), () => (V)(!R.Truth(R.Get(v_49, R.Concat(v_158, R.Concat((V)":", v_157)))))), () => (V)(!R.Truth(R.Get(v_148, R.Concat(v_158, R.Concat((V)":", v_157)))))))) {
var values_160 = R.Join(new V[] {}, new Results(v_159));
var obj_161 = R.Get(v_82, v_158);
V key_162 = v_157;
R.Set(obj_161, key_162, values_160[0]);
}
return Results.Empty;
}));
if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"observatory"))) {
foreach (var row_163 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)3.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)(v_46 - (V)2.0d)), new Entry((V)2, (V)(v_47 - (V)3.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)(v_46 - (V)3.0d)), new Entry((V)2, (V)3.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)(v_47 - (V)2.0d))))))))))) {
V v_164 = row_163[0];
V v_165 = row_163[1];
R.Call(v_155, R.Join(new V[] {R.Get(v_165, (V)1.0d), R.Get(v_165, (V)2.0d)}, new Results((V)"i")));
}
} else if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"archive"))) {
foreach (var row_166 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)3.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)(v_46 - (V)4.0d)), new Entry((V)2, (V)(v_47 - (V)2.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)(v_47 - (V)3.0d))))))))))) {
V v_167 = row_166[0];
V v_168 = row_166[1];
R.Call(v_155, R.Join(new V[] {R.Get(v_168, (V)1.0d), R.Get(v_168, (V)2.0d)}, new Results((V)"m")));
}
if (R.Truth((V)(v_46 < (V)15.0d))) {
R.Call(v_155, R.Join(new V[] {(V)3.0d, (V)((V)(v_47 + (V)1.0d) / (V)2.0d)}, new Results((V)"m")));
}
} else if (R.Truth((V)(R.Get(v_28, (V)"strategy") == (V)"forge"))) {
foreach (var row_169 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)4.0d), new Entry((V)2, (V)4.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)(v_46 - (V)3.0d)), new Entry((V)2, (V)(v_47 - (V)3.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)5.0d), new Entry((V)2, (V)(v_47 - (V)2.0d))))))))))) {
V v_170 = row_169[0];
V v_171 = row_169[1];
R.Call(v_155, R.Join(new V[] {R.Get(v_171, (V)1.0d), R.Get(v_171, (V)2.0d)}, new Results((V)"c")));
}
if (R.Truth((V)(v_46 < (V)15.0d))) {
R.Call(v_155, R.Join(new V[] {(V)3.0d, (V)((V)(v_47 + (V)1.0d) / (V)2.0d)}, new Results((V)"c")));
}
}
{ double end_174 = R.Num(v_47), step_175 = R.Num((V)1.0d);
for (double i_173 = R.Num((V)1.0d); step_175 > 0 ? i_173 <= end_174 : i_173 >= end_174; i_173 += step_175) {
V v_172 = i_173;
var values_176 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(v_82, v_172)))));
var obj_177 = R.Get(v_41, (V)"rows");
V key_178 = v_172;
R.Set(obj_177, key_178, values_176[0]);
}
}
var values_179 = R.Join(new V[] {}, new Results(R.Table()));
var obj_180 = v_41;
V key_181 = (V)"editable";
R.Set(obj_180, key_181, values_179[0]);
foreach (var row_182 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_27, (V)"editable"), () => R.Table()))))))) {
V v_183 = row_182[0];
V v_184 = row_182[1];
if (R.Truth((V)(!R.Truth(R.Get(v_84, R.Concat(R.Get(v_184, (V)"y"), R.Concat((V)":", R.Get(v_184, (V)"x")))))))) {
var values_185 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_184, (V)"x")), new Entry((V)"y", R.Get(v_184, (V)"y")))));
var obj_186 = R.Get(v_41, (V)"editable");
V key_187 = (V)((V)R.Length(R.Get(v_41, (V)"editable")) + (V)1.0d);
R.Set(obj_186, key_187, values_185[0]);
}
}
return R.Join(new V[] {}, new Results(v_41));
return Results.Empty;
}));
{ double end_190 = R.Num((V)R.Length(R.Get(v_4, (V)"order"))), step_191 = R.Num((V)1.0d);
for (double i_189 = R.Num((V)2.0d); step_191 > 0 ? i_189 <= end_190 : i_189 >= end_190; i_189 += step_191) {
V v_188 = i_189;
var values_192 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_4, (V)"order"), v_188)))));
V v_193 = values_192[0];
foreach (var row_194 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_195 = row_194[0];
V v_196 = row_194[1];
var values_197 = R.Join(new V[] {}, R.Call(v_25, R.Join(new V[] {R.Get(R.Get(v_8, (V)"definitions"), v_196)}, new Results(v_193))));
V v_198 = values_197[0];
var values_199 = R.Join(new V[] {}, new Results(v_198));
var obj_200 = R.Get(v_8, (V)"definitions");
V key_201 = R.Get(v_198, (V)"id");
R.Set(obj_200, key_201, values_199[0]);
}
}
}
var values_202 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.room_expansion"))));
V v_203 = values_202[0];
foreach (var row_204 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_203, (V)"order"))))))) {
V v_205 = row_204[0];
V v_206 = row_204[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Get(v_8, (V)"definitions"), v_206)))}, new Results((V)"duplicate expanded room")));
var values_207 = R.Join(new V[] {}, new Results(v_206));
var obj_208 = R.Get(v_8, (V)"order");
V key_209 = (V)((V)R.Length(R.Get(v_8, (V)"order")) + (V)1.0d);
R.Set(obj_208, key_209, values_207[0]);
}
foreach (var row_210 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_203, (V)"definitions"))))))) {
V v_211 = row_210[0];
V v_212 = row_210[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(R.Get(v_8, (V)"definitions"), v_211)))}, new Results(R.Concat((V)"duplicate expanded room: ", v_211))));
var values_213 = R.Join(new V[] {}, new Results(v_212));
var obj_214 = R.Get(v_8, (V)"definitions");
V key_215 = v_211;
R.Set(obj_214, key_215, values_213[0]);
}
var values_216 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"N", (V)true), new Entry((V)"E", (V)true), new Entry((V)"S", (V)true), new Entry((V)"W", (V)true))));
V v_217 = values_216[0];
var values_218 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"player", (V)true), new Entry((V)"enemy", (V)true), new Entry((V)"chest", (V)true), new Entry((V)"landmark", (V)true), new Entry((V)"elite", (V)true), new Entry((V)"secret_anchor", (V)true), new Entry((V)"secret_wall", (V)true), new Entry((V)"stair", (V)true))));
V v_219 = values_218[0];
var values_220 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"start", (V)true), new Entry((V)"combat", (V)true), new Entry((V)"treasure", (V)true), new Entry((V)"landmark", (V)true), new Entry((V)"elite", (V)true), new Entry((V)"secret", (V)true))));
V v_221 = values_220[0];
V v_222 = V.Nil;
v_222 = new V((RuleFunction)(args_223 => {
V v_224 = args_223[0];
V v_225 = args_223[1];
return R.Join(new V[] {}, new Results(R.Concat(v_224, R.Concat((V)",", v_225))));
return Results.Empty;
}));
V v_226 = V.Nil;
v_226 = new V((RuleFunction)(args_227 => {
V v_228 = args_227[0];
V v_229 = args_227[1];
foreach (var row_230 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_228, (V)"roles"))))))) {
V v_231 = row_230[0];
V v_232 = row_230[1];
if (R.Truth((V)(v_232 == v_229))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
var values_233 = R.Join(new V[] {}, new Results(v_226));
var obj_234 = v_8;
V key_235 = (V)"has_role";
R.Set(obj_234, key_235, values_233[0]);
R.Set(v_8, (V)"validate", new V((RuleFunction)(args_236 => {
V v_237 = args_236[0];
V v_238 = args_236[1];
V v_239 = args_236[2];
var values_240 = R.Join(new V[] {R.Or(v_237, () => R.Get(v_8, (V)"definitions"))}, new Results(R.Or(v_238, () => R.Get(v_8, (V)"order"))));
v_237 = values_240[0];
v_238 = values_240[1];
var values_241 = R.Join(new V[] {}, new Results(R.Or(v_239, () => R.Get(v_8, (V)"biome"))));
v_239 = values_241[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"id")))).First == (V)"string"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"floor_id")))).First == (V)"string")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"width")))).First == (V)"number")), () => (V)((V)(R.Get(v_239, (V)"width") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_239, (V)"width") >= (V)80.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"height")))).First == (V)"number")), () => (V)((V)(R.Get(v_239, (V)"height") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_239, (V)"height") >= (V)60.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"room_min")))).First == (V)"number")), () => (V)((V)(R.Get(v_239, (V)"room_min") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"room_max")))).First == (V)"number")), () => (V)((V)(R.Get(v_239, (V)"room_max") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_239, (V)"room_min") >= (V)8.0d)), () => (V)(R.Get(v_239, (V)"room_max") <= (V)12.0d)), () => (V)(R.Get(v_239, (V)"room_min") <= R.Get(v_239, (V)"room_max"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"standard_chests")))).First == (V)"number")), () => (V)((V)(R.Get(v_239, (V)"standard_chests") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_239, (V)"standard_chests") > (V)0.0d)), () => (V)(R.Get(v_239, (V)"standard_chests") <= (V)(R.Get(v_239, (V)"room_min") - (V)2.0d))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"retry_limit")))).First == (V)"number")), () => (V)((V)(R.Get(v_239, (V)"retry_limit") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_239, (V)"retry_limit") >= (V)0.0d)), () => (V)(R.Get(v_239, (V)"retry_limit") <= (V)8.0d))}, new Results((V)"invalid Overgrown Ruins biome rules")));
var values_242 = R.Join(new V[] {}, new Results(R.Table()));
V v_243 = values_242[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"enemy_ids")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_239, (V)"enemy_ids")) == (V)3.0d))}, new Results((V)"biome needs three enemy archetypes")));
foreach (var row_244 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_239, (V)"enemy_ids"))))))) {
V v_245 = row_244[0];
V v_246 = row_244[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"get_enemy"), R.Join(new V[] {}, new Results(v_246))).First, () => (V)(!R.Truth(R.Get(v_243, v_246))))}, new Results(R.Concat((V)"invalid biome enemy reference: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_246))).First))));
var values_247 = R.Join(new V[] {}, new Results((V)true));
var obj_248 = v_243;
V key_249 = v_246;
R.Set(obj_248, key_249, values_247[0]);
}
var values_250 = R.Join(new V[] {}, new Results(R.Table()));
V v_251 = values_250[0];
foreach (var row_252 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_238)))))) {
V v_253 = row_252[0];
V v_254 = row_252[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(!R.Truth(R.Get(v_251, v_254))), () => R.Get(v_237, v_254)), () => (V)(R.Get(R.Get(v_237, v_254), (V)"id") == v_254))}, new Results(R.Concat((V)"duplicate or missing room ID: ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_254))).First))));
var values_255 = R.Join(new V[] {}, new Results((V)true));
var obj_256 = v_251;
V key_257 = v_254;
R.Set(obj_256, key_257, values_255[0]);
var values_258 = R.Join(new V[] {}, new Results(R.Get(v_237, v_254)));
V v_259 = values_258[0];
var values_260 = R.Join(new V[] {(V)R.Length(R.Get(R.Get(v_259, (V)"rows"), (V)1.0d))}, new Results((V)R.Length(R.Get(v_259, (V)"rows"))));
V v_261 = values_260[0];
V v_262 = values_260[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(v_261 >= (V)7.0d), () => (V)(v_262 >= (V)7.0d)), () => (V)((V)(v_261 % (V)2.0d) == (V)1.0d)), () => (V)((V)(v_262 % (V)2.0d) == (V)1.0d))}, new Results(R.Concat((V)"invalid room dimensions: ", v_254))));
foreach (var row_263 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"rows"))))))) {
V v_264 = row_263[0];
V v_265 = row_263[1];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_265) == v_261)}, new Results(R.Concat((V)"invalid room tile in ", R.Concat(v_254, R.Concat((V)" row ", v_264))))));
{ double end_268 = R.Num((V)R.Length(v_265)), step_269 = R.Num((V)1.0d);
for (double i_267 = R.Num((V)1.0d); step_269 > 0 ? i_267 <= end_268 : i_267 >= end_268; i_267 += step_269) {
V v_266 = i_267;
var values_270 = R.Join(new V[] {}, R.Invoke(v_265, "sub", R.Join(new V[] {v_266}, new Results(v_266))));
V v_271 = values_270[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(v_271 == (V)" "), () => R.Call(R.Get(v_6, (V)"is_valid"), R.Join(new V[] {}, new Results(v_271))).First)}, new Results(R.Concat((V)"invalid room tile in ", R.Concat(v_254, R.Concat((V)" row ", v_264))))));
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Get(v_259, (V)"protected_default") == (V)true)}, new Results(R.Concat((V)"room needs a protected mask: ", v_254))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"roles")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_259, (V)"roles")) > (V)0.0d))}, new Results(R.Concat((V)"room needs role: ", v_254))));
var values_272 = R.Join(new V[] {}, new Results(R.Table()));
V v_273 = values_272[0];
foreach (var row_274 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"roles"))))))) {
V v_275 = row_274[0];
V v_276 = row_274[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_221, v_276), () => (V)(!R.Truth(R.Get(v_273, v_276))))}, new Results(R.Concat((V)"invalid room role: ", v_254))));
var values_277 = R.Join(new V[] {}, new Results((V)true));
var obj_278 = v_273;
V key_279 = v_276;
R.Set(obj_278, key_279, values_277[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"rotations")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_259, (V)"rotations")) > (V)0.0d))}, new Results(R.Concat((V)"room needs rotations: ", v_254))));
var values_280 = R.Join(new V[] {}, new Results(R.Table()));
V v_281 = values_280[0];
foreach (var row_282 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"rotations"))))))) {
V v_283 = row_282[0];
V v_284 = row_282[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_284))).First == (V)"number"), () => (V)((V)(v_284 % (V)1.0d) == (V)0.0d)), () => (V)(v_284 >= (V)0.0d)), () => (V)(v_284 <= (V)3.0d)), () => (V)(!R.Truth(R.Get(v_281, v_284))))}, new Results(R.Concat((V)"invalid room rotation: ", v_254))));
var values_285 = R.Join(new V[] {}, new Results((V)true));
var obj_286 = v_281;
V key_287 = v_284;
R.Set(obj_286, key_287, values_285[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"mirror")))).First == (V)"boolean")}, new Results(R.Concat((V)"invalid mirror permission: ", v_254))));
var values_288 = R.Join(new V[] {}, new Results(R.Table()));
V v_289 = values_288[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"sockets")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_259, (V)"sockets")) == (V)4.0d))}, new Results(R.Concat((V)"room needs four sockets: ", v_254))));
var values_290 = R.Join(new V[] {}, new Results(R.Table()));
V v_291 = values_290[0];
foreach (var row_292 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"sockets"))))))) {
V v_293 = row_292[0];
V v_294 = row_292[1];
var values_295 = R.Join(new V[] {R.Get(v_294, (V)"x"), R.Get(v_294, (V)"y")}, new Results(R.Get(v_294, (V)"facing")));
V v_296 = values_295[0];
V v_297 = values_295[1];
V v_298 = values_295[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.Get(v_217, v_298), () => (V)(!R.Truth(R.Get(v_291, v_298)))), () => (V)(R.Get(v_294, (V)"width") == (V)1.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_296))).First == (V)"number")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_297))).First == (V)"number")), () => (V)((V)(v_296 % (V)1.0d) == (V)0.0d)), () => (V)((V)(v_297 % (V)1.0d) == (V)0.0d)), () => (V)(v_296 >= (V)1.0d)), () => (V)(v_296 <= v_261)), () => (V)(v_297 >= (V)1.0d)), () => (V)(v_297 <= v_262))}, new Results(R.Concat((V)"invalid room socket: ", v_254))));
var values_299 = R.Join(new V[] {}, new Results((V)true));
var obj_300 = v_291;
V key_301 = v_298;
R.Set(obj_300, key_301, values_299[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Or(R.Or(R.Or(R.And((V)(v_298 == (V)"N"), () => (V)(v_297 == (V)1.0d)), () => R.And((V)(v_298 == (V)"S"), () => (V)(v_297 == v_262))), () => R.And((V)(v_298 == (V)"W"), () => (V)(v_296 == (V)1.0d))), () => R.And((V)(v_298 == (V)"E"), () => (V)(v_296 == v_261))), () => (V)(R.Invoke(R.Get(R.Get(v_259, (V)"rows"), v_297), "sub", R.Join(new V[] {v_296}, new Results(v_296))).First == (V)"."))}, new Results(R.Concat((V)"socket must open at boundary: ", v_254))));
var values_302 = R.Join(new V[] {}, new Results((V)(v_296 + R.Or(R.Or(R.And((V)(v_298 == (V)"W"), () => (V)1.0d), () => R.And((V)(v_298 == (V)"E"), () => (-(V)1.0d))), () => (V)0.0d))));
V v_303 = values_302[0];
var values_304 = R.Join(new V[] {}, new Results((V)(v_297 + R.Or(R.Or(R.And((V)(v_298 == (V)"N"), () => (V)1.0d), () => R.And((V)(v_298 == (V)"S"), () => (-(V)1.0d))), () => (V)0.0d))));
V v_305 = values_304[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Invoke(R.Get(R.Get(v_259, (V)"rows"), v_305), "sub", R.Join(new V[] {v_303}, new Results(v_303))).First == (V)".")}, new Results(R.Concat((V)"socket lacks inner clearance: ", v_254))));
var values_306 = R.Join(new V[] {}, new Results((V)true));
var obj_307 = v_289;
V key_308 = R.Call(v_222, R.Join(new V[] {v_296}, new Results(v_297))).First;
R.Set(obj_307, key_308, values_306[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"markers")))).First == (V)"table")}, new Results(R.Concat((V)"room needs markers: ", v_254))));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(!R.Truth(R.Get(R.Get(v_259, (V)"markers"), (V)"stair"))), () => (V)((V)R.Length(R.Get(R.Get(v_259, (V)"markers"), (V)"stair")) <= (V)1.0d))}, new Results(R.Concat((V)"room may have only one stair marker: ", v_254))));
var values_309 = R.Join(new V[] {}, new Results(R.Table()));
V v_310 = values_309[0];
foreach (var row_311 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_259, (V)"markers"))))))) {
V v_312 = row_311[0];
V v_313 = row_311[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_219, v_312), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_313))).First == (V)"table"))}, new Results(R.Concat((V)"invalid marker kind: ", v_254))));
foreach (var row_314 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_313)))))) {
V v_315 = row_314[0];
V v_316 = row_314[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_316, (V)"x")))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_316, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_316, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)((V)(R.Get(v_316, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_316, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_316, (V)"x") <= v_261)), () => (V)(R.Get(v_316, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_316, (V)"y") <= v_262)), () => (V)(R.Invoke(R.Get(R.Get(v_259, (V)"rows"), R.Get(v_316, (V)"y")), "sub", R.Join(new V[] {R.Get(v_316, (V)"x")}, new Results(R.Get(v_316, (V)"x")))).First == R.Or(R.And((V)(v_312 == (V)"secret_wall"), () => (V)"#"), () => (V)".")))}, new Results(R.Concat((V)"invalid ", R.Concat(v_312, R.Concat((V)" marker: ", v_254))))));
var values_317 = R.Join(new V[] {}, new Results((V)true));
var obj_318 = v_289;
V key_319 = R.Call(v_222, R.Join(new V[] {R.Get(v_316, (V)"x")}, new Results(R.Get(v_316, (V)"y")))).First;
R.Set(obj_318, key_319, values_317[0]);
if (R.Truth(R.Or(R.Or(R.Or((V)(v_312 == (V)"player"), () => (V)(v_312 == (V)"enemy")), () => (V)(v_312 == (V)"chest")), () => (V)(v_312 == (V)"stair")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_310, R.Call(v_222, R.Join(new V[] {R.Get(v_316, (V)"x")}, new Results(R.Get(v_316, (V)"y")))).First)))}, new Results(R.Concat((V)"overlapping blocking markers: ", v_254))));
var values_320 = R.Join(new V[] {}, new Results((V)true));
var obj_321 = v_310;
V key_322 = R.Call(v_222, R.Join(new V[] {R.Get(v_316, (V)"x")}, new Results(R.Get(v_316, (V)"y")))).First;
R.Set(obj_321, key_322, values_320[0]);
}
}
}
if (R.Truth((V)((V)R.Length(R.Or(R.Get(R.Get(v_259, (V)"markers"), (V)"secret_wall"), () => R.Table())) > (V)0.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)((V)R.Length(R.Get(R.Get(v_259, (V)"markers"), (V)"secret_wall")) == (V)1.0d), () => (V)((V)R.Length(R.Or(R.Get(R.Get(v_259, (V)"markers"), (V)"secret_anchor"), () => R.Table())) == (V)1.0d))}, new Results(R.Concat((V)"secret wall needs one approach anchor: ", v_254))));
var values_323 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_259, (V)"markers"), (V)"secret_wall"), (V)1.0d)));
V v_324 = values_323[0];
var values_325 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_259, (V)"markers"), (V)"secret_anchor"), (V)1.0d)));
V v_326 = values_325[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Or(R.Or(R.Or((V)(R.Get(v_324, (V)"x") == (V)1.0d), () => (V)(R.Get(v_324, (V)"x") == v_261)), () => (V)(R.Get(v_324, (V)"y") == (V)1.0d)), () => (V)(R.Get(v_324, (V)"y") == v_262)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_324, (V)"x") - R.Get(v_326, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_324, (V)"y") - R.Get(v_326, (V)"y"))))).First) == (V)1.0d))}, new Results(R.Concat((V)"secret wall needs reachable adjacent approach: ", v_254))));
}
if (R.Truth(R.Call(v_226, R.Join(new V[] {v_259}, new Results((V)"start"))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_259, (V)"markers"), (V)"player"), () => (V)((V)R.Length(R.Get(R.Get(v_259, (V)"markers"), (V)"player")) == (V)1.0d))}, new Results(R.Concat((V)"start room needs one player marker: ", v_254))));
}
if (R.Truth(R.Call(v_226, R.Join(new V[] {v_259}, new Results((V)"elite"))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_259, (V)"markers"), (V)"elite"), () => (V)((V)R.Length(R.Get(R.Get(v_259, (V)"markers"), (V)"elite")) == (V)1.0d))}, new Results(R.Concat((V)"elite room needs one arena marker: ", v_254))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Or(R.Call(v_226, R.Join(new V[] {v_259}, new Results((V)"start"))).First, () => (V)((V)R.Length(R.Or(R.Get(R.Get(v_259, (V)"markers"), (V)"player"), () => R.Table())) == (V)0.0d)), () => R.Or(R.Call(v_226, R.Join(new V[] {v_259}, new Results((V)"elite"))).First, () => (V)((V)R.Length(R.Or(R.Get(R.Get(v_259, (V)"markers"), (V)"elite"), () => R.Table())) == (V)0.0d)))}, new Results(R.Concat((V)"marker incompatible with room role: ", v_254))));
if (R.Truth(R.Call(v_226, R.Join(new V[] {v_259}, new Results((V)"combat"))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_259, (V)"markers"), (V)"enemy"), () => (V)((V)R.Length(R.Get(R.Get(v_259, (V)"markers"), (V)"enemy")) > (V)0.0d))}, new Results(R.Concat((V)"combat room needs enemy marker: ", v_254))));
}
if (R.Truth(R.Call(v_226, R.Join(new V[] {v_259}, new Results((V)"treasure"))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_259, (V)"markers"), (V)"chest"), () => (V)((V)R.Length(R.Get(R.Get(v_259, (V)"markers"), (V)"chest")) > (V)0.0d))}, new Results(R.Concat((V)"treasure room needs chest marker: ", v_254))));
}
if (R.Truth(R.Call(v_226, R.Join(new V[] {v_259}, new Results((V)"landmark"))).First)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_259, (V)"markers"), (V)"landmark"), () => (V)((V)R.Length(R.Get(R.Get(v_259, (V)"markers"), (V)"landmark")) > (V)0.0d))}, new Results(R.Concat((V)"landmark room needs landmark marker: ", v_254))));
}
var values_327 = R.Join(new V[] {}, new Results(R.Table()));
V v_328 = values_327[0];
foreach (var row_329 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_259, (V)"editable"), () => R.Table()))))))) {
V v_330 = row_329[0];
V v_331 = row_329[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_331, (V)"x")))).First == (V)"number"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_331, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_331, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)((V)(R.Get(v_331, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_331, (V)"x") > (V)1.0d)), () => (V)(R.Get(v_331, (V)"x") < v_261)), () => (V)(R.Get(v_331, (V)"y") > (V)1.0d)), () => (V)(R.Get(v_331, (V)"y") < v_262)), () => R.Get(R.Table(new Entry((V)".", (V)true), new Entry((V)"#", (V)true), new Entry((V)"~", (V)true)), R.Invoke(R.Get(R.Get(v_259, (V)"rows"), R.Get(v_331, (V)"y")), "sub", R.Join(new V[] {R.Get(v_331, (V)"x")}, new Results(R.Get(v_331, (V)"x")))).First)), () => (V)(!R.Truth(R.Get(v_289, R.Call(v_222, R.Join(new V[] {R.Get(v_331, (V)"x")}, new Results(R.Get(v_331, (V)"y")))).First)))), () => (V)(!R.Truth(R.Get(v_328, R.Call(v_222, R.Join(new V[] {R.Get(v_331, (V)"x")}, new Results(R.Get(v_331, (V)"y")))).First))))}, new Results(R.Concat((V)"editable mask violates protected geometry: ", v_254))));
var values_332 = R.Join(new V[] {}, new Results((V)true));
var obj_333 = v_328;
V key_334 = R.Call(v_222, R.Join(new V[] {R.Get(v_331, (V)"x")}, new Results(R.Get(v_331, (V)"y")))).First;
R.Set(obj_333, key_334, values_332[0]);
}
var values_335 = R.Join(new V[] {}, new Results(R.Table()));
V v_336 = values_335[0];
foreach (var row_337 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_259, (V)"structures"), () => R.Table()))))))) {
V v_338 = row_337[0];
V v_339 = row_337[1];
var values_340 = R.Join(new V[] {}, R.Call(v_222, R.Join(new V[] {R.Get(v_339, (V)"x")}, new Results(R.Get(v_339, (V)"y")))));
V v_341 = values_340[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_339, (V)"kind")))).First == (V)"string"), () => R.Get(R.Table(new Entry((V)"ward", (V)true), new Entry((V)"mushroom", (V)true), new Entry((V)"pillar", (V)true), new Entry((V)"brazier", (V)true), new Entry((V)"crystal", (V)true)), R.Get(v_339, (V)"kind"))), () => (V)(R.Get(v_339, (V)"x") > (V)1.0d)), () => (V)(R.Get(v_339, (V)"x") < v_261)), () => (V)(R.Get(v_339, (V)"y") > (V)1.0d)), () => (V)(R.Get(v_339, (V)"y") < v_262)), () => (V)(R.Invoke(R.Get(R.Get(v_259, (V)"rows"), R.Get(v_339, (V)"y")), "sub", R.Join(new V[] {R.Get(v_339, (V)"x")}, new Results(R.Get(v_339, (V)"x")))).First == (V)".")), () => (V)(!R.Truth(R.Get(v_289, v_341)))), () => (V)(!R.Truth(R.Get(v_328, v_341)))), () => (V)(!R.Truth(R.Get(v_336, v_341))))}, new Results(R.Concat((V)"invalid optional structure: ", v_254))));
var values_342 = R.Join(new V[] {}, new Results((V)true));
var obj_343 = v_336;
V key_344 = v_341;
R.Set(obj_343, key_344, values_342[0]);
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
var values_345 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"N", (V)"E"), new Entry((V)"E", (V)"S"), new Entry((V)"S", (V)"W"), new Entry((V)"W", (V)"N"))));
V v_346 = values_345[0];
R.Set(v_8, (V)"transform", new V((RuleFunction)(args_347 => {
V v_348 = args_347[0];
V v_349 = args_347[1];
V v_350 = args_347[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(v_348, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_349))).First == (V)"number")), () => (V)((V)(v_349 % (V)1.0d) == (V)0.0d)), () => (V)(v_349 >= (V)0.0d)), () => (V)(v_349 <= (V)3.0d))}, new Results((V)"invalid room transformation")));
var values_351 = R.Join(new V[] {}, new Results((V)false));
V v_352 = values_351[0];
foreach (var row_353 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_348, (V)"rotations"))))))) {
V v_354 = row_353[0];
V v_355 = row_353[1];
if (R.Truth((V)(v_355 == v_349))) {
var values_356 = R.Join(new V[] {}, new Results((V)true));
v_352 = values_356[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(v_352, () => R.Or((V)(!R.Truth(v_350)), () => R.Get(v_348, (V)"mirror")))}, new Results((V)"room transformation not permitted")));
var values_357 = R.Join(new V[] {(V)R.Length(R.Get(R.Get(v_348, (V)"rows"), (V)1.0d))}, new Results((V)R.Length(R.Get(v_348, (V)"rows"))));
V v_358 = values_357[0];
V v_359 = values_357[1];
V v_360 = V.Nil;
v_360 = new V((RuleFunction)(args_361 => {
V v_362 = args_361[0];
V v_363 = args_361[1];
V v_364 = args_361[2];
if (R.Truth(v_350)) {
var values_365 = R.Join(new V[] {}, new Results((V)((V)(v_358 + (V)1.0d) - v_362)));
v_362 = values_365[0];
if (R.Truth((V)(v_364 == (V)"E"))) {
var values_366 = R.Join(new V[] {}, new Results((V)"W"));
v_364 = values_366[0];
} else if (R.Truth((V)(v_364 == (V)"W"))) {
var values_367 = R.Join(new V[] {}, new Results((V)"E"));
v_364 = values_367[0];
}
}
var values_368 = R.Join(new V[] {v_358}, new Results(v_359));
V v_369 = values_368[0];
V v_370 = values_368[1];
{ double end_373 = R.Num(v_349), step_374 = R.Num((V)1.0d);
for (double i_372 = R.Num((V)1.0d); step_374 > 0 ? i_372 <= end_373 : i_372 >= end_373; i_372 += step_374) {
V v_371 = i_372;
var values_375 = R.Join(new V[] {(V)((V)(v_370 + (V)1.0d) - v_363)}, new Results(v_362));
v_362 = values_375[0];
v_363 = values_375[1];
var values_376 = R.Join(new V[] {v_370}, new Results(v_369));
v_369 = values_376[0];
v_370 = values_376[1];
if (R.Truth(v_364)) {
var values_377 = R.Join(new V[] {}, new Results(R.Get(v_346, v_364)));
v_364 = values_377[0];
}
}
}
return R.Join(new V[] {v_362, v_363}, new Results(v_364));
return Results.Empty;
}));
var values_378 = R.Join(new V[] {v_358}, new Results(v_359));
V v_379 = values_378[0];
V v_380 = values_378[1];
if (R.Truth((V)((V)(v_349 % (V)2.0d) == (V)1.0d))) {
var values_381 = R.Join(new V[] {v_359}, new Results(v_358));
v_379 = values_381[0];
v_380 = values_381[1];
}
var values_382 = R.Join(new V[] {}, new Results(R.Table()));
V v_383 = values_382[0];
{ double end_386 = R.Num(v_380), step_387 = R.Num((V)1.0d);
for (double i_385 = R.Num((V)1.0d); step_387 > 0 ? i_385 <= end_386 : i_385 >= end_386; i_385 += step_387) {
V v_384 = i_385;
var values_388 = R.Join(new V[] {}, new Results(R.Table()));
var obj_389 = v_383;
V key_390 = v_384;
R.Set(obj_389, key_390, values_388[0]);
}
}
{ double end_393 = R.Num(v_359), step_394 = R.Num((V)1.0d);
for (double i_392 = R.Num((V)1.0d); step_394 > 0 ? i_392 <= end_393 : i_392 >= end_393; i_392 += step_394) {
V v_391 = i_392;
{ double end_397 = R.Num(v_358), step_398 = R.Num((V)1.0d);
for (double i_396 = R.Num((V)1.0d); step_398 > 0 ? i_396 <= end_397 : i_396 >= end_397; i_396 += step_398) {
V v_395 = i_396;
var values_399 = R.Join(new V[] {}, R.Call(v_360, R.Join(new V[] {v_395}, new Results(v_391))));
V v_400 = values_399[0];
V v_401 = values_399[1];
var values_402 = R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_348, (V)"rows"), v_391), "sub", R.Join(new V[] {v_395}, new Results(v_395))));
var obj_403 = R.Get(v_383, v_401);
V key_404 = v_400;
R.Set(obj_403, key_404, values_402[0]);
}
}
}
}
var values_405 = R.Join(new V[] {}, new Results(R.Table()));
V v_406 = values_405[0];
{ double end_409 = R.Num(v_380), step_410 = R.Num((V)1.0d);
for (double i_408 = R.Num((V)1.0d); step_410 > 0 ? i_408 <= end_409 : i_408 >= end_409; i_408 += step_410) {
V v_407 = i_408;
var values_411 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(v_383, v_407)))));
var obj_412 = v_406;
V key_413 = v_407;
R.Set(obj_412, key_413, values_411[0]);
}
}
var values_414 = R.Join(new V[] {}, new Results(R.Table()));
V v_415 = values_414[0];
foreach (var row_416 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_348, (V)"sockets"))))))) {
V v_417 = row_416[0];
V v_418 = row_416[1];
var values_419 = R.Join(new V[] {}, R.Call(v_360, R.Join(new V[] {R.Get(v_418, (V)"x"), R.Get(v_418, (V)"y")}, new Results(R.Get(v_418, (V)"facing")))));
V v_420 = values_419[0];
V v_421 = values_419[1];
V v_422 = values_419[2];
var values_423 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_420), new Entry((V)"y", v_421), new Entry((V)"facing", v_422), new Entry((V)"width", R.Get(v_418, (V)"width")))));
var obj_424 = v_415;
V key_425 = (V)((V)R.Length(v_415) + (V)1.0d);
R.Set(obj_424, key_425, values_423[0]);
}
var values_426 = R.Join(new V[] {}, new Results(R.Table()));
V v_427 = values_426[0];
foreach (var row_428 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_348, (V)"markers"))))))) {
V v_429 = row_428[0];
V v_430 = row_428[1];
var values_431 = R.Join(new V[] {}, new Results(R.Table()));
var obj_432 = v_427;
V key_433 = v_429;
R.Set(obj_432, key_433, values_431[0]);
foreach (var row_434 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_430)))))) {
V v_435 = row_434[0];
V v_436 = row_434[1];
var values_437 = R.Join(new V[] {}, R.Call(v_360, R.Join(new V[] {R.Get(v_436, (V)"x")}, new Results(R.Get(v_436, (V)"y")))));
V v_438 = values_437[0];
V v_439 = values_437[1];
var values_440 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_438), new Entry((V)"y", v_439))));
var obj_441 = R.Get(v_427, v_429);
V key_442 = (V)((V)R.Length(R.Get(v_427, v_429)) + (V)1.0d);
R.Set(obj_441, key_442, values_440[0]);
}
}
var values_443 = R.Join(new V[] {}, new Results(R.Table()));
V v_444 = values_443[0];
foreach (var row_445 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_348, (V)"editable"), () => R.Table()))))))) {
V v_446 = row_445[0];
V v_447 = row_445[1];
var values_448 = R.Join(new V[] {}, R.Call(v_360, R.Join(new V[] {R.Get(v_447, (V)"x")}, new Results(R.Get(v_447, (V)"y")))));
V v_449 = values_448[0];
V v_450 = values_448[1];
var values_451 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_449), new Entry((V)"y", v_450))));
var obj_452 = v_444;
V key_453 = (V)((V)R.Length(v_444) + (V)1.0d);
R.Set(obj_452, key_453, values_451[0]);
}
var values_454 = R.Join(new V[] {}, new Results(R.Table()));
V v_455 = values_454[0];
foreach (var row_456 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_348, (V)"structures"), () => R.Table()))))))) {
V v_457 = row_456[0];
V v_458 = row_456[1];
var values_459 = R.Join(new V[] {}, R.Call(v_360, R.Join(new V[] {R.Get(v_458, (V)"x")}, new Results(R.Get(v_458, (V)"y")))));
V v_460 = values_459[0];
V v_461 = values_459[1];
var values_462 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_460), new Entry((V)"y", v_461), new Entry((V)"kind", R.Get(v_458, (V)"kind")))));
var obj_463 = v_455;
V key_464 = (V)((V)R.Length(v_455) + (V)1.0d);
R.Set(obj_463, key_464, values_462[0]);
}
var values_465 = R.Join(new V[] {}, new Results(R.Table()));
V v_466 = values_465[0];
foreach (var row_467 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_348, (V)"studio_protected"), () => R.Table()))))))) {
V v_468 = row_467[0];
V v_469 = row_467[1];
var values_470 = R.Join(new V[] {}, R.Call(v_360, R.Join(new V[] {R.Get(v_469, (V)"x")}, new Results(R.Get(v_469, (V)"y")))));
V v_471 = values_470[0];
V v_472 = values_470[1];
var values_473 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_471), new Entry((V)"y", v_472))));
var obj_474 = v_466;
V key_475 = (V)((V)R.Length(v_466) + (V)1.0d);
R.Set(obj_474, key_475, values_473[0]);
}
var values_476 = R.Join(new V[] {}, new Results(R.Table()));
V v_477 = values_476[0];
foreach (var row_478 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_348, (V)"studio_visual_cells"), () => R.Table()))))))) {
V v_479 = row_478[0];
V v_480 = row_478[1];
var values_481 = R.Join(new V[] {}, R.Invoke(v_479, "match", R.Join(new V[] {}, new Results((V)"^(%d+),(%d+)$"))));
V v_482 = values_481[0];
V v_483 = values_481[1];
var values_484 = R.Join(new V[] {}, R.Call(v_360, R.Join(new V[] {R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_482))).First}, R.Call(rt.Global("tonumber"), R.Join(new V[] {}, new Results(v_483))))));
v_482 = values_484[0];
v_483 = values_484[1];
var values_485 = R.Join(new V[] {}, new Results(v_480));
var obj_486 = v_477;
V key_487 = R.Concat(v_482, R.Concat((V)",", v_483));
R.Set(obj_486, key_487, values_485[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_348, (V)"id")), new Entry((V)"roles", R.Get(v_348, (V)"roles")), new Entry((V)"width", v_379), new Entry((V)"height", v_380), new Entry((V)"rows", v_406), new Entry((V)"sockets", v_415), new Entry((V)"markers", v_427), new Entry((V)"editable", v_444), new Entry((V)"structures", v_455), new Entry((V)"turns", v_349), new Entry((V)"mirror", v_350), new Entry((V)"protected_default", (V)true), new Entry((V)"studio_visual_cells", v_477), new Entry((V)"studio_protected", v_466))));
return Results.Empty;
})));
R.Set(v_8, (V)"for_role", new V((RuleFunction)(args_488 => {
V v_489 = args_488[0];
V v_490 = args_488[1];
var values_491 = R.Join(new V[] {}, new Results(R.Table()));
V v_492 = values_491[0];
var values_493 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(R.Or(v_490, () => R.Get(R.Get(v_8, (V)"biome"), (V)"id"))))));
V v_494 = values_493[0];
R.Call(rt.Global("assert"), R.Join(new V[] {v_494}, new Results((V)"unknown room biome")));
foreach (var row_495 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_8, (V)"order"))))))) {
V v_496 = row_495[0];
V v_497 = row_495[1];
var values_498 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_494, (V)"room_prefix"), R.Invoke(v_497, "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First)));
V v_499 = values_498[0];
var values_500 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_8, (V)"definitions"), v_499)));
V v_501 = values_500[0];
if (R.Truth(R.Call(v_226, R.Join(new V[] {v_501}, new Results(v_489))).First)) {
var values_502 = R.Join(new V[] {}, new Results(v_501));
var obj_503 = v_492;
V key_504 = (V)((V)R.Length(v_492) + (V)1.0d);
R.Set(obj_503, key_504, values_502[0]);
}
}
return R.Join(new V[] {}, new Results(v_492));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_8));
return Results.Empty;
} }
