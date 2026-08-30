import { mkdir, readdir, rm } from 'node:fs/promises';
import { join } from 'node:path';
import { promisify } from 'node:util';
import { execFile } from 'node:child_process';

const run = promisify(execFile);
const database = process.env.GAME_DATABASE;
const backupDirectory = process.env.BACKUP_DIR;
if (!database || !backupDirectory) throw new Error('GAME_DATABASE and BACKUP_DIR are required');
if (database.includes("'") || backupDirectory.includes("'")) throw new Error('backup paths cannot contain single quotes');

await mkdir(backupDirectory, { recursive: true });
const stamp = new Date().toISOString().replace(/[:.]/g, '-');
const target = join(backupDirectory, `rooms-${stamp}.sqlite`);
await run('sqlite3', [database, `.backup '${target}'`]);
const backups = (await readdir(backupDirectory)).filter((name) => name.endsWith('.sqlite')).sort();
await Promise.all(backups.slice(0, -7).map((name) => rm(join(backupDirectory, name))));
console.info(`created ${target}`);
