import React, { useState } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { LiveDoc, User, Workspace } from '@/lib/schemas';
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

  const handleCreate = async (liveDoc: Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    const newLiveDoc: LiveDoc = {
      id: uuidv4(),
      ...liveDoc,
      updatedAt: new Date().toISOString(),
      isDeleted: false,
    };
    await upsertDocument(newLiveDoc);
  };

  const handleUpdate = async (liveDoc: Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
    if (editingLiveDoc) {
      const updatedLiveDoc: LiveDoc = {
        ...editingLiveDoc,
        ...liveDoc,
        updatedAt: new Date().toISOString(),
      };
      await upsertDocument(updatedLiveDoc);
      setEditingLiveDoc(null);
    }
  };

  if (isLoadingLiveDocs || isLoadingUsers || isLoadingWorkspaces) {
    return (
      <CenteredBox sx={{ height: '50vh' }}>
        <StyledCircularProgress />
      </CenteredBox>
    );
  }

  return (
    <>
      {liveDocError ? (
        <StyledAlert severity="error" sx={{ mb: 2 }}>
          {liveDocError.message}
        </StyledAlert>
      ) : null}
      <ContentPaper>
        <SectionTitle variant="h6">{editingLiveDoc ? 'Edit LiveDoc' : 'Create LiveDoc'}</SectionTitle>
        <LiveDocForm
          liveDoc={editingLiveDoc ?? undefined}
          users={users}
          workspaces={workspaces}
          onSubmit={editingLiveDoc ? handleUpdate : handleCreate}
        />
        {editingLiveDoc ? (
          <SpaceBetweenBox sx={{ mt: 2 }}>
            <PrimaryButton onClick={(): void => setEditingLiveDoc(null)}>Cancel Editing</PrimaryButton>
          </SpaceBetweenBox>
        ) : null}
      </ContentPaper>
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
    </>
  );
};

export default LiveDocsPageContent;
