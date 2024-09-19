// src\components\UserForm.tsx
import React, { useState, useEffect } from 'react';
import { TextField, Button, Box, Select, MenuItem, InputLabel, FormControl } from '@mui/material';
import type { User, Workspace } from '@/lib/schemas';
import { UserRole } from '@/lib/schemas';

interface UserFormProps {
  user: User | undefined;
  workspaces: Workspace[];
  onSubmit: (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>) => Promise<void>;
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
      // Reset form when user is undefined (creating a new user)
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
      // Reset form after submission when creating a new user
      setFirstName('');
      setLastName('');
      setEmail('');
      setRole(UserRole.StandardUser);
      setWorkspaceId('');
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <TextField label="First Name" value={firstName} onChange={(e): void => setFirstName(e.target.value)} required />
        <TextField label="Last Name" value={lastName} onChange={(e): void => setLastName(e.target.value)} required />
        <TextField label="Email" type="email" value={email} onChange={(e): void => setEmail(e.target.value)} required />
        <FormControl fullWidth>
          <InputLabel>Role</InputLabel>
          <Select value={role} onChange={(e): void => setRole(e.target.value as UserRole)} required>
            {Object.values(UserRole).map((roleValue) => (
              <MenuItem key={roleValue} value={roleValue}>
                {roleValue}
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
        <Button type="submit" variant="contained">
          {user ? 'Update' : 'Create'} User
        </Button>
      </Box>
    </form>
  );
};

export default UserForm;
