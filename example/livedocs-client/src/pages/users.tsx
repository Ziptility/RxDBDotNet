import React, { useState, useEffect } from 'react';
import { Typography, Box, Button } from '@mui/material';
import UserList from '../components/UserList';
import UserForm from '../components/UserForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { UserDocType, WorkspaceDocType } from '../lib/schemas';

const UsersPage: React.FC = () => {
  const [db, setDb] = useState<Awaited<ReturnType<typeof getDatabase>> | null>(null);
  const [editingUser, setEditingUser] = useState<UserDocType | null>(null);
  const [workspaces, setWorkspaces] = useState<WorkspaceDocType[]>([]);

  useEffect(() => {
    const initDb = async () => {
      const database = await getDatabase();
      await setupReplication(database);
      setDb(database);

      const workspacesSubscription = database.workspaces.find({
        selector: {
          isDeleted: false
        }
      }).$
        .subscribe(docs => {
          setWorkspaces(docs.map(doc => doc.toJSON()));
        });

      return () => workspacesSubscription.unsubscribe();
    };
    initDb();
  }, []);

  const handleCreate = async (user: Omit<UserDocType, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db) {
      await db.users.insert({
        id: Date.now().toString(),
        ...user,
        updatedAt: new Date().toISOString(),
        isDeleted: false
      });
    }
  };

  const handleUpdate = async (user: Omit<UserDocType, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db && editingUser) {
      await db.users.upsert({
        ...editingUser,
        ...user,
        updatedAt: new Date().toISOString()
      });
      setEditingUser(null);
    }
  };
  
  const handleDelete = async (user: UserDocType) => {
    if (db) {
      await db.users.upsert({
        ...user,
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
        Users
      </Typography>
      <Box sx={{ mb: 4 }}>
        <UserForm
          user={editingUser || undefined}
          workspaces={workspaces.map(w => ({ id: w.id, name: w.name }))}
          onSubmit={editingUser ? handleUpdate : handleCreate}
        />
      </Box>
      {editingUser && (
        <Button onClick={() => setEditingUser(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      )}
      <UserList
        db={db}
        onEdit={setEditingUser}
        onDelete={handleDelete}
      />
    </Box>
  );
};

export default UsersPage;