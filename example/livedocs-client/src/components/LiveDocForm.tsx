import React, { useState, useEffect } from 'react';
import { TextField, Button, Box, Select, MenuItem, InputLabel, FormControl } from '@mui/material';
import type { LiveDoc, User, Workspace } from '@/lib/schemas';

interface LiveDocFormProps {
  readonly liveDoc: LiveDoc | undefined;
  readonly users: User[];
  readonly workspaces: Workspace[];
  readonly onSubmit: (liveDoc: Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>) => Promise<void>;
}

const LiveDocForm: React.FC<LiveDocFormProps> = ({ liveDoc, users, workspaces, onSubmit }) => {
  const [content, setContent] = useState<string>('');
  const [ownerId, setOwnerId] = useState<string>('');
  const [workspaceId, setWorkspaceId] = useState<string>('');
  const [isFormValid, setIsFormValid] = useState<boolean>(false);

  useEffect(() => {
    if (liveDoc) {
      setContent(liveDoc.content);
      setOwnerId(liveDoc.ownerId);
      setWorkspaceId(liveDoc.workspaceId);
    } else {
      setContent('');
      setOwnerId('');
      setWorkspaceId('');
    }
  }, [liveDoc]);

  useEffect(() => {
    setIsFormValid(content.trim() !== '' && ownerId !== '' && workspaceId !== '');
  }, [content, ownerId, workspaceId]);

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    if (isFormValid) {
      void onSubmit({ content, ownerId, workspaceId });
      if (!liveDoc) {
        setContent('');
        setOwnerId('');
        setWorkspaceId('');
      }
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <TextField
          label="Content"
          multiline
          rows={4}
          value={content}
          onChange={(e): void => setContent(e.target.value)}
          required
        />
        <FormControl fullWidth>
          <InputLabel>Owner</InputLabel>
          <Select value={ownerId} onChange={(e): void => setOwnerId(e.target.value)} required>
            {users.map((user) => (
              <MenuItem key={user.id} value={user.id}>
                {`${user.firstName} ${user.lastName}`}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl fullWidth>
          <InputLabel>Workspace</InputLabel>
          <Select value={workspaceId} onChange={(e): void => setWorkspaceId(e.target.value)} required>
            {workspaces.map((workspace) => (
              <MenuItem key={workspace.id} value={workspace.id}>
                {workspace.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Button type="submit" variant="contained" disabled={!isFormValid}>
          {liveDoc ? 'Update' : 'Create'} LiveDoc
        </Button>
      </Box>
    </form>
  );
};

export default LiveDocForm;
