// src\components\LiveDocsPageContent.tsx
import React, { useState } from 'react';
import { Box, Button, Alert } from '@mui/material';
import LiveDocList from './LiveDocList';
import LiveDocForm from './LiveDocForm';
import type { LiveDoc, User, Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';

const LiveDocsPageContent: React.FC = () => {
  const [editingLiveDoc, setEditingLiveDoc] = useState<LiveDoc | null>(null);
  const {
    documents: liveDocs,
    isLoading: isLoadingLiveDocs,
    error: liveDocError,
    upsertDocument,
    deleteDocument,
  } = useDocuments<LiveDoc>('livedoc');

  const { documents: users, isLoading: isLoadingUsers, error: userError } = useDocuments<User>('user');

  const {
    documents: workspaces,
    isLoading: isLoadingWorkspaces,
    error: workspaceError,
  } = useDocuments<Workspace>('workspace');

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
    return <Box>Loading...</Box>;
  }

  const error = liveDocError ?? userError ?? workspaceError;

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error.message}
        </Alert>
      )}
      <Box sx={{ mb: 4 }}>
        <LiveDocForm
          liveDoc={editingLiveDoc ?? undefined}
          users={users}
          workspaces={workspaces}
          onSubmit={editingLiveDoc ? handleUpdate : handleCreate}
        />
      </Box>
      {editingLiveDoc && (
        <Button onClick={(): void => setEditingLiveDoc(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      )}
      <LiveDocList
        liveDocs={liveDocs}
        onEdit={setEditingLiveDoc}
        onDelete={(liveDoc): void => {
          void deleteDocument(liveDoc.id);
        }}
      />
    </Box>
  );
};

export default LiveDocsPageContent;
