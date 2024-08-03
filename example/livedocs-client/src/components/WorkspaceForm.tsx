import React, { useState, useEffect } from 'react';
import { TextField, Button, Box } from '@mui/material';
import { WorkspaceDocType } from '@/lib/schemas';

interface WorkspaceFormProps {
  workspace?: WorkspaceDocType;
  onSubmit: (workspace: Omit<WorkspaceDocType, 'id' | 'updatedAt' | 'isDeleted'>) => void;
}

const WorkspaceForm: React.FC<WorkspaceFormProps> = ({ workspace, onSubmit }) => {
  const [name, setName] = useState('');

  useEffect(() => {
    if (workspace) {
      setName(workspace.name);
    }
  }, [workspace]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ name });
    setName('');
  };

  return (
    <form onSubmit={handleSubmit}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <TextField
          label="Workspace Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
        <Button type="submit" variant="contained">
          {workspace ? 'Update' : 'Create'} Workspace
        </Button>
      </Box>
    </form>
  );
};

export default WorkspaceForm;