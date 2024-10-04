// example/livedocs-client/src/components/LiveDocsPageContent.tsx
import React, { useState, useCallback } from 'react';
import { Add as AddIcon } from '@mui/icons-material';
import { Box, Fab, Tooltip } from '@mui/material';
import { motion, AnimatePresence } from 'framer-motion';
import { v4 as uuidv4 } from 'uuid';
import type { LiveDoc, User, Workspace } from '@/generated/graphql';
import { useDocuments } from '@/hooks/useDocuments';
import {
  ContentPaper,
  SectionTitle,
  ListContainer,
  StyledAlert,
  StyledCircularProgress,
  CenteredBox,
} from '@/styles/StyledComponents';
import { motionProps, staggeredChildren } from '@/utils/motionSystem';
import LiveDocForm from './LiveDocForm';
import LiveDocList from './LiveDocList';

const LiveDocsPageContent: React.FC = () => {
  const [isCreating, setIsCreating] = useState(false);
  const [editingLiveDoc, setEditingLiveDoc] = useState<LiveDoc | null>(null);
  const {
    documents: liveDocs,
    isLoading: isLoadingLiveDocs,
    error: liveDocError,
    upsertDocument: upsertDocument,
    deleteDocument: deleteDocument,
  } = useDocuments<LiveDoc>('livedoc');

  const { documents: users, isLoading: isLoadingUsers } = useDocuments<User>('user');
  const { documents: workspaces, isLoading: isLoadingWorkspaces } = useDocuments<Workspace>('workspace');

  const handleSubmit = useCallback(
    async (liveDocData: Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingLiveDoc) {
        const updatedLiveDoc: LiveDoc = {
          ...editingLiveDoc,
          ...liveDocData,
          updatedAt: new Date().toISOString(),
        };
        await upsertDocument(updatedLiveDoc);
      } else {
        const newLiveDoc: LiveDoc = {
          id: uuidv4(),
          ...liveDocData,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        };
        await upsertDocument(newLiveDoc);
      }
      setEditingLiveDoc(null);
      setIsCreating(false);
    },
    [editingLiveDoc, upsertDocument]
  );

  const handleCancel = useCallback((): void => {
    setEditingLiveDoc(null);
    setIsCreating(false);
  }, []);

  const handleEdit = useCallback((liveDoc: LiveDoc): void => {
    setEditingLiveDoc(liveDoc);
    setIsCreating(true);
  }, []);

  const handleDelete = useCallback(
    (liveDoc: LiveDoc) => {
      void deleteDocument(liveDoc.id);
    },
    [deleteDocument]
  );

  const handleCreateNew = useCallback(() => {
    setEditingLiveDoc(null);
    setIsCreating(true);
  }, []);

  if (isLoadingLiveDocs || isLoadingUsers || isLoadingWorkspaces) {
    return (
      <CenteredBox sx={{ height: '50vh' }}>
        <StyledCircularProgress />
      </CenteredBox>
    );
  }

  return (
    <motion.div {...staggeredChildren}>
      {liveDocError ? (
        <motion.div {...motionProps['fadeIn']}>
          <StyledAlert severity="error" sx={{ mb: 2 }}>
            {liveDocError.message}
          </StyledAlert>
        </motion.div>
      ) : null}
      <AnimatePresence>
        {isCreating ? (
          <motion.div {...motionProps['slideInFromTop']}>
            <ContentPaper>
              <SectionTitle variant="h6">{editingLiveDoc ? 'Edit LiveDoc' : 'Create LiveDoc'}</SectionTitle>
              <LiveDocForm
                liveDoc={editingLiveDoc ?? undefined}
                users={users}
                workspaces={workspaces}
                onSubmit={(e) => {
                  void handleSubmit(e);
                }}
                onCancel={handleCancel}
              />
            </ContentPaper>
          </motion.div>
        ) : null}
      </AnimatePresence>
      <motion.div {...motionProps['slideInFromBottom']}>
        <ListContainer>
          <SectionTitle variant="h6">LiveDoc List</SectionTitle>
          <LiveDocList liveDocs={liveDocs} onEdit={handleEdit} onDelete={handleDelete} />
        </ListContainer>
      </motion.div>
      <Box sx={{ position: 'fixed', bottom: 24, right: 24 }}>
        <Tooltip title="Create new LiveDoc" arrow>
          <Fab color="primary" onClick={handleCreateNew}>
            <AddIcon />
          </Fab>
        </Tooltip>
      </Box>
    </motion.div>
  );
};

export default LiveDocsPageContent;
