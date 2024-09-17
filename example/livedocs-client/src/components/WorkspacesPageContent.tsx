// src\components\WorkspacesPageContent.tsx
import React, { useState, useEffect } from 'react';
import { Box, Button } from '@mui/material';
import WorkspaceList from './WorkspaceList';
import WorkspaceForm from './WorkspaceForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { Workspace } from '../lib/schemas';
import { LiveDocsDatabase } from '@/types';
import { v4 as uuidv4 } from 'uuid';

const WorkspacesPageContent: React.FC = (): JSX.Element => {
  const [db, setDb] = useState<LiveDocsDatabase | null>(null);
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);

  useEffect(() => {
    const initDb = async (): Promise<void> => {
      try {
        const database = await getDatabase();
        await setupReplication(database);
        setDb(database);
      } catch (error) {
        console.error('Error initializing database:', error);
      }
    };

    void initDb();
  }, []);

  const handleCreate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (db) {
      try {
        await db.workspaces.insert({
          id: uuidv4(),
          ...workspace,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
      } catch (error) {
        console.error('Error creating workspace:', error);
      }
    }
  };

  const handleUpdate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (db && editingWorkspace) {
      try {
        await db.workspaces.upsert({
          ...editingWorkspace,
          ...workspace,
          updatedAt: new Date().toISOString(),
        });
        setEditingWorkspace(null);
      } catch (error) {
        console.error('Error updating workspace:', error);
      }
    }
  };

  const handleDelete = async (workspace: Workspace): Promise<void> => {
    if (db) {
      try {
        await db.workspaces.upsert({
          ...workspace,
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        });
      } catch (error) {
        console.error('Error deleting workspace:', error);
      }
    }
  };

  if (!db) {
    return <Box>Initializing database...</Box>;
  }

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
      <WorkspaceList
        db={db}
        onEdit={setEditingWorkspace}
        onDelete={(workspace): void => {
          void handleDelete(workspace);
        }}
      />
    </Box>
  );
};

export default WorkspacesPageContent;
