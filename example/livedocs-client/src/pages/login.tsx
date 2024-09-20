// src\pages\login.tsx
import React, { useState, useEffect } from 'react';
import { MenuItem, FormControl, InputLabel, type SelectChangeEvent } from '@mui/material';
import { useRouter } from 'next/router';
import { useAuth } from '@/contexts/AuthContext';
import type { Workspace, User } from '@/lib/schemas';
import {
  PageContainer,
  ContentPaper,
  PageTitle,
  FormContainer,
  PrimaryButton,
  CircularProgress,
  CenteredBox,
  ErrorText,
  Select,
} from '@/styles/StyledComponents';

const LoginPage: React.FC = (): JSX.Element => {
  const [selectedWorkspace, setSelectedWorkspace] = useState<string>('');
  const [selectedUser, setSelectedUser] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const { login, workspaces, users, isLoggedIn, isInitialized } = useAuth();
  const router = useRouter();

  console.log('LoginPage: Render', {
    isLoggedIn,
    isInitialized,
    isLoading,
    workspacesCount: workspaces.length,
  });

  useEffect(() => {
    console.log('LoginPage: useEffect (initialization)');
    if (isInitialized) {
      setIsLoading(false);
    }
  }, [isInitialized]);

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
      <CenteredBox sx={{ height: '100vh' }}>
        <CircularProgress />
      </CenteredBox>
    );
  }

  console.log('LoginPage: Rendering login form');
  return (
    <PageContainer>
      <ContentPaper sx={{ maxWidth: 400, mx: 'auto', mt: 4, p: 3 }}>
        <PageTitle variant="h5" component="h1" sx={{ mb: 3, textAlign: 'center' }}>
          Sign in to LiveDocs
        </PageTitle>
        <FormContainer
          as="form"
          onSubmit={(e: React.FormEvent): void => {
            void handleSubmit(e);
          }}
          sx={{ width: '100%' }}
        >
          <FormControl fullWidth sx={{ mb: 2 }}>
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
          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel id="user-select-label">User</InputLabel>
            <Select
              labelId="user-select-label"
              id="user-select"
              value={selectedUser}
              label="User"
              onChange={handleUserChange}
              disabled={!selectedWorkspace}
            >
              {users
                .filter((user: User) => user.workspaceId === selectedWorkspace)
                .map((user: User) => (
                  <MenuItem
                    key={user.id}
                    value={user.id}
                  >{`${user.firstName} ${user.lastName} (${user.role})`}</MenuItem>
                ))}
            </Select>
          </FormControl>
          <PrimaryButton
            type="submit"
            fullWidth
            variant="contained"
            sx={{ mt: 2, mb: 2 }}
            disabled={!selectedWorkspace || !selectedUser}
          >
            Sign In
          </PrimaryButton>
          {error ? <ErrorText sx={{ textAlign: 'center' }}>{error}</ErrorText> : null}
        </FormContainer>
      </ContentPaper>
    </PageContainer>
  );
};

export default LoginPage;
