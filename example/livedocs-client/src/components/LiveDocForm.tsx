import React, { useState, useEffect } from 'react';
import { MenuItem } from '@mui/material';
import type { LiveDoc, User, Workspace } from '@/lib/schemas';
import {
  FormContainer,
  PrimaryButton,
  StyledForm,
  StyledFormControl,
  StyledInputLabel,
  StyledSelect,
  StyledTextField,
} from '@/styles/StyledComponents';

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

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    void onSubmit({ content, ownerId, workspaceId });
    if (!liveDoc) {
      setContent('');
      setOwnerId('');
      setWorkspaceId('');
    }
  };

  return (
    <StyledForm onSubmit={handleSubmit}>
      <FormContainer>
        <StyledTextField
          label="Content"
          multiline
          rows={4}
          value={content}
          onChange={(e): void => setContent(e.target.value)}
          required
          fullWidth
        />
        <StyledFormControl>
          <StyledInputLabel id="owner-select-label">Owner</StyledInputLabel>
          <StyledSelect
            labelId="owner-select-label"
            value={ownerId}
            onChange={(e): void => setOwnerId(e.target.value)}
            label="Owner"
            required
          >
            {users.map((user) => (
              <MenuItem key={user.id} value={user.id}>
                {`${user.firstName} ${user.lastName}`}
              </MenuItem>
            ))}
          </StyledSelect>
        </StyledFormControl>
        <StyledFormControl>
          <StyledInputLabel id="workspace-select-label">Workspace</StyledInputLabel>
          <StyledSelect
            labelId="workspace-select-label"
            value={workspaceId}
            onChange={(e): void => setWorkspaceId(e.target.value)}
            label="Workspace"
            required
          >
            {workspaces.map((workspace) => (
              <MenuItem key={workspace.id} value={workspace.id}>
                {workspace.name}
              </MenuItem>
            ))}
          </StyledSelect>
        </StyledFormControl>
        <PrimaryButton type="submit" variant="contained">
          {liveDoc ? 'Update' : 'Create'} LiveDoc
        </PrimaryButton>
      </FormContainer>
    </StyledForm>
  );
};

export default LiveDocForm;
