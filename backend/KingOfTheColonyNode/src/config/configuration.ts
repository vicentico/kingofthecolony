export default () => ({
  app: {
    name: process.env.APP_NAME ?? 'kingofthecolony-node',
    port: Number(process.env.PORT ?? 3000),
    apiPrefix: process.env.API_PREFIX ?? '/api',
    nodeEnv: process.env.NODE_ENV ?? 'development',
    enableSwagger: (process.env.ENABLE_SWAGGER ?? 'true').toLowerCase() === 'true',
    corsOrigin: process.env.CORS_ORIGIN ?? 'http://localhost:4200',
    logLevel: process.env.LOG_LEVEL ?? 'info',
  },
  database: {
    provider: process.env.DATABASE_PROVIDER ?? 'postgres',
    postgresUrl: process.env.POSTGRES_URL ?? '',
    mongodbUrl: process.env.MONGODB_URL ?? '',
    redisUrl: process.env.REDIS_URL ?? '',
  },
  auth: {
    accessSecret: process.env.JWT_ACCESS_SECRET ?? 'replace_me',
    refreshSecret: process.env.JWT_REFRESH_SECRET ?? 'replace_me',
    accessTtl: process.env.JWT_ACCESS_TTL ?? '15m',
    refreshTtl: process.env.JWT_REFRESH_TTL ?? '7d',
    issuer: process.env.JWT_ISSUER ?? 'kingofthecolony-node',
    audience: process.env.JWT_AUDIENCE ?? 'kingofthecolony-clients',
  },
});