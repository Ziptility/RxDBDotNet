// src\components\UsersPageContent.tsx
import React, { useState, useCallback } from 'react';
import { Box, Button, Alert } from '@mui/material';
import UserList, { UserListProps } from './UserList';
import UserForm from './UserForm';
import { User, Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';
import { getDatabase } from '@/lib/database';
import { handleAsyncError } from '@/utils/errorHandling';

const UsersPageContent: React.FC = () => {
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const { documents: users, refetch, error } = useDocuments<User>('users');
  const { documents: workspaces } = useDocuments<Workspace>('workspaces');

  const handleCreate = useCallback(
    async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      await handleAsyncError(async () => {
        const db = await getDatabase();
        await db.users.insert({
          id: uuidv4(),
          ...user,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
        await refetch();
      }, 'Creating user');
    },
    [refetch]
  );

  const handleUpdate = useCallback(
    async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingUser) {
        await handleAsyncError(async () => {
          const db = await getDatabase();
          await db.users.upsert({
            ...editingUser,
            ...user,
            updatedAt: new Date().toISOString(),
          });
          setEditingUser(null);
          await refetch();
        }, 'Updating user');
      }
    },
    [editingUser, refetch]
  );

  const handleDelete = useCallback(
    async (user: User): Promise<void> => {
      await handleAsyncError(async () => {
        const db = await getDatabase();
        await db.users.upsert({
          ...user,
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        });
        await refetch();
      }, 'Deleting user');
    },
    [refetch]
  );

  const userListProps: UserListProps = {
    users, // This is now correctly typed
    onEdit: setEditingUser,
    onDelete: (user): void => {
      void handleDelete(user);
    },
  };

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error.message}
        </Alert>
      )}
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
      <UserList {...userListProps} />
    </Box>
  );
};

export default UsersPageContent;
