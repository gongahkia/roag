//go:build !linux

package controller

import "errors"

type State struct {
	LX, LY, RX, RY                                  float64
	South, East, North, West, TL, TR, Select, Start bool
}
type Device struct{}

func Open(string) (*Device, error) { return nil, errors.New("controller input requires Linux evdev") }
func (*Device) Close()             {}
func (*Device) Name() string       { return "none" }
func (*Device) Poll() State        { return State{} }
