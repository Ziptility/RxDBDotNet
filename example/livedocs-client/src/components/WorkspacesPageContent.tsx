// src/components/WorkspacesPageContent.tsx
import React, { useState, useEffect } from 'react';
import { Box, Button } from '@mui/material';
import WorkspaceList from './WorkspaceList';
import WorkspaceForm from './WorkspaceForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { WorkspaceDocType } from '../lib/schemas';

const WorkspacesPageContent: React.FC = () => {
  const [db, setDb] = useState<Awaited<ReturnType<typeof getDatabase>> | null>(null);
  const [editingWorkspace, setEditingWorkspace] = useState<WorkspaceDocType | null>(null);

  useEffect(() => {
    const initDb = async () => {
      const database = await getDatabase();
      await setupReplication(database);
      setDb(database);
    };
    initDb();
  }, []);

  const handleCreate = async (workspace: Omit<WorkspaceDocType, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db) {
      await db.workspaces.insert({
        id: Date.now().toString(),
        ...workspace,
        updatedAt: new Date().toISOString(),
        isDeleted: false
      });
    }
  };

  const handleUpdate = async (workspace: Omit<WorkspaceDocType, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db && editingWorkspace) {
      await db.workspaces.upsert({
        ...editingWorkspace,
        ...workspace,
        updatedAt: new Date().toISOString()
      });
      setEditingWorkspace(null);
    }
  };
  
  const handleDelete = async (workspace: WorkspaceDocType) => {
    if (db) {
      await db.workspaces.upsert({
        ...workspace,
        isDeleted: true,
        updatedAt: new Date().toISOString()
      });
    }
  };

  if (!db) {
    return <Box>Initializing database...</Box>;
  }

  return (
    <Box>
      <Box sx={{ mb: 4 }}>
        <WorkspaceForm
          workspace={editingWorkspace || undefined}
          onSubmit={editingWorkspace ? handleUpdate : handleCreate}
        />
      </Box>
      {editingWorkspace && (
        <Button onClick={() => setEditingWorkspace(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      )}
      <WorkspaceList
        db={db}
        onEdit={setEditingWorkspace}
        onDelete={handleDelete}
      />
    </Box>
  );
};

export default WorkspacesPageContent;