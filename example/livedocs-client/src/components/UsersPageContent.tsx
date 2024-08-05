import React, { useState, useEffect } from 'react';
import { Box, Button } from '@mui/material';
import UserList from './UserList';
import UserForm from './UserForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { UserDocType, WorkspaceDocType } from '../lib/schemas';
import { LiveDocsDatabase } from '@/types';

const UsersPageContent: React.FC = (): JSX.Element => {
  const [db, setDb] = useState<LiveDocsDatabase | null>(null);
  const [editingUser, setEditingUser] = useState<UserDocType | null>(null);
  const [workspaces, setWorkspaces] = useState<WorkspaceDocType[]>([]);

  useEffect(() => {
    const initDb = async (): Promise<void> => {
      try {
        const database = await getDatabase();
        await setupReplication(database);
        setDb(database);

        const workspacesSubscription = database.workspaces
          .find({
            selector: {
              isDeleted: false,
            },
          })
          .$.subscribe((docs) => {
            setWorkspaces(docs.map((doc) => doc.toJSON()));
          });

        return () => workspacesSubscription.unsubscribe();
      } catch (error) {
        console.error('Error initializing database:', error);
      }
    };

    void initDb();
  }, []);

  const handleCreate = async (user: Omit<UserDocType, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (db) {
      try {
        await db.users.insert({
          id: Date.now().toString(),
          ...user,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
      } catch (error) {
        console.error('Error creating user:', error);
      }
    }
  };

  const handleUpdate = async (user: Omit<UserDocType, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (db && editingUser) {
      try {
        await db.users.upsert({
          ...editingUser,
          ...user,
          updatedAt: new Date().toISOString(),
        });
        setEditingUser(null);
      } catch (error) {
        console.error('Error updating user:', error);
      }
    }
  };

  const handleDelete = async (user: UserDocType): Promise<void> => {
    if (db) {
      try {
        await db.users.upsert({
          ...user,
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        });
      } catch (error) {
        console.error('Error deleting user:', error);
      }
    }
  };

  if (!db) {
    return <Box>Initializing database...</Box>;
  }

  return (
    <Box>
      <Box sx={{ mb: 4 }}>
        <UserForm
          user={editingUser || undefined}
          workspaces={workspaces.map((w) => ({ id: w.id, name: w.name }))}
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
        onDelete={(user) => {
          void handleDelete(user);
        }}
      />
    </Box>
  );
};

export default UsersPageContent;
