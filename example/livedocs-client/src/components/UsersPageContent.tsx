// src\components\UsersPageContent.tsx
import React, { useState, useCallback } from 'react';
import { motion } from 'framer-motion';
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
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const {
    documents: users,
    isLoading: isLoadingUsers,
    error: userError,
    upsertDocument,
    deleteDocument,
  } = useDocuments<User>('user');

  const { documents: workspaces, isLoading: isLoadingWorkspaces } = useDocuments<Workspace>('workspace');

  const handleSubmit = useCallback(
    async (userData: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingUser) {
        const updatedUser: User = {
          ...editingUser,
          ...userData,
          updatedAt: new Date().toISOString(),
        };
        await upsertDocument(updatedUser);
      } else {
        const newUser: User = {
          id: uuidv4(),
          ...userData,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        };
        await upsertDocument(newUser);
      }
      setEditingUser(null);
    },
    [editingUser, upsertDocument]
  );

  const handleCancel = useCallback((): void => {
    setEditingUser(null);
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
      <motion.div {...motionProps['slideInFromBottom']}>
        <ContentPaper>
          <SectionTitle variant="h6">{editingUser ? 'Edit User' : 'Create User'}</SectionTitle>
          <UserForm
            user={editingUser ?? undefined}
            workspaces={workspaces}
            onSubmit={(e) => {
              void handleSubmit(e);
            }}
            onCancel={handleCancel}
          />
        </ContentPaper>
      </motion.div>
      <motion.div {...motionProps['slideInFromBottom']}>
        <ListContainer>
          <SectionTitle variant="h6">User List</SectionTitle>
          <UserList
            users={users}
            onEdit={setEditingUser}
            onDelete={(user): void => {
              void deleteDocument(user.id);
            }}
          />
        </ListContainer>
      </motion.div>
    </motion.div>
  );
};

export default UsersPageContent;
