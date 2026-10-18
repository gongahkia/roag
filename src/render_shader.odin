package sandbox

import "core:fmt"
import "core:os"
import gl "vendor:OpenGL"

shader :: proc(kind: u32, source: string) -> u32 {
	id := gl.CreateShader(kind)
	ptr := cstring(raw_data(source))
	length := i32(len(source))
	gl.ShaderSource(id, 1, &ptr, &length)
	gl.CompileShader(id)
	ok: i32
	gl.GetShaderiv(id, gl.COMPILE_STATUS, &ok)
	if ok == 0 {
		buf: [4096]u8
		length: i32
		gl.GetShaderInfoLog(id, 4096, &length, &buf[0])
		fmt.eprintln("Shader:", string(buf[:length]))
		os.exit(1)
	}
	return id
}

program :: proc(vs, fs: string) -> u32 {
	v, f := shader(gl.VERTEX_SHADER, vs), shader(gl.FRAGMENT_SHADER, fs)
	p := gl.CreateProgram()
	gl.AttachShader(p, v)
	gl.AttachShader(p, f)
	gl.LinkProgram(p)
	gl.DeleteShader(v)
	gl.DeleteShader(f)
	ok: i32
	gl.GetProgramiv(p, gl.LINK_STATUS, &ok)
	if ok == 0 {
		fmt.eprintln("Shader link failed")
		os.exit(1)
	}
	return p
}
