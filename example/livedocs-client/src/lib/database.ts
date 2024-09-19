import { createRxDatabase, addRxPlugin, type RxDatabase } from 'rxdb';
import { RxDBDevModePlugin } from 'rxdb/plugins/dev-mode';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { API_CONFIG } from '@/config';
import type { LiveDocsDatabase, LiveDocsCollections, LiveDocsCollectionConfig } from '@/types';
import { setupReplication } from './replication';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';

addRxPlugin(RxDBDevModePlugin);

let db: Promise<LiveDocsDatabase> | null = null;

export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  if (db !== null) return db;

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

  // Use the default JWT token for initial setup
  const defaultToken = API_CONFIG.DEFAULT_JWT_TOKEN;
  const replicationStates = await setupReplication(database, defaultToken);

  // Add replicationStates to the database object
  (database as LiveDocsDatabase).replicationStates = replicationStates;

  return database as LiveDocsDatabase;
};

export type { LiveDocsDatabase };
