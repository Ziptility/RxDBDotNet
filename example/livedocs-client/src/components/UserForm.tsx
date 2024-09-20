import React, { useState, useEffect } from 'react';
import { MenuItem } from '@mui/material';
import type { User, Workspace } from '@/lib/schemas';
import { UserRole } from '@/lib/schemas';
import {
  FormContainer,
  PrimaryButton,
  StyledForm,
  StyledFormControl,
  StyledInputLabel,
  StyledSelect,
  StyledTextField,
} from '@/styles/StyledComponents';

interface UserFormProps {
  readonly user: User | undefined;
  readonly workspaces: Workspace[];
  readonly onSubmit: (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>) => Promise<void>;
}

const UserForm: React.FC<UserFormProps> = ({ user, workspaces, onSubmit }) => {
  const [firstName, setFirstName] = useState<string>('');
  const [lastName, setLastName] = useState<string>('');
  const [email, setEmail] = useState<string>('');
  const [role, setRole] = useState<UserRole>(UserRole.StandardUser);
  const [workspaceId, setWorkspaceId] = useState<string>('');

  useEffect(() => {
    if (user) {
      setFirstName(user.firstName);
      setLastName(user.lastName);
      setEmail(user.email);
      setRole(user.role);
      setWorkspaceId(user.workspaceId);
    } else {
      setFirstName('');
      setLastName('');
      setEmail('');
      setRole(UserRole.StandardUser);
      setWorkspaceId('');
    }
  }, [user]);

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    void onSubmit({ firstName, lastName, email, role, workspaceId });
    if (!user) {
      setFirstName('');
      setLastName('');
      setEmail('');
      setRole(UserRole.StandardUser);
      setWorkspaceId('');
    }
  };

  return (
    <StyledForm onSubmit={handleSubmit}>
      <FormContainer>
        <StyledTextField
          label="First Name"
          value={firstName}
          onChange={(e): void => setFirstName(e.target.value)}
          required
          fullWidth
        />
        <StyledTextField
          label="Last Name"
          value={lastName}
          onChange={(e): void => setLastName(e.target.value)}
          required
          fullWidth
        />
        <StyledTextField
          label="Email"
          type="email"
          value={email}
          onChange={(e): void => setEmail(e.target.value)}
          required
          fullWidth
        />
        <StyledFormControl>
          <StyledInputLabel id="role-select-label">Role</StyledInputLabel>
          <StyledSelect
            labelId="role-select-label"
            value={role}
            onChange={(e): void => setRole(e.target.value as UserRole)}
            label="Role"
            required
          >
            {Object.values(UserRole).map((roleValue) => (
              <MenuItem key={roleValue} value={roleValue}>
                {roleValue}
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
          {user ? 'Update' : 'Create'} User
        </PrimaryButton>
      </FormContainer>
    </StyledForm>
  );
};

export default UserForm;
