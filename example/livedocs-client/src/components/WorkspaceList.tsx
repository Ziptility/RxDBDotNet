// src\components\WorkspaceList.tsx
import React from 'react';
import { useDocuments } from '@/hooks/useDocuments';
import { Workspace } from '@/lib/schemas';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  CircularProgress,
} from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';

export interface WorkspaceListProps {
  workspaces: Workspace[];
  onEdit: (workspace: Workspace) => void;
  onDelete: (workspace: Workspace) => void;
}

const WorkspaceList: React.FC<WorkspaceListProps> = ({ onEdit, onDelete }) => {
  const { documents: workspaces, isLoading, error } = useDocuments<Workspace>('workspaces');

  if (isLoading) {
    return <CircularProgress />;
  }

  if (error) {
    return <div>Error: {error.message}</div>;
  }

  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Updated At</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {workspaces.map((workspace) => (
            <TableRow key={workspace.id}>
              <TableCell>{workspace.name}</TableCell>
              <TableCell>{new Date(workspace.updatedAt).toLocaleString()}</TableCell>
              <TableCell>
                <IconButton onClick={(): void => onEdit(workspace)}>
                  <EditIcon />
                </IconButton>
                <IconButton onClick={(): void => onDelete(workspace)}>
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

export default WorkspaceList;
