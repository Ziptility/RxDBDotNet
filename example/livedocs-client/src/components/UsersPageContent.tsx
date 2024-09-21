// src/components/UsersPageContent.tsx
import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { User, Workspace } from '@/lib/schemas';
import {
  ContentPaper,
  SectionTitle,
  PrimaryButton,
  ListContainer,
  SpaceBetweenBox,
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

  const handleCreate = async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    const newUser: User = {
      id: uuidv4(),
      ...user,
      updatedAt: new Date().toISOString(),
      isDeleted: false,
    };
    await upsertDocument(newUser);
  };

  const handleUpdate = async (user: Omit<User, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (editingUser) {
      const updatedUser: User = {
        ...editingUser,
        ...user,
        updatedAt: new Date().toISOString(),
      };
      await upsertDocument(updatedUser);
      setEditingUser(null);
    }
  };

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
            onSubmit={editingUser ? handleUpdate : handleCreate}
          />
          {editingUser ? (
            <SpaceBetweenBox sx={{ mt: 2 }}>
              <PrimaryButton onClick={(): void => setEditingUser(null)}>Cancel Editing</PrimaryButton>
            </SpaceBetweenBox>
          ) : null}
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
