// src\lib\database.ts
import { createRxDatabase, addRxPlugin, type RxDatabase } from 'rxdb';
import { RxDBDevModePlugin } from 'rxdb/plugins/dev-mode';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import type { LiveDocsDatabase, LiveDocsCollections, LiveDocsCollectionConfig } from '@/types';
import { setupReplication } from './replication';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';

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
  });

  const database: RxDatabase<LiveDocsCollections> = await db;
  await database.addCollections(collections);
  await setupReplication(database);

  return database as LiveDocsDatabase;
};

export type { LiveDocsDatabase };
