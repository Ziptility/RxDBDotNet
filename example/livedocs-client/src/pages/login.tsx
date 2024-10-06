// example/livedocs-client/src/pages/login.tsx
import React, { useEffect } from 'react';
import { FormControl, InputLabel, MenuItem, Select, Typography, Button } from '@mui/material';
import { motion } from 'framer-motion';
import { useForm, Controller } from 'react-hook-form';
import { useRouter } from 'next/router';
import { FormLayout, FormError } from '@/components/FormComponents';
import { useAuth } from '@/contexts/AuthContext';
import type { Workspace, User } from '@/generated/graphql';
import {
  PageContainer,
  ContentPaper,
  PageTitle,
  FormContainer,
  StyledCircularProgress,
  CenteredBox,
} from '@/styles/StyledComponents';
import { motionProps, staggeredChildren } from '@/utils/motionSystem';

interface LoginFormData {
  workspaceId: string;
  userId: string;
}

const LoginPage: React.FC = () => {
  const { login, workspaces, users, isLoggedIn, isInitialized } = useAuth();
  const router = useRouter();

  const {
    control,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting, isValid },
    reset,
  } = useForm<LoginFormData>({
    defaultValues: {
      workspaceId: '',
      userId: '',
    },
    mode: 'onChange',
  });

  const selectedWorkspaceId = watch('workspaceId');

  useEffect(() => {
    if (isLoggedIn && isInitialized) {
      void router.push('/');
    }
  }, [isLoggedIn, isInitialized, router]);

  useEffect(() => {
    // Reset userId when workspaceId changes
    reset((formValues) => ({
      ...formValues,
      userId: '',
    }));
  }, [selectedWorkspaceId, reset]);

  const onSubmit = handleSubmit(async (data) => {
    try {
      await login(data.userId, data.workspaceId);
    } catch (err) {
      console.error('Login error:', err);
    }
  });

  if (!isInitialized) {
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
            <FormLayout
              title=""
              onSubmit={(e) => {
                void onSubmit(e);
              }}
            >
              <FormContainer>
                <motion.div {...motionProps['slideInFromBottom']}>
                  <Controller
                    name="workspaceId"
                    control={control}
                    rules={{ required: 'Workspace is required' }}
                    render={({ field }) => (
                      <FormControl fullWidth error={!!errors.workspaceId}>
                        <InputLabel id="workspace-select-label">Workspace</InputLabel>
                        <Select {...field} labelId="workspace-select-label" label="Workspace">
                          {workspaces.map((workspace: Workspace) => (
                            <MenuItem key={workspace.id} value={workspace.id}>
                              <Typography variant="body1">{workspace.name}</Typography>
                            </MenuItem>
                          ))}
                        </Select>
                        {errors.workspaceId ? <FormError error={errors.workspaceId.message ?? null} /> : null}
                      </FormControl>
                    )}
                  />
                </motion.div>
                <motion.div {...motionProps['slideInFromBottom']}>
                  <Controller
                    name="userId"
                    control={control}
                    rules={{ required: 'User is required' }}
                    render={({ field }) => (
                      <FormControl fullWidth error={!!errors.userId}>
                        <InputLabel id="user-select-label">User</InputLabel>
                        <Select {...field} labelId="user-select-label" label="User" disabled={!selectedWorkspaceId}>
                          {users
                            .filter((user: User) => user.workspaceId === selectedWorkspaceId)
                            .map((user: User) => (
                              <MenuItem key={user.id} value={user.id}>
                                <Typography variant="body1">
                                  {`${user.firstName} ${user.lastName}`}
                                  <Typography
                                    component="span"
                                    variant="caption"
                                    sx={{ ml: 1, color: 'text.secondary' }}
                                  >
                                    ({user.role})
                                  </Typography>
                                </Typography>
                              </MenuItem>
                            ))}
                        </Select>
                        {errors.userId ? <FormError error={errors.userId.message ?? null} /> : null}
                      </FormControl>
                    )}
                  />
                </motion.div>
                <motion.div {...motionProps['slideInFromBottom']}>
                  <Button
                    type="submit"
                    variant="contained"
                    color="primary"
                    disabled={isSubmitting || !isValid}
                    fullWidth
                    size="large"
                    sx={{ py: 1.5 }}
                  >
                    Sign in
                  </Button>
                </motion.div>
              </FormContainer>
            </FormLayout>
          </motion.div>
        </ContentPaper>
      </motion.div>
    </PageContainer>
  );
};

export default LoginPage;
