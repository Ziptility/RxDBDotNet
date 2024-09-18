// src\components\UsersPageContent.tsx
import React, { useState, useCallback } from 'react';
import { Box, Button, Alert } from '@mui/material';
import UserList, { UserListProps } from './UserList';
import UserForm from './UserForm';
import { User, Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';

const UsersPageContent: React.FC = () => {
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const {
    documents: users,
    isLoading: isLoadingUsers,
    error: userError,
    upsertDocument,
    deleteDocument,
  } = useDocuments<User>('users');

  const {
    documents: workspaces,
    isLoading: isLoadingWorkspaces,
    error: workspaceError,
  } = useDocuments<Workspace>('workspaces');

  const handleCreate = useCallback(
    async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      const newUser: User = {
        id: uuidv4(),
        ...user,
        updatedAt: new Date().toISOString(),
        isDeleted: false,
      };
      await upsertDocument(newUser);
    },
    [upsertDocument]
  );

  const handleUpdate = useCallback(
    async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingUser) {
        const updatedUser: User = {
          ...editingUser,
          ...user,
          updatedAt: new Date().toISOString(),
        };
        await upsertDocument(updatedUser);
        setEditingUser(null);
      }
    },
    [editingUser, upsertDocument]
  );

  const handleDelete = useCallback(
    async (user: User): Promise<void> => {
      await deleteDocument(user.id);
    },
    [deleteDocument]
  );

  const userListProps: UserListProps = {
    users,
    onEdit: setEditingUser,
    onDelete: (user: User): void => {
      void handleDelete(user);
    },
  };

  if (isLoadingUsers || isLoadingWorkspaces) {
    return <Box>Loading...</Box>;
  }

  const error = userError ?? workspaceError;

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
          workspaces={workspaces}
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
