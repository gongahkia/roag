import { describe, expect, it } from 'vitest';
import { presentFeedback } from '../src/feedback';

describe('feedback presentation', () => {
  it('reserves course callouts for major match moments', () => {
    expect(presentFeedback('enemy-1 sinks it in 3')).toEqual({ tone: 'good', major: true });
    expect(presentFeedback('golfer-1 finds the void')).toEqual({ tone: 'danger', major: true });
    expect(presentFeedback('hole 2: course shuffler reveals a new course')).toEqual({ tone: 'info', major: false });
  });
});
