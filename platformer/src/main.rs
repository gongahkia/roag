use std::error::Error;
use std::thread;
use std::time::{Duration, Instant};

use gilrs::{Axis, Button, Gilrs};
use pancurses::{
    Attribute, BUTTON1_CLICKED, COLOR_BLACK, COLOR_BLUE, COLOR_CYAN, COLOR_GREEN, COLOR_MAGENTA,
    COLOR_RED, COLOR_WHITE, COLOR_YELLOW, ColorPair, Input, Window, cbreak, curs_set, endwin,
    getmouse, init_pair, initscr, mouseinterval, mousemask, noecho, start_color,
};
use station_echo::{
    EnemyKind, FIXED_DT, FrameInput, Game, Phase, PickupKind, Tile, WORLD_HEIGHT, WORLD_WIDTH,
};

const HUD_ROWS: i32 = 2;
const CONTROL_ROWS: i32 = 2;
const MIN_COLS: i32 = 64;
const MIN_ROWS: i32 = 20;

const PAIR_HULL: u8 = 1;
const PAIR_PLAYER: u8 = 2;
const PAIR_DANGER: u8 = 3;
const PAIR_RELAY: u8 = 4;
const PAIR_UI: u8 = 5;
const PAIR_GATE: u8 = 6;
const PAIR_SHOT: u8 = 7;

#[derive(Default)]
struct ButtonEdges {
    jump: bool,
    shoot: bool,
    dash: bool,
    pause: bool,
    restart: bool,
}

#[derive(Clone, Copy)]
struct Screen {
    rows: i32,
    cols: i32,
}

struct TerminalGuard;

impl Drop for TerminalGuard {
    fn drop(&mut self) {
        endwin();
    }
}

fn main() -> Result<(), Box<dyn Error>> {
    let window = setup_terminal();
    let _terminal_guard = TerminalGuard;
    let mut gilrs = Gilrs::new().ok();
    let mut pad_edges = ButtonEdges::default();
    let mut game = Game::new();
    let mut key_motion = 0.0_f32;
    let mut pending_input = FrameInput::default();
    let mut previous = Instant::now();
    let mut accumulator = 0.0_f32;

    loop {
        let frame_start = Instant::now();
        let elapsed = frame_start.duration_since(previous).as_secs_f32().min(0.1);
        previous = frame_start;
        accumulator += elapsed;

        let (rows, cols) = window.get_max_yx();
        let (mut input, quit) = read_terminal_input(&window, rows, cols, &game, &mut key_motion);
        if quit {
            break;
        }
        if let Some(gamepad) = gilrs.as_mut() {
            merge_gamepad_input(gamepad, &mut pad_edges, &mut input);
        }
        let gamepad_available = gilrs
            .as_ref()
            .is_some_and(|source| source.gamepads().any(|(_, pad)| pad.is_connected()));
        pending_input.move_x = input.move_x;
        pending_input.jump |= input.jump;
        pending_input.shoot |= input.shoot;
        pending_input.dash |= input.dash;
        pending_input.toggle_pause |= input.toggle_pause;
        pending_input.restart |= input.restart;

        if rows >= MIN_ROWS && cols >= MIN_COLS {
            while accumulator >= FIXED_DT {
                game.tick(pending_input, FIXED_DT);
                pending_input.jump = false;
                pending_input.shoot = false;
                pending_input.dash = false;
                pending_input.toggle_pause = false;
                pending_input.restart = false;
                accumulator -= FIXED_DT;
            }
        } else {
            accumulator = 0.0;
        }

        key_motion = approach_zero(key_motion, elapsed * 3.0);
        render(&window, &game, rows, cols, gamepad_available);

        let spent = frame_start.elapsed();
        let target = Duration::from_millis(16);
        if spent < target {
            thread::sleep(target - spent);
        }
    }

    Ok(())
}

fn setup_terminal() -> Window {
    let window = initscr();
    cbreak();
    noecho();
    curs_set(0);
    window.keypad(true);
    window.nodelay(true);
    mouseinterval(180);
    mousemask(BUTTON1_CLICKED, None);
    start_color();
    init_pair(PAIR_HULL.into(), COLOR_BLUE, COLOR_BLACK);
    init_pair(PAIR_PLAYER.into(), COLOR_CYAN, COLOR_BLACK);
    init_pair(PAIR_DANGER.into(), COLOR_RED, COLOR_BLACK);
    init_pair(PAIR_RELAY.into(), COLOR_YELLOW, COLOR_BLACK);
    init_pair(PAIR_UI.into(), COLOR_WHITE, COLOR_BLACK);
    init_pair(PAIR_GATE.into(), COLOR_MAGENTA, COLOR_BLACK);
    init_pair(PAIR_SHOT.into(), COLOR_GREEN, COLOR_BLACK);
    window
}

fn read_terminal_input(
    window: &Window,
    rows: i32,
    cols: i32,
    game: &Game,
    key_motion: &mut f32,
) -> (FrameInput, bool) {
    let mut input = FrameInput::default();
    let mut quit = false;
    while let Some(event) = window.getch() {
        match event {
            Input::Character('q' | 'Q') => quit = true,
            Input::Character('a' | 'A') | Input::KeyLeft => *key_motion = -1.0,
            Input::Character('d' | 'D') | Input::KeyRight => *key_motion = 1.0,
            Input::Character(' ' | 'w' | 'W') | Input::KeyUp => input.jump = true,
            Input::Character('x' | 'X') => input.shoot = true,
            Input::Character('z' | 'Z') => input.dash = true,
            Input::Character('p' | 'P' | '\u{1b}') => input.toggle_pause = true,
            Input::Character('r' | 'R') => input.restart = true,
            Input::KeyMouse => merge_mouse_input(rows, cols, game, &mut input),
            _ => {}
        }
    }
    if input.move_x.abs() > 0.1 {
        *key_motion = input.move_x;
    }
    input.move_x = *key_motion;
    (input, quit)
}

fn merge_mouse_input(rows: i32, cols: i32, game: &Game, input: &mut FrameInput) {
    let Ok(event) = getmouse() else {
        return;
    };
    if event.bstate & BUTTON1_CLICKED == 0 {
        return;
    }

    if event.y >= rows - CONTROL_ROWS {
        let sixth = (cols / 6).max(1);
        match event.x / sixth {
            0 => input.move_x = -1.0,
            1 => input.move_x = 1.0,
            2 => input.jump = true,
            3 => input.shoot = true,
            4 => input.dash = true,
            _ if game.phase == Phase::Won => input.restart = true,
            _ => input.toggle_pause = true,
        }
        return;
    }

    let camera_x = (game.player.x as i32 - cols / 3).clamp(0, (WORLD_WIDTH - cols).max(0));
    let screen_player_x = game.player.x.round() as i32 - camera_x;
    if event.y < rows / 2 {
        input.jump = true;
    } else if event.x < screen_player_x - 2 {
        input.move_x = -1.0;
    } else if event.x > screen_player_x + 2 {
        input.move_x = 1.0;
    } else {
        input.shoot = true;
    }
}

fn merge_gamepad_input(gilrs: &mut Gilrs, edges: &mut ButtonEdges, input: &mut FrameInput) {
    while gilrs.next_event().is_some() {}
    let Some((_, pad)) = gilrs.gamepads().find(|(_, pad)| pad.is_connected()) else {
        *edges = ButtonEdges::default();
        return;
    };

    let axis = pad.value(Axis::LeftStickX);
    let pad_x = if pad.is_pressed(Button::DPadLeft) {
        -1.0
    } else if pad.is_pressed(Button::DPadRight) {
        1.0
    } else if axis.abs() >= 0.25 {
        axis
    } else {
        0.0
    };
    if pad_x.abs() > input.move_x.abs() {
        input.move_x = pad_x;
    }

    let jump = pad.is_pressed(Button::South);
    let shoot = pad.is_pressed(Button::West) || pad.is_pressed(Button::RightTrigger);
    let dash = pad.is_pressed(Button::East) || pad.is_pressed(Button::LeftTrigger);
    let pause = pad.is_pressed(Button::Start);
    let restart = pad.is_pressed(Button::Select);
    input.jump |= jump && !edges.jump;
    input.shoot |= shoot && !edges.shoot;
    input.dash |= dash && !edges.dash;
    input.toggle_pause |= pause && !edges.pause;
    input.restart |= restart && !edges.restart;
    edges.jump = jump;
    edges.shoot = shoot;
    edges.dash = dash;
    edges.pause = pause;
    edges.restart = restart;
}

fn render(window: &Window, game: &Game, rows: i32, cols: i32, gamepad_available: bool) {
    window.erase();
    if rows < MIN_ROWS || cols < MIN_COLS {
        draw_centered(
            window,
            rows / 2 - 1,
            cols,
            "TERMINAL TOO SMALL",
            PAIR_DANGER,
        );
        draw_centered(
            window,
            rows / 2,
            cols,
            &format!("Need {MIN_COLS}x{MIN_ROWS}; current {cols}x{rows}"),
            PAIR_UI,
        );
        draw_centered(
            window,
            rows / 2 + 1,
            cols,
            "Resize or press Q to quit",
            PAIR_UI,
        );
        window.refresh();
        return;
    }

    render_hud(window, game, cols, gamepad_available);
    render_world(window, game, rows, cols);
    render_controls(window, game, rows, cols);

    match game.phase {
        Phase::Paused => render_overlay(
            window,
            rows,
            cols,
            "PAUSED",
            "P / START / click PAUSE to resume",
        ),
        Phase::Won => render_overlay(
            window,
            rows,
            cols,
            "EVACUATION COMPLETE",
            "R restarts // Q exits // click RESTART",
        ),
        Phase::Running => {}
    }
    window.refresh();
}

fn render_hud(window: &Window, game: &Game, cols: i32, gamepad_available: bool) {
    let hearts: String = (0..game.player.max_hp)
        .map(|index| if index < game.player.hp { '#' } else { '.' })
        .collect();
    let controller = if gamepad_available {
        "PAD:ON"
    } else {
        "PAD:--"
    };
    let status = format!(
        " STATION ECHO  HP[{hearts}]  RELAYS:{}/3  SCORE:{:05}  {:>6}s  {controller} ",
        game.relays,
        game.score,
        game.elapsed_seconds()
    );
    draw_clipped(window, 0, 0, cols, &status, PAIR_UI, true);
    let line = if game.message_visible() {
        format!(" {}", game.message)
    } else {
        format!(" {}", game.objective())
    };
    draw_clipped(window, 1, 0, cols, &line, PAIR_RELAY, false);
}

fn render_world(window: &Window, game: &Game, rows: i32, cols: i32) {
    let screen = Screen { rows, cols };
    let view_height = rows - HUD_ROWS - CONTROL_ROWS;
    let camera_x = (game.player.x as i32 - cols / 3).clamp(0, (WORLD_WIDTH - cols).max(0));
    let camera_y =
        (game.player.y as i32 - view_height / 2).clamp(0, (WORLD_HEIGHT - view_height).max(0));

    for sy in 0..view_height {
        let wy = camera_y + sy;
        for sx in 0..cols {
            let wx = camera_x + sx;
            let tile = game.tile(wx, wy);
            let glyph = tile.glyph(game.gate_open());
            if glyph != ' ' {
                let pair = match tile {
                    Tile::Hull => PAIR_HULL,
                    Tile::Hazard => PAIR_DANGER,
                    Tile::Gate => PAIR_GATE,
                    Tile::Exit => PAIR_RELAY,
                    Tile::Empty => PAIR_UI,
                };
                draw_char(window, sy + HUD_ROWS, sx, glyph, pair, tile == Tile::Exit);
            } else if (wx * 37 + wy * 17).rem_euclid(173) == 0 {
                draw_char(window, sy + HUD_ROWS, sx, '.', PAIR_UI, false);
            }
        }
    }

    for pickup in game.pickups.iter().filter(|pickup| !pickup.collected) {
        let (sx, sy) = world_to_screen(pickup.x, pickup.y, camera_x, camera_y);
        let pair = match pickup.kind {
            PickupKind::Repair => PAIR_SHOT,
            PickupKind::Relay | PickupKind::DashModule | PickupKind::JumpModule => PAIR_RELAY,
        };
        draw_if_visible(window, screen, sx, sy, pickup.kind.glyph(), pair, true);
    }
    for enemy in game.enemies.iter().filter(|enemy| enemy.hp > 0) {
        let (sx, sy) = world_to_screen(enemy.x, enemy.y, camera_x, camera_y);
        draw_if_visible(
            window,
            screen,
            sx,
            sy,
            enemy.kind.glyph(),
            PAIR_DANGER,
            enemy.kind == EnemyKind::Sentinel,
        );
        if enemy.kind == EnemyKind::Sentinel && sy > HUD_ROWS && sy < rows - CONTROL_ROWS {
            let hp = format!(
                "SENTINEL [{}{}]",
                "=".repeat(enemy.hp.max(0) as usize),
                " ".repeat((10 - enemy.hp.max(0)) as usize)
            );
            draw_clipped(
                window,
                sy - 1,
                (sx - 9).max(0),
                cols - (sx - 9).max(0),
                &hp,
                PAIR_DANGER,
                true,
            );
        }
    }
    for projectile in &game.projectiles {
        let (sx, sy) = world_to_screen(projectile.x, projectile.y, camera_x, camera_y);
        draw_if_visible(
            window,
            screen,
            sx,
            sy,
            if projectile.hostile { 'o' } else { '-' },
            if projectile.hostile {
                PAIR_DANGER
            } else {
                PAIR_SHOT
            },
            false,
        );
    }

    let (player_x, player_y) = world_to_screen(game.player.x, game.player.y, camera_x, camera_y);
    let blinking = game.elapsed_seconds().is_multiple_of(2) && game.player.hp < game.player.max_hp;
    draw_if_visible(
        window,
        screen,
        player_x,
        player_y,
        '@',
        PAIR_PLAYER,
        blinking,
    );
}

fn render_controls(window: &Window, game: &Game, rows: i32, cols: i32) {
    let pad = cols / 6;
    let labels = [
        "[< A]",
        "[D >]",
        "[JUMP]",
        "[FIRE]",
        "[DASH]",
        if game.phase == Phase::Won {
            "[RESTART]"
        } else {
            "[PAUSE]"
        },
    ];
    for (index, label) in labels.iter().enumerate() {
        let x = index as i32 * pad;
        draw_clipped(
            window,
            rows - 2,
            x,
            pad,
            label,
            if index < 2 { PAIR_PLAYER } else { PAIR_UI },
            true,
        );
    }
    let help = " arrows/A,D move | space/W jump | X fire | Z dash | P pause | R restart | Q quit ";
    draw_clipped(window, rows - 1, 0, cols, help, PAIR_UI, false);
}

fn render_overlay(window: &Window, rows: i32, cols: i32, title: &str, detail: &str) {
    let width = (detail.len() as i32 + 6)
        .max(title.len() as i32 + 6)
        .min(cols - 4);
    let left = (cols - width) / 2;
    let top = rows / 2 - 2;
    let border = format!("+{}+", "-".repeat((width - 2) as usize));
    let blank = format!("|{}|", " ".repeat((width - 2) as usize));
    draw_clipped(window, top, left, width, &border, PAIR_GATE, true);
    draw_clipped(window, top + 1, left, width, &blank, PAIR_GATE, false);
    draw_clipped(window, top + 2, left, width, &blank, PAIR_GATE, false);
    draw_clipped(window, top + 3, left, width, &blank, PAIR_GATE, false);
    draw_clipped(window, top + 4, left, width, &border, PAIR_GATE, true);
    draw_centered_in(window, top + 1, left, width, title, PAIR_RELAY, true);
    draw_centered_in(window, top + 3, left, width, detail, PAIR_UI, false);
}

fn world_to_screen(x: f32, y: f32, camera_x: i32, camera_y: i32) -> (i32, i32) {
    (
        x.round() as i32 - camera_x,
        y.floor() as i32 - camera_y + HUD_ROWS,
    )
}

fn draw_if_visible(
    window: &Window,
    screen: Screen,
    x: i32,
    y: i32,
    glyph: char,
    pair: u8,
    bold: bool,
) {
    if x >= 0 && x < screen.cols && y >= HUD_ROWS && y < screen.rows - CONTROL_ROWS {
        draw_char(window, y, x, glyph, pair, bold);
    }
}

fn draw_char(window: &Window, y: i32, x: i32, glyph: char, pair: u8, bold: bool) {
    window.attron(ColorPair(pair));
    if bold {
        window.attron(Attribute::Bold);
    }
    window.mvaddch(y, x, glyph);
    if bold {
        window.attroff(Attribute::Bold);
    }
    window.attroff(ColorPair(pair));
}

fn draw_clipped(window: &Window, y: i32, x: i32, width: i32, text: &str, pair: u8, bold: bool) {
    if width <= 0 {
        return;
    }
    let clipped: String = text.chars().take(width as usize).collect();
    window.attron(ColorPair(pair));
    if bold {
        window.attron(Attribute::Bold);
    }
    window.mvaddstr(y, x, clipped);
    if bold {
        window.attroff(Attribute::Bold);
    }
    window.attroff(ColorPair(pair));
}

fn draw_centered(window: &Window, y: i32, cols: i32, text: &str, pair: u8) {
    draw_centered_in(window, y, 0, cols, text, pair, false);
}

fn draw_centered_in(
    window: &Window,
    y: i32,
    left: i32,
    width: i32,
    text: &str,
    pair: u8,
    bold: bool,
) {
    let x = left + ((width - text.len() as i32) / 2).max(0);
    draw_clipped(window, y, x, width - (x - left), text, pair, bold);
}

fn approach_zero(value: f32, amount: f32) -> f32 {
    if value > 0.0 {
        (value - amount).max(0.0)
    } else {
        (value + amount).min(0.0)
    }
}
