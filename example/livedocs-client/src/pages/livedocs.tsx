import React, { useState, useEffect } from 'react';
import { Typography, Box, Button } from '@mui/material';
import LiveDocList from '../components/LiveDocList';
import LiveDocForm from '../components/LiveDocForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { LiveDocDocType, UserDocType, WorkspaceDocType } from '../lib/schemas';

const LiveDocsPage: React.FC = () => {
  const [db, setDb] = useState<Awaited<ReturnType<typeof getDatabase>> | null>(null);
  const [editingLiveDoc, setEditingLiveDoc] = useState<LiveDocDocType | null>(null);
  const [users, setUsers] = useState<UserDocType[]>([]);
  const [workspaces, setWorkspaces] = useState<WorkspaceDocType[]>([]);

  useEffect(() => {
    const initDb = async () => {
      const database = await getDatabase();
      await setupReplication(database);
      setDb(database);

      const usersSubscription = database.users.find({
        selector: {
          isDeleted: false
        }
      }).$
        .subscribe(docs => {
          setUsers(docs.map(doc => doc.toJSON()));
        });

      const workspacesSubscription = database.workspaces.find({
        selector: {
          isDeleted: false
        }
      }).$
        .subscribe(docs => {
          setWorkspaces(docs.map(doc => doc.toJSON()));
        });

      return () => {
        usersSubscription.unsubscribe();
        workspacesSubscription.unsubscribe();
      };
    };
    initDb();
  }, []);

  const handleCreate = async (liveDoc: Omit<LiveDocDocType, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db) {
      await db.liveDocs.insert({
        id: Date.now().toString(),
        ...liveDoc,
        updatedAt: new Date().toISOString(),
        isDeleted: false
      });
    }
  };

  const handleUpdate = async (liveDoc: Omit<LiveDocDocType, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db && editingLiveDoc) {
      await db.liveDocs.upsert({
        ...editingLiveDoc,
        ...liveDoc,
        updatedAt: new Date().toISOString()
      });
      setEditingLiveDoc(null);
    }
  };

  const handleDelete = async (liveDoc: LiveDocDocType) => {
    if (db) {
      await db.liveDocs.upsert({
        ...liveDoc,
        isDeleted: true,
        updatedAt: new Date().toISOString()
      });
    }
  };

  if (!db) {
    return <Typography>Loading...</Typography>;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Live Documents
      </Typography>
      <Box sx={{ mb: 4 }}>
        <LiveDocForm
          liveDoc={editingLiveDoc || undefined}
          users={users.map(u => ({ id: u.id, name: `${u.firstName} ${u.lastName}` }))}
          workspaces={workspaces.map(w => ({ id: w.id, name: w.name }))}
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
        onDelete={handleDelete}
      />
    </Box>
  );
};

export default LiveDocsPage;