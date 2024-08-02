import { createRxDatabase, RxDatabase, RxCollection, addRxPlugin } from 'rxdb';
import { getRxStorageDexie } from 'rxdb/plugins/storage-dexie';
import { RxDBDevModePlugin } from 'rxdb/plugins/dev-mode';
import { workspaceSchema, userSchema, liveDocSchema, WorkspaceDocType, UserDocType, LiveDocDocType } from '@/lib/schemas';

addRxPlugin(RxDBDevModePlugin);

export type LiveDocsCollections = {
  workspaces: RxCollection<WorkspaceDocType>;
  users: RxCollection<UserDocType>;
  liveDocs: RxCollection<LiveDocDocType>;
};

export type LiveDocsDatabase = RxDatabase<LiveDocsCollections>;

let dbPromise: Promise<LiveDocsDatabase> | null = null;

export const getDatabase = async (): Promise<LiveDocsDatabase> => {
  if (dbPromise) return dbPromise;

  dbPromise = createRxDatabase<LiveDocsCollections>({
    name: 'livedocsdb',
    storage: getRxStorageDexie()
  }).then(async (db) => {
    await db.addCollections({
      workspaces: {
        schema: workspaceSchema
      },
      users: {
        schema: userSchema
      },
      liveDocs: {
        schema: liveDocSchema
      }
    });

    return db;
  });

  return dbPromise;
};