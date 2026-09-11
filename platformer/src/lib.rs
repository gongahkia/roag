//! Deterministic game simulation for Station Echo.
//!
//! The simulation is independent of ncurses so gameplay can be tested without a
//! terminal. Coordinates use terminal cells, with positive Y pointing down.

pub const WORLD_WIDTH: i32 = 180;
pub const WORLD_HEIGHT: i32 = 42;
pub const FIXED_DT: f32 = 1.0 / 30.0;

const PLAYER_HALF_WIDTH: f32 = 0.34;
const PLAYER_HALF_HEIGHT: f32 = 0.46;
const MOVE_SPEED: f32 = 8.0;
const JUMP_SPEED: f32 = 15.0;
const GRAVITY: f32 = 26.0;
const MAX_FALL_SPEED: f32 = 18.0;

#[derive(Clone, Copy, Debug, Default)]
pub struct FrameInput {
    pub move_x: f32,
    pub jump: bool,
    pub shoot: bool,
    pub dash: bool,
    pub toggle_pause: bool,
    pub restart: bool,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Phase {
    Running,
    Paused,
    Won,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Tile {
    Empty,
    Hull,
    Hazard,
    Gate,
    Exit,
}

impl Tile {
    pub fn glyph(self, gate_open: bool) -> char {
        match self {
            Self::Empty => ' ',
            Self::Hull => '#',
            Self::Hazard => '^',
            Self::Gate if gate_open => ':',
            Self::Gate => '|',
            Self::Exit => 'E',
        }
    }
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum PickupKind {
    Relay,
    DashModule,
    JumpModule,
    Repair,
}

impl PickupKind {
    pub fn glyph(self) -> char {
        match self {
            Self::Relay => '*',
            Self::DashModule => '>',
            Self::JumpModule => '+',
            Self::Repair => 'h',
        }
    }
}

#[derive(Clone, Debug)]
pub struct Pickup {
    pub x: f32,
    pub y: f32,
    pub kind: PickupKind,
    pub collected: bool,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum EnemyKind {
    Crawler,
    Drone,
    Sentinel,
}

impl EnemyKind {
    pub fn glyph(self) -> char {
        match self {
            Self::Crawler => 'm',
            Self::Drone => 'v',
            Self::Sentinel => 'S',
        }
    }
}

#[derive(Clone, Debug)]
pub struct Enemy {
    pub x: f32,
    pub y: f32,
    pub hp: i32,
    pub kind: EnemyKind,
    min_x: f32,
    max_x: f32,
    direction: f32,
    shot_timer: f32,
}

#[derive(Clone, Debug)]
pub struct Projectile {
    pub x: f32,
    pub y: f32,
    pub hostile: bool,
    vx: f32,
    ttl: f32,
}

#[derive(Clone, Debug)]
pub struct Player {
    pub x: f32,
    pub y: f32,
    pub vx: f32,
    pub vy: f32,
    pub hp: i32,
    pub max_hp: i32,
    pub facing: f32,
    pub grounded: bool,
    pub has_dash: bool,
    pub has_double_jump: bool,
    pub air_jump_available: bool,
    dash_timer: f32,
    dash_cooldown: f32,
    shoot_cooldown: f32,
    invulnerable: f32,
}

impl Player {
    fn at(x: f32, y: f32) -> Self {
        Self {
            x,
            y,
            vx: 0.0,
            vy: 0.0,
            hp: 5,
            max_hp: 5,
            facing: 1.0,
            grounded: false,
            has_dash: false,
            has_double_jump: false,
            air_jump_available: false,
            dash_timer: 0.0,
            dash_cooldown: 0.0,
            shoot_cooldown: 0.0,
            invulnerable: 0.0,
        }
    }
}

#[derive(Clone, Debug)]
pub struct Game {
    tiles: Vec<Tile>,
    pub player: Player,
    pub pickups: Vec<Pickup>,
    pub enemies: Vec<Enemy>,
    pub projectiles: Vec<Projectile>,
    pub relays: u8,
    pub score: u32,
    pub phase: Phase,
    pub boss_defeated: bool,
    pub message: String,
    message_timer: f32,
    checkpoint: (f32, f32),
    elapsed: f32,
}

impl Default for Game {
    fn default() -> Self {
        Self::new()
    }
}

impl Game {
    pub fn new() -> Self {
        let mut game = Self {
            tiles: vec![Tile::Empty; (WORLD_WIDTH * WORLD_HEIGHT) as usize],
            player: Player::at(4.0, 34.0),
            pickups: Vec::new(),
            enemies: Vec::new(),
            projectiles: Vec::new(),
            relays: 0,
            score: 0,
            phase: Phase::Running,
            boss_defeated: false,
            message: "Restore three relays and reach the escape pod".into(),
            message_timer: 7.0,
            checkpoint: (4.0, 34.0),
            elapsed: 0.0,
        };
        game.build_station();
        game.spawn_pickups();
        game.enemies = Self::station_enemies();
        game
    }

    pub fn tile(&self, x: i32, y: i32) -> Tile {
        tile_at(&self.tiles, x, y)
    }

    pub fn gate_open(&self) -> bool {
        self.relays >= 3
    }

    pub fn elapsed_seconds(&self) -> u32 {
        self.elapsed as u32
    }

    pub fn objective(&self) -> &'static str {
        if self.boss_defeated {
            "ESCAPE POD ONLINE - move to E"
        } else if self.gate_open() {
            "CORE GATE OPEN - defeat the sentinel"
        } else {
            "RESTORE RELAYS"
        }
    }

    pub fn tick(&mut self, input: FrameInput, dt: f32) {
        if input.restart {
            *self = Self::new();
            return;
        }
        if input.toggle_pause && self.phase != Phase::Won {
            self.phase = if self.phase == Phase::Paused {
                Phase::Running
            } else {
                Phase::Paused
            };
        }
        if self.phase != Phase::Running {
            return;
        }

        let dt = dt.clamp(0.0, 0.05);
        self.elapsed += dt;
        self.message_timer = (self.message_timer - dt).max(0.0);
        self.player.dash_timer = (self.player.dash_timer - dt).max(0.0);
        self.player.dash_cooldown = (self.player.dash_cooldown - dt).max(0.0);
        self.player.shoot_cooldown = (self.player.shoot_cooldown - dt).max(0.0);
        self.player.invulnerable = (self.player.invulnerable - dt).max(0.0);

        self.update_player(input, dt);
        self.collect_pickups();
        self.update_enemies(dt);
        self.update_projectiles(dt);
        self.check_contacts();

        if self.player.y > WORLD_HEIGHT as f32 - 1.0 || self.player.hp <= 0 {
            self.respawn();
        }
        if self.boss_defeated && self.player.x >= 175.0 {
            self.phase = Phase::Won;
            self.message = "SIGNAL RESTORED // EVACUATION COMPLETE".into();
            self.message_timer = f32::INFINITY;
        }
    }

    pub fn message_visible(&self) -> bool {
        self.message_timer > 0.0
    }

    fn set_tile(&mut self, x: i32, y: i32, tile: Tile) {
        if (0..WORLD_WIDTH).contains(&x) && (0..WORLD_HEIGHT).contains(&y) {
            self.tiles[(y * WORLD_WIDTH + x) as usize] = tile;
        }
    }

    fn fill(&mut self, x0: i32, y0: i32, x1: i32, y1: i32, tile: Tile) {
        for y in y0..=y1 {
            for x in x0..=x1 {
                self.set_tile(x, y, tile);
            }
        }
    }

    fn build_station(&mut self) {
        self.fill(0, 0, WORLD_WIDTH - 1, 0, Tile::Hull);
        self.fill(0, 0, 0, WORLD_HEIGHT - 1, Tile::Hull);
        self.fill(
            WORLD_WIDTH - 1,
            0,
            WORLD_WIDTH - 1,
            WORLD_HEIGHT - 1,
            Tile::Hull,
        );

        self.fill(0, 37, 56, WORLD_HEIGHT - 1, Tile::Hull);
        self.fill(67, 37, 106, WORLD_HEIGHT - 1, Tile::Hull);
        self.fill(117, 37, WORLD_WIDTH - 1, WORLD_HEIGHT - 1, Tile::Hull);
        self.fill(57, 39, 66, 41, Tile::Hazard);
        self.fill(107, 39, 116, 41, Tile::Hazard);

        self.fill(13, 33, 23, 33, Tile::Hull);
        self.fill(29, 29, 38, 29, Tile::Hull);
        self.fill(43, 33, 52, 33, Tile::Hull);
        self.fill(70, 33, 79, 33, Tile::Hull);
        self.fill(84, 29, 94, 29, Tile::Hull);
        self.fill(96, 33, 104, 33, Tile::Hull);
        self.fill(121, 33, 141, 33, Tile::Hull);
        self.fill(126, 29, 140, 29, Tile::Hull);
        self.fill(130, 25, 138, 25, Tile::Hull);

        self.fill(151, 24, 151, 36, Tile::Gate);
        self.fill(151, 23, 166, 23, Tile::Hull);
        self.set_tile(176, 36, Tile::Exit);

        for x in [19, 72, 101, 127, 145] {
            self.set_tile(x, 36, Tile::Hazard);
        }
    }

    fn spawn_pickups(&mut self) {
        self.pickups = vec![
            Pickup {
                x: 34.0,
                y: 27.8,
                kind: PickupKind::Relay,
                collected: false,
            },
            Pickup {
                x: 49.0,
                y: 31.8,
                kind: PickupKind::DashModule,
                collected: false,
            },
            Pickup {
                x: 76.0,
                y: 31.8,
                kind: PickupKind::Repair,
                collected: false,
            },
            Pickup {
                x: 89.0,
                y: 27.8,
                kind: PickupKind::Relay,
                collected: false,
            },
            Pickup {
                x: 100.0,
                y: 31.8,
                kind: PickupKind::JumpModule,
                collected: false,
            },
            Pickup {
                x: 134.0,
                y: 23.8,
                kind: PickupKind::Relay,
                collected: false,
            },
            Pickup {
                x: 143.0,
                y: 35.8,
                kind: PickupKind::Repair,
                collected: false,
            },
        ];
    }

    fn station_enemies() -> Vec<Enemy> {
        vec![
            Self::enemy(EnemyKind::Crawler, 25.0, 35.9, 22.0, 28.0),
            Self::enemy(EnemyKind::Drone, 44.0, 29.0, 40.0, 53.0),
            Self::enemy(EnemyKind::Crawler, 72.0, 35.9, 66.0, 81.0),
            Self::enemy(EnemyKind::Drone, 92.0, 24.0, 83.0, 101.0),
            Self::enemy(EnemyKind::Crawler, 123.0, 35.9, 119.0, 138.0),
            Self::enemy(EnemyKind::Drone, 139.0, 27.0, 125.0, 148.0),
            Self::enemy(EnemyKind::Sentinel, 165.0, 36.0, 157.0, 172.0),
        ]
    }

    fn enemy(kind: EnemyKind, x: f32, y: f32, min_x: f32, max_x: f32) -> Enemy {
        Enemy {
            x,
            y,
            hp: if kind == EnemyKind::Sentinel { 10 } else { 2 },
            kind,
            min_x,
            max_x,
            direction: -1.0,
            shot_timer: if kind == EnemyKind::Sentinel {
                1.0
            } else {
                0.0
            },
        }
    }

    fn update_player(&mut self, input: FrameInput, dt: f32) {
        let move_x = input.move_x.clamp(-1.0, 1.0);
        if move_x.abs() > 0.1 {
            self.player.facing = move_x.signum();
        }

        if input.dash && self.player.has_dash && self.player.dash_cooldown <= 0.0 {
            self.player.dash_timer = 0.18;
            self.player.dash_cooldown = 0.65;
            self.player.vx = self.player.facing * 24.0;
            self.player.vy *= 0.25;
        }

        if input.jump {
            if self.player.grounded {
                self.player.vy = -JUMP_SPEED;
                self.player.grounded = false;
            } else if self.player.has_double_jump && self.player.air_jump_available {
                self.player.vy = -JUMP_SPEED;
                self.player.air_jump_available = false;
                self.show_message("THRUSTER BURST", 0.8);
            }
        }

        if input.shoot && self.player.shoot_cooldown <= 0.0 {
            self.projectiles.push(Projectile {
                x: self.player.x + self.player.facing * 0.6,
                y: self.player.y,
                vx: self.player.facing * 22.0,
                ttl: 1.8,
                hostile: false,
            });
            self.player.shoot_cooldown = 0.22;
        }

        if self.player.dash_timer <= 0.0 {
            let target_vx = move_x * MOVE_SPEED;
            let acceleration = if move_x.abs() > 0.1 { 42.0 } else { 34.0 };
            self.player.vx = approach(self.player.vx, target_vx, acceleration * dt);
            self.player.vy = (self.player.vy + GRAVITY * dt).min(MAX_FALL_SPEED);
        }

        self.move_player_x(self.player.vx * dt);
        self.player.grounded = false;
        self.move_player_y(self.player.vy * dt);
        if self.player.grounded {
            self.player.air_jump_available = self.player.has_double_jump;
        }
    }

    fn move_player_x(&mut self, delta: f32) {
        let steps = (delta.abs() / 0.2).ceil().max(1.0) as usize;
        let step = delta / steps as f32;
        for _ in 0..steps {
            let next = self.player.x + step;
            if self.solid_at(next, self.player.y) {
                self.player.vx = 0.0;
                break;
            }
            self.player.x = next;
        }
    }

    fn move_player_y(&mut self, delta: f32) {
        let steps = (delta.abs() / 0.18).ceil().max(1.0) as usize;
        let step = delta / steps as f32;
        for _ in 0..steps {
            let next = self.player.y + step;
            if self.solid_at(self.player.x, next) {
                if step > 0.0 {
                    self.player.grounded = true;
                }
                self.player.vy = 0.0;
                break;
            }
            self.player.y = next;
        }
    }

    fn solid_at(&self, x: f32, y: f32) -> bool {
        let left = (x - PLAYER_HALF_WIDTH).floor() as i32;
        let right = (x + PLAYER_HALF_WIDTH).floor() as i32;
        let top = (y - PLAYER_HALF_HEIGHT).floor() as i32;
        let bottom = (y + PLAYER_HALF_HEIGHT).floor() as i32;
        for tile_y in top..=bottom {
            for tile_x in left..=right {
                match self.tile(tile_x, tile_y) {
                    Tile::Hull => return true,
                    Tile::Gate if !self.gate_open() => return true,
                    _ => {}
                }
            }
        }
        false
    }

    fn collect_pickups(&mut self) {
        let mut collected = Vec::new();
        for (index, pickup) in self.pickups.iter_mut().enumerate() {
            if !pickup.collected
                && (pickup.x - self.player.x).abs() < 0.9
                && (pickup.y - self.player.y).abs() < 1.0
            {
                pickup.collected = true;
                collected.push((index, pickup.kind, pickup.x, pickup.y));
            }
        }

        for (_, kind, x, y) in collected {
            match kind {
                PickupKind::Relay => {
                    self.relays += 1;
                    self.score += 500;
                    self.checkpoint = (x, y - 1.0);
                    self.player.hp = self.player.max_hp;
                    if self.gate_open() {
                        self.show_message("RELAY 3/3 // CORE GATE OPEN", 4.0);
                    } else {
                        self.show_message(&format!("RELAY {}/3 RESTORED", self.relays), 3.0);
                    }
                }
                PickupKind::DashModule => {
                    self.player.has_dash = true;
                    self.score += 250;
                    self.show_message("DASH MODULE // press Z or controller B", 4.0);
                }
                PickupKind::JumpModule => {
                    self.player.has_double_jump = true;
                    self.player.air_jump_available = true;
                    self.score += 250;
                    self.show_message("THRUSTER MODULE // jump again in air", 4.0);
                }
                PickupKind::Repair => {
                    self.player.hp = self.player.max_hp;
                    self.score += 100;
                    self.show_message("HULL INTEGRITY RESTORED", 2.0);
                }
            }
        }
    }

    fn update_enemies(&mut self, dt: f32) {
        let player_x = self.player.x;
        let player_y = self.player.y;
        let mut hostile_shots = Vec::new();

        for enemy in &mut self.enemies {
            if enemy.hp <= 0 {
                continue;
            }
            let speed = match enemy.kind {
                EnemyKind::Crawler => 2.0,
                EnemyKind::Drone => 2.8,
                EnemyKind::Sentinel => 1.6,
            };
            enemy.x += enemy.direction * speed * dt;
            if enemy.x <= enemy.min_x || enemy.x >= enemy.max_x {
                enemy.x = enemy.x.clamp(enemy.min_x, enemy.max_x);
                enemy.direction *= -1.0;
            }

            if enemy.kind == EnemyKind::Sentinel {
                enemy.shot_timer -= dt;
                if enemy.shot_timer <= 0.0 && (enemy.x - player_x).abs() < 30.0 {
                    let direction = (player_x - enemy.x).signum();
                    hostile_shots.push(Projectile {
                        x: enemy.x + direction,
                        y: enemy.y + (player_y - enemy.y).clamp(-2.0, 2.0) * 0.2,
                        vx: direction * 13.0,
                        ttl: 2.8,
                        hostile: true,
                    });
                    enemy.shot_timer = 1.15;
                }
            }
        }
        self.projectiles.extend(hostile_shots);
    }

    fn update_projectiles(&mut self, dt: f32) {
        let tiles = &self.tiles;
        let gate_open = self.relays >= 3;
        for projectile in &mut self.projectiles {
            projectile.x += projectile.vx * dt;
            projectile.ttl -= dt;
            let tx = projectile.x.round() as i32;
            let ty = projectile.y.floor() as i32;
            let tile = tile_at(tiles, tx, ty);
            if tile == Tile::Hull || (tile == Tile::Gate && !gate_open) {
                projectile.ttl = 0.0;
            }
        }

        let mut killed = Vec::new();
        for projectile in &mut self.projectiles {
            if projectile.ttl <= 0.0 || projectile.hostile {
                continue;
            }
            for (index, enemy) in self.enemies.iter_mut().enumerate() {
                if enemy.hp > 0
                    && (enemy.x - projectile.x).abs() < 0.9
                    && (enemy.y - projectile.y).abs() < 0.9
                {
                    enemy.hp -= 1;
                    projectile.ttl = 0.0;
                    if enemy.hp == 0 {
                        killed.push((index, enemy.kind));
                    }
                    break;
                }
            }
        }

        for (_, kind) in killed {
            match kind {
                EnemyKind::Sentinel => {
                    self.boss_defeated = true;
                    self.score += 2_000;
                    self.show_message("SENTINEL DOWN // ESCAPE POD ONLINE", 5.0);
                }
                _ => self.score += 100,
            }
        }

        let player_x = self.player.x;
        let player_y = self.player.y;
        let player_can_be_hit = self.player.invulnerable <= 0.0;
        let mut projectile_hit = false;
        for projectile in &mut self.projectiles {
            if projectile.ttl > 0.0
                && projectile.hostile
                && player_can_be_hit
                && (projectile.x - player_x).abs() < 0.65
                && (projectile.y - player_y).abs() < 0.65
            {
                projectile.ttl = 0.0;
                projectile_hit = true;
                break;
            }
        }
        if projectile_hit {
            self.damage_player(1);
        }

        self.projectiles.retain(|projectile| projectile.ttl > 0.0);
    }

    fn check_contacts(&mut self) {
        let touching_hazard = {
            let x = self.player.x.round() as i32;
            let y = (self.player.y + PLAYER_HALF_HEIGHT).floor() as i32;
            self.tile(x, y) == Tile::Hazard
        };
        let touching_enemy = self.enemies.iter().any(|enemy| {
            enemy.hp > 0
                && (enemy.x - self.player.x).abs() < 0.75
                && (enemy.y - self.player.y).abs() < 0.8
        });
        if (touching_hazard || touching_enemy) && self.player.invulnerable <= 0.0 {
            self.damage_player(1);
        }

        if !self.boss_defeated && self.player.x >= 174.0 && self.message_timer <= 0.0 {
            self.show_message("ESCAPE POD LOCKED // SENTINEL ACTIVE", 2.0);
        }
    }

    fn damage_player(&mut self, amount: i32) {
        self.player.hp -= amount;
        self.player.invulnerable = 1.0;
        self.player.vx = -self.player.facing * 7.0;
        self.player.vy = -6.0;
        self.show_message("HULL BREACH", 1.0);
    }

    fn respawn(&mut self) {
        let dash = self.player.has_dash;
        let double_jump = self.player.has_double_jump;
        self.player = Player::at(self.checkpoint.0, self.checkpoint.1);
        self.player.has_dash = dash;
        self.player.has_double_jump = double_jump;
        self.player.air_jump_available = double_jump;
        self.enemies = Self::station_enemies();
        self.projectiles.clear();
        self.boss_defeated = false;
        self.show_message("SYSTEM RECONSTRUCTED AT LAST RELAY", 3.0);
    }

    fn show_message(&mut self, message: &str, seconds: f32) {
        self.message.clear();
        self.message.push_str(message);
        self.message_timer = seconds;
    }
}

fn approach(current: f32, target: f32, amount: f32) -> f32 {
    if current < target {
        (current + amount).min(target)
    } else {
        (current - amount).max(target)
    }
}

fn tile_at(tiles: &[Tile], x: i32, y: i32) -> Tile {
    if !(0..WORLD_WIDTH).contains(&x) || !(0..WORLD_HEIGHT).contains(&y) {
        return Tile::Hull;
    }
    tiles[(y * WORLD_WIDTH + x) as usize]
}

#[cfg(test)]
mod tests {
    use super::*;

    fn settle(game: &mut Game) {
        for _ in 0..90 {
            game.tick(FrameInput::default(), FIXED_DT);
        }
    }

    #[test]
    fn spawn_settles_on_the_starting_floor() {
        let mut game = Game::new();
        settle(&mut game);
        assert!(game.player.grounded);
        assert!((game.player.y - 36.5).abs() < 0.2);
        assert_eq!(game.player.hp, 5);
    }

    #[test]
    fn first_relay_updates_progress_and_checkpoint() {
        let mut game = Game::new();
        game.player.x = 34.0;
        game.player.y = 27.8;
        game.tick(FrameInput::default(), FIXED_DT);
        assert_eq!(game.relays, 1);
        assert_eq!(game.score, 500);
        assert_eq!(game.player.hp, game.player.max_hp);
    }

    #[test]
    fn collecting_modules_unlocks_traversal_moves() {
        let mut game = Game::new();
        game.player.x = 49.0;
        game.player.y = 31.8;
        game.tick(FrameInput::default(), FIXED_DT);
        assert!(game.player.has_dash);

        game.player.x = 100.0;
        game.player.y = 31.8;
        game.tick(FrameInput::default(), FIXED_DT);
        assert!(game.player.has_double_jump);
    }

    #[test]
    fn dash_requires_module() {
        let mut game = Game::new();
        settle(&mut game);
        game.tick(
            FrameInput {
                dash: true,
                ..FrameInput::default()
            },
            FIXED_DT,
        );
        assert!(game.player.vx.abs() < 24.0);

        game.player.has_dash = true;
        game.tick(
            FrameInput {
                dash: true,
                ..FrameInput::default()
            },
            FIXED_DT,
        );
        assert!(game.player.vx.abs() > 20.0);
    }

    #[test]
    fn double_jump_is_consumed_until_landing() {
        let mut game = Game::new();
        settle(&mut game);
        game.player.has_double_jump = true;
        game.player.air_jump_available = true;
        game.tick(
            FrameInput {
                jump: true,
                ..FrameInput::default()
            },
            FIXED_DT,
        );
        game.tick(
            FrameInput {
                jump: true,
                ..FrameInput::default()
            },
            FIXED_DT,
        );
        assert!(!game.player.air_jump_available);
    }

    #[test]
    fn final_gate_opens_after_all_relays() {
        let mut game = Game::new();
        assert!(!game.gate_open());
        for (x, y) in [(34.0, 27.8), (89.0, 27.8), (134.0, 23.8)] {
            game.player.x = x;
            game.player.y = y;
            game.tick(FrameInput::default(), FIXED_DT);
        }
        assert!(game.gate_open());
        assert_eq!(game.tile(151, 32), Tile::Gate);
        assert!(!game.solid_at(151.0, 32.0));
    }

    #[test]
    fn dash_can_cross_the_first_hull_breach() {
        let mut game = Game::new();
        game.enemies.clear();
        game.player.x = 53.0;
        game.player.y = 35.0;
        settle(&mut game);
        game.player.has_dash = true;

        for frame in 0..65 {
            game.tick(
                FrameInput {
                    move_x: 1.0,
                    jump: frame == 0,
                    dash: frame == 8,
                    ..FrameInput::default()
                },
                FIXED_DT,
            );
        }

        assert!(game.player.x > 67.0, "player remained at {}", game.player.x);
    }

    #[test]
    fn player_projectile_can_destroy_enemy() {
        let mut game = Game::new();
        game.enemies = vec![Game::enemy(EnemyKind::Crawler, 6.0, 34.0, 6.0, 6.0)];
        game.player.x = 4.0;
        game.player.y = 34.0;
        for _ in 0..2 {
            game.player.shoot_cooldown = 0.0;
            game.tick(
                FrameInput {
                    shoot: true,
                    ..FrameInput::default()
                },
                FIXED_DT,
            );
            for _ in 0..5 {
                game.tick(FrameInput::default(), FIXED_DT);
            }
        }
        assert_eq!(game.enemies[0].hp, 0);
        assert_eq!(game.score, 100);
    }

    #[test]
    fn reaching_pod_after_boss_wins() {
        let mut game = Game::new();
        game.boss_defeated = true;
        game.player.x = 175.1;
        game.player.y = 35.5;
        game.tick(FrameInput::default(), FIXED_DT);
        assert_eq!(game.phase, Phase::Won);
    }

    #[test]
    fn restart_returns_to_a_fresh_run() {
        let mut game = Game::new();
        game.score = 999;
        game.relays = 3;
        game.tick(
            FrameInput {
                restart: true,
                ..FrameInput::default()
            },
            FIXED_DT,
        );
        assert_eq!(game.score, 0);
        assert_eq!(game.relays, 0);
        assert_eq!(game.phase, Phase::Running);
    }

    #[test]
    fn pause_freezes_simulation() {
        let mut game = Game::new();
        game.tick(
            FrameInput {
                toggle_pause: true,
                ..FrameInput::default()
            },
            FIXED_DT,
        );
        let before = (game.player.x, game.player.y, game.elapsed);
        game.tick(
            FrameInput {
                move_x: 1.0,
                ..FrameInput::default()
            },
            FIXED_DT,
        );
        assert_eq!(before, (game.player.x, game.player.y, game.elapsed));
    }
}
