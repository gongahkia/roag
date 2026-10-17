package main

import (
	"encoding/binary"
	"math"

	"github.com/hajimehoshi/ebiten/v2/audio"
	"rend3rer/internal/gameplay"
)

// Short synthesized sounds need no external assets. Players are pooled, so a
// fast weapon does not accumulate audio objects or open streams every shot.
type soundBank struct {
	clips map[gameplay.EventKind][]*audio.Player
	next  map[gameplay.EventKind]int
}

func newSoundBank() *soundBank {
	ctx := audio.CurrentContext()
	if ctx == nil {
		ctx = audio.NewContext(44100)
	}
	b := &soundBank{clips: map[gameplay.EventKind][]*audio.Player{}, next: map[gameplay.EventKind]int{}}
	for _, s := range []struct {
		kind                     gameplay.EventKind
		from, to, seconds, noise float64
	}{
		{gameplay.Shot, 220, 50, .13, .65}, {gameplay.Hit, 800, 300, .065, .3},
		{gameplay.Hurt, 110, 45, .24, .45}, {gameplay.Warning, 440, 660, .14, .05},
		{gameplay.BoltFired, 700, 210, .14, .1}, {gameplay.RoomClear, 440, 880, .5, 0},
		{gameplay.UpgradeTaken, 550, 1100, .3, 0}, {gameplay.Healed, 600, 950, .2, 0},
		{gameplay.Victory, 440, 1320, .8, 0}, {gameplay.Defeat, 220, 35, .8, .15},
		{gameplay.Dashed, 180, 70, .16, .6},
		{gameplay.Jumped, 260, 430, .1, .08}, {gameplay.Landed, 90, 45, .1, .25},
	} {
		data := tone(s.from, s.to, s.seconds, s.noise)
		for i := 0; i < 3; i++ {
			p := ctx.NewPlayerFromBytes(data)
			p.SetVolume(.32)
			b.clips[s.kind] = append(b.clips[s.kind], p)
		}
	}
	return b
}
func tone(from, to, seconds, noise float64) []byte {
	n := int(44100 * seconds)
	data := make([]byte, n*4)
	phase := 0.0
	random := uint32(0x74ab119)
	for i := 0; i < n; i++ {
		t := float64(i) / float64(n)
		freq := from + (to-from)*t
		phase += 2 * math.Pi * freq / 44100
		random ^= random << 13
		random ^= random >> 17
		random ^= random << 5
		ns := float64(random&65535)/32767.5 - 1
		envelope := math.Min(1, t*40) * math.Pow(1-t, 2)
		sample := int16((math.Sin(phase)*(1-noise) + ns*noise) * envelope * 18000)
		binary.LittleEndian.PutUint16(data[i*4:], uint16(sample))
		binary.LittleEndian.PutUint16(data[i*4+2:], uint16(sample))
	}
	return data
}
func (b *soundBank) play(events []gameplay.Event) {
	seen := map[gameplay.EventKind]bool{}
	for _, e := range events {
		ps := b.clips[e.Kind]
		if len(ps) == 0 || seen[e.Kind] {
			continue
		}
		seen[e.Kind] = true
		i := b.next[e.Kind] % len(ps)
		b.next[e.Kind]++
		p := ps[i]
		p.Pause()
		if p.Rewind() == nil {
			p.Play()
		}
	}
}
func (b *soundBank) mute(muted bool) {
	volume := .32
	if muted {
		volume = 0
	}
	for _, ps := range b.clips {
		for _, p := range ps {
			p.SetVolume(volume)
			if muted {
				p.Pause()
			}
		}
	}
}
