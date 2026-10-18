#+build linux
package sandbox

import "core:c"
import "core:dynlib"
import glfw "vendor:glfw"

// XFixes hides the native sprite explicitly, in addition to GLFW's blank
// cursor image. This also covers X11/WSLg compositors that leak that image.
// Load it optionally so GLFW remains sufficient on systems without XFixes.
Cursor_Visibility :: struct {
	library:    dynlib.Library,
	display:    rawptr,
	window:     c.ulong,
	hide, show: proc "c" (display: rawptr, window: c.ulong),
	flush:      proc "c" (display: rawptr) -> c.int,
	hidden:     bool,
}

cursor_visibility_init :: proc(window: glfw.WindowHandle) -> Cursor_Visibility {
	display := glfw.GetX11Display()
	if display == nil {return {}}
	library, loaded := dynlib.load_library("libXfixes.so.3")
	if !loaded {return {}}
	result := Cursor_Visibility {
		library = library,
		display = display,
		window  = c.ulong(glfw.GetX11Window(window)),
	}
	query := cast(proc "c" (_: rawptr, _: ^c.int, _: ^c.int) -> c.int)dynlib.symbol_address(
		library,
		"XFixesQueryVersion",
	)
	result.hide = cast(type_of(result.hide))dynlib.symbol_address(library, "XFixesHideCursor")
	result.show = cast(type_of(result.show))dynlib.symbol_address(library, "XFixesShowCursor")
	result.flush = cast(type_of(result.flush))dynlib.symbol_address(library, "XFlush")
	major, minor: c.int = 4, 0
	if query == nil ||
	   result.hide == nil ||
	   result.show == nil ||
	   result.flush == nil ||
	   query(display, &major, &minor) == 0 ||
	   major < 4 {
		dynlib.unload_library(library)
		return {}
	}
	return result
}

cursor_visibility_set_hidden :: proc(cursor: ^Cursor_Visibility, hidden: bool) {
	if cursor.library == nil || cursor.hidden == hidden {return}
	if hidden {cursor.hide(cursor.display, cursor.window)} else {cursor.show(cursor.display, cursor.window)}
	cursor.hidden = hidden
	cursor.flush(cursor.display)
}

cursor_visibility_destroy :: proc(cursor: ^Cursor_Visibility) {
	if cursor.library == nil {return}
	cursor_visibility_set_hidden(cursor, false)
	dynlib.unload_library(cursor.library)
	cursor^ = {}
}
