// src/pages/login.tsx
import React, { useState, useEffect } from 'react';
import { FormControl, InputLabel, MenuItem, Select, type SelectChangeEvent } from '@mui/material';
import { useRouter } from 'next/router';
import { FormLayout, FormError, SubmitButton } from '@/components/FormComponents';
import { useAuth } from '@/contexts/AuthContext';
import { PageContainer, ContentPaper } from '@/styles/StyledComponents';

const LoginPage: React.FC = () => {
  const [selectedWorkspace, setSelectedWorkspace] = useState<string>('');
  const [selectedUser, setSelectedUser] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [isValid, setIsValid] = useState<boolean>(false);
  const { login, workspaces, users, isLoggedIn, isInitialized } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoggedIn && isInitialized) {
      void router.push('/');
    }
  }, [isLoggedIn, isInitialized, router]);

  useEffect(() => {
    setIsValid(!!selectedWorkspace && !!selectedUser);
  }, [selectedWorkspace, selectedUser]);

  const handleWorkspaceChange = (event: SelectChangeEvent): void => {
    setSelectedWorkspace(event.target.value);
    setSelectedUser('');
  };

  const handleUserChange = (event: SelectChangeEvent): void => {
    setSelectedUser(event.target.value);
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    if (!isValid) return;
    setIsSubmitting(true);
    setError(null);
    try {
      await login(selectedUser, selectedWorkspace);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid user or workspace');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isInitialized) {
    return null; // or a loading spinner
  }

  return (
    <PageContainer maxWidth="sm">
      <ContentPaper>
        <FormLayout
          title="Sign in to LiveDocs"
          onSubmit={(e: React.FormEvent<HTMLFormElement>): void => {
            void handleSubmit(e);
          }}
        >
          <FormControl fullWidth>
            <InputLabel id="workspace-select-label">Workspace</InputLabel>
            <Select
              labelId="workspace-select-label"
              value={selectedWorkspace}
              label="Workspace"
              onChange={handleWorkspaceChange}
            >
              {workspaces.map((workspace) => (
                <MenuItem key={workspace.id} value={workspace.id}>
                  {workspace.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl fullWidth>
            <InputLabel id="user-select-label">User</InputLabel>
            <Select
              labelId="user-select-label"
              value={selectedUser}
              label="User"
              onChange={handleUserChange}
              disabled={!selectedWorkspace}
            >
              {users
                .filter((user) => user.workspaceId === selectedWorkspace)
                .map((user) => (
                  <MenuItem key={user.id} value={user.id}>
                    {`${user.firstName} ${user.lastName} (${user.role})`}
                  </MenuItem>
                ))}
            </Select>
          </FormControl>
          <FormError error={error} />
          <SubmitButton label="Sign in" isSubmitting={isSubmitting} isValid={isValid} />
        </FormLayout>
      </ContentPaper>
    </PageContainer>
  );
};

export default LoginPage;
