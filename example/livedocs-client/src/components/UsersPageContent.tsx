import React, { useState, useCallback } from 'react';
import { Add as AddIcon } from '@mui/icons-material';
import { Box, Fab, Tooltip } from '@mui/material';
import { motion, AnimatePresence } from 'framer-motion';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { User, Workspace } from '@/lib/schemas';
import {
  ContentPaper,
  SectionTitle,
  ListContainer,
  StyledAlert,
  StyledCircularProgress,
  CenteredBox,
} from '@/styles/StyledComponents';
import { motionProps, staggeredChildren } from '@/utils/motionSystem';
import UserForm from './UserForm';
import UserList from './UserList';

const UsersPageContent: React.FC = () => {
  const [isCreating, setIsCreating] = useState(false);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const {
    documents: users,
    isLoading: isLoadingUsers,
    error: userError,
    upsertDocument: upsertUser,
    deleteDocument: deleteUser,
  } = useDocuments<User>('user');

  const { documents: workspaces, isLoading: isLoadingWorkspaces } = useDocuments<Workspace>('workspace');

  const handleSubmit = useCallback(
    async (userData: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingUserId !== null) {
        const existingUser = users.find((u) => u.id === editingUserId);
        if (existingUser) {
          const updatedUser: User = {
            ...existingUser,
            ...userData,
            updatedAt: new Date().toISOString(),
          };
          await upsertUser(updatedUser);
          setEditingUserId(null);
        }
      } else {
        const newUser: User = {
          id: uuidv4(),
          ...userData,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        };
        await upsertUser(newUser);
        setIsCreating(false);
      }
    },
    [editingUserId, users, upsertUser]
  );

  const handleEdit = useCallback((userId: string) => {
    setEditingUserId(userId);
  }, []);

  const handleCancelEdit = useCallback(() => {
    setEditingUserId(null);
  }, []);

  const handleDelete = useCallback(
    (userId: string) => {
      void deleteUser(userId);
    },
    [deleteUser]
  );

  const handleCreateNew = useCallback(() => {
    setIsCreating(true);
  }, []);

  const handleCancelCreate = useCallback(() => {
    setIsCreating(false);
  }, []);

  if (isLoadingUsers || isLoadingWorkspaces) {
    return (
      <CenteredBox sx={{ height: '50vh' }}>
        <StyledCircularProgress />
      </CenteredBox>
    );
  }

  return (
    <motion.div {...staggeredChildren}>
      {userError ? (
        <motion.div {...motionProps['fadeIn']}>
          <StyledAlert severity="error" sx={{ mb: 2 }}>
            {userError.message}
          </StyledAlert>
        </motion.div>
      ) : null}
      <AnimatePresence>
        {isCreating ? (
          <motion.div {...motionProps['slideInFromTop']}>
            <ContentPaper>
              <SectionTitle variant="h6">Create User</SectionTitle>
              <UserForm
                onSubmit={(data) => {
                  void handleSubmit(data);
                }}
                onCancel={handleCancelCreate}
                user={null}
                workspaces={workspaces}
                isInline={false}
              />
            </ContentPaper>
          </motion.div>
        ) : null}
      </AnimatePresence>
      <motion.div {...motionProps['slideInFromBottom']}>
        <ListContainer>
          <SectionTitle variant="h6">Users</SectionTitle>
          <UserList
            users={users}
            workspaces={workspaces}
            editingUserId={editingUserId}
            onEdit={handleEdit}
            onCancelEdit={handleCancelEdit}
            onDelete={handleDelete}
            onSubmit={(data) => {
              void handleSubmit(data);
            }}
          />
        </ListContainer>
      </motion.div>
      <Box sx={{ position: 'fixed', bottom: 24, right: 24 }}>
        <Tooltip title="Create new user" arrow>
          <Fab color="primary" onClick={handleCreateNew}>
            <AddIcon />
          </Fab>
        </Tooltip>
      </Box>
    </motion.div>
  );
};

export default UsersPageContent;
