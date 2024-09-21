// src\components\UserForm.tsx
import React, { useState, useEffect } from 'react';
import { Grid, MenuItem, FormControl, InputLabel, Select } from '@mui/material';
import type { User, Workspace } from '@/lib/schemas';
import { UserRole } from '@/lib/schemas';
import { FormContainer, PrimaryButton, TextField } from '@/styles/StyledComponents';

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
  const [isFormValid, setIsFormValid] = useState<boolean>(false);

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

  useEffect(() => {
    setIsFormValid(firstName.trim() !== '' && lastName.trim() !== '' && email.trim() !== '' && workspaceId !== '');
  }, [firstName, lastName, email, workspaceId]);

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    if (isFormValid) {
      void onSubmit({ firstName, lastName, email, role, workspaceId });
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <FormContainer>
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <TextField
              label="First Name"
              value={firstName}
              onChange={(e): void => setFirstName(e.target.value)}
              required
              fullWidth
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              label="Last Name"
              value={lastName}
              onChange={(e): void => setLastName(e.target.value)}
              required
              fullWidth
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              label="Email"
              type="email"
              value={email}
              onChange={(e): void => setEmail(e.target.value)}
              required
              fullWidth
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <FormControl fullWidth>
              <InputLabel id="role-select-label">Role</InputLabel>
              <Select
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
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6}>
            <FormControl fullWidth>
              <InputLabel id="workspace-select-label">Workspace</InputLabel>
              <Select
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
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12}>
            <PrimaryButton type="submit" variant="contained" disabled={!isFormValid} fullWidth>
              {user ? 'Update' : 'Create'} User
            </PrimaryButton>
          </Grid>
        </Grid>
      </FormContainer>
    </form>
  );
};

export default UserForm;
