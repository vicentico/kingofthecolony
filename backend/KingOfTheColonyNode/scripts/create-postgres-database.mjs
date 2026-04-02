import process from 'node:process';
import { Client } from 'pg';

function getRequiredEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }

  return value;
}

function quoteIdentifier(value) {
  return `"${value.replaceAll('"', '""')}"`;
}

function quoteLiteral(value) {
  return `'${value.replaceAll("'", "''")}'`;
}

async function main() {
  const adminUrl = getRequiredEnv('POSTGRES_ADMIN_URL');
  const databaseName = getRequiredEnv('POSTGRES_DB_NAME');
  const appUser = process.env.POSTGRES_APP_USER?.trim();
  const appPassword = process.env.POSTGRES_APP_PASSWORD?.trim();

  const client = new Client({ connectionString: adminUrl });

  try {
    await client.connect();

    const databaseExists = await client.query(
      'SELECT 1 FROM pg_database WHERE datname = $1',
      [databaseName],
    );

    if (databaseExists.rowCount === 0) {
      await client.query(`CREATE DATABASE ${quoteIdentifier(databaseName)}`);
      console.log(`Database created: ${databaseName}`);
    } else {
      console.log(`Database already exists: ${databaseName}`);
    }

    if (appUser) {
      const userExists = await client.query(
        'SELECT 1 FROM pg_roles WHERE rolname = $1',
        [appUser],
      );

      if (userExists.rowCount === 0) {
        if (!appPassword) {
          throw new Error('POSTGRES_APP_PASSWORD is required when POSTGRES_APP_USER is provided.');
        }

        await client.query(
          `CREATE ROLE ${quoteIdentifier(appUser)} LOGIN PASSWORD ${quoteLiteral(appPassword)}`,
        );
        console.log(`Role created: ${appUser}`);
      } else {
        console.log(`Role already exists: ${appUser}`);
      }

      await client.query(
        `GRANT ALL PRIVILEGES ON DATABASE ${quoteIdentifier(databaseName)} TO ${quoteIdentifier(appUser)}`,
      );
      console.log(`Privileges granted on ${databaseName} to ${appUser}`);
    }
  } finally {
    await client.end();
  }
}

main().catch((error) => {
  console.error('PostgreSQL bootstrap failed.');
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});