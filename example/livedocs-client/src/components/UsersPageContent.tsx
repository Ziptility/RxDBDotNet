// src\components\UsersPageContent.tsx
import React, { useState, useCallback } from 'react';
import { Box, Button } from '@mui/material';
import UserList, { UserListProps } from './UserList';
import UserForm from './UserForm';
import { User, Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';
import { getDatabase } from '@/lib/database';

const UsersPageContent: React.FC = () => {
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const { refetch } = useDocuments<User>('users');
  const { documents: workspaces } = useDocuments<Workspace>('workspaces');

  const handleCreate = useCallback(
    async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      const db = await getDatabase();
      try {
        await db.users.insert({
          id: uuidv4(),
          ...user,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
        await refetch();
      } catch (error) {
        console.error('Error creating user:', error);
      }
    },
    [refetch]
  );

  const handleUpdate = useCallback(
    async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingUser) {
        const db = await getDatabase();
        try {
          await db.users.upsert({
            ...editingUser,
            ...user,
            updatedAt: new Date().toISOString(),
          });
          setEditingUser(null);
          await refetch();
        } catch (error) {
          console.error('Error updating user:', error);
        }
      }
    },
    [editingUser, refetch]
  );

  const handleDelete = useCallback(
    async (user: User): Promise<void> => {
      const db = await getDatabase();
      try {
        await db.users.upsert({
          ...user,
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        });
        await refetch();
      } catch (error) {
        console.error('Error deleting user:', error);
      }
    },
    [refetch]
  );

  const userListProps: UserListProps = {
    onEdit: setEditingUser,
    onDelete: (user): void => {
      void handleDelete(user);
    },
  };

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
      <UserList {...userListProps} />
    </Box>
  );
};

export default UsersPageContent;
