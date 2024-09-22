import React, { useState, useCallback } from 'react';
import { Add as AddIcon } from '@mui/icons-material';
import { Box, Fab, Tooltip } from '@mui/material';
import { motion, AnimatePresence } from 'framer-motion';
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
  const [isCreating, setIsCreating] = useState(false);
  const [editingWorkspaceId, setEditingWorkspaceId] = useState<string | null>(null);
  const {
    documents: workspaces,
    isLoading,
    error: workspaceError,
    upsertDocument,
    deleteDocument,
  } = useDocuments<Workspace>('workspace');

  const handleSubmit = useCallback(
    async (workspaceData: Omit<Workspace, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingWorkspaceId !== null) {
        const existingWorkspace = workspaces.find((w) => w.id === editingWorkspaceId);
        if (existingWorkspace) {
          const updatedWorkspace: Workspace = {
            ...existingWorkspace,
            ...workspaceData,
            updatedAt: new Date().toISOString(),
          };
          await upsertDocument(updatedWorkspace);
          setEditingWorkspaceId(null);
        }
      } else {
        const newWorkspace: Workspace = {
          id: uuidv4(),
          ...workspaceData,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        };
        await upsertDocument(newWorkspace);
        setIsCreating(false);
      }
    },
    [editingWorkspaceId, workspaces, upsertDocument]
  );

  const handleEdit = useCallback((workspaceId: string) => {
    setEditingWorkspaceId(workspaceId);
  }, []);

  const handleCancelEdit = useCallback(() => {
    setEditingWorkspaceId(null);
  }, []);

  const handleDelete = useCallback(
    (workspaceId: string) => {
      void deleteDocument(workspaceId);
    },
    [deleteDocument]
  );

  const handleCreateNew = useCallback(() => {
    setIsCreating(true);
  }, []);

  const handleCancelCreate = useCallback(() => {
    setIsCreating(false);
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
      <AnimatePresence>
        {isCreating ? (
          <motion.div {...motionProps['slideInFromTop']}>
            <ContentPaper>
              <SectionTitle variant="h6">Create Workspace</SectionTitle>
              <WorkspaceForm
                onSubmit={(data) => {
                  void handleSubmit(data);
                }}
                onCancel={handleCancelCreate}
                workspace={null}
                isInline={false}
              />
            </ContentPaper>
          </motion.div>
        ) : null}
      </AnimatePresence>
      <motion.div {...motionProps['slideInFromBottom']}>
        <ListContainer>
          <SectionTitle variant="h6">Workspaces</SectionTitle>
          <WorkspaceList
            workspaces={workspaces}
            editingWorkspaceId={editingWorkspaceId}
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
        <Tooltip title="Create new workspace" arrow>
          <Fab color="primary" onClick={handleCreateNew}>
            <AddIcon />
          </Fab>
        </Tooltip>
      </Box>
    </motion.div>
  );
};

export default WorkspacesPageContent;
