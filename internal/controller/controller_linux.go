//go:build linux

package controller

import (
	"encoding/binary"
	"errors"
	"fmt"
	"math"
	"os"
	"path/filepath"
	"strings"
	"syscall"
	"unsafe"
)

const (
	evKey   = 1
	evAbs   = 3
	absX    = 0
	absY    = 1
	absRX   = 3
	absRY   = 4
	absHatX = 16
	absHatY = 17
)
const (
	btnSouth  = 304
	btnEast   = 305
	btnNorth  = 307
	btnWest   = 308
	btnTL     = 310
	btnTR     = 311
	btnSelect = 314
	btnStart  = 315
)

type State struct {
	LX, LY, RX, RY                                  float64
	South, East, North, West, TL, TR, Select, Start bool
}
type Device struct {
	fd         int
	path, name string
	state      State
	axes       map[uint16]axisRange
}

type axisRange struct{ min, max, flat int32 }
type absInfo struct{ value, minimum, maximum, fuzz, flat, resolution int32 }

func Open(path string) (*Device, error) {
	if path != "" {
		return openOne(path)
	}
	paths, _ := filepath.Glob("/dev/input/event*")
	for _, p := range paths {
		base := filepath.Base(p)
		raw, err := os.ReadFile(filepath.Join("/sys/class/input", base, "device/name"))
		if err != nil {
			continue
		}
		name := strings.ToLower(string(raw))
		if strings.Contains(name, "gamepad") || strings.Contains(name, "controller") || strings.Contains(name, "xbox") || strings.Contains(name, "joystick") {
			if d, e := openOne(p); e == nil {
				return d, nil
			}
		}
	}
	return nil, errors.New("no readable controller found")
}
func openOne(path string) (*Device, error) {
	fd, err := syscall.Open(path, syscall.O_RDONLY|syscall.O_NONBLOCK, 0)
	if err != nil {
		return nil, fmt.Errorf("open controller %s: %w", path, err)
	}
	raw, _ := os.ReadFile(filepath.Join("/sys/class/input", filepath.Base(path), "device/name"))
	d := &Device{fd: fd, path: path, name: strings.TrimSpace(string(raw)), axes: map[uint16]axisRange{}}
	for _, code := range []uint16{absX, absY, absRX, absRY} {
		var info absInfo
		// EVIOCGABS(code), from linux/input.h.
		request := uintptr((2 << 30) | (uint32(unsafe.Sizeof(info)) << 16) | ('E' << 8) | uint32(0x40+code))
		if _, _, errno := syscall.Syscall(syscall.SYS_IOCTL, uintptr(fd), request, uintptr(unsafe.Pointer(&info))); errno == 0 && info.maximum > info.minimum {
			d.axes[code] = axisRange{min: info.minimum, max: info.maximum, flat: info.flat}
		}
	}
	return d, nil
}
func (d *Device) Close() {
	if d != nil && d.fd >= 0 {
		_ = syscall.Close(d.fd)
		d.fd = -1
	}
}
func (d *Device) Name() string {
	if d == nil {
		return "none"
	}
	if d.name != "" {
		return d.name
	}
	return d.path
}
func (d *Device) axis(code uint16, v int32) float64 {
	x := float64(v) / 32767
	deadzone := .16
	if r, ok := d.axes[code]; ok {
		center := float64(r.min+r.max) / 2
		half := float64(r.max-r.min) / 2
		x = (float64(v) - center) / half
		if r.flat > 0 {
			deadzone = math.Max(deadzone, float64(r.flat)/half)
		}
	}
	if x > 1 {
		x = 1
	}
	if x < -1 {
		x = -1
	}
	if x < deadzone && x > -deadzone {
		return 0
	}
	return x
}
func (d *Device) Poll() State {
	if d == nil {
		return State{}
	}
	buf := make([]byte, 24*32)
	for {
		n, err := syscall.Read(d.fd, buf)
		if err != nil || n < 24 {
			break
		}
		for off := 0; off+24 <= n; off += 24 {
			typ := binary.NativeEndian.Uint16(buf[off+16:])
			code := binary.NativeEndian.Uint16(buf[off+18:])
			value := int32(binary.NativeEndian.Uint32(buf[off+20:]))
			if typ == evAbs {
				switch code {
				case absX:
					d.state.LX = d.axis(code, value)
				case absY:
					d.state.LY = d.axis(code, value)
				case absRX:
					d.state.RX = d.axis(code, value)
				case absRY:
					d.state.RY = d.axis(code, value)
				case absHatX:
					d.state.LX = float64(value)
				case absHatY:
					d.state.LY = float64(value)
				}
			} else if typ == evKey {
				down := value != 0
				switch code {
				case btnSouth:
					d.state.South = down
				case btnEast:
					d.state.East = down
				case btnNorth:
					d.state.North = down
				case btnWest:
					d.state.West = down
				case btnTL:
					d.state.TL = down
				case btnTR:
					d.state.TR = down
				case btnSelect:
					d.state.Select = down
				case btnStart:
					d.state.Start = down
				}
			}
		}
	}
	return d.state
}
