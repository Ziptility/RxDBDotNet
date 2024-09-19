// src\lib\database.ts
import { createRxDatabase, addRxPlugin, RxDatabase } from 'rxdb';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { RxDBDevModePlugin } from 'rxdb/plugins/dev-mode';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';
import { LiveDocsDatabase, LiveDocsCollections, LiveDocsCollectionConfig } from '@/types';
import { setupReplication } from './replication';

addRxPlugin(RxDBDevModePlugin);

let db: Promise<LiveDocsDatabase> | null = null;

export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  if (db) return db;

  const collections: LiveDocsCollectionConfig = {
    workspace: {
      schema: workspaceSchema,
    },
    user: {
      schema: userSchema,
    },
    livedoc: {
      schema: liveDocSchema,
    },
  };

  db = createRxDatabase<LiveDocsCollections>({
    name: 'livedocsdb',
    storage: getRxStorageDexie(),
  }).then(async (database: RxDatabase<LiveDocsCollections>): Promise<LiveDocsDatabase> => {
    await database.addCollections(collections);

    // Set up replication for all collections
    await setupReplication(database);

    return database as LiveDocsDatabase;
  });

  return db;
};

export type { LiveDocsDatabase };
