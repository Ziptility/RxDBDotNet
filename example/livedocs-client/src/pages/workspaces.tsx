import React, { useState, useEffect } from 'react';
import { Typography, Box, Button } from '@mui/material';
import WorkspaceList from '../components/WorkspaceList';
import WorkspaceForm from '../components/WorkspaceForm';
import { getDatabase } from '../lib/database';
import { setupReplication } from '../lib/replication';
import { Workspace } from '../types';

const WorkspacesPage: React.FC = () => {
  const [db, setDb] = useState<Awaited<ReturnType<typeof getDatabase>> | null>(null);
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);

  useEffect(() => {
    const initDb = async () => {
      const database = await getDatabase();
      await setupReplication(database);
      setDb(database);
    };
    initDb();
  }, []);

  const handleCreate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db) {
      await db.workspaces.insert({
        id: Date.now().toString(),
        ...workspace,
        updatedAt: new Date().toISOString(),
        isDeleted: false
      });
    }
  };

  const handleUpdate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>) => {
    if (db && editingWorkspace) {
      await db.workspaces.atomicUpdate(editingWorkspace.id, (oldDoc) => {
        oldDoc.name = workspace.name;
        oldDoc.updatedAt = new Date().toISOString();
        return oldDoc;
      });
      setEditingWorkspace(null);
    }
  };

  const handleDelete = async (workspace: Workspace) => {
    if (db) {
      await db.workspaces.atomicUpdate(workspace.id, (oldDoc) => {
        oldDoc.isDeleted = true;
        oldDoc.updatedAt = new Date().toISOString();
        return oldDoc;
      });
    }
  };

  if (!db) {
    return <Typography>Loading...</Typography>;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Workspaces
      </Typography>
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

export default WorkspacesPage;