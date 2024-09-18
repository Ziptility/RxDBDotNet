import React, { useState, useCallback } from 'react';
import { Box, Button, Alert } from '@mui/material';
import WorkspaceList, { WorkspaceListProps } from './WorkspaceList';
import WorkspaceForm from './WorkspaceForm';
import { Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';

const WorkspacesPageContent: React.FC = () => {
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);
  const {
    documents: workspaces,
    isLoading,
    error,
    upsertDocument,
    deleteDocument,
  } = useDocuments<Workspace>('workspaces');

  const handleCreate = useCallback(
    async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      const newWorkspace: Workspace = {
        id: uuidv4(),
        ...workspace,
        updatedAt: new Date().toISOString(),
        isDeleted: false,
      };
      await upsertDocument(newWorkspace);
    },
    [upsertDocument]
  );

  const handleUpdate = useCallback(
    async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingWorkspace) {
        const updatedWorkspace: Workspace = {
          ...editingWorkspace,
          ...workspace,
          updatedAt: new Date().toISOString(),
        };
        await upsertDocument(updatedWorkspace);
        setEditingWorkspace(null);
      }
    },
    [editingWorkspace, upsertDocument]
  );

  const handleDelete = useCallback(
    async (workspace: Workspace): Promise<void> => {
      await deleteDocument(workspace.id);
    },
    [deleteDocument]
  );

  const workspaceListProps: WorkspaceListProps = {
    workspaces,
    onEdit: setEditingWorkspace,
    onDelete: (workspace): void => {
      void handleDelete(workspace);
    },
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
      <WorkspaceList {...workspaceListProps} />
    </Box>
  );
};

export default WorkspacesPageContent;
