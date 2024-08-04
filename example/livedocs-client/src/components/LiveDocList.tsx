import React, { useState, useEffect } from 'react';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton } from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { LiveDocsDatabase } from '@/types';
import { LiveDocDocType } from '@/lib/schemas';

interface LiveDocListProps {
  db: LiveDocsDatabase;
  onEdit: (liveDoc: LiveDocDocType) => void;
  onDelete: (liveDoc: LiveDocDocType) => void;
}

const LiveDocList: React.FC<LiveDocListProps> = ({ db, onEdit, onDelete }) => {
  const [liveDocs, setLiveDocs] = useState<LiveDocDocType[]>([]);

  useEffect(() => {
    const subscription = db.livedocs.find({
      selector: {
        isDeleted: false
      },
      sort: [{ updatedAt: 'desc' }]
    }).$
      .subscribe(docs => {
        setLiveDocs(docs.map(doc => doc.toJSON()));
      });

    return () => subscription.unsubscribe();
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
                <IconButton onClick={() => onEdit(liveDoc)}>
                  <EditIcon />
                </IconButton>
                <IconButton onClick={() => onDelete(liveDoc)}>
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