import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import process from 'node:process';
import { Client } from 'pg';

function getRequiredEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }

  return value;
}

async function main() {
  const connectionString = getRequiredEnv('POSTGRES_URL');
  const sqlPath = resolve(process.cwd(), 'database', 'postgres', 'init-auth-schema.sql');
  const sql = await readFile(sqlPath, 'utf8');

  const client = new Client({ connectionString });

  try {
    await client.connect();
    await client.query(sql);
    console.log('PostgreSQL auth schema applied successfully.');
  } finally {
    await client.end();
  }
}

main().catch((error) => {
  console.error('Failed to apply PostgreSQL auth schema.');
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});