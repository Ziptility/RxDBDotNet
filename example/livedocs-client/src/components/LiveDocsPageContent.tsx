import React, { useState, useEffect } from 'react';
import { Box, Button } from '@mui/material';
import LiveDocList from './LiveDocList';
import LiveDocForm from './LiveDocForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { LiveDocDocType, UserDocType, WorkspaceDocType } from '@/lib/schemas';
import { LiveDocsDatabase } from '@/types';

const LiveDocsPageContent: React.FC = (): JSX.Element => {
  const [db, setDb] = useState<LiveDocsDatabase | null>(null);
  const [editingLiveDoc, setEditingLiveDoc] = useState<LiveDocDocType | null>(null);
  const [users, setUsers] = useState<UserDocType[]>([]);
  const [workspaces, setWorkspaces] = useState<WorkspaceDocType[]>([]);

  useEffect(() => {
    const initDb = async (): Promise<void> => {
      try {
        const database = await getDatabase();
        await setupReplication(database);
        setDb(database);

        const usersSubscription = database.users
          .find({
            selector: {
              isDeleted: false,
            },
          })
          .$.subscribe((docs) => {
            setUsers(docs.map((doc) => doc.toJSON()));
          });

        const workspacesSubscription = database.workspaces
          .find({
            selector: {
              isDeleted: false,
            },
          })
          .$.subscribe((docs) => {
            setWorkspaces(docs.map((doc) => doc.toJSON()));
          });

        return () => {
          usersSubscription.unsubscribe();
          workspacesSubscription.unsubscribe();
        };
      } catch (error) {
        console.error('Error initializing database:', error);
      }
    };

    void initDb();
  }, []);

  const handleCreate = async (liveDoc: Omit<LiveDocDocType, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (db) {
      try {
        await db.livedocs.insert({
          id: Date.now().toString(),
          ...liveDoc,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
      } catch (error) {
        console.error('Error creating LiveDoc:', error);
      }
    }
  };

  const handleUpdate = async (liveDoc: Omit<LiveDocDocType, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (db && editingLiveDoc) {
      try {
        await db.livedocs.upsert({
          ...editingLiveDoc,
          ...liveDoc,
          updatedAt: new Date().toISOString(),
        });
        setEditingLiveDoc(null);
      } catch (error) {
        console.error('Error updating LiveDoc:', error);
      }
    }
  };

  const handleDelete = async (liveDoc: LiveDocDocType): Promise<void> => {
    if (db) {
      try {
        await db.livedocs.upsert({
          ...liveDoc,
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        });
      } catch (error) {
        console.error('Error deleting LiveDoc:', error);
      }
    }
  };

  if (!db) {
    return <Box>Initializing database...</Box>;
  }

  return (
    <Box>
      <Box sx={{ mb: 4 }}>
        <LiveDocForm
          liveDoc={editingLiveDoc || undefined}
          users={users.map((u) => ({ id: u.id, name: `${u.firstName} ${u.lastName}` }))}
          workspaces={workspaces.map((w) => ({ id: w.id, name: w.name }))}
          onSubmit={editingLiveDoc ? handleUpdate : handleCreate}
        />
      </Box>
      {editingLiveDoc && (
        <Button onClick={() => setEditingLiveDoc(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      )}
      <LiveDocList
        db={db}
        onEdit={setEditingLiveDoc}
        onDelete={(liveDoc) => {
          void handleDelete(liveDoc);
        }}
      />
    </Box>
  );
};

export default LiveDocsPageContent;
