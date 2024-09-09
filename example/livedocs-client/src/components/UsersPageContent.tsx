import React, { useState, useEffect } from 'react';
import { Box, Button } from '@mui/material';
import { Subscription } from 'rxjs';
import UserList from './UserList';
import UserForm from './UserForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { User, Workspace } from '../lib/schemas';
import { LiveDocsDatabase } from '@/types';
import { v4 as uuidv4 } from 'uuid';

const UsersPageContent: React.FC = (): JSX.Element => {
  const [db, setDb] = useState<LiveDocsDatabase | null>(null);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);

  useEffect(() => {
    let workspacesSubscription: Subscription | undefined;

    const initDb = async (): Promise<void> => {
      try {
        const database = await getDatabase();
        await setupReplication(database);
        setDb(database);

        workspacesSubscription = database.workspaces
          .find({
            selector: {
              isDeleted: false,
            },
          })
          .$.subscribe((docs) => {
            setWorkspaces(docs.map((doc) => doc.toJSON()));
          });
      } catch (error) {
        console.error('Error initializing database:', error);
      }
    };

    void initDb();

    return () => {
      workspacesSubscription?.unsubscribe();
    };
  }, []);

  const handleCreate = async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (db) {
      try {
        await db.users.insert({
          id: uuidv4(),
          ...user,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
      } catch (error) {
        console.error('Error creating user:', error);
      }
    }
  };

  const handleUpdate = async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
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

  const handleDelete = async (user: User): Promise<void> => {
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
          user={editingUser ?? undefined}
          workspaces={workspaces.map((w) => ({ id: w.id, name: w.name }))}
          onSubmit={editingUser ? handleUpdate : handleCreate}
        />
      </Box>
      {editingUser && (
        <Button onClick={(): void => setEditingUser(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      )}
      <UserList
        db={db}
        onEdit={setEditingUser}
        onDelete={(user): void => {
          void handleDelete(user);
        }}
      />
    </Box>
  );
};

export default UsersPageContent;
