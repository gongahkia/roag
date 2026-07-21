import { directionFor } from './engine/input'

export const shouldPreventKeyboardDefault = (command: string): boolean => command === 'settings' || Boolean(directionFor(command)) || [' ', 'Escape', '`', '[', ']', 'Tab', 'h', 'H', 'j', 'J', 'u', 'U', 'd', 'D', 't', 'T', 'e', 'E', 'a', 'A', 'b', 'B', 'r', 'R', 'g', 'G', 'c', 'C', 'q', 'Q', 'x', 'X', 's', 'S'].includes(command)
