// Static C# port of src/achievements.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 63c8d4ff16d143a35417c9d6c872a5b0d8234b95bf4dda45925e8b0312a03071
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_achievements { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.achievement_content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table()));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order")));
V v_8 = values_7[0];
R.Set(v_6, (V)"pool", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
var values_11 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"normal_order"), Results.Empty));
V v_12 = values_11[0];
foreach (var row_13 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_8)))))) {
V v_14 = row_13[0];
V v_15 = row_13[1];
var values_16 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_15))).First, (V)"reward")));
V v_17 = values_16[0];
if (R.Truth(R.And(R.And(v_17, () => v_10), () => R.Get(v_10, v_15)))) {
var values_18 = R.Join(new V[] {}, new Results(v_17));
var obj_19 = v_12;
V key_20 = (V)((V)R.Length(v_12) + (V)1.0d);
R.Set(obj_19, key_20, values_18[0]);
}
}
return R.Join(new V[] {}, new Results(v_12));
return Results.Empty;
})));
R.Set(v_6, (V)"validate_pool", new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_22))).First == (V)"table"), () => (V)((V)R.Length(v_22) >= R.Get(v_4, (V)"starter_count"))), () => (V)((V)R.Length(v_22) <= (V)R.Length(R.Get(v_4, (V)"order"))))}, new Results((V)"invalid available boon pool")));
var values_23 = R.Join(new V[] {}, new Results(R.Table()));
V v_24 = values_23[0];
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_22)))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_27))).First == (V)"string"), () => R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_27))).First), () => (V)(!R.Truth(R.Get(v_24, v_27))))}, new Results((V)"invalid available boon ID")));
if (R.Truth((V)(v_26 <= R.Get(v_4, (V)"starter_count")))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_27 == R.Get(R.Get(v_4, (V)"order"), v_26))}, new Results((V)"starter boon pool changed")));
}
var values_28 = R.Join(new V[] {}, new Results((V)true));
var obj_29 = v_24;
V key_30 = v_27;
R.Set(obj_29, key_30, values_28[0]);
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_6, (V)"new_run", new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
V v_33 = args_31[1];
if (R.Truth(v_32)) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_33))).First == (V)"string"), () => (V)((V)R.Length(v_33) > (V)0.0d)), () => (V)((V)R.Length(v_33) <= (V)100.0d))}, new Results((V)"eligible run needs unique ID")));
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"eligible", (V)(v_32 == (V)true)), new Entry((V)"run_id", R.Or(v_33, () => (V)"fixture")), new Entry((V)"earned", R.Table()), new Entry((V)"pending", R.Table()), new Entry((V)"chains", R.Table()))));
return Results.Empty;
})));
R.Set(v_6, (V)"validate", new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_35))).First == (V)"table"), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"eligible")))).First == (V)"boolean")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"run_id")))).First == (V)"string")), () => (V)((V)R.Length(R.Get(v_35, (V)"run_id")) > (V)0.0d)), () => (V)((V)R.Length(R.Get(v_35, (V)"run_id")) <= (V)100.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"earned")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"pending")))).First == (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"chains")))).First == (V)"table"))}, new Results((V)"invalid achievement state")));
foreach (var row_36 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"earned"))))))) {
V v_37 = row_36[0];
V v_38 = row_36[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_37))).First, () => (V)(v_38 == (V)true))}, new Results((V)"invalid earned achievement")));
}
foreach (var row_39 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"pending"))))))) {
V v_40 = row_39[0];
V v_41 = row_39[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_40))).First, () => (V)(v_41 == (V)true)), () => R.Get(R.Get(v_35, (V)"earned"), v_40))}, new Results((V)"invalid pending achievement")));
}
var values_42 = R.Join(new V[] {}, new Results((V)0.0d));
V v_43 = values_42[0];
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"chains"))))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
var values_47 = R.Join(new V[] {}, new Results((V)(v_43 + (V)1.0d)));
v_43 = values_47[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_45))).First == (V)"number"), () => (V)((V)(v_45 % (V)1.0d) == (V)0.0d)), () => (V)(v_45 >= (V)1.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_46))).First == (V)"table"))}, new Results((V)"invalid chain observation")));
foreach (var row_48 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_46)))))) {
V v_49 = row_48[0];
V v_50 = row_48[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Call(R.Get(v_4, (V)"get"), R.Join(new V[] {}, new Results(v_49))).First, () => (V)(v_50 == (V)true))}, new Results((V)"invalid chain boon observation")));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(v_43 <= (V)10000.0d)}, new Results((V)"too many achievement chains")));
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
V v_51 = V.Nil;
v_51 = new V((RuleFunction)(args_52 => {
V v_53 = args_52[0];
V v_54 = args_52[1];
V v_55 = args_52[2];
var values_56 = R.Join(new V[] {}, new Results(R.Get(v_53, (V)"meta")));
V v_57 = values_56[0];
if (R.Truth(R.Get(R.Get(v_57, (V)"earned"), v_54))) {
return Results.Empty;
}
var values_58 = R.Join(new V[] {}, new Results((V)true));
var obj_59 = R.Get(v_57, (V)"earned");
V key_60 = v_54;
R.Set(obj_59, key_60, values_58[0]);
var values_61 = R.Join(new V[] {}, new Results((V)true));
var obj_62 = R.Get(v_57, (V)"pending");
V key_63 = v_54;
R.Set(obj_62, key_63, values_61[0]);
R.Call(v_55, R.Join(new V[] {(V)"AchievementEarned", R.Table(new Entry((V)"achievement_id", v_54), new Entry((V)"reward_boon_id", R.Get(R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_54))).First, (V)"reward")), new Entry((V)"run_id", R.Get(v_57, (V)"run_id")))}, new Results(R.Get(v_53, (V)"player_id"))));
return Results.Empty;
}));
R.Set(v_6, (V)"observe", new V((RuleFunction)(args_64 => {
V v_65 = args_64[0];
V v_66 = args_64[1];
V v_67 = args_64[2];
var values_68 = R.Join(new V[] {}, new Results(R.Get(v_65, (V)"meta")));
V v_69 = values_68[0];
if (R.Truth(R.Or((V)(!R.Truth(v_69)), () => (V)(!R.Truth(R.Get(v_69, (V)"eligible")))))) {
return Results.Empty;
}
if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"EliteDefeated"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)1.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"SecretDiscovered"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)2.0d)}, new Results(v_67)));
} else if (R.Truth(R.And(R.And((V)(R.Get(v_66, (V)"kind") == (V)"BoonActivated"), () => R.Get(v_66, (V)"boon_id")), () => (V)(R.Get(v_66, (V)"source_id") == R.Get(v_65, (V)"player_id"))))) {
var values_70 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_69, (V)"chains"), R.Get(v_66, (V)"chain_id")), () => R.Table())));
V v_71 = values_70[0];
var values_72 = R.Join(new V[] {}, new Results(v_71));
var obj_73 = R.Get(v_69, (V)"chains");
V key_74 = R.Get(v_66, (V)"chain_id");
R.Set(obj_73, key_74, values_72[0]);
var values_75 = R.Join(new V[] {}, new Results((V)true));
var obj_76 = v_71;
V key_77 = R.Get(v_66, (V)"boon_id");
R.Set(obj_76, key_77, values_75[0]);
var values_78 = R.Join(new V[] {}, new Results((V)0.0d));
V v_79 = values_78[0];
foreach (var row_80 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_71)))))) {
V v_81 = row_80[0];
var values_82 = R.Join(new V[] {}, new Results((V)(v_79 + (V)1.0d)));
v_79 = values_82[0];
}
if (R.Truth((V)(v_79 >= (V)3.0d))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)3.0d)}, new Results(v_67)));
}
} else if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == (V)"Victory"), () => R.Get(v_65, (V)"victory")))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)4.0d)}, new Results(v_67)));
}
if (R.Truth(R.And(R.And((V)(R.Get(v_66, (V)"kind") == (V)"EntityKilled"), () => (V)(R.Get(v_66, (V)"source_id") == R.Get(v_65, (V)"player_id"))), () => (V)(R.Get(v_66, (V)"target_id") != R.Get(v_65, (V)"player_id"))))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)5.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"FloorChanged"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)6.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"ChestClaimed"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)7.0d)}, new Results(v_67)));
} else if (R.Truth(R.And(R.And((V)(R.Get(v_66, (V)"kind") == (V)"CurrencyCollected"), () => (V)(R.Get(v_66, (V)"source_id") == R.Get(v_65, (V)"player_id"))), () => (V)(R.Or(R.Get(v_66, (V)"new_currency"), () => (V)0.0d) >= (V)50.0d)))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)8.0d)}, new Results(v_67)));
} else if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == (V)"LevelUp"), () => (V)(R.Get(v_66, (V)"target_id") == R.Get(v_65, (V)"player_id"))))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)9.0d)}, new Results(v_67)));
if (R.Truth((V)(R.Or(R.Get(v_66, (V)"new_level"), () => (V)0.0d) >= (V)5.0d))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)10.0d)}, new Results(v_67)));
}
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"BoonGranted"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)11.0d)}, new Results(v_67)));
} else if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == (V)"TerrainBroken"), () => (V)(R.Get(v_66, (V)"source_id") == R.Get(v_65, (V)"player_id"))))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)12.0d)}, new Results(v_67)));
} else if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == (V)"OreBroken"), () => (V)(R.Get(v_66, (V)"source_id") == R.Get(v_65, (V)"player_id"))))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)13.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"TimedGateOpened"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)14.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"VaultKeyCollected"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)15.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"WardFlaskCollected"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)16.0d)}, new Results(v_67)));
} else if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == (V)"BrazierLit"), () => (V)(R.Get(v_66, (V)"source_id") == R.Get(v_65, (V)"player_id"))))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)17.0d)}, new Results(v_67)));
} else if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == (V)"SupplyCollected"), () => (V)(R.Get(v_66, (V)"source_id") == R.Get(v_65, (V)"player_id"))))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)18.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"PursuerArrived"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)19.0d)}, new Results(v_67)));
} else if (R.Truth(R.And((V)(R.Get(v_66, (V)"kind") == (V)"StageEntered"), () => (V)(R.Or(R.Get(v_66, (V)"stage_index"), () => (V)0.0d) >= (V)2.0d)))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)20.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"FinalSanctumEntered"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)21.0d)}, new Results(v_67)));
} else if (R.Truth((V)(R.Get(v_66, (V)"kind") == (V)"RegentPhaseChanged"))) {
R.Call(v_51, R.Join(new V[] {v_65, R.Get(v_8, (V)22.0d)}, new Results(v_67)));
}
return Results.Empty;
})));
R.Set(v_6, (V)"prune", new V((RuleFunction)(args_83 => {
V v_84 = args_83[0];
var values_85 = R.Join(new V[] {}, new Results(R.Get(v_84, (V)"meta")));
V v_86 = values_85[0];
if (R.Truth((V)(!R.Truth(v_86)))) {
return Results.Empty;
}
if (R.Truth(R.Or(R.Get(R.Get(v_86, (V)"earned"), R.Get(v_8, (V)3.0d)), () => (V)(!R.Truth(R.Get(v_86, (V)"eligible")))))) {
var values_87 = R.Join(new V[] {}, new Results(R.Table()));
var obj_88 = v_86;
V key_89 = (V)"chains";
R.Set(obj_88, key_89, values_87[0]);

return Results.Empty;
}
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_86, (V)"chains"))))))) {
V v_91 = row_90[0];
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_84, (V)"chain_history"), v_91))))) {
var values_92 = R.Join(new V[] {}, new Results(V.Nil));
var obj_93 = R.Get(v_86, (V)"chains");
V key_94 = v_91;
R.Set(obj_93, key_94, values_92[0]);
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_6));
return Results.Empty;
} }
