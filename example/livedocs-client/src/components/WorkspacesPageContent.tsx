// src/components/WorkspacesPageContent.tsx
import React, { useState, useCallback } from 'react';
import { Box, Button, Alert } from '@mui/material';
import WorkspaceList, { WorkspaceListProps } from './WorkspaceList';
import WorkspaceForm from './WorkspaceForm';
import { Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';
import { getDatabase } from '@/lib/database';
import { handleAsyncError } from '@/utils/errorHandling';

const WorkspacesPageContent: React.FC = () => {
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);
  const { documents: workspaces, refetch, error } = useDocuments<Workspace>('workspaces');

  const handleCreate = useCallback(
    async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      await handleAsyncError(async () => {
        const db = await getDatabase();
        await db.workspaces.insert({
          id: uuidv4(),
          ...workspace,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
        await refetch();
      }, 'Creating workspace');
    },
    [refetch]
  );

  const handleUpdate = useCallback(
    async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingWorkspace) {
        await handleAsyncError(async () => {
          const db = await getDatabase();
          await db.workspaces.upsert({
            ...editingWorkspace,
            ...workspace,
            updatedAt: new Date().toISOString(),
          });
          setEditingWorkspace(null);
          await refetch();
        }, 'Updating workspace');
      }
    },
    [editingWorkspace, refetch]
  );

  const handleDelete = useCallback(
    async (workspace: Workspace): Promise<void> => {
      await handleAsyncError(async () => {
        const db = await getDatabase();
        await db.workspaces.upsert({
          ...workspace,
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        });
        await refetch();
      }, 'Deleting workspace');
    },
    [refetch]
  );

  const workspaceListProps: WorkspaceListProps = {
    workspaces,
    onEdit: setEditingWorkspace,
    onDelete: (workspace): void => {
      void handleDelete(workspace);
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
