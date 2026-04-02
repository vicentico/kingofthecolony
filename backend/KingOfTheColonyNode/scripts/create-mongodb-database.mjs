import process from 'node:process';
import { MongoClient } from 'mongodb';

function getRequiredEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }

  return value;
}

async function main() {
  const adminUrl = getRequiredEnv('MONGODB_ADMIN_URL');
  const databaseName = getRequiredEnv('MONGODB_DB_NAME');
  const bootstrapCollection = process.env.MONGODB_BOOTSTRAP_COLLECTION?.trim() || 'app_bootstrap';
  const appUser = process.env.MONGODB_APP_USER?.trim();
  const appPassword = process.env.MONGODB_APP_PASSWORD?.trim();

  const client = new MongoClient(adminUrl);

  try {
    await client.connect();

    const adminDb = client.db('admin');
    const targetDb = client.db(databaseName);
    const collections = await targetDb.listCollections({}, { nameOnly: true }).toArray();
    const hasBootstrapCollection = collections.some(
      (collection) => collection.name === bootstrapCollection,
    );

    if (!hasBootstrapCollection) {
      await targetDb.createCollection(bootstrapCollection);
      console.log(`Collection created: ${databaseName}.${bootstrapCollection}`);
    } else {
      console.log(`Collection already exists: ${databaseName}.${bootstrapCollection}`);
    }

    await targetDb.collection(bootstrapCollection).updateOne(
      { _id: 'bootstrap' },
      {
        $set: {
          createdAt: new Date(),
          managedBy: 'create-mongodb-database.mjs',
        },
      },
      { upsert: true },
    );
    console.log(`Database ready: ${databaseName}`);

    if (appUser) {
      if (!appPassword) {
        throw new Error('MONGODB_APP_PASSWORD is required when MONGODB_APP_USER is provided.');
      }

      const usersInfo = await adminDb.command({ usersInfo: { user: appUser, db: databaseName } });
      const userExists = Array.isArray(usersInfo.users) && usersInfo.users.length > 0;

      if (!userExists) {
        await targetDb.command({
          createUser: appUser,
          pwd: appPassword,
          roles: [{ role: 'readWrite', db: databaseName }],
        });
        console.log(`MongoDB user created: ${appUser}`);
      } else {
        console.log(`MongoDB user already exists: ${appUser}`);
      }
    }
  } finally {
    await client.close();
  }
}

main().catch((error) => {
  console.error('MongoDB bootstrap failed.');
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});