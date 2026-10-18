package sandbox

import "core:fmt"
import "core:os"
import "core:testing"

@(test)
test_save_roundtrip_and_rejection :: proc(t: ^testing.T) {
	dir, err := os.make_directory_temp("", "voxel-test-*", context.allocator)
	testing.expect(t, err == nil)
	if err != nil {
		return
	}
	defer delete(dir)
	defer os.remove(dir)
	path := fmt.tprintf("%s/world.vox", dir)
	defer os.remove(path)
	w: World
	world_init(&w, 99)
	defer world_destroy(&w)
	world_apply(&w, {1, {-33, 150, 8}, .Leaves})
	world_apply(&w, {2, {16, 77, -1}, .Air})
	testing.expect(t, world_save(&w, {2.5, 95, -33}, path))
	testing.expect(t, world_save(&w, {2.5, 95, -33}, path))
	loaded: World
	world_init(&loaded, 0)
	defer world_destroy(&loaded)
	p: V3
	testing.expect(t, world_load(&loaded, &p, path))
	testing.expect(
		t,
		loaded.seed == 99 &&
		loaded.sequence == 2 &&
		loaded.edit_count == 2 &&
		p == V3{2.5, 95, -33},
	)
	testing.expect(t, world_get(&loaded, {-33, 150, 8}) == .Leaves)
	testing.expect(t, world_get(&loaded, {16, 77, -1}) == .Air)
	testing.expect(t, os.write_entire_file(path, "VOX1-broken") == nil)
	testing.expect(t, !world_load(&loaded, &p, path))
	testing.expect(t, loaded.seed == 99 && loaded.edit_count == 2)
}

@(test)
test_invalid_save_payload_is_transactional :: proc(t: ^testing.T) {
	dir, err := os.make_directory_temp("", "voxel-save-test-*", context.allocator)
	testing.expect(t, err == nil)
	if err != nil {
		return
	}
	defer delete(dir)
	defer os.remove(dir)
	path := fmt.tprintf("%s/world.vox", dir)
	defer os.remove(path)
	w: World
	world_init(&w, 42)
	defer world_destroy(&w)
	world_apply(&w, {1, {1, 90, 1}, .Stone})
	world_apply(&w, {2, {2, 91, 2}, .Wood})
	testing.expect(t, world_save(&w, {8, 100, 8}, path))
	original, read_err := os.read_entire_file(path, context.allocator)
	testing.expect(t, read_err == nil)
	defer delete(original)
	for scenario in 0 ..< 5 {
		data := make([]u8, len(original))
		copy(data, original)
		switch scenario {
		case 0:
			data[64] = 255 // Invalid block identifier.
		case 1:
			for i in 56 ..< 60 {
				data[i] = 0
			}
		// Protected bottom layer.
		case 2:
			copy(data[65:77], data[52:64]) // Duplicate edited position.
		case 3:
			data[26] = 0xf8
			data[27] = 0x7f // Non-finite player X.
		case 4:
			data[44] = 255 // Count does not match payload size.
		}
		testing.expect(t, os.write_entire_file(path, data) == nil)
		delete(data)
		p := V3{7, 8, 9}
		testing.expect(t, !world_load(&w, &p, path))
		testing.expect(t, w.seed == 42 && w.edit_count == 2 && p == V3{7, 8, 9})
	}
}

@(test)
test_save_generator_version_legacy_and_fluid_levels :: proc(t: ^testing.T) {
	dir, err := os.make_directory_temp("", "voxel-fluid-test-*", context.allocator)
	testing.expect(t, err == nil); if err != nil {return}
	defer delete(dir); defer os.remove(dir)
	path := fmt.tprintf("%s/world.vox", dir); defer os.remove(path)
	w: World; world_init(&w, 42); defer world_destroy(&w)
	world_apply(&w, {1, {0, 180, 0}, .Water_5})
	world_apply(&w, {2, {0, 181, 0}, .Diamond_Ore})
	testing.expect(t, world_save(&w, {0, 190, 0}, path))
	loaded: World; world_init(&loaded, 7); defer world_destroy(&loaded)
	pos: V3
	testing.expect(t, world_load(&loaded, &pos, path))
	testing.expect(t, loaded.generator == GENERATOR_VERSION && world_get(&loaded, {0, 180, 0}) == .Water_5)
	// A minimal genuine VOX1 file must keep its old terrain algorithm on re-save.
	legacy := make([dynamic]u8); defer delete(legacy)
	append(&legacy, ..([]u8{'V', 'O', 'X', '1'})); put64(&legacy, 42); put64(&legacy, 0)
	legacy_pos := V3{0, 190, 0}
	for v in legacy_pos {put64(&legacy, transmute(u64)v)}
	put32(&legacy, 0)
	testing.expect(t, os.write_entire_file(path, legacy[:]) == nil)
	testing.expect(t, world_load(&loaded, &pos, path) && loaded.generator == 1)
	testing.expect(t, world_save(&loaded, pos, path))
	testing.expect(t, world_load(&loaded, &pos, path) && loaded.generator == 1)
}
