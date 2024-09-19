// src\components\WorkspacesPageContent.tsx
import React, { useState } from 'react';
import { Box, Button, Alert } from '@mui/material';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { Workspace } from '@/lib/schemas';
import WorkspaceForm from './WorkspaceForm';
import WorkspaceList from './WorkspaceList';

const WorkspacesPageContent: React.FC = () => {
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);
  const {
    documents: workspaces,
    isLoading,
    error,
    upsertDocument,
    deleteDocument,
  } = useDocuments<Workspace>('workspace');

  const handleCreate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    const newWorkspace: Workspace = {
      id: uuidv4(),
      ...workspace,
      updatedAt: new Date().toISOString(),
      isDeleted: false,
    };
    await upsertDocument(newWorkspace);
  };

  const handleUpdate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (editingWorkspace) {
      const updatedWorkspace: Workspace = {
        ...editingWorkspace,
        ...workspace,
        updatedAt: new Date().toISOString(),
      };
      await upsertDocument(updatedWorkspace);
      setEditingWorkspace(null);
    }
  };

  if (isLoading) {
    return <Box>Loading...</Box>;
  }

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error.message}
        </Alert>
      )}
      <Box sx={{ mb: 4 }}>
        <WorkspaceForm
          workspace={editingWorkspace ?? undefined}
          onSubmit={editingWorkspace ? handleUpdate : handleCreate}
        />
      </Box>
      {editingWorkspace && (
        <Button onClick={(): void => setEditingWorkspace(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      )}
      <WorkspaceList
        workspaces={workspaces}
        onEdit={setEditingWorkspace}
        onDelete={(workspace): void => {
          void deleteDocument(workspace.id);
        }}
      />
    </Box>
  );
};

export default WorkspacesPageContent;
