// src\components\WorkspaceForm.tsx
import React, { useState, useEffect } from 'react';
import { TextField, Button, Box } from '@mui/material';
import type { Workspace } from '@/lib/schemas';

interface WorkspaceFormProps {
  readonly workspace: Workspace | undefined;
  readonly onSubmit: (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>) => Promise<void>;
}

const WorkspaceForm: React.FC<WorkspaceFormProps> = ({ workspace, onSubmit }) => {
  const [name, setName] = useState<string>('');

  useEffect(() => {
    if (workspace) {
      setName(workspace.name);
    } else {
      setName('');
    }
  }, [workspace]);

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    void onSubmit({ name });
    if (!workspace) {
      setName('');
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <TextField label="Workspace Name" value={name} onChange={(e): void => setName(e.target.value)} required />
        <Button type="submit" variant="contained">
          {workspace ? 'Update' : 'Create'} Workspace
        </Button>
      </Box>
    </form>
  );
};

export default WorkspaceForm;
