package terminal

/*
#cgo pkg-config: ncursesw
#define _XOPEN_SOURCE_EXTENDED 1
#include <locale.h>
#include <ncurses.h>
#include <stdlib.h>
#include <wchar.h>

#define NS_PIXEL_COLORS 7

static int ns_pixel_pairs = 0;

static short ns_palette_color(int i) {
	if (COLORS >= 256) {
		static const short palette[NS_PIXEL_COLORS] = { 0, 17, 236, 244, 252, 160, 172 };
		return palette[i];
	}
	static const short palette[NS_PIXEL_COLORS] = {
		COLOR_BLACK, COLOR_BLUE, COLOR_BLACK, COLOR_WHITE, COLOR_WHITE, COLOR_RED, COLOR_YELLOW
	};
	return palette[i];
}

static int ns_init(void) {
	setlocale(LC_ALL, "");
	if (initscr() == NULL) return -1;
	raw(); noecho(); keypad(stdscr, TRUE); nodelay(stdscr, TRUE);
	curs_set(0); set_escdelay(25);
	if (has_colors()) {
		start_color(); use_default_colors();
		init_pair(1, COLOR_WHITE, -1);
		init_pair(2, COLOR_RED, -1);
		init_pair(3, COLOR_YELLOW, -1);
		init_pair(4, COLOR_CYAN, -1);
		init_pair(5, COLOR_GREEN, -1);
		init_pair(6, COLOR_BLUE, -1);
		if (COLOR_PAIRS >= 7 + NS_PIXEL_COLORS * NS_PIXEL_COLORS) {
			for (int fg = 0; fg < NS_PIXEL_COLORS; fg++) {
				for (int bg = 0; bg < NS_PIXEL_COLORS; bg++) {
					init_pair(7 + fg * NS_PIXEL_COLORS + bg,
						ns_palette_color(fg), ns_palette_color(bg));
				}
			}
			ns_pixel_pairs = 1;
		}
	}
	mousemask(ALL_MOUSE_EVENTS | REPORT_MOUSE_POSITION, NULL);
	mouseinterval(0);
	return 0;
}
static int ns_getch(void) { return getch(); }
static void ns_size(int *h, int *w) { getmaxyx(stdscr, *h, *w); }
static int ns_mouse(int *x, int *y, unsigned long *b) { MEVENT e; if (getmouse(&e) != OK) return 0; *x=e.x;*y=e.y;*b=e.bstate;return 1; }
static void ns_put(int y, int x, unsigned int c, int pair, int bold) {
	wchar_t text[2] = { (wchar_t)c, 0 };
	attrset(COLOR_PAIR(pair) | (bold ? A_BOLD : 0));
	mvaddnwstr(y, x, text, 1);
}
static void ns_pixel(int y, int x, int top, int bottom) {
	if (top < 0 || top >= NS_PIXEL_COLORS) top = 0;
	if (bottom < 0 || bottom >= NS_PIXEL_COLORS) bottom = 0;
	if (ns_pixel_pairs) {
		ns_put(y, x, 0x2580, 7 + top * NS_PIXEL_COLORS + bottom, 0);
	} else {
		// A monochrome/diminished-color fallback still keeps the doubled geometry readable.
		static const wchar_t shades[NS_PIXEL_COLORS] = { L' ', L'.', L'\u2591', L'\u2592', L'\u2588', L'\u2593', L'\u2593' };
		wchar_t text[2] = { shades[top > bottom ? top : bottom], 0 };
		attrset(A_NORMAL);
		mvaddnwstr(y, x, text, 1);
	}
}
static int ns_key_mouse(void) { return KEY_MOUSE; }
static int ns_key_resize(void) { return KEY_RESIZE; }
static int ns_key_up(void) { return KEY_UP; }
static int ns_key_down(void) { return KEY_DOWN; }
static int ns_key_left(void) { return KEY_LEFT; }
static int ns_key_right(void) { return KEY_RIGHT; }
static unsigned long ns_b1(void) { return BUTTON1_PRESSED; }
static unsigned long ns_b2(void) { return BUTTON2_PRESSED; }
static unsigned long ns_b3(void) { return BUTTON3_PRESSED; }
static unsigned long ns_b4(void) { return BUTTON4_PRESSED; }
static unsigned long ns_b5(void) { return BUTTON5_PRESSED; }
*/
import "C"

import (
	"errors"
	"fmt"
	"os"
)

type Kind int

const (
	Key Kind = iota
	Mouse
	Resize
)

type Event struct {
	Kind    Kind
	Key     int
	X, Y    int
	Buttons uint64
}

type Screen struct{ closed bool }

func Open() (*Screen, error) {
	if os.Getenv("TERM") == "" || os.Getenv("TERM") == "dumb" {
		return nil, errors.New("a real terminal is required (TERM is unset or dumb)")
	}
	if C.ns_init() != 0 {
		return nil, errors.New("ncurses initialization failed")
	}
	// Ask xterm-compatible terminals for all mouse motion; ncurses parses the replies.
	fmt.Print("\x1b[?1003h")
	return &Screen{}, nil
}
func (s *Screen) Close() {
	if s == nil || s.closed {
		return
	}
	fmt.Print("\x1b[?1003l")
	C.endwin()
	s.closed = true
}
func (s *Screen) Size() (int, int) { var h, w C.int; C.ns_size(&h, &w); return int(w), int(h) }
func (s *Screen) Clear()           { C.erase() }
func (s *Screen) Refresh()         { C.refresh() }
func (s *Screen) Put(x, y int, ch rune, color int, bold bool) {
	b := C.int(0)
	if bold {
		b = 1
	}
	C.ns_put(C.int(y), C.int(x), C.uint(ch), C.int(color), b)
}

// Pixel draws two vertically stacked color samples in one terminal cell.
// Colors are palette indexes from PixelBlack through PixelAmber.
func (s *Screen) Pixel(x, y, top, bottom int) {
	C.ns_pixel(C.int(y), C.int(x), C.int(top), C.int(bottom))
}
func (s *Screen) Text(x, y int, text string, color int, bold bool) {
	for i, r := range text {
		s.Put(x+i, y, r, color, bold)
	}
}

func (s *Screen) Poll() []Event {
	var out []Event
	for {
		k := C.ns_getch()
		if k == C.ERR {
			break
		}
		switch k {
		case C.ns_key_mouse():
			var x, y C.int
			var b C.ulong
			if C.ns_mouse(&x, &y, &b) != 0 {
				out = append(out, Event{Kind: Mouse, X: int(x), Y: int(y), Buttons: uint64(b)})
			}
		case C.ns_key_resize():
			out = append(out, Event{Kind: Resize})
		default:
			out = append(out, Event{Kind: Key, Key: int(k)})
		}
	}
	return out
}
func KeyUp() int      { return int(C.ns_key_up()) }
func KeyDown() int    { return int(C.ns_key_down()) }
func KeyLeft() int    { return int(C.ns_key_left()) }
func KeyRight() int   { return int(C.ns_key_right()) }
func Button1() uint64 { return uint64(C.ns_b1()) }
func Button2() uint64 { return uint64(C.ns_b2()) }
func Button3() uint64 { return uint64(C.ns_b3()) }
func Button4() uint64 { return uint64(C.ns_b4()) }
func Button5() uint64 { return uint64(C.ns_b5()) }
