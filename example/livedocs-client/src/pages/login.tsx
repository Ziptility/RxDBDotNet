import React, { useState, useEffect } from 'react';
import { FormControl, InputLabel, MenuItem, type SelectChangeEvent } from '@mui/material';
import { useRouter } from 'next/router';
import { useAuth } from '@/contexts/AuthContext';
import type { Workspace, User } from '@/lib/schemas';
import {
  PageContainer,
  ContentPaper,
  PageTitle,
  FormContainer,
  PrimaryButton,
  StyledCircularProgress,
  CenteredBox,
  ErrorText,
  StyledSelect,
} from '@/styles/StyledComponents';

const LoginPage: React.FC = () => {
  const [selectedWorkspace, setSelectedWorkspace] = useState<string>('');
  const [selectedUser, setSelectedUser] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const { login, workspaces, users, isLoggedIn, isInitialized } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isInitialized) {
      setIsLoading(false);
    }
  }, [isInitialized]);

  useEffect(() => {
    if (isLoggedIn && isInitialized) {
      void router.push('/');
    }
  }, [isLoggedIn, isInitialized, router]);

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
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid user or workspace');
    } finally {
      setIsLoading(false);
    }
  };

  if (!isInitialized || isLoading) {
    return (
      <CenteredBox sx={{ height: '100vh' }}>
        <StyledCircularProgress />
      </CenteredBox>
    );
  }

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
          <FormControl fullWidth>
            <InputLabel id="workspace-select-label">Workspace</InputLabel>
            <StyledSelect
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
            </StyledSelect>
          </FormControl>
          <FormControl fullWidth>
            <InputLabel id="user-select-label">User</InputLabel>
            <StyledSelect
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
                  <MenuItem key={user.id} value={user.id}>
                    {`${user.firstName} ${user.lastName} (${user.role})`}
                  </MenuItem>
                ))}
            </StyledSelect>
          </FormControl>
          <PrimaryButton type="submit" fullWidth disabled={!selectedWorkspace || !selectedUser}>
            Sign In
          </PrimaryButton>
          {error ? <ErrorText sx={{ mt: 2, textAlign: 'center' }}>{error}</ErrorText> : null}
        </FormContainer>
      </ContentPaper>
    </PageContainer>
  );
};

export default LoginPage;
