#+build windows, darwin
package sandbox

import glfw "vendor:glfw"

// GLFW implements native pointer hiding on these platforms.
Cursor_Visibility :: struct {}
cursor_visibility_init :: proc(window: glfw.WindowHandle) -> Cursor_Visibility {return {}}
cursor_visibility_set_hidden :: proc(cursor: ^Cursor_Visibility, hidden: bool) {}
cursor_visibility_destroy :: proc(cursor: ^Cursor_Visibility) {}
