import React, { useState, useEffect } from 'react';
import type { Workspace } from '@/lib/schemas';
import { FormContainer, StyledTextField, PrimaryButton, StyledForm } from '@/styles/StyledComponents';

interface WorkspaceFormProps {
  readonly workspace: Workspace | undefined;
  readonly onSubmit: (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>) => Promise<void>;
}

const WorkspaceForm: React.FC<WorkspaceFormProps> = ({ workspace, onSubmit }) => {
  const [name, setName] = useState<string>('');
  const [isFormValid, setIsFormValid] = useState<boolean>(false);

  useEffect(() => {
    if (workspace) {
      setName(workspace.name);
    } else {
      setName('');
    }
  }, [workspace]);

  useEffect(() => {
    setIsFormValid(name.trim() !== '');
  }, [name]);

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    if (isFormValid) {
      void onSubmit({ name });
      if (!workspace) {
        setName('');
      }
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
        <PrimaryButton type="submit" variant="contained" disabled={!isFormValid}>
          {workspace ? 'Update' : 'Create'} Workspace
        </PrimaryButton>
      </FormContainer>
    </StyledForm>
  );
};

export default WorkspaceForm;
