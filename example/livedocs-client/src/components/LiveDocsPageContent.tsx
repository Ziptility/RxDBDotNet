import { useState } from 'react';
import { Alert, Box, Button } from '@mui/material';
import { v4 as uuidv4 } from 'uuid';
import { useDocuments } from '@/hooks/useDocuments';
import type { LiveDoc, User, Workspace } from '@/lib/schemas';
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
      {error ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error.message}
        </Alert>
      ) : null}
      <Box sx={{ mb: 4 }}>
        <LiveDocForm
          liveDoc={editingLiveDoc ?? undefined}
          users={users}
          workspaces={workspaces}
          onSubmit={editingLiveDoc ? handleUpdate : handleCreate}
        />
      </Box>
      {editingLiveDoc ? (
        <Button onClick={(): void => setEditingLiveDoc(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      ) : null}
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
