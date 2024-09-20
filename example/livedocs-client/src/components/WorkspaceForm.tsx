import React, { useState, useEffect } from 'react';
import type { Workspace } from '@/lib/schemas';
import { FormContainer, StyledTextField, PrimaryButton, StyledForm } from '@/styles/StyledComponents';

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
    <StyledForm onSubmit={handleSubmit}>
      <FormContainer>
        <StyledTextField
          label="Workspace Name"
          value={name}
          onChange={(e): void => setName(e.target.value)}
          required
          fullWidth
        />
        <PrimaryButton type="submit" variant="contained">
          {workspace ? 'Update' : 'Create'} Workspace
        </PrimaryButton>
      </FormContainer>
    </StyledForm>
  );
};

export default WorkspaceForm;
