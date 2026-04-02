import { Pool } from 'pg';

let poolInstance: Pool | null = null;

export function getPostgresPool(connectionString: string): Pool {
  if (!poolInstance) {
    poolInstance = new Pool({
      connectionString,
      max: 10,
      idleTimeoutMillis: 30000,
    });
  }

  return poolInstance;
}