// src\components\UsersPageContent.tsx
import React, { useState } from 'react';
import { Box, Button, Alert } from '@mui/material';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { User, Workspace } from '@/lib/schemas';
import UserForm from './UserForm';
import UserList from './UserList';

const UsersPageContent: React.FC = () => {
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const {
    documents: users,
    isLoading: isLoadingUsers,
    error: userError,
    upsertDocument,
    deleteDocument,
  } = useDocuments<User>('user');

  const {
    documents: workspaces,
    isLoading: isLoadingWorkspaces,
    error: workspaceError,
  } = useDocuments<Workspace>('workspace');

  const handleCreate = async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    const newUser: User = {
      id: uuidv4(),
      ...user,
      updatedAt: new Date().toISOString(),
      isDeleted: false,
    };
    await upsertDocument(newUser);
  };

  const handleUpdate = async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (editingUser) {
      const updatedUser: User = {
        ...editingUser,
        ...user,
        updatedAt: new Date().toISOString(),
      };
      await upsertDocument(updatedUser);
      setEditingUser(null);
    }
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
      <UserList
        users={users}
        onEdit={setEditingUser}
        onDelete={(user): void => {
          void deleteDocument(user.id);
        }}
      />
    </Box>
  );
};

export default UsersPageContent;
