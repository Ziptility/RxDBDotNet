// src\components\WorkspacesPageContent.tsx
import React, { useState, useCallback } from 'react';
import { motion } from 'framer-motion';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { Workspace } from '@/lib/schemas';
import {
  ContentPaper,
  SectionTitle,
  ListContainer,
  StyledAlert,
  StyledCircularProgress,
  CenteredBox,
} from '@/styles/StyledComponents';
import { motionProps, staggeredChildren } from '@/utils/motionSystem';
import WorkspaceForm from './WorkspaceForm';
import WorkspaceList from './WorkspaceList';

const WorkspacesPageContent: React.FC = () => {
  const [editingWorkspace, setEditingWorkspace] = useState<Workspace | null>(null);
  const {
    documents: workspaces,
    isLoading,
    error: workspaceError,
    upsertDocument,
    deleteDocument,
  } = useDocuments<Workspace>('workspace');

  const handleSubmit = useCallback(
    async (workspaceData: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingWorkspace) {
        const updatedWorkspace: Workspace = {
          ...editingWorkspace,
          ...workspaceData,
          updatedAt: new Date().toISOString(),
        };
        await upsertDocument(updatedWorkspace);
      } else {
        const newWorkspace: Workspace = {
          id: uuidv4(),
          ...workspaceData,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        };
        await upsertDocument(newWorkspace);
      }
      setEditingWorkspace(null);
    },
    [editingWorkspace, upsertDocument]
  );

  const handleCancel = useCallback((): void => {
    setEditingWorkspace(null);
  }, []);

  if (isLoading) {
    return (
      <CenteredBox sx={{ height: '50vh' }}>
        <StyledCircularProgress />
      </CenteredBox>
    );
  }

  return (
    <motion.div {...staggeredChildren}>
      {workspaceError ? (
        <motion.div {...motionProps['fadeIn']}>
          <StyledAlert severity="error" sx={{ mb: 2 }}>
            {workspaceError.message}
          </StyledAlert>
        </motion.div>
      ) : null}
      <motion.div {...motionProps['slideInFromBottom']}>
        <ContentPaper>
          <SectionTitle variant="h6">{editingWorkspace ? 'Edit Workspace' : 'Create Workspace'}</SectionTitle>
          <WorkspaceForm
            workspace={editingWorkspace ?? undefined}
            onSubmit={(e) => {
              void handleSubmit(e);
            }}
            onCancel={handleCancel}
          />
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
  );
};

export default WorkspacesPageContent;
