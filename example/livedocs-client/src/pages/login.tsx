import React, { useState, useEffect, useCallback, useRef } from 'react';
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

const LoginPage: React.FC = (): JSX.Element => {
  const [selectedWorkspace, setSelectedWorkspace] = useState<string>('');
  const [selectedUser, setSelectedUser] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const { login, fetchWorkspaces, fetchUsers, workspaces, users, isLoggedIn, isInitialized } = useAuth();
  const router = useRouter();
  const hasFetchedWorkspaces = useRef<boolean>(false);

  console.log('LoginPage: Render', { isLoggedIn, isInitialized, isLoading, workspacesCount: workspaces.length });

  const initFetch = useCallback(async (): Promise<void> => {
    console.log('LoginPage: initFetch called', { hasFetchedWorkspaces: hasFetchedWorkspaces.current });
    if (isInitialized && !hasFetchedWorkspaces.current) {
      setIsLoading(true);
      await fetchWorkspaces();
      hasFetchedWorkspaces.current = true;
      setIsLoading(false);
    }
  }, [isInitialized, fetchWorkspaces]);

  useEffect(() => {
    console.log('LoginPage: useEffect (initFetch)');
    void initFetch();
  }, [initFetch]);

  useEffect(() => {
    console.log('LoginPage: useEffect (fetchUsers)', { selectedWorkspace });
    if (selectedWorkspace) {
      void fetchUsers(selectedWorkspace);
    }
  }, [selectedWorkspace, fetchUsers]);

  useEffect(() => {
    console.log('LoginPage: useEffect (redirect)', { isLoggedIn, isInitialized });
    if (isLoggedIn && isInitialized) {
      void router.push('/');
    }
  }, [isLoggedIn, isInitialized, router]);

  const handleWorkspaceChange = (event: SelectChangeEvent): void => {
    console.log('LoginPage: handleWorkspaceChange', event.target.value);
    setSelectedWorkspace(event.target.value);
    setSelectedUser('');
  };

  const handleUserChange = (event: SelectChangeEvent): void => {
    console.log('LoginPage: handleUserChange', event.target.value);
    setSelectedUser(event.target.value);
  };

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    console.log('LoginPage: handleSubmit', { selectedWorkspace, selectedUser });
    setIsLoading(true);
    setError('');
    try {
      await login(selectedUser, selectedWorkspace);
      console.log('LoginPage: Login successful');
    } catch (err) {
      console.error('LoginPage: Login error', err);
      setError('Invalid user or workspace');
    } finally {
      setIsLoading(false);
    }
  };

  if (!isInitialized || isLoading) {
    console.log('LoginPage: Showing loading spinner');
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100vh">
        <CircularProgress />
      </Box>
    );
  }

  console.log('LoginPage: Rendering login form');
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
