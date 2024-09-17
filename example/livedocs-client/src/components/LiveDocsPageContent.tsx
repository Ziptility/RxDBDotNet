// src\components\LiveDocsPageContent.tsx
import React, { useState, useCallback } from 'react';
import { Box, Button } from '@mui/material';
import LiveDocList, { LiveDocListProps } from './LiveDocList';
import LiveDocForm from './LiveDocForm';
import { LiveDoc, User, Workspace } from '@/lib/schemas';
import { useDocuments } from '@/hooks/useDocuments';
import { v4 as uuidv4 } from 'uuid';
import { getDatabase } from '@/lib/database';

const LiveDocsPageContent: React.FC = () => {
  const [editingLiveDoc, setEditingLiveDoc] = useState<LiveDoc | null>(null);
  const { refetch } = useDocuments<LiveDoc>('livedocs');
  const { documents: users } = useDocuments<User>('users');
  const { documents: workspaces } = useDocuments<Workspace>('workspaces');

  const handleCreate = useCallback(
    async (liveDoc: Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      const db = await getDatabase();
      try {
        await db.livedocs.insert({
          id: uuidv4(),
          ...liveDoc,
          updatedAt: new Date().toISOString(),
          isDeleted: false,
        });
        await refetch();
      } catch (error) {
        console.error('Error creating live doc:', error);
      }
    },
    [refetch]
  );

  const handleUpdate = useCallback(
    async (liveDoc: Omit<LiveDoc, 'id' | 'updatedAt' | 'isDeleted'>): Promise<void> => {
      if (editingLiveDoc) {
        const db = await getDatabase();
        try {
          await db.livedocs.upsert({
            ...editingLiveDoc,
            ...liveDoc,
            updatedAt: new Date().toISOString(),
          });
          setEditingLiveDoc(null);
          await refetch();
        } catch (error) {
          console.error('Error updating live doc:', error);
        }
      }
    },
    [editingLiveDoc, refetch]
  );

  const handleDelete = useCallback(
    async (liveDoc: LiveDoc): Promise<void> => {
      const db = await getDatabase();
      try {
        await db.livedocs.upsert({
          ...liveDoc,
          isDeleted: true,
          updatedAt: new Date().toISOString(),
        });
        await refetch();
      } catch (error) {
        console.error('Error deleting live doc:', error);
      }
    },
    [refetch]
  );

  const liveDocListProps: LiveDocListProps = {
    onEdit: setEditingLiveDoc,
    onDelete: (liveDoc): void => {
      void handleDelete(liveDoc);
    },
  };

  return (
    <Box>
      <Box sx={{ mb: 4 }}>
        <LiveDocForm
          liveDoc={editingLiveDoc ?? undefined}
          users={users.map((u) => ({ id: u.id, name: `${u.firstName} ${u.lastName}` }))}
          workspaces={workspaces.map((w) => ({ id: w.id, name: w.name }))}
          onSubmit={editingLiveDoc ? handleUpdate : handleCreate}
        />
      </Box>
      {editingLiveDoc && (
        <Button onClick={(): void => setEditingLiveDoc(null)} sx={{ mb: 2 }}>
          Cancel Editing
        </Button>
      )}
      <LiveDocList {...liveDocListProps} />
    </Box>
  );
};

export default LiveDocsPageContent;
