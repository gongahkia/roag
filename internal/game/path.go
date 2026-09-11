package game

import "container/heap"

type pathNode struct {
	p    Point
	g, f float64
	i    int
}
type nodeHeap []*pathNode

func (h nodeHeap) Len() int           { return len(h) }
func (h nodeHeap) Less(i, j int) bool { return h[i].f < h[j].f }
func (h nodeHeap) Swap(i, j int)      { h[i], h[j] = h[j], h[i]; h[i].i = i; h[j].i = j }
func (h *nodeHeap) Push(x any)        { n := x.(*pathNode); n.i = len(*h); *h = append(*h, n) }
func (h *nodeHeap) Pop() any          { old := *h; n := old[len(old)-1]; *h = old[:len(old)-1]; return n }
func abs(v int) int {
	if v < 0 {
		return -v
	}
	return v
}

func (w *World) Path(start, goal Point, monster bool) []Point {
	if !w.Inside(goal) {
		return nil
	}
	open := &nodeHeap{}
	heap.Init(open)
	s := &pathNode{p: start}
	heap.Push(open, s)
	cost := map[Point]float64{start: 0}
	came := map[Point]Point{}
	dirs := []Point{{1, 0}, {-1, 0}, {0, 1}, {0, -1}}
	for open.Len() > 0 {
		cur := heap.Pop(open).(*pathNode)
		if cur.p == goal {
			var out []Point
			for p := goal; p != start; p = came[p] {
				out = append(out, p)
			}
			for i, j := 0, len(out)-1; i < j; i, j = i+1, j-1 {
				out[i], out[j] = out[j], out[i]
			}
			return out
		}
		if cur.g > cost[cur.p] {
			continue
		}
		for _, d := range dirs {
			n := Point{cur.p.X + d.X, cur.p.Y + d.Y}
			if !w.Inside(n) || w.Cells[n.Y][n.X] == '#' {
				continue
			}
			step := 1.0
			if open, door := w.Doors[n]; door && !open {
				if !monster {
					continue
				}
				step = 2.5
			}
			ng := cur.g + step
			if old, ok := cost[n]; ok && old <= ng {
				continue
			}
			cost[n] = ng
			came[n] = cur.p
			heap.Push(open, &pathNode{p: n, g: ng, f: ng + float64(abs(n.X-goal.X)+abs(n.Y-goal.Y))})
		}
	}
	return nil
}
