// src\components\LiveDocsPageContent.tsx
import React, { useState, useCallback } from 'react';
import { motion } from 'framer-motion';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { LiveDoc, User, Workspace } from '@/lib/schemas';
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
  const [editingLiveDoc, setEditingLiveDoc] = useState<LiveDoc | null>(null);
  const {
    documents: liveDocs,
    isLoading: isLoadingLiveDocs,
    error: liveDocError,
    upsertDocument,
    deleteDocument,
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
    },
    [editingLiveDoc, upsertDocument]
  );

  const handleCancel = useCallback((): void => {
    setEditingLiveDoc(null);
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
      <motion.div {...motionProps['slideInFromBottom']}>
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
      <motion.div {...motionProps['slideInFromBottom']}>
        <ListContainer>
          <SectionTitle variant="h6">LiveDoc List</SectionTitle>
          <LiveDocList
            liveDocs={liveDocs}
            onEdit={setEditingLiveDoc}
            onDelete={(liveDoc): void => {
              void deleteDocument(liveDoc.id);
            }}
          />
        </ListContainer>
      </motion.div>
    </motion.div>
  );
};

export default LiveDocsPageContent;
