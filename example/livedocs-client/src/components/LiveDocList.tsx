// src/components/LiveDocList.tsx
import React from 'react';
import { LiveDoc } from '@/lib/schemas';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton } from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';

export interface LiveDocListProps {
  liveDocs: LiveDoc[];
  onEdit: (liveDoc: LiveDoc) => void;
  onDelete: (liveDoc: LiveDoc) => void;
}

const LiveDocList: React.FC<LiveDocListProps> = ({ liveDocs, onEdit, onDelete }) => {
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
