// src\components\WorkspacesPageContent.tsx
import React, { useState } from 'react';
import { Box, Button } from '@mui/material';
import WorkspaceList, { WorkspaceListProps } from './WorkspaceList';
import WorkspaceForm from './WorkspaceForm';
import { Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';
import { getDatabase } from '@/lib/database';

const WorkspacesPageContent: React.FC = () => {
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);
  const { documents: workspaces, refetch } = useDocuments<Workspace>('workspaces');

  const handleCreate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    const db = await getDatabase();
    try {
      await db.workspaces.insert({
        id: uuidv4(),
        ...workspace,
        updatedAt: new Date().toISOString(),
        isDeleted: false,
      });
      await refetch();
    } catch (error) {
      console.error('Error creating workspace:', error);
    }
  };

  const handleUpdate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (editingWorkspace) {
      const db = await getDatabase();
      try {
        await db.workspaces.upsert({
          ...editingWorkspace,
          ...workspace,
          updatedAt: new Date().toISOString(),
        });
        setEditingWorkspace(null);
        await refetch();
      } catch (error) {
        console.error('Error updating workspace:', error);
      }
    }
  };

  const handleDelete = async (workspace: Workspace): Promise<void> => {
    const db = await getDatabase();
    try {
      await db.workspaces.upsert({
        ...workspace,
        isDeleted: true,
        updatedAt: new Date().toISOString(),
      });
      await refetch();
    } catch (error) {
      console.error('Error deleting workspace:', error);
    }
  };

  const workspaceListProps: WorkspaceListProps = {
    workspaces,
    onEdit: setEditingWorkspace,
    onDelete: (workspace): void => {
      void handleDelete(workspace);
    },
  };

  return (
    <Box>
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
