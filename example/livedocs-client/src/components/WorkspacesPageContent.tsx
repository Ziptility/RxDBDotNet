import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { Workspace } from '@/lib/schemas';
import {
  PageContainer,
  ContentPaper,
  SectionTitle,
  PrimaryButton,
  ListContainer,
  SpaceBetweenBox,
  StyledAlert,
  StyledCircularProgress,
} from '@/styles/StyledComponents';
import { motionProps, staggeredChildren } from '@/utils/motionSystem';
import WorkspaceForm from './WorkspaceForm';
import WorkspaceList from './WorkspaceList';

const WorkspacesPageContent: React.FC = () => {
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);
  const {
    documents: workspaces,
    isLoading,
    error,
    upsertDocument,
    deleteDocument,
  } = useDocuments<Workspace>('workspace');

  const handleCreate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    const newWorkspace: Workspace = {
      id: uuidv4(),
      ...workspace,
      updatedAt: new Date().toISOString(),
      isDeleted: false,
    };
    await upsertDocument(newWorkspace);
  };

  const handleUpdate = async (workspace: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (editingWorkspace) {
      const updatedWorkspace: Workspace = {
        ...editingWorkspace,
        ...workspace,
        updatedAt: new Date().toISOString(),
      };
      await upsertDocument(updatedWorkspace);
      setEditingWorkspace(null);
    }
  };

  if (isLoading) {
    return (
      <PageContainer>
        <StyledCircularProgress />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <motion.div {...staggeredChildren}>
        {error ? (
          <motion.div {...motionProps['fadeIn']}>
            <StyledAlert severity="error" sx={{ mb: 2 }}>
              {error.message}
            </StyledAlert>
          </motion.div>
        ) : null}
        <motion.div {...motionProps['slideInFromBottom']}>
          <ContentPaper>
            <SectionTitle variant="h6">{editingWorkspace ? 'Edit Workspace' : 'Create Workspace'}</SectionTitle>
            <WorkspaceForm
              workspace={editingWorkspace ?? undefined}
              onSubmit={editingWorkspace ? handleUpdate : handleCreate}
            />
            {editingWorkspace ? (
              <SpaceBetweenBox sx={{ mt: 2 }}>
                <PrimaryButton onClick={(): void => setEditingWorkspace(null)}>Cancel Editing</PrimaryButton>
              </SpaceBetweenBox>
            ) : null}
          </ContentPaper>
        </motion.div>
        <motion.div {...motionProps['slideInFromBottom']}>
          <ListContainer>
            <SectionTitle variant="h6">Workspace List</SectionTitle>
            <WorkspaceList
              workspaces={workspaces}
              onEdit={setEditingWorkspace}
              onDelete={(workspace): void => {
                void deleteDocument(workspace.id);
              }}
            />
          </ListContainer>
        </motion.div>
      </motion.div>
    </PageContainer>
  );
};

export default WorkspacesPageContent;
