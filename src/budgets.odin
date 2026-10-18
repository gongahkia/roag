package sandbox

// Admission policy is shared by CLI validation, live resizing, worker scheduling,
// and diagnostics. These are managed reservations, not physical driver queries.
WORKER_MEMORY_BYTES :: 16 * MiB
CPU_BACKGROUND_BYTES :: 8 * MiB
WORLD_EDIT_HEADROOM_BYTES :: 4 * MiB
RENDERER_MEMORY_BYTES :: 2 * MiB
FRAMEBUFFER_BYTES_PER_PIXEL :: 16

cpu_reservation :: proc(workers: int) -> int {
	return workers * WORKER_MEMORY_BYTES + CPU_BACKGROUND_BYTES
}

graphics_reserved_bytes :: proc(mesh_bytes, width, height: int) -> int {
	return mesh_bytes + RENDERER_MEMORY_BYTES + width * height * FRAMEBUFFER_BYTES_PER_PIXEL
}
