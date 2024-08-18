import React, { useState, useEffect } from 'react';
import { TextField, Button, Box, Select, MenuItem, InputLabel, FormControl } from '@mui/material';
import { UserDocType, UserRole } from '@/lib/schemas';

interface UserFormProps {
  user?: UserDocType | undefined;
  workspaces: { id: string; name: string }[];
  onSubmit: (user: Omit<UserDocType, 'id' | 'updatedAt' | 'isDeleted'>) => Promise<void>;
}

const UserForm: React.FC<UserFormProps> = ({ user, workspaces, onSubmit }): JSX.Element => {
  const [firstName, setFirstName] = useState<string>('');
  const [lastName, setLastName] = useState<string>('');
  const [email, setEmail] = useState<string>('');
  const [role, setRole] = useState<UserRole>(UserRole.User);
  const [workspaceId, setWorkspaceId] = useState<string>('');

  useEffect((): void => {
    if (user) {
      setFirstName(user.firstName);
      setLastName(user.lastName);
      setEmail(user.email);
      // setRole(user.role as UserRole);
      setWorkspaceId(user.workspaceId);
    }
  }, [user]);

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    void onSubmit({ firstName, lastName, email, workspaceId });
    if (!user) {
      setFirstName('');
      setLastName('');
      setEmail('');
      setRole(UserRole.User);
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
