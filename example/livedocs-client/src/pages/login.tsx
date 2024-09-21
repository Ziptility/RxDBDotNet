// src\pages\login.tsx
import React, { useState, useEffect } from 'react';
import { FormControl, InputLabel, MenuItem, type SelectChangeEvent, Typography } from '@mui/material';
import { motion } from 'framer-motion';
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
  StyledForm,
} from '@/styles/StyledComponents';
import { motionProps, staggeredChildren } from '@/utils/motionSystem';

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
    <PageContainer maxWidth="sm">
      <motion.div {...motionProps['fadeIn']}>
        <ContentPaper elevation={3} sx={{ p: 4 }}>
          <motion.div {...staggeredChildren}>
            <motion.div {...motionProps['slideInFromBottom']}>
              <PageTitle variant="h4" align="center" gutterBottom>
                Sign in to LiveDocs
              </PageTitle>
            </motion.div>
            <StyledForm
              onSubmit={(e: React.FormEvent): void => {
                void handleSubmit(e);
              }}
            >
              <FormContainer>
                <motion.div {...motionProps['slideInFromBottom']}>
                  <FormControl fullWidth variant="outlined" sx={{ mb: 2 }}>
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
                          <Typography variant="body1">{workspace.name}</Typography>
                        </MenuItem>
                      ))}
                    </StyledSelect>
                  </FormControl>
                </motion.div>
                <motion.div {...motionProps['slideInFromBottom']}>
                  <FormControl fullWidth variant="outlined" sx={{ mb: 3 }}>
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
                            <Typography variant="body1">
                              {`${user.firstName} ${user.lastName}`}
                              <Typography component="span" variant="caption" sx={{ ml: 1, color: 'text.secondary' }}>
                                ({user.role})
                              </Typography>
                            </Typography>
                          </MenuItem>
                        ))}
                    </StyledSelect>
                  </FormControl>
                </motion.div>
                <motion.div {...motionProps['slideInFromBottom']}>
                  <PrimaryButton
                    type="submit"
                    disabled={!selectedWorkspace || !selectedUser}
                    fullWidth
                    size="large"
                    sx={{ py: 1.5 }}
                  >
                    Sign in
                  </PrimaryButton>
                </motion.div>
                {error ? (
                  <motion.div {...motionProps['fadeIn']}>
                    <ErrorText variant="body2" sx={{ mt: 2, textAlign: 'center' }}>
                      {error}
                    </ErrorText>
                  </motion.div>
                ) : null}
              </FormContainer>
            </StyledForm>
          </motion.div>
        </ContentPaper>
      </motion.div>
    </PageContainer>
  );
};

export default LoginPage;
