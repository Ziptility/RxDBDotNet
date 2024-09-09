import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/router';
import {
  Button,
  Typography,
  Box,
  Container,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  SelectChangeEvent,
  CircularProgress,
} from '@mui/material';
import { useAuth } from '@/contexts/AuthContext';
import { Workspace, User } from '@/lib/schemas';

const LoginPage: React.FC = () => {
  const [selectedWorkspace, setSelectedWorkspace] = useState<string>('');
  const [selectedUser, setSelectedUser] = useState<string>('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { login, fetchWorkspaces, fetchUsers, workspaces, users, isLoggedIn } = useAuth();
  const router = useRouter();

  useEffect(() => {
    const initFetch = async (): Promise<void> => {
      setIsLoading(true);
      await fetchWorkspaces();
      setIsLoading(false);
    };
    void initFetch();
  }, [fetchWorkspaces]);

  useEffect(() => {
    if (selectedWorkspace) {
      void fetchUsers(selectedWorkspace);
    }
  }, [selectedWorkspace, fetchUsers]);

  useEffect(() => {
    if (isLoggedIn) {
      void router.push('/');
    }
  }, [isLoggedIn, router]);

  const handleWorkspaceChange = (event: SelectChangeEvent): void => {
    setSelectedWorkspace(event.target.value);
    setSelectedUser('');
  };

  const handleUserChange = (event: SelectChangeEvent): void => {
    setSelectedUser(event.target.value);
  };

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    setIsLoading(true);
    setError('');
    try {
      await login(selectedUser, selectedWorkspace);
      // No need to redirect here, the useEffect hook will handle it
    } catch (err) {
      setError('Invalid user or workspace');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100vh">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container component="main" maxWidth="xs">
      <Box
        sx={{
          marginTop: 8,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Typography component="h1" variant="h5">
          Sign in to LiveDocs
        </Typography>
        <Box
          component="form"
          onSubmit={(e: React.FormEvent): void => {
            void handleSubmit(e);
          }}
          noValidate
          sx={{ mt: 1, width: '100%' }}
        >
          <FormControl fullWidth margin="normal">
            <InputLabel id="workspace-select-label">Workspace</InputLabel>
            <Select
              labelId="workspace-select-label"
              id="workspace-select"
              value={selectedWorkspace}
              label="Workspace"
              onChange={handleWorkspaceChange}
            >
              {workspaces.map((workspace: Workspace) => (
                <MenuItem key={workspace.id} value={workspace.id}>
                  {workspace.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl fullWidth margin="normal">
            <InputLabel id="user-select-label">User</InputLabel>
            <Select
              labelId="user-select-label"
              id="user-select"
              value={selectedUser}
              label="User"
              onChange={handleUserChange}
              disabled={!selectedWorkspace}
            >
              {users.map((user: User) => (
                <MenuItem key={user.id} value={user.id}>{`${user.firstName} ${user.lastName} (${user.role})`}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <Button
            type="submit"
            fullWidth
            variant="contained"
            sx={{ mt: 3, mb: 2 }}
            disabled={!selectedWorkspace || !selectedUser}
          >
            Sign In
          </Button>
          {error && (
            <Typography color="error" align="center">
              {error}
            </Typography>
          )}
        </Box>
      </Box>
    </Container>
  );
};

export default LoginPage;
