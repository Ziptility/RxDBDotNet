import React, { useState, useEffect } from 'react';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton } from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { LiveDocsDatabase } from '@/types';
import { LiveDoc } from '@/lib/schemas';

interface LiveDocListProps {
  db: LiveDocsDatabase;
  onEdit: (liveDoc: LiveDoc) => void;
  onDelete: (liveDoc: LiveDoc) => void;
}

const LiveDocList: React.FC<LiveDocListProps> = ({ db, onEdit, onDelete }): JSX.Element => {
  const [liveDocs, setLiveDocs] = useState<LiveDoc[]>([]);

  useEffect(() => {
    const subscription = db.livedocs
      .find({
        selector: {
          isDeleted: false,
        },
        sort: [{ updatedAt: 'desc' }],
      })
      .$.subscribe((docs) => {
        setLiveDocs(docs.map((doc) => doc.toJSON()));
      });

    return (): void => subscription.unsubscribe();
  }, [db]);

  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Content</TableCell>
            <TableCell>Owner ID</TableCell>
            <TableCell>Workspace ID</TableCell>
            <TableCell>Updated At</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {liveDocs.map((liveDoc) => (
            <TableRow key={liveDoc.id}>
              <TableCell>{liveDoc.content.substring(0, 50)}...</TableCell>
              <TableCell>{liveDoc.ownerId}</TableCell>
              <TableCell>{liveDoc.workspaceId}</TableCell>
              <TableCell>{new Date(liveDoc.updatedAt).toLocaleString()}</TableCell>
              <TableCell>
                <IconButton onClick={(): void => onEdit(liveDoc)}>
                  <EditIcon />
                </IconButton>
                <IconButton onClick={(): void => onDelete(liveDoc)}>
                  <DeleteIcon />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default LiveDocList;
