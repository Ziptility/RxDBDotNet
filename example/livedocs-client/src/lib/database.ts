// src/lib/database.ts
import { createRxDatabase, addRxPlugin } from 'rxdb';
import { RxDBDevModePlugin, disableWarnings } from 'rxdb/plugins/dev-mode';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { API_CONFIG } from '@/config';
import type { LiveDocsDatabase, LiveDocsCollections, LiveDocsCollectionConfig } from '@/types';
import { setupReplication } from './replication';
import { workspaceSchema, userSchema, liveDocSchema } from './schemas';

disableWarnings();

addRxPlugin(RxDBDevModePlugin);

let databaseInstance: LiveDocsDatabase | null = null;

const createCollections = (): LiveDocsCollectionConfig => ({
  workspace: { schema: workspaceSchema },
  user: { schema: userSchema },
  livedoc: { schema: liveDocSchema },
});

const initializeDatabase = async (): Promise<LiveDocsDatabase> => {
  try {
    const db = await createRxDatabase<LiveDocsCollections>({
      name: 'livedocsdb',
      storage: getRxStorageDexie(),
      ignoreDuplicate: true, // This allows reconnection to an existing database
    });

    await db.addCollections(createCollections());

    const replicationStates = await setupReplication(db, API_CONFIG.DEFAULT_JWT_TOKEN);

    return Object.assign(db, { replicationStates }) as LiveDocsDatabase;
  } catch (error) {
    console.error('Failed to initialize database:', error);
    throw error;
  }
};

export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  if (!databaseInstance) {
    databaseInstance = await initializeDatabase();
  }
  return databaseInstance;
};

export type { LiveDocsDatabase };
