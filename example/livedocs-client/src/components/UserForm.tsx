import React, { useState, useEffect } from 'react';
import { TextField, Button, Box, Select, MenuItem, InputLabel, FormControl } from '@mui/material';
import { User } from '@/types';

interface UserFormProps {
  user?: User;
  workspaces: { id: string; name: string }[];
  onSubmit: (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>) => void;
}

const UserForm: React.FC<UserFormProps> = ({ user, workspaces, onSubmit }) => {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<'User' | 'Admin' | 'SuperAdmin'>('User');
  const [workspaceId, setWorkspaceId] = useState('');

  useEffect(() => {
    if (user) {
      setFirstName(user.firstName);
      setLastName(user.lastName);
      setEmail(user.email);
      setRole(user.role);
      setWorkspaceId(user.workspaceId);
    }
  }, [user]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ firstName, lastName, email, role, workspaceId });
    setFirstName('');
    setLastName('');
    setEmail('');
    setRole('User');
    setWorkspaceId('');
  };

  return (
    <form onSubmit={handleSubmit}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <TextField
          label="First Name"
          value={firstName}
          onChange={(e) => setFirstName(e.target.value)}
          required
        />
        <TextField
          label="Last Name"
          value={lastName}
          onChange={(e) => setLastName(e.target.value)}
          required
        />
        <TextField
          label="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <FormControl fullWidth>
          <InputLabel>Role</InputLabel>
          <Select
            value={role}
            onChange={(e) => setRole(e.target.value as 'User' | 'Admin' | 'SuperAdmin')}
            required
          >
            <MenuItem value="User">User</MenuItem>
            <MenuItem value="Admin">Admin</MenuItem>
            <MenuItem value="SuperAdmin">SuperAdmin</MenuItem>
          </Select>
        </FormControl>
        <FormControl fullWidth>
          <InputLabel>Workspace</InputLabel>
          <Select
            value={workspaceId}
            onChange={(e) => setWorkspaceId(e.target.value)}
            required
          >
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