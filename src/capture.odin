package sandbox

import "core:fmt"
import "core:os"
import gl "vendor:OpenGL"

// Read and write one scanline at a time so captures respect small CPU budgets.
screenshot :: proc(width, height: i32, path: string) -> bool {
	file, err := os.create(path)
	if err != nil {return false}
	defer os.close(file)
	header := fmt.tprintf("P6\n%d %d\n255\n", width, height)
	_, header_err := os.write(file, transmute([]u8)header)
	if header_err != nil {return false}
	row := make([]u8, int(width) * 3)
	defer delete(row)
	gl.PixelStorei(gl.PACK_ALIGNMENT, 1)
	gl.ReadBuffer(gl.BACK)
	for y := height - 1; y >= 0; y -= 1 {
		gl.ReadPixels(0, y, width, 1, gl.RGB, gl.UNSIGNED_BYTE, raw_data(row))
		_, write_err := os.write(file, row)
		if write_err != nil {return false}
	}
	return gl.GetError() == gl.NO_ERROR
}
