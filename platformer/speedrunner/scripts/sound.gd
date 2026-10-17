extends RefCounted

## Original, synthesized cues; no VVVVVV audio is imported.
static func tone(start_hz: float, end_hz: float, seconds: float) -> AudioStreamWAV:
	var rate := 22050
	var count := int(rate * seconds)
	var data := PackedByteArray()
	data.resize(count * 2)
	var phase := 0.0
	for i in count:
		var t := float(i) / count
		phase += TAU * lerpf(start_hz, end_hz, t) / rate
		var envelope := sin(minf(t * 20.0, 1.0) * PI * 0.5) * pow(1.0 - t, 2.0)
		var sample := int(sin(phase) * envelope * 11000)
		data[i * 2] = sample & 0xff
		data[i * 2 + 1] = (sample >> 8) & 0xff
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = rate
	stream.data = data
	return stream
